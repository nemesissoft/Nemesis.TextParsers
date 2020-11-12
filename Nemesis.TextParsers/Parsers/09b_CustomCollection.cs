using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class CustomCollectionTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly CollectionSettings _settings;
        public CustomCollectionTransformerCreator(ITransformerStore transformerStore, CollectionSettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }
        
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);
            var supportsDeserializationLogic = typeof(IDeserializationCallback).IsAssignableFrom(collectionType);

            if (IsCustomCollection(collectionType, out var elementType))
            {
                var createMethod = Method.OfExpression<
                    Func<CustomCollectionTransformerCreator, bool, ITransformer<List<int>>>
                >((@this, supp) => @this.CreateCustomsCollectionTransformer<int, List<int>>(supp)
                ).GetGenericMethodDefinition();

                createMethod = createMethod.MakeGenericMethod(elementType, collectionType);

                return (ITransformer<TCollection>)createMethod.Invoke(this, new object[] { supportsDeserializationLogic });
            }
            else if (IsReadOnlyCollection(collectionType, out var meta))
            {
                var createMethod = Method.OfExpression<
                    Func<CustomCollectionTransformerCreator, bool, ConstructorInfo, ITransformer<List<int>>>
                >((@this, supp, ci) => @this.CreateReadOnlyCollectionTransformer<int, List<int>>(supp, ci)
                ).GetGenericMethodDefinition();

                createMethod = createMethod.MakeGenericMethod(meta.elementType, collectionType);

                return (ITransformer<TCollection>)createMethod.Invoke(this,
                    new object[] { supportsDeserializationLogic, meta.ctor }
                );
            }
            else
                throw new NotSupportedException("Only concrete types based on ICollection or IReadOnlyCollection are supported");
        }

        private ITransformer<TCollection> CreateCustomsCollectionTransformer<TElement, TCollection>
            (bool supportsDeserializationLogic)
                where TCollection : ICollection<TElement>, new()
            => new CustomCollectionTransformer<TElement, TCollection>(
                _transformerStore.GetTransformer<TElement>(),
                _settings, supportsDeserializationLogic
            );

        private ITransformer<TCollection> CreateReadOnlyCollectionTransformer<TElement, TCollection>
            (bool supportsDeserializationLogic, ConstructorInfo ctor)
                where TCollection : IReadOnlyCollection<TElement>
        {
            var listConversion = ReadOnlyCollectionTransformer<TElement, TCollection>
                .GetListConverter(ctor);

            return new ReadOnlyCollectionTransformer<TElement, TCollection>(
                _transformerStore.GetTransformer<TElement>(),
                _settings, supportsDeserializationLogic, listConversion
            );
        }

        private static bool IsCustomCollection(Type collectionType, out Type elementType)
        {
            Type iCollection = typeof(ICollection<>);
            bool isCustomCollection =
                !collectionType.IsAbstract && !collectionType.IsInterface &&
                collectionType.DerivesOrImplementsGeneric(iCollection) &&
                collectionType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;

            if (isCustomCollection)
            {
                var genericInterfaceType =
                    collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == iCollection
                        ? collectionType
                        : TypeMeta.GetGenericRealization(collectionType, iCollection)
                          ?? throw new InvalidOperationException($"Type has to be or implement {iCollection.Name}<>");
                elementType = genericInterfaceType.GenericTypeArguments[0];
                return true;
            }
            else
            {
                elementType = default;
                return false;
            }
        }

        private static bool IsReadOnlyCollection(Type collectionType, out (Type elementType, ConstructorInfo ctor) meta)
        {
            meta = default;

            Type iReadOnlyColl = typeof(IReadOnlyCollection<>);
            bool isReadOnlyCollection =
                !collectionType.IsAbstract && !collectionType.IsInterface &&
                collectionType.DerivesOrImplementsGeneric(iReadOnlyColl);

            if (isReadOnlyCollection)
            {
                var genericInterfaceType =
                    collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == iReadOnlyColl
                        ? collectionType
                        : TypeMeta.GetGenericRealization(collectionType, iReadOnlyColl)
                          ?? throw new InvalidOperationException($"Type has to be or implement {iReadOnlyColl.Name}<>");
                var elementType = genericInterfaceType.GenericTypeArguments[0];
                var iListType = typeof(IList<>).MakeGenericType(elementType);

                var ctor = collectionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(
                    c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.DerivesOrImplementsGeneric(iListType)
                );
                if (ctor != null)
                {
                    meta = (elementType, ctor);
                    return true;
                }
            }
            return false;
        }

        public bool CanHandle(Type type) =>
            IsCustomCollection(type, out var elementType) &&
            _transformerStore.IsSupportedForTransformation(elementType)
            ||
            IsReadOnlyCollection(type, out var meta) &&
            _transformerStore.IsSupportedForTransformation(meta.elementType)
        ;

        public sbyte Priority => 72;
        
        public override string ToString() =>
            $"Create transformer for custom collections with settings:{_settings}";
    }



    public abstract class CustomCollectionTransformerBase<TElement, TCollection> : EnumerableTransformerBase<TElement, TCollection>
            where TCollection : IEnumerable<TElement>
    {
        private readonly bool _supportsDeserializationLogic;
        protected CustomCollectionTransformerBase(ITransformer<TElement> elementTransformer,
            CollectionSettings settings, bool supportsDeserializationLogic)
            : base(elementTransformer, settings)
            => _supportsDeserializationLogic = supportsDeserializationLogic;


        protected override TCollection ParseCore(in ReadOnlySpan<char> input)
        {
            var stream = ParseStream(input);
            var result = GetCollection(stream);

            if (_supportsDeserializationLogic && result is IDeserializationCallback callback)
                callback.OnDeserialization(this);

            return result;
        }

        protected abstract TCollection GetCollection(in ParsingSequence stream);


        public sealed override string ToString() => $"Transform custom {typeof(TCollection).GetFriendlyName()} with {typeof(TElement).GetFriendlyName()} elements";
    }

    public sealed class CustomCollectionTransformer<TElement, TCollection> : CustomCollectionTransformerBase<TElement, TCollection>
        where TCollection : ICollection<TElement>, new()
    {
        public CustomCollectionTransformer(ITransformer<TElement> elementTransformer, CollectionSettings settings,
            bool supportsDeserializationLogic) : base(elementTransformer, settings, supportsDeserializationLogic) { }

        protected override TCollection GetCollection(in ParsingSequence stream)
        {
            var result = new TCollection();

            foreach (var element in stream)
                result.Add(element.ParseWith(ElementTransformer));

            return result;
        }

        public override TCollection GetEmpty() => new TCollection();
    }

    public sealed class ReadOnlyCollectionTransformer<TElement, TCollection> : CustomCollectionTransformerBase<TElement, TCollection>
        where TCollection : IReadOnlyCollection<TElement>
    {
        private readonly Func<IList<TElement>, TCollection> _listConversion;

        public ReadOnlyCollectionTransformer(ITransformer<TElement> elementTransformer, CollectionSettings settings,
            bool supportsDeserializationLogic, Func<IList<TElement>, TCollection> listConversion)
            : base(elementTransformer, settings, supportsDeserializationLogic)
            => _listConversion = listConversion;

        internal static Func<IList<TElement>, TCollection> GetListConverter(ConstructorInfo ctorInfo)
        {
            Type elementType = typeof(TElement),
                iListType = typeof(IList<>).MakeGenericType(elementType);

            var param = Expression.Parameter(iListType, "list");
            var ctor = Expression.New(ctorInfo, param);

            var λ = Expression.Lambda<Func<IList<TElement>, TCollection>>(ctor, param);
            return λ.Compile();
        }

        protected override TCollection GetCollection(in ParsingSequence stream)
        {
            var innerList = new List<TElement>();

            foreach (var element in stream)
                innerList.Add(element.ParseWith(ElementTransformer));

            var result = _listConversion(innerList);
            return result;
        }

        public override TCollection GetEmpty() => _listConversion(new List<TElement>());
    }
}
