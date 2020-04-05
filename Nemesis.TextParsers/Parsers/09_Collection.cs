using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class CollectionTransformerCreator : ICanCreateTransformer //standard .net framework collection handler 
    {
        private readonly ITransformerStore _transformerStore;
        private readonly CollectionSettings _settings;
        public CollectionTransformerCreator(ITransformerStore transformerStore, CollectionSettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }


        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            if (!TryGetElements(collectionType, out _, out var kind, out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {collectionType.GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = typeof(CollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType, kind);
        }
        
        public bool CanHandle(Type type) =>
            TryGetElements(type, out bool isArray, out var kind, out var elementType) &&
            !isArray &&
            kind != CollectionKind.Unknown &&
            _transformerStore.IsSupportedForTransformation(elementType);

        private static bool TryGetElements(Type type, out bool isArray, out CollectionKind kind, out Type elementType)
        {
            if (CollectionMetaHelper.IsTypeSupported(type))
            {
                (isArray, kind, elementType) = CollectionMetaHelper.GetCollectionMeta(type);
                return true;
            }
            else
            {
                (isArray, kind, elementType) = (default, default, default);
                return false;
            }
        }

        public sbyte Priority => 70;
    }

    public sealed class CollectionTransformer<TElement, TCollection> : TransformerBase<TCollection>
        where TCollection : IEnumerable<TElement>
    {
        private readonly CollectionKind _kind;
        public CollectionTransformer(CollectionKind kind) => _kind = kind;


        protected override TCollection ParseCore(in ReadOnlySpan<char> input) =>
            (TCollection)SpanCollectionSerializer.DefaultInstance.ParseCollection<TElement>(input, _kind);

        public override string Format(TCollection coll) =>
            SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

        public override TCollection GetEmpty() =>
            (TCollection)(_kind switch
            {
                CollectionKind.ReadOnlyCollection => new ReadOnlyCollection<TElement>(new List<TElement>(0)),
                CollectionKind.HashSet => new HashSet<TElement>(),
                CollectionKind.SortedSet => new SortedSet<TElement>(),
                CollectionKind.LinkedList => new LinkedList<TElement>(),
                CollectionKind.Stack => new Stack<TElement>(0),
                CollectionKind.Queue => new Queue<TElement>(0),
                CollectionKind.ObservableCollection => new ObservableCollection<TElement>(),
                CollectionKind.Unknown => throw new NotSupportedException($"Collection kind {_kind} is not supported for empty element query"),
                //CollectionKind.List
                _ => (IEnumerable<TElement>)new List<TElement>(0),
            });


        public override string ToString() => $"Transform {typeof(TCollection).GetFriendlyName()} AS {_kind}<{typeof(TElement).GetFriendlyName()}>";
    }
}
