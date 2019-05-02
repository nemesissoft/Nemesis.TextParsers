using System;
using System.Globalization;
using System.Text;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class LinqTests
    {
        private static ParsedSequence<double> GetSequence(string text) => SpanCollectionSerializer.DefaultInstance.ParseStream<double>(text.AsSpan());

        [TestCase(@"", false, 0)]
        [TestCase(@"1", true, 1)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
        public void Sum(string text, bool success, double result)
        {
            var pair = GetSequence(text).Sum();
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
        }


        [TestCase(@"", false, 0)]
        [TestCase(@"10", true, 10)]
        [TestCase(@"1|-1|2|-2", true, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
        public void Average(string text, bool success, double result)
        {
            var pair = GetSequence(text).Average();
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
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
            var pair = GetSequence(text).Variance();
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
        public void Max(string text, bool success, double result)
        {
            var pair = GetSequence(text).Max();
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
        public void Min(string text, bool success, double result)
        {
            var pair = GetSequence(text).Min();
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", false, 0)]
        [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
        public void Aggregate(string text, bool success, double result)
        {
            var pair = GetSequence(text).Aggregate((a, b) => a + b);
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        }

        [TestCase(@"", "")]
        [TestCase(@"1|2|3|4|5|6|7|8|9", "102030405060708090")]
        public void AggregateSeed(string text, string result)
        {
            var actual = GetSequence(text).Aggregate(
                new StringBuilder(), 
                (sb, current) => sb.Append((current * 10.0).ToString(null, CultureInfo.InvariantCulture))
                );
            Assert.That(actual.ToString(), Is.EqualTo(result));
        }

        [TestCase(@"", "")]
        [TestCase(@"1|2|3|4|5|6|7|8|9", "102030405060708090")]
        public void AggregateSeedResult(string text, string result)
        {
            var actual = GetSequence(text).Aggregate(
                new StringBuilder(), 
                (sb, current) => sb.Append((current * 10.0).ToString(null, CultureInfo.InvariantCulture)),
                sb=>sb.ToString()
                );
            Assert.That(actual, Is.EqualTo(result));
        }
    }
}
