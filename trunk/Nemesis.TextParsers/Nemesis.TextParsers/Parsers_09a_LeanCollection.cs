using System;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class LeanCollectionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            Type elementType = typeof(TCollection).GenericTypeArguments[0];
            
            var transType = typeof(InnerLeanCollectionTransformer<>).MakeGenericType(elementType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType);
        }

        private class InnerLeanCollectionTransformer<TElement> : ITransformer<LeanCollection<TElement>>, IParser<LeanCollection<TElement>>
        {
            LeanCollection<TElement> IParser<LeanCollection<TElement>>.ParseText(string input) => Parse(input.AsSpan());

            public LeanCollection<TElement> Parse(ReadOnlySpan<char> input) =>
                SpanCollectionSerializer.DefaultInstance.ParseLeanCollection<TElement>(input);

            public string Format(LeanCollection<TElement> coll) =>
                SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public override string ToString() => $"Transform LeanCollection<{typeof(TElement).GetFriendlyName()}>";
        }

        public bool CanHandle(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LeanCollection<>);

        public sbyte Priority => 71;
    }
}
