﻿using System.Globalization;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture(TypeArgs = [typeof(byte), typeof(ByteTransformer)])]
[TestFixture(TypeArgs = [typeof(sbyte), typeof(SByteTransformer)])]
[TestFixture(TypeArgs = [typeof(short), typeof(Int16Transformer)])]
[TestFixture(TypeArgs = [typeof(ushort), typeof(UInt16Transformer)])]
[TestFixture(TypeArgs = [typeof(int), typeof(Int32Transformer)])]
[TestFixture(TypeArgs = [typeof(uint), typeof(UInt32Transformer)])]
[TestFixture(TypeArgs = [typeof(long), typeof(Int64Transformer)])]
[TestFixture(TypeArgs = [typeof(ulong), typeof(UInt64Transformer)])]
#if NET7_0_OR_GREATER
[TestFixture(TypeArgs = [typeof(Int128), typeof(Int128Transformer)])]
[TestFixture(TypeArgs = [typeof(UInt128), typeof(UInt128Transformer)])]
#endif
public class NumberTests<TUnderlying, TNumberHandler>
    where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IEquatable<TUnderlying>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TUnderlying>
#endif
    where TNumberHandler : NumberTransformer<TUnderlying>
{
    private static readonly TNumberHandler _sut = (TNumberHandler)NumberTransformerCache.Instance.GetNumberHandler<TUnderlying>();

    [Test]
    public void Zero_ShouldBeLessThanOne() => Assert.That(_sut.Zero, Is.LessThan(_sut.One));

    [Test]
    public void Min_ShouldBeLessThanMax() => Assert.That(_sut.MinValue, Is.LessThan(_sut.MaxValue));

    [Test]
    public void UncheckedOverflow_ShouldOccur()
    {
        var min = _sut.MinValue;
        var minMinus1 = _sut.Sub(min, _sut.One);
        Assert.That(minMinus1, Is.GreaterThan(min));

        var max = _sut.MaxValue;
        var maxPlus1 = _sut.Add(max, _sut.One);
        Assert.That(maxPlus1, Is.LessThan(max));
    }

    [Test]
    public void AddShouldBePossibleToGetToNextValue()
    {
        var actual = _sut.Add(_sut.FromInt64(100), _sut.One);

        Assert.That(actual, Is.EqualTo(_sut.FromInt64(101)));
    }

    [Test]
    public void Add_LoopTest()
    {
        TUnderlying min = _sut.MinValue, max = _sut.MaxValue;

        var loopFrom = _sut.Add(min, _sut.One);
        var increment = _sut.Div(max, _sut.FromInt64(120));
        var loopMax = _sut.Sub(_sut.MaxValue, increment);


        bool atLeaseOnePass = false;
        var prev = min;
        for (TUnderlying next = loopFrom; next.CompareTo(loopMax) < 0; next = _sut.Add(next, increment))
        {
            Assert.That(next, Is.GreaterThan(prev));
            prev = next;
            atLeaseOnePass = true;
        }

        Assert.That(atLeaseOnePass, Is.True);
    }

    [Test]
    public void TryParseTest()
    {
        TUnderlying min = _sut.MinValue, max = _sut.MaxValue;

        var incBig = (BigInteger.Parse(_sut.Format(max))
                                 -
                      BigInteger.Parse(_sut.Format(min))
        ) / new BigInteger(100);

        var increment = _sut.Parse(incBig.ToString("R", CultureInfo.InvariantCulture));

        var loopFrom = _sut.Add(min, _sut.One);
        var loopMax = _sut.Sub(max, increment);

        var prev = min;
        uint passes = 0;
        for (TUnderlying next = loopFrom; next.CompareTo(loopMax) < 0; next = _sut.Add(next, increment))
        {
            string text = next.ToString(null, CultureInfo.InvariantCulture);
            bool success = _sut.TryParse(text.AsSpan(), out var value);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True, $"Failed at '{text}'");
                Assert.That(value, Is.GreaterThan(prev));
            });
            prev = value;

            var expected = BigInteger.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

            string actualText = value.ToString(null, CultureInfo.InvariantCulture);
            var actual = BigInteger.Parse(actualText, NumberStyles.Number, CultureInfo.InvariantCulture);
            Assert.That(actual, Is.EqualTo(expected));

            passes++;
        }

        Assert.That(passes, Is.EqualTo(
            min is byte or sbyte ? 126 : 100
            ));
    }

    [Test]
    public void Shr_ReturnsOne_AfterGivenNumberOfShiftsFromMax()
    {
        TUnderlying one = _sut.One, max = _sut.MaxValue;

        var bitSize = Unsafe.SizeOf<TUnderlying>() * 8;
        var shiftsToOne = (byte)(_sut.SupportsNegative ? bitSize - 2 : bitSize - 1);

        var actual = _sut.ShR(max, shiftsToOne);

        Assert.That(actual, Is.EqualTo(one));
    }

    [Test]
    public void Shl_ReturnsMax_AfterGivenNumberOfShiftsFromOne()
    {
        TUnderlying one = _sut.One, max = _sut.MaxValue;
        var halfPlusOne = _sut.Add(_sut.ShR(max, 1), one);

        var bitSize = Unsafe.SizeOf<TUnderlying>() * 8;
        var shiftsToHalfPlusOne = (byte)(_sut.SupportsNegative ? bitSize - 2 : bitSize - 1);

        var actual = _sut.ShL(one, shiftsToHalfPlusOne);

        Assert.That(actual, Is.EqualTo(halfPlusOne));
    }
}