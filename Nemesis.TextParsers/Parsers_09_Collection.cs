using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    //TODO add CollectionMeta DictionaryMeta here
    [UsedImplicitly]
    public sealed class CollectionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            var genericInterfaceType =
                collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? collectionType
                    : TypeMeta.GetConcreteInterfaceOfType(collectionType, typeof(IEnumerable<>))
                        ?? throw new InvalidOperationException("Type has to be or implement IEnumerable<>");

            Type elementType = genericInterfaceType.GenericTypeArguments[0];

            var kind = GetCollectionKind(collectionType);

            var transType = typeof(InnerCollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType, kind);
        }

        private static CollectionKind GetCollectionKind(Type collectionType)
        {
            if (collectionType.DerivesOrImplementsGeneric(typeof(Queue<>)))
                return CollectionKind.Queue;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(Stack<>)))
                return CollectionKind.Stack;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(LinkedList<>)))
                return CollectionKind.LinkedList;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(SortedSet<>)))
                return CollectionKind.SortedSet;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(HashSet<>))
                     ||
                     collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(ISet<>)
                    )
                return CollectionKind.HashSet;
            else if (collectionType.DerivesOrImplementsGeneric(typeof(ReadOnlyCollection<>))
                     ||
                     collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                     ||
                     collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                    )
                return CollectionKind.ReadOnlyCollection;
            else
                return CollectionKind.List;
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

            public override string ToString() => $"Transform IReadOnlyCollection<{typeof(TElement).GetFriendlyName()}> as {typeof(TCollection).GetFriendlyName()}, {_kind}";
        }

        public bool CanHandle(Type type) =>
            type.DerivesOrImplementsGeneric(typeof(IEnumerable<>))
                    ||
            type.DerivesOrImplementsGeneric(typeof(ICollection<>))
                    ||
            type.DerivesOrImplementsGeneric(typeof(IReadOnlyCollection<>))
                    ||
            type.DerivesOrImplementsGeneric(typeof(IReadOnlyList<>))
                    ||
            type.DerivesOrImplementsGeneric(typeof(ISet<>));

        public sbyte Priority => 70;
    }
}
