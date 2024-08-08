﻿using System.Globalization;
using Nemesis.Essentials.Design;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests;

readonly struct LowPrecisionFloat(float value) : IEquatable<LowPrecisionFloat>
{
    public float Value { get; } = value;

    public bool Equals(LowPrecisionFloat other) => AlmostEqualUlps(Value, other.Value);

    public override bool Equals(object obj) => obj is not null && obj is LowPrecisionFloat other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(LowPrecisionFloat left, LowPrecisionFloat right) => left.Equals(right);

    public static bool operator !=(LowPrecisionFloat left, LowPrecisionFloat right) => !left.Equals(right);

    public override string ToString() => Value.ToString("G17", CultureInfo.InvariantCulture);

    public static LowPrecisionFloat FromText(ReadOnlySpan<char> text) => new(
        SingleTransformer.Instance.Parse(text)
        );

    private static bool AlmostEqualUlps(float a, float b, uint maxUlpsDiff = 100 * 1024)
    {
        uint aNum = Unsafe.As<float, uint>(ref a);
        uint bNum = Unsafe.As<float, uint>(ref b);

        bool aNegative = (aNum >> 31) == 1;
        bool bNegative = (bNum >> 31) == 1;

        // Different signs means they do not match.
        if (aNegative != bNegative)
            return a == b; // Check for equality to make sure +0==-0

        // Find the difference in ULPs.
        long ulpsDiff = Math.Abs((long)aNum - bNum);
        return ulpsDiff <= maxUlpsDiff;
    }
}

readonly struct CarrotAndOnionFactors(float carrot, float[] onionFactors) : IEquatable<CarrotAndOnionFactors>
{
    public float Carrot { get; } = carrot;
    public float[] OnionFactors { get; } = onionFactors;

    public bool Equals(CarrotAndOnionFactors other) =>
        Carrot.Equals(other.Carrot) &&
        EnumerableEqualityComparer<float>.DefaultInstance.Equals(OnionFactors, other.OnionFactors);

    public override bool Equals(object obj) => obj is not null && obj is CarrotAndOnionFactors other && Equals(other);

    public override int GetHashCode() => unchecked((Carrot.GetHashCode() * 397) ^ (OnionFactors?.GetHashCode() ?? 0));

    public static bool operator ==(CarrotAndOnionFactors left, CarrotAndOnionFactors right) => left.Equals(right);

    public static bool operator !=(CarrotAndOnionFactors left, CarrotAndOnionFactors right) => !left.Equals(right);

    private const string NULL = "∅";

    public override string ToString() => FormattableString.Invariant(
        $"{Carrot:G9};{(OnionFactors == null ? NULL : string.Join(",", OnionFactors.Select(of => of.ToString("G9", CultureInfo.InvariantCulture))))}"
        );

    public static CarrotAndOnionFactors FromText(ReadOnlySpan<char> text)
    {
        var stream = text.Split(';').GetEnumerator();
        var floatParser = Sut.GetTransformer<float>();

        if (!stream.MoveNext()) throw new FormatException($"At least one element is expected to parse {nameof(CarrotAndOnionFactors)}");
        var carrot = floatParser.Parse(stream.Current);

        byte onionCount = 0;
        Span<float> onionFactors = stackalloc float[10];

        if (stream.MoveNext())
        {
            if (EqualsOrdinalIgnoreCase(stream.Current, NULL.AsSpan()))
                return new(carrot, null);

            var onionStream = stream.Current.Split(',', true).GetEnumerator();
            while (onionStream.MoveNext())
            {
                float onion = floatParser.Parse(onionStream.Current);
                onionFactors[onionCount++] = onion;
            }
        }

        return new(carrot, onionFactors[..onionCount].ToArray());
    }

    private static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        if (span.Length != value.Length)
            return false;
        if (value.Length == 0)  // span.Length == value.Length == 0
            return true;
        for (int i = span.Length - 1; i >= 0; i--)
            if (char.ToUpperInvariant(span[i]) != char.ToUpperInvariant(value[i]))
                return false;

        return true;
    }
}

[TestFixture]
public class SpecialNumbersTests
{
    private static IEnumerable<(string, float)> ValidValuesForFactory()
        => new[]
        {
            ("3.1415", 3.1415f),
            ("0.000000000001", 0.000000000001f),
            ("-0.000000000001", -0.000000000001f),
        };

    [TestCaseSource(nameof(ValidValuesForFactory))]
    public void LowPrecisionFloat_FromText_ShouldParse((string inputText, float expected) data)
    {
        LowPrecisionFloat actual = Sut.GetTransformer<LowPrecisionFloat>()
            .Parse(data.inputText.AsSpan());
        Assert.Multiple(() =>
        {
            Assert.That(actual.Value, Is.EqualTo(data.expected).Within(3).Ulps);
            Assert.That(actual, Is.EqualTo(new LowPrecisionFloat(data.expected)));
        });
    }

    [Test]
    public void LowPrecisionFloat_ParseList()
    {
        var actual = Sut.GetTransformer<List<LowPrecisionFloat>>()
            .Parse("3.14|1|2|3.0005");

        Assert.Multiple(() =>
        {
            Assert.That(actual, Has.Count.EqualTo(4));
            Assert.That(actual.Select(lpf => lpf.Value), Is.EqualTo(new[] { 3.14f, 1f, 2f, 3.0005f }));
        });
    }


    [Test]
    public void CarrotAndOnionFactors_ParseList()
    {
        var list = new List<CarrotAndOnionFactors>()
        {
            new(1.1f, [2.1f, 3.1f, 4.1f]),
            new(10.1f, [20.1f, 30.1f, 40.1f]),
        };

        var trans = Sut.GetTransformer<List<CarrotAndOnionFactors>>();

        var formatted = trans.Format(list);
        Assert.That(formatted, Is.EqualTo("1.10000002;2.0999999,3.0999999,4.0999999|10.1000004;20.1000004,30.1000004,40.0999985"));

        var parsed = Sut.GetTransformer<List<CarrotAndOnionFactors>>()
            .Parse(formatted);

        Assert.That(parsed, Has.Count.EqualTo(2));
        Assert.That(parsed, Is.EqualTo(list));
    }
}
