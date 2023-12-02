using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Utils;
using Nemesis.TextParsers.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;

namespace Nemesis.TextParsers.Tests.Entities;

/// <summary>
/// Represents BasisPoint. 1bs is 1/100th of 1%
/// </summary>
[Transformer(typeof(BasisPointTransformer))]
public readonly struct BasisPoint(double realValue) : IEquatable<BasisPoint>
{
    private const double BPS_FACTOR = 10_000d;

    public double RealValue { get; } = realValue;
    internal double BpsValue => RealValue * BPS_FACTOR;

    public static BasisPoint FromBps(double bps) => new(bps / BPS_FACTOR);

    #region Equals
    public bool Equals(BasisPoint other) =>
       Math.Round(Math.Abs(BpsValue - other.BpsValue), 2) < 1E-02;//two decimal points of bps

    public override bool Equals(object obj) => obj is BasisPoint other && Equals(other);

    public override int GetHashCode() => RealValue.GetHashCode();

    public static BasisPoint operator -(BasisPoint left, BasisPoint right)
        => FromBps(left.BpsValue - right.BpsValue);

    #endregion

    public override string ToString() => FormattableString.Invariant($"{BpsValue} bps");

    public static bool operator ==(BasisPoint left, BasisPoint right) => left.Equals(right);

    public static bool operator !=(BasisPoint left, BasisPoint right) => !(left == right);
}

[TextConverterSyntax("Number followed by optional 'bps' letters. BasisPoint - 1bs is 1/100th of 1%")]
internal sealed class BasisPointTransformer : TransformerBase<BasisPoint>
{
    protected override BasisPoint ParseCore(in ReadOnlySpan<char> input)
    {
        var text = input.Trim();
        var length = text.Length;

        bool IsCharEqual(ReadOnlySpan<char> t, int fromEnd, char upperExpectedChar)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(char.IsUpper(upperExpectedChar), $"'{upperExpectedChar}' is not in upper case");
            System.Diagnostics.Debug.Assert(fromEnd <= length, $"NOT fromEnd<=length for {fromEnd} <= {length}");
#endif
            return t[length - fromEnd] is { } c &&
                   char.ToUpperInvariant(c) == upperExpectedChar;

        }

        if (length > 3 &&
            IsCharEqual(text, 3, 'B') &&
            IsCharEqual(text, 2, 'P') &&
            IsCharEqual(text, 1, 'S')
           )
            text = text[..(length - 3)];
        else if (length > 2 &&
                 IsCharEqual(text, 2, 'B') &&
                 IsCharEqual(text, 1, 'P')
                )
            text = text[..(length - 2)];

        var bps = DoubleTransformer.Instance.Parse(text);
        return BasisPoint.FromBps(bps);
    }

    public override string Format(BasisPoint bp) =>
        FormattableString.Invariant($"{bp.BpsValue:N2} bps");
}

[TestFixture]
public class BasisPointTests
{
    private static IEnumerable<TCD> CorrectData() => new[]
    {
        new TCD("01", BasisPoint.FromBps(0), null),
        new TCD("02", BasisPoint.FromBps(0), ""),
        new TCD("03", BasisPoint.FromBps(0), "0"),
        new TCD("04", BasisPoint.FromBps(0), "0 bp"),
        new TCD("05", BasisPoint.FromBps(0.123456789), "0.12 bp"),
        new TCD("06", BasisPoint.FromBps(1), "1 bp"),
        new TCD("07", BasisPoint.FromBps(2), "2 bps"),
        new TCD("08", BasisPoint.FromBps(-1), "-1 bps"),
        new TCD("09", BasisPoint.FromBps(-123.453), "-123.45 bps"),
        new TCD("10", BasisPoint.FromBps(100), "100"),
        new TCD("11", BasisPoint.FromBps(1000), "1000"),
        new TCD("12", BasisPoint.FromBps(1_000_000_000), "1000000000 BPS"),
        new TCD("13", BasisPoint.FromBps(Math.PI), "3.14bpS"),
        new TCD("14", BasisPoint.FromBps(Math.E), "2.72 bPS"),
        new TCD("15", BasisPoint.FromBps(10.15), "10.15 BpS"),
    };

    [TestCaseSource(nameof(CorrectData))]
    public void BasisPoint_ParseAndFormat(string _, BasisPoint instance, string text)
        => ParseAndFormat(instance, text);

    [Test]
    public void PrecisionTests()
    {
        var bps = BasisPoint.FromBps(123.45);
        Assert.That(
            bps,
            Is.EqualTo(BasisPoint.FromBps(123.453))
        );

        Assert.That(
            bps,
            Is.EqualTo(BasisPoint.FromBps(123.4501))
        );


        var sut = Sut.GetTransformer<BasisPoint>();

        var bps2 = BasisPoint.FromBps(123.455);
        var text2 = sut.Format(bps2);
        var greater = sut.Parse(text2);
        Assert.That(
            bps,
            Is.Not.EqualTo(greater)
        );

        var diff = greater - bps;
        var marginBps = BasisPoint.FromBps(0.01);

        Assert.That(diff.BpsValue, Is.LessThan(marginBps.BpsValue));
    }
}
