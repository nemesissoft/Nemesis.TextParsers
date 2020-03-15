using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    internal sealed class CollectionTransformerCreator : ICanCreateTransformer //standard .net framework collection handler 
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            var (_, kind, elementType) = CollectionMetaHelper.GetCollectionMeta(collectionType);

            var transType = typeof(InnerCollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType, kind);
        }

        private sealed class InnerCollectionTransformer<TElement, TCollection> : TransformerBase<TCollection>
            where TCollection : IEnumerable<TElement>
        {
            private readonly CollectionKind _kind;
            public InnerCollectionTransformer(CollectionKind kind) => _kind = kind;


            public override TCollection Parse(in ReadOnlySpan<char> input) => //input.IsEmpty ? default :
                (TCollection)SpanCollectionSerializer.DefaultInstance.ParseCollection<TElement>(input, _kind);

            public override string Format(TCollection coll) => //coll == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public override string ToString() => $"Transform {typeof(TCollection).GetFriendlyName()} AS {_kind}<{typeof(TElement).GetFriendlyName()}>";
        }

        public bool CanHandle(Type type) => CollectionMetaHelper.IsTypeSupported(type);

        public sbyte Priority => 70;
    }
}
