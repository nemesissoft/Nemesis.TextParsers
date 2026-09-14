using System.Globalization;

namespace Nemesis.TextParsers.Parsers;

file static class Culture
{
    internal static CultureInfo InvCult => CultureInfo.InvariantCulture;
}

public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement>
    where TElement : struct, IFormattable
{
    public sealed override string Format(TElement element) => element.ToString(FormatString, Culture.InvCult);

    protected virtual string FormatString => null;
}

#if NET5_0_OR_GREATER

public sealed class HalfTransformer : SimpleFormattableTransformer<Half>
{
    protected override Half ParseCore(in ReadOnlySpan<char> input) => input switch
    {
        "∞" => Half.PositiveInfinity,
        "-∞" => Half.NegativeInfinity,
        _ => Half.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
    };

    protected override string FormatString => "G17";

    public static readonly ITransformer<Half> Instance = new HalfTransformer();

    private HalfTransformer()
    {
    }
}

#endif

public sealed class SingleTransformer : SimpleFormattableTransformer<float>
{
    protected override float ParseCore(in ReadOnlySpan<char> input) => input switch
    {
        "∞" => float.PositiveInfinity,
        "-∞" => float.NegativeInfinity,
        _ => float.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
    };

    protected override string FormatString => "R";

    public static readonly ITransformer<float> Instance = new SingleTransformer();

    private SingleTransformer()
    {
    }
}

public sealed class DoubleTransformer : SimpleFormattableTransformer<double>
{
    protected override double ParseCore(in ReadOnlySpan<char> input) => input switch
    {
        "∞" => double.PositiveInfinity,
        "-∞" => double.NegativeInfinity,
        _ => double.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
    };

    protected override string FormatString => "R";

    public static readonly ITransformer<double> Instance = new DoubleTransformer();

    private DoubleTransformer()
    {
    }
}

public sealed class DecimalTransformer : SimpleFormattableTransformer<decimal>
{
    protected override decimal ParseCore(in ReadOnlySpan<char> input) =>
        decimal.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , NumberStyles.Number, Culture.InvCult);

    public static readonly ITransformer<decimal> Instance = new DecimalTransformer();

    private DecimalTransformer()
    {
    }
}

#if NET11_0_OR_GREATER

public sealed class BFloat16Transformer : IeeeTransformer<BFloat16>
{
    public static readonly ITransformer<BFloat16> Instance = new BFloat16Transformer();

    private BFloat16Transformer()
    {
    }
}

public sealed class Decimal32Transformer : IeeeTransformer<Decimal32>
{
    public static readonly ITransformer<Decimal32> Instance = new Decimal32Transformer();

    private Decimal32Transformer()
    {
    }
}

public sealed class Decimal64Transformer : IeeeTransformer<Decimal64>
{
    public static readonly ITransformer<Decimal64> Instance = new Decimal64Transformer();

    private Decimal64Transformer()
    {
    }
}

public sealed class Decimal128Transformer : IeeeTransformer<Decimal128>
{
    public static readonly ITransformer<Decimal128> Instance = new Decimal128Transformer();

    private Decimal128Transformer()
    {
    }
}

public abstract class IeeeTransformer<TNumber> : SimpleTransformer<TNumber>
    where TNumber : IFloatingPointIeee754<TNumber>
{
    protected sealed override TNumber ParseCore(in ReadOnlySpan<char> input) => input switch
    {
        "∞" => TNumber.PositiveInfinity,
        "-∞" => TNumber.NegativeInfinity,
        "NaN" => TNumber.NaN,
        _ => TNumber.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
    };

    public sealed override string Format(TNumber element)
    {
        if (TNumber.IsPositiveInfinity(element))
            return "∞";

        if (TNumber.IsNegativeInfinity(element))
            return "-∞";

        return TNumber.IsNaN(element) ? "NaN" : element.ToString("R", Culture.InvCult);
    }
}

#endif

public sealed class TimeSpanTransformer : SimpleFormattableTransformer<TimeSpan>
{
    protected override TimeSpan ParseCore(in ReadOnlySpan<char> input) =>
        TimeSpan.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , Culture.InvCult);


    public static readonly ITransformer<TimeSpan> Instance = new TimeSpanTransformer();

    private TimeSpanTransformer()
    {
    }
}

public sealed class DateTimeTransformer : SimpleFormattableTransformer<DateTime>
{
    protected override DateTime ParseCore(in ReadOnlySpan<char> input) =>
        DateTime.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , Culture.InvCult, DateTimeStyles.RoundtripKind);

    protected override string FormatString => "o";

    public static readonly ITransformer<DateTime> Instance = new DateTimeTransformer();

    private DateTimeTransformer()
    {
    }
}

public sealed class DateTimeOffsetTransformer : SimpleFormattableTransformer<DateTimeOffset>
{
    protected override DateTimeOffset ParseCore(in ReadOnlySpan<char> input) =>
        DateTimeOffset.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , Culture.InvCult, DateTimeStyles.RoundtripKind);

    protected override string FormatString => "o";

    public static readonly ITransformer<DateTimeOffset> Instance = new DateTimeOffsetTransformer();

    private DateTimeOffsetTransformer()
    {
    }
}

public sealed class GuidTransformer : SimpleFormattableTransformer<Guid>
{
    protected override Guid ParseCore(in ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
    );

    protected override string FormatString => "D";


    public static readonly ITransformer<Guid> Instance = new GuidTransformer();

    private GuidTransformer()
    {
    }
}

#if NET6_0_OR_GREATER

public sealed class DateOnlyTransformer : SimpleFormattableTransformer<DateOnly>
{
    protected override DateOnly ParseCore(in ReadOnlySpan<char> input) =>
        DateOnly.Parse(input, Culture.InvCult,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite |
            DateTimeStyles.AllowInnerWhite);

    protected override string FormatString => "o";

    public static readonly ITransformer<DateOnly> Instance = new DateOnlyTransformer();

    private DateOnlyTransformer()
    {
    }
}

public sealed class TimeOnlyTransformer : SimpleFormattableTransformer<TimeOnly>
{
    protected override TimeOnly ParseCore(in ReadOnlySpan<char> input) =>
        TimeOnly.Parse(input, Culture.InvCult,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite |
            DateTimeStyles.AllowInnerWhite);

    protected override string FormatString => "o";

    public static readonly ITransformer<TimeOnly> Instance = new TimeOnlyTransformer();

    private TimeOnlyTransformer()
    {
    }
}

#endif