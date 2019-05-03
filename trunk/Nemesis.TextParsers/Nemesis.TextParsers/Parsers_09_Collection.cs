﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class CollectionTransformerCreator : ICanCreateTransformer //standard .net framework collection handler 
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            Type elementType = CollectionMetaHelper.GetElementType(collectionType);

            var kind = CollectionMetaHelper.GetCollectionKind(collectionType);

            var transType = typeof(InnerCollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType, kind);
        }

        private class InnerCollectionTransformer<TElement, TCollection> : ITransformer<TCollection>, IParser<TCollection>
            where TCollection : IEnumerable<TElement>
        {
            private readonly CollectionKind _kind;
            public InnerCollectionTransformer(CollectionKind kind) => _kind = kind;


            TCollection IParser<TCollection>.ParseText(string input) => Parse(input.AsSpan());

            public TCollection Parse(ReadOnlySpan<char> input) => //input.IsEmpty ? default :
                (TCollection)SpanCollectionSerializer.DefaultInstance.ParseCollection<TElement>(input, _kind);

            public string Format(TCollection coll) => //coll == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public override string ToString() => $"Transform IEnumerable<{typeof(TElement).GetFriendlyName()}> as {typeof(TCollection).GetFriendlyName()}, {_kind}";
        }

        public bool CanHandle(Type type) => CollectionMetaHelper.IsTypeSupported(type);

        public sbyte Priority => 70;
    }
}
