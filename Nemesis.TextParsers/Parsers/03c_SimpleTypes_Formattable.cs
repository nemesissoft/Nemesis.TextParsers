using System.Globalization;
using JetBrains.Annotations;

namespace Nemesis.TextParsers.Parsers;

file static class Culture
{
    internal static CultureInfo InvCult => CultureInfo.InvariantCulture;
    internal static NumberFormatInfo InvInfo => NumberFormatInfo.InvariantInfo;
}

public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement>
    where TElement : struct, IFormattable
{
    public sealed override string Format(TElement element) =>
    element.ToString(FormatString, Culture.InvCult);

    protected virtual string FormatString { get; }
}

#if NET

[UsedImplicitly]
public sealed class HalfTransformer : SimpleFormattableTransformer<Half>
{
    protected override Half ParseCore(in ReadOnlySpan<char> input) =>
        input.Length switch
        {
            1 when input[0] == '∞' => Half.PositiveInfinity,
            2 when input[0] == '-' && input[1] == '∞' => Half.NegativeInfinity,
            _ => Half.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
        };

    protected override string FormatString { get; } = "G17";

    public static readonly ITransformer<Half> Instance = new HalfTransformer();

    private HalfTransformer() { }
}

#endif

[UsedImplicitly]
public sealed class SingleTransformer : SimpleFormattableTransformer<float>
{
    protected override float ParseCore(in ReadOnlySpan<char> input) =>
        input.Length switch
        {
            1 when input[0] == '∞' => float.PositiveInfinity,
            2 when input[0] == '-' && input[1] == '∞' => float.NegativeInfinity,
            _ => float.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
        };

    protected override string FormatString { get; } = "R";

    public static readonly ITransformer<float> Instance = new SingleTransformer();

    private SingleTransformer() { }
}

[UsedImplicitly]
public sealed class DoubleTransformer : SimpleFormattableTransformer<double>
{
    protected override double ParseCore(in ReadOnlySpan<char> input) =>
        input.Length switch
        {
            1 when input[0] == '∞' => double.PositiveInfinity,
            2 when input[0] == '-' && input[1] == '∞' => double.NegativeInfinity,
            _ => double.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
        };

    protected override string FormatString { get; } = "R";

    public static readonly ITransformer<double> Instance = new DoubleTransformer();

    private DoubleTransformer() { }
}

[UsedImplicitly]
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

    private DecimalTransformer() { }
}

[UsedImplicitly]
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

    private TimeSpanTransformer() { }
}

[UsedImplicitly]
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

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<DateTime> Instance = new DateTimeTransformer();

    private DateTimeTransformer() { }
}

[UsedImplicitly]
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

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<DateTimeOffset> Instance = new DateTimeOffsetTransformer();

    private DateTimeOffsetTransformer() { }
}

[UsedImplicitly]
public sealed class GuidTransformer : SimpleFormattableTransformer<Guid>
{
    protected override Guid ParseCore(in ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    protected override string FormatString { get; } = "D";


    public static readonly ITransformer<Guid> Instance = new GuidTransformer();

    private GuidTransformer() { }
}

#if NET6_0_OR_GREATER

[UsedImplicitly]
public sealed class DateOnlyTransformer : SimpleFormattableTransformer<DateOnly>
{
    protected override DateOnly ParseCore(in ReadOnlySpan<char> input) =>
        DateOnly.Parse(input, Culture.InvCult, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowInnerWhite);

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<DateOnly> Instance = new DateOnlyTransformer();

    private DateOnlyTransformer() { }
}

[UsedImplicitly]
public sealed class TimeOnlyTransformer : SimpleFormattableTransformer<TimeOnly>
{
    protected override TimeOnly ParseCore(in ReadOnlySpan<char> input) =>
        TimeOnly.Parse(input, Culture.InvCult, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowInnerWhite);

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<TimeOnly> Instance = new TimeOnlyTransformer();

    private TimeOnlyTransformer() { }
}

#endif