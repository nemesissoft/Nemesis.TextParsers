using System.ComponentModel;

using JetBrains.Annotations;

using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class TypeConverterTransformerHandler : ITransformerHandler
{
    public ITransformer<TElement> CreateTransformer<TElement>() =>
        TypeDescriptor.GetConverter(typeof(TElement)) switch
        {
            ITransformer<TElement> t => t,

            var t1 when t1.GetType() == typeof(TypeConverter) => throw new NotSupportedException(
                $"Type '{typeof(TElement).GetFriendlyName()}' is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself"
            ),

            var t2 when t2.CanConvertFrom(typeof(string)) && t2.CanConvertTo(typeof(string)) =>
                new ConverterTransformer<TElement>(t2),

            _ => throw new NotSupportedException(
                $"Cannot create transformer for {typeof(TElement).GetFriendlyName()} based on {nameof(TypeConverterTransformerHandler)}"
            )
        };

    public bool CanHandle(Type type) =>
        TypeDescriptor.GetConverter(type) is { } typeConverter && (
            typeConverter.GetType().DerivesOrImplementsGeneric(typeof(ITransformer<>))
                ||
            IsProperTypeConverter(typeConverter)
    );

    private static bool IsProperTypeConverter(TypeConverter typeConverter) =>
        typeConverter.GetType() != typeof(TypeConverter) &&
        typeConverter.CanConvertFrom(typeof(string)) &&
        typeConverter.CanConvertTo(typeof(string));

    public sbyte Priority => 120;

    public override string ToString() => "Create transformer based on registered TypeConverter";

    string ITransformerHandler.DescribeHandlerMatch() => $"Type decorated with {nameof(TypeConverter)}";
}

sealed class ConverterTransformer<TElement> : TransformerBase<TElement>
{
    private readonly TypeConverter _typeConverter;
    public ConverterTransformer(TypeConverter typeConverter) => _typeConverter = typeConverter;


    protected override TElement ParseCore(in ReadOnlySpan<char> input) =>
        (TElement)_typeConverter.ConvertFromInvariantString(input.ToString());

    public override string Format(TElement element) =>
        element is null
            ? null
            : _typeConverter.ConvertToInvariantString(element);

    public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()} using {_typeConverter?.GetType().GetFriendlyName()}";
}
