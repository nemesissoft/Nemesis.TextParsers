using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class CustomCollectionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);
            var supportsDeserializationLogic = typeof(IDeserializationCallback).IsAssignableFrom(collectionType);

            if (IsCustomCollection(collectionType, out var elementType))
            {
                var transType = typeof(CustomCollectionTransformer<,>).MakeGenericType(elementType, collectionType);
                return (ITransformer<TCollection>)Activator.CreateInstance(transType, new object[] { supportsDeserializationLogic });
            }
            else if (IsReadOnlyCollection(collectionType, out var meta))
            {
                var transType = typeof(ReadOnlyCollectionTransformer<,>).MakeGenericType(meta.elementType, collectionType);
                var getListConverterMethod =
                        (GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                            .SingleOrDefault(mi => mi.Name == nameof(GetListConverter))
                        ?? throw new MissingMethodException($"Method {nameof(GetListConverter)} does not exist"))
                    .MakeGenericMethod(meta.elementType, collectionType);

                var funcConverter = getListConverterMethod.Invoke(null, new object[] { meta.ctor });

                return (ITransformer<TCollection>)Activator.CreateInstance(transType, supportsDeserializationLogic, funcConverter);
            }
            else
                throw new NotSupportedException(
                    "Only concrete types based on ICollection or IReadOnlyCollection are supported");
        }

        private static Func<IList<TElement>, TCollection> GetListConverter<TElement, TCollection>(ConstructorInfo ctorInfo) where TCollection : IReadOnlyCollection<TElement>
        {
            Type elementType = typeof(TElement),
                 iListType = typeof(IList<>).MakeGenericType(elementType);

            var param = Expression.Parameter(iListType, "list");
            var ctor = Expression.New(ctorInfo, param);

            var λ = Expression.Lambda<Func<IList<TElement>, TCollection>>(ctor, param);
            return λ.Compile();
        }

        private abstract class CollectionTransformer<TElement, TCollection> : TransformerBase<TCollection>
            where TCollection : IEnumerable<TElement>
        {
            private readonly bool _supportsDeserializationLogic;
            protected CollectionTransformer(bool supportsDeserializationLogic) => _supportsDeserializationLogic = supportsDeserializationLogic;

            
            public override TCollection Parse(ReadOnlySpan<char> input) //input.IsEmpty ? default :
            {
                var stream = SpanCollectionSerializer.DefaultInstance.ParseStream<TElement>(input, out _);
                TCollection result = GetCollection(stream);

                if (_supportsDeserializationLogic && result is IDeserializationCallback callback)
                    callback.OnDeserialization(this);

                return result;
            }

            protected abstract TCollection GetCollection(in ParsedSequence<TElement> stream);

            public override string Format(TCollection coll) => //coll == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public sealed override string ToString() => $"Transform custom {typeof(TCollection).GetFriendlyName()} with {typeof(TElement).GetFriendlyName()} elements";
        }

        private sealed class CustomCollectionTransformer<TElement, TCollection> : CollectionTransformer<TElement, TCollection>
            where TCollection : ICollection<TElement>, new()
        {
            public CustomCollectionTransformer(bool supportsDeserializationLogic) : base(supportsDeserializationLogic)
            {
            }

            protected override TCollection GetCollection(in ParsedSequence<TElement> stream)
            {
                var result = new TCollection();

                foreach (var element in stream)
                    result.Add(element);

                return result;
            }
        }

        private sealed class ReadOnlyCollectionTransformer<TElement, TCollection> : CollectionTransformer<TElement, TCollection>
            where TCollection : IReadOnlyCollection<TElement>
        {
            private readonly Func<IList<TElement>, TCollection> _listConversion;

            public ReadOnlyCollectionTransformer(bool supportsDeserializationLogic, Func<IList<TElement>, TCollection> listConversion) : base(supportsDeserializationLogic) => _listConversion = listConversion;

            protected override TCollection GetCollection(in ParsedSequence<TElement> stream)
            {
                var innerList = new List<TElement>();

                foreach (var element in stream)
                    innerList.Add(element);

                var result = _listConversion(innerList);
                return result;
            }
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
                        : TypeMeta.GetConcreteInterfaceOfType(collectionType, iCollection)
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
                        : TypeMeta.GetConcreteInterfaceOfType(collectionType, iReadOnlyColl)
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

        public bool CanHandle(Type type) => IsCustomCollection(type, out _) || IsReadOnlyCollection(type, out _);

        public sbyte Priority => 72;
    }
}
