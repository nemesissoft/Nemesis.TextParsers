using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class CollectionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            var genericInterfaceType = TypeMeta.GetConcreteInterfaceOfType(collectionType, typeof(ICollection<>))
                    ?? throw new InvalidOperationException("Type has to implement ICollection<>");

            Type elementType = genericInterfaceType.GenericTypeArguments[0];

            var kind = GetCollectionKind(collectionType);

            var transType = typeof(InnerCollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType, kind);

        }

        private static CollectionKind GetCollectionKind(Type collectionType)
        {
            if (collectionType.DerivesOrImplementsGeneric(typeof(LinkedList<>)))
                return CollectionKind.LinkedList;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(SortedSet<>)))
                return CollectionKind.SortedSet;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(HashSet<>)))//TODO or equal to ISet<>
                return CollectionKind.HashSet;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(ReadOnlyCollection<>))) //TODO or equal to IReadOnlyCollection<>
                return CollectionKind.ReadOnlyCollection;
            else
                return CollectionKind.List;
        }

        private class InnerCollectionTransformer<TElement, TCollection> : ITransformer<TCollection>
            where TCollection : ICollection<TElement>
        {
            private readonly CollectionKind _kind;

            public InnerCollectionTransformer(CollectionKind kind) => _kind = kind;

            public TCollection Parse(ReadOnlySpan<char> input) =>
                //input.IsEmpty ? default :
                (TCollection)SpanCollectionSerializer.DefaultInstance.ParseCollection<TElement>(input, _kind);

            public string Format(TCollection coll) =>
                    //coll == null ? null :
                    SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public override string ToString() => $"Transform ICollection<{typeof(TElement).GetFriendlyName()}>, {_kind}";
        }

        public bool CanHandle(Type type) => type.DerivesOrImplementsGeneric(typeof(ICollection<>));

        public sbyte Priority => 70;
    }
}
