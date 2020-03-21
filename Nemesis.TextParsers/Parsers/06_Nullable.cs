using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class NullableTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        public NullableTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;



        public ITransformer<TNullable> CreateTransformer<TNullable>()
        {
            if(!TryGetElement(typeof(TNullable), out var underlyingType) || underlyingType==null)
                throw new NotSupportedException($"Type {typeof(TNullable).GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = typeof(InnerNullableTransformer<>).MakeGenericType(underlyingType);

            return (ITransformer<TNullable>)Activator.CreateInstance(transType, _transformerStore);
        }

        private sealed class InnerNullableTransformer<TElement> : TransformerBase<TElement?> where TElement : struct
        {
            private readonly ITransformer<TElement> _elementParser;

            public InnerNullableTransformer(ITransformerStore transformerStore) =>
                _elementParser = transformerStore.GetTransformer<TElement>();


            public override TElement? Parse(in ReadOnlySpan<char> input) =>
                input.IsEmpty ? (TElement?)null : _elementParser.Parse(input);

            public override string Format(TElement? element) =>
                element.HasValue ? _elementParser.Format(element.Value) : null;

            public override string ToString() => $"Transform {typeof(TElement?).GetFriendlyName()}";
        }

        public bool CanHandle(Type type) =>
            TryGetElement(type, out var underlyingType) &&
            _transformerStore.IsSupportedForTransformation(underlyingType);

        private static bool TryGetElement(Type type, out Type underlyingType)
        {
            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                underlyingType = Nullable.GetUnderlyingType(type);
                return true;
            }
            else
            {
                underlyingType = null;
                return false;
            }
        }

        public sbyte Priority => 40;
    }
}
