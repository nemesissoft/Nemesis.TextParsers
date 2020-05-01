using System;
using System.Globalization;
using Nemesis.TextParsers.Parsers;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TypeArgs = new[] { typeof(byte), typeof(ByteTransformer) })]
    [TestFixture(TypeArgs = new[] { typeof(sbyte), typeof(SByteTransformer) })]
    [TestFixture(TypeArgs = new[] { typeof(short), typeof(Int16Transformer) })]
    [TestFixture(TypeArgs = new[] { typeof(ushort), typeof(UInt16Transformer) })]
    [TestFixture(TypeArgs = new[] { typeof(int), typeof(Int32Transformer) })]
    [TestFixture(TypeArgs = new[] { typeof(uint), typeof(UInt32Transformer) })]
    [TestFixture(TypeArgs = new[] { typeof(long), typeof(Int64Transformer) })]
    [TestFixture(TypeArgs = new[] { typeof(ulong), typeof(UInt64Transformer) })]
    public class NumberTests<TUnderlying, TNumberHandler>
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
        where TNumberHandler : NumberTransformer<TUnderlying>
    {
        private static readonly TNumberHandler _sut = (TNumberHandler)NumberTransformerCache.GetNumberHandler<TUnderlying>();
        
        [Test]
        public void AddTest()
        {
            var min = _sut.MinValue;
            var minMinus1 = _sut.Sub(min, _sut.One);
            Assert.That(minMinus1, Is.GreaterThan(min));

            var max = _sut.MaxValue;
            var maxPlus1 = _sut.Add(max, _sut.One);

            Assert.That(maxPlus1, Is.LessThan(max));


            var loopFrom = _sut.Add(min, _sut.One);
            var increment = _sut.Div(max, _sut.FromInt64(120));
            var loopMax = _sut.Sub(_sut.MaxValue, increment);


            bool atLeaseOnePass = false;
            var prev = min;
            for (TUnderlying next = loopFrom;
                next.CompareTo(loopMax) < 0;
                next = _sut.Add(next, increment))
            {
                Assert.That(next, Is.GreaterThan(prev));
                prev = next;
                atLeaseOnePass = true;
            }

            Assert.IsTrue(atLeaseOnePass);
        }

        [Test]
        public void TryParseTest()
        {
            var min = _sut.MinValue;
            var max = _sut.MaxValue;

            var loopFrom = _sut.Add(min, _sut.One);
            var increment = _sut.Div(max, _sut.FromInt64(120));
            var loopMax = _sut.Sub(_sut.MaxValue, increment);

            var prev = min;
            uint passes = 0;
            for (TUnderlying i = loopFrom;
                i.CompareTo(loopMax) < 0;
                i = _sut.Add(i, increment))
            {
                string text = i.ToString(null, CultureInfo.InvariantCulture);
                bool success = _sut.TryParse(text.AsSpan(), out var value);

                Assert.That(success, Is.True, $"Failed at '{text}'");

                Assert.That(value, Is.GreaterThan(prev));
                prev = value;

                passes++;

                //Console.WriteLine($"✔ '{text}'");
            }

            Assert.That(passes, Is.GreaterThanOrEqualTo(120));
        }

    }
}