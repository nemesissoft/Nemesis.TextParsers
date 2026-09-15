using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers;

public sealed class NullableTransformerHandler(ITransformerStore transformerStore) : ITransformerHandler
{
    public ITransformer<TNullable> CreateTransformer<TNullable>()
    {
        if (!TryGetElement(typeof(TNullable), out var underlyingType) || underlyingType == null)
            throw new NotSupportedException($"Type {typeof(TNullable).GetFriendlyName()} is not supported by {nameof(NullableTransformerHandler)}");

        var transType = typeof(InnerNullableTransformer<>).MakeGenericType(underlyingType);

        return (ITransformer<TNullable>)Activator.CreateInstance(transType, transformerStore);
    }

    private sealed class InnerNullableTransformer<TElement>(ITransformerStore transformerStore) : TransformerBase<TElement?>
        where TElement : struct
    {
        private readonly ITransformer<TElement> _elementParser = transformerStore.GetTransformer<TElement>();


        protected override TElement? ParseCore(in ReadOnlySpan<char> input) => _elementParser.Parse(input);

        public override string Format(TElement? element) =>
            element.HasValue ? _elementParser.Format(element.Value) : null;
    }

    public bool CanHandle(Type type) =>
        TryGetElement(type, out var underlyingType) &&
        transformerStore.IsSupportedForTransformation(underlyingType);

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

    public override string ToString() => "Create transformer for Nullable generic realization";

    string ITransformerHandler.DescribeHandlerMatch() => "Nullable with value supported for transformation";
}
