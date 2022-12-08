using System;
using System.Globalization;
using System.Text;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class LightLinqTests
    {
        private static readonly ITransformer<double> _doubleTransformer = DoubleTransformer.Instance;
        private static readonly ITransformer<int> _intTransformer = Int32Transformer.Instance;
#if NET
        private static readonly ITransformer<Half> _halfTransformer = HalfTransformer.Instance;
#endif

        private static ParsingSequence GetSequence(string text)
        {
            var tokens = text.AsSpan().Tokenize('|', '\\', true);
            return tokens.PreParse('\\', '∅', '|');
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1", true, 1)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
        public void Sum(string text, bool success, double result)
        {
            var pair = GetSequence(text).Sum(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
        public void Average(string text, bool expectedSuccess, double expectedResult)
        {
            var (success, result) = GetSequence(text).Average(_doubleTransformer);
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 5.16122380952381)]
        [TestCase(@"0", true, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 3.3333333333333335d)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 7.5)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, 33.529411764705891d)]
        public void Variance(string text, bool success, double result)
        {
            var pair = GetSequence(text).Variance(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }


        [TestCase(@"", false, 0)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 7.22)]
        [TestCase(@"0", true, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 2)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 9)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, 9)]

        [TestCase(@"-∞|0|10|∞", true, double.PositiveInfinity)]
        [TestCase(@"-∞|0|10|NaN|∞", true, double.PositiveInfinity)]
        [TestCase(@"NaN|NaN|-∞|0|10|NaN|∞", true, double.PositiveInfinity)]
        [TestCase(@"NaN|NaN|10", true, 10)]
        [TestCase(@"NaN|NaN|NaN", true, double.NaN)]
        [TestCase(@"NaN|5.0", true, 5.0)]
        [TestCase(@"5.0|NaN", true, 5.0)]
        public void Max(string text, bool success, double result)
        {
            var pair = GetSequence(text).Max(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 1.21)]
        [TestCase(@"0", true, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, -2)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 1)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, -9)]

        [TestCase(@"-∞|0|10|∞", true, double.NegativeInfinity)]
        [TestCase(@"-∞|0|10|NaN|∞", true, double.NaN)]
        [TestCase(@"NaN|NaN|-∞|0|10|NaN|∞", true, double.NaN)]
        [TestCase(@"NaN|NaN|10", true, double.NaN)]
        [TestCase(@"-10|NaN|NaN|10", true, double.NaN)]
        [TestCase(@"NaN|NaN|NaN", true, double.NaN)]
        [TestCase(@"NaN|5.0", true, double.NaN)]
        [TestCase(@"5.0|NaN", true, double.NaN)]
        public void Min(string text, bool success, double result)
        {
            var pair = GetSequence(text).Min(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
        public void Aggregate(string text, bool success, double result)
        {
            var pair = GetSequence(text).Aggregate(_doubleTransformer, (a, b) => a + b);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", "")]
        [TestCase(@"1|2|3|4|5|6|7|8|9", "102030405060708090")]
        public void AggregateSeed(string text, string result)
        {
            var actual = GetSequence(text).Aggregate(_doubleTransformer,
                new StringBuilder(),
                (sb, current) => sb.Append((current * 10.0).ToString(null, CultureInfo.InvariantCulture))
                );
            Assert.That(actual.ToString(), Is.EqualTo(result));
        }

        [TestCase(@"", "")]
        [TestCase(@"1|2|3|4|5|6|7|8|9", "102030405060708090")]
        public void AggregateSeedResult(string text, string result)
        {
            var actual = GetSequence(text).Aggregate(_doubleTransformer,
                new StringBuilder(),
                (sb, current) => sb.Append((current * 10.0).ToString(null, CultureInfo.InvariantCulture)),
                sb => sb.ToString()
                );
            Assert.That(actual, Is.EqualTo(result));
        }


#if NET7_0_OR_GREATER
        [TestCase(@"", false, 0)]
        [TestCase(@"1", true, 1)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
        public void SumGeneric(string text, bool success, int result)
        {
            var pair = GetSequence(text).Sum(_intTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|10", true, 5.5)]
        public void AverageGeneric(string text, bool expectedSuccess, double expectedResult)
        {
            var (success, result) = GetSequence(text).Average<int, double>(_intTransformer);
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo(expectedResult));
        }

        [Test]
        public void AverageGeneric()
        {
            var (success, result) = GetSequence(@"1|2|3|4|5|6|7|8|9|10").Average<int, int>(_intTransformer);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(5)); //5, not 5.5
        }

        [TestCase(@"", false, 0f)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 5.1593833f)]
        [TestCase(@"0", true, 0f)]
        [TestCase(@"10", true, 10f)]
        [TestCase(@"1|-1|2|-2", true, 3.3333333333333335f)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 7.5f)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, 33.529411764705891f)]
        public void VarianceGeneric(string text, bool success, float result)
        {
            var pair = GetSequence(text).Variance<Half, float>(_halfTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 7.22)]
        [TestCase(@"0", true, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 2)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 9)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, 9)]

        [TestCase(@"-∞|0|10|∞", true, double.PositiveInfinity)]
        [TestCase(@"-∞|0|10|NaN|∞", true, double.PositiveInfinity)]
        [TestCase(@"NaN|NaN|-∞|0|10|NaN|∞", true, double.PositiveInfinity)]
        [TestCase(@"NaN|NaN|10", true, 10)]
        [TestCase(@"NaN|NaN|NaN", true, double.NaN)]
        [TestCase(@"NaN|5.0", true, 5.0)]
        [TestCase(@"5.0|NaN", true, 5.0)]
        public void MaxGeneric(string text, bool success, double result)
        {
            var pair = GetSequence(text).Max<double>(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1.21|3.4|2|4.66|1.5|5.61|7.22", true, 1.21)]
        [TestCase(@"0", true, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, -2)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 1)]
        [TestCase(@"1|2|3|4|5|6|7|8|9|-1|-2|-3|-4|-5|-6|-7|-8|-9", true, -9)]

        [TestCase(@"-∞|0|10|∞", true, double.NegativeInfinity)]
        [TestCase(@"-∞|0|10|NaN|∞", true, double.NaN)]
        [TestCase(@"NaN|NaN|-∞|0|10|NaN|∞", true, double.NaN)]
        [TestCase(@"NaN|NaN|10", true, double.NaN)]
        [TestCase(@"-10|NaN|NaN|10", true, double.NaN)]
        [TestCase(@"NaN|NaN|NaN", true, double.NaN)]
        [TestCase(@"NaN|5.0", true, double.NaN)]
        [TestCase(@"5.0|NaN", true, double.NaN)]
        public void MinGeneric(string text, bool success, double result)
        {
            var pair = GetSequence(text).Min<double>(_doubleTransformer);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }
#endif
    }
}
