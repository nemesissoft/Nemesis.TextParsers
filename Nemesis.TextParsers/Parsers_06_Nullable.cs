using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class NullableTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TNullable> CreateTransformer<TNullable>()
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(TNullable));

            var transType = typeof(InnerNullableTransformer<>).MakeGenericType(underlyingType);

            return (ITransformer<TNullable>)Activator.CreateInstance(transType);
        }

        private sealed class InnerNullableTransformer<TElement> : TransformerBase<TElement?> where TElement : struct
        {
            private readonly ITransformer<TElement> _elementParser;

            public InnerNullableTransformer() => _elementParser = TextTransformer.Default.GetTransformer<TElement>();


            public override TElement? Parse(ReadOnlySpan<char> input) =>
                input.IsEmpty ? (TElement?)null : _elementParser.Parse(input);

            public override string Format(TElement? element) =>
                element.HasValue ? _elementParser.Format(element.Value) : null;

            public override string ToString() => $"Transform {typeof(TElement?).GetFriendlyName()}";
        }

        public bool CanHandle(Type type) => type.IsValueType && Nullable.GetUnderlyingType(type) != null;

        public sbyte Priority => 40;
    }
}
