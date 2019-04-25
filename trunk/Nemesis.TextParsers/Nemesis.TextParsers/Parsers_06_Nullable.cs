using System;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class NullableTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TNullable> CreateTransformer<TNullable>()
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(TNullable));

            var transType = typeof(InnerNullableTransformer<>).MakeGenericType(underlyingType);

            return (ITransformer<TNullable>)Activator.CreateInstance(transType);
        }

        private class InnerNullableTransformer<TElement> : ITransformer<TElement?> where TElement : struct
        {
            private readonly ITransformer<TElement> _elementParser;

            public InnerNullableTransformer() => _elementParser = TextTransformer.Default.GetTransformer<TElement>();


            public TElement? Parse(ReadOnlySpan<char> input) =>
                input.IsEmpty ? (TElement?)null : _elementParser.Parse(input);

            public string Format(TElement? element) =>
                element.HasValue ? _elementParser.Format(element.Value) : null;

            public override string ToString() => $"Transform {typeof(TElement).Name}?";
        }

        public bool CanHandle(Type type) => type.IsValueType && Nullable.GetUnderlyingType(type) != null;

        public sbyte Priority => 40;
    }
}
