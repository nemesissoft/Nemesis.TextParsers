﻿using System.Globalization;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests;

[TestFixture]
class LightLinqTests
{
    private static readonly ITransformer<double> _doubleTransformer = DoubleTransformer.Instance;

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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
        });
    }

    [TestCase(@"", false, 0)]
    [TestCase(@"10", true, 10)]
    [TestCase(@"1|-1|2|-2", true, 0)]
    [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
    [TestCase(@"1|2|3|4|5|6|7|8|9|10", true, 5.5)]
    [TestCase(@"1.01|1.02|1.03|1.04|1.05|1.06|1.07|1.08|1.09|1.10|1.11|1.12|1.13|1.14|1.15|1.16|1.17|1.18|1.19|1.20|1.21|1.22|1.23|1.24|1.25|1.26|1.27|1.28|1.29|1.30|1.31|1.32|1.33|1.34|1.35|1.36|1.37|1.38|1.39|1.40|1.41|1.42|1.43|1.44|1.45|1.46|1.47|1.48|1.49|1.50|1.51|1.52|1.53|1.54|1.55|1.56|1.57|1.58|1.59|1.60|1.61|1.62|1.63|1.64|1.65|1.66|1.67|1.68|1.69|1.70|1.71|1.72|1.73|1.74|1.75|1.76|1.77|1.78|1.79|1.80|1.81|1.82|1.83|1.84|1.85|1.86|1.87|1.88|1.89|1.90|1.91|1.92|1.93|1.94|1.95|1.96|1.97|1.98|1.99|2.00|2.01|2.02|2.03|2.04|2.05|2.06|2.07|2.08|2.09|2.10|2.11|2.12|2.13|2.14|2.15|2.16|2.17|2.18|2.19|2.20|2.21|2.22|2.23|2.24|2.25|2.26|2.27|2.28|2.29|2.30|2.31|2.32|2.33|2.34|2.35|2.36|2.37|2.38|2.39|2.40|2.41|2.42|2.43|2.44|2.45|2.46|2.47|2.48|2.49|2.50|2.51|2.52|2.53|2.54|2.55|2.56|2.57|2.58|2.59|2.60|2.61|2.62|2.63|2.64|2.65|2.66|2.67|2.68|2.69|2.70|2.71|2.72|2.73|2.74|2.75|2.76|2.77|2.78|2.79|2.80|2.81|2.82|2.83|2.84|2.85|2.86|2.87|2.88|2.89|2.90|2.91|2.92|2.93|2.94|2.95|2.96|2.97|2.98|2.99|3.00|3.01|3.02|3.03|3.04|3.05|3.06|3.07|3.08|3.09|3.10|3.11|3.12|3.13|3.14|3.15|3.16|3.17|3.18|3.19|3.20|3.21|3.22|3.23|3.24|3.25|3.26|3.27|3.28|3.29|3.30|3.31|3.32|3.33|3.34|3.35|3.36|3.37|3.38|3.39|3.40|3.41|3.42|3.43|3.44|3.45|3.46|3.47|3.48|3.49|3.50|3.51|3.52|3.53|3.54|3.55|3.56|3.57|3.58|3.59|3.60|3.61|3.62|3.63|3.64|3.65|3.66|3.67|3.68|3.69|3.70|3.71|3.72|3.73|3.74|3.75|3.76|3.77|3.78|3.79|3.80|3.81|3.82|3.83|3.84|3.85|3.86|3.87|3.88|3.89|3.90|3.91|3.92|3.93|3.94|3.95|3.96|3.97|3.98|3.99|4.00|4.01|4.02|4.03|4.04|4.05|4.06|4.07|4.08|4.09|4.10|4.11|4.12|4.13|4.14|4.15|4.16|4.17|4.18|4.19|4.20|4.21|4.22|4.23|4.24|4.25|4.26|4.27|4.28|4.29|4.30|4.31|4.32|4.33|4.34|4.35|4.36|4.37|4.38|4.39|4.40|4.41|4.42|4.43|4.44|4.45|4.46|4.47|4.48|4.49|4.50|4.51|4.52|4.53|4.54|4.55|4.56|4.57|4.58|4.59|4.60|4.61|4.62|4.63|4.64|4.65|4.66|4.67|4.68|4.69|4.70|4.71|4.72|4.73|4.74|4.75|4.76|4.77|4.78|4.79|4.80|4.81|4.82|4.83|4.84|4.85|4.86|4.87|4.88|4.89|4.90|4.91|4.92|4.93|4.94|4.95|4.96|4.97|4.98|4.99|5.00|5.01|5.02|5.03|5.04|5.05|5.06|5.07|5.08|5.09|5.10|5.11|5.12|5.13|5.14|5.15|5.16|5.17|5.18|5.19|5.20|5.21|5.22|5.23|5.24|5.25|5.26|5.27|5.28|5.29|5.30|5.31|5.32|5.33|5.34|5.35|5.36|5.37|5.38|5.39|5.40|5.41|5.42|5.43|5.44|5.45|5.46|5.47|5.48|5.49|5.50|5.51|5.52|5.53|5.54|5.55|5.56|5.57|5.58|5.59|5.60|5.61|5.62|5.63|5.64|5.65|5.66|5.67|5.68|5.69|5.70|5.71|5.72|5.73|5.74|5.75|5.76|5.77|5.78|5.79|5.80|5.81|5.82|5.83|5.84|5.85|5.86|5.87|5.88|5.89|5.90|5.91|5.92|5.93|5.94|5.95|5.96|5.97|5.98|5.99|6.00", true, 3.5050000000000008d)] //string.Join("|", Enumerable.Range(1, 500).Select(i=>(1.0+0.01*i).ToString("0.00", CultureInfo.InvariantCulture)))
    public void Average(string text, bool expectedSuccess, double expectedResult)
    {
        var (success, result) = GetSequence(text).Average(_doubleTransformer);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo(expectedResult));
        });
    }

    [TestCase(@"", false, 0)]
    [TestCase(@"10", true, 10)]
    [TestCase(@"1|-1|2|-2", true, 0)]
    [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
    [TestCase(@"1|2|3|4|5|6|7|8|9|10", true, 5.5)]
    [TestCase(@"1.01|1.02|1.03|1.04|1.05|1.06|1.07|1.08|1.09|1.10|1.11|1.12|1.13|1.14|1.15|1.16|1.17|1.18|1.19|1.20|1.21|1.22|1.23|1.24|1.25|1.26|1.27|1.28|1.29|1.30|1.31|1.32|1.33|1.34|1.35|1.36|1.37|1.38|1.39|1.40|1.41|1.42|1.43|1.44|1.45|1.46|1.47|1.48|1.49|1.50|1.51|1.52|1.53|1.54|1.55|1.56|1.57|1.58|1.59|1.60|1.61|1.62|1.63|1.64|1.65|1.66|1.67|1.68|1.69|1.70|1.71|1.72|1.73|1.74|1.75|1.76|1.77|1.78|1.79|1.80|1.81|1.82|1.83|1.84|1.85|1.86|1.87|1.88|1.89|1.90|1.91|1.92|1.93|1.94|1.95|1.96|1.97|1.98|1.99|2.00|2.01|2.02|2.03|2.04|2.05|2.06|2.07|2.08|2.09|2.10|2.11|2.12|2.13|2.14|2.15|2.16|2.17|2.18|2.19|2.20|2.21|2.22|2.23|2.24|2.25|2.26|2.27|2.28|2.29|2.30|2.31|2.32|2.33|2.34|2.35|2.36|2.37|2.38|2.39|2.40|2.41|2.42|2.43|2.44|2.45|2.46|2.47|2.48|2.49|2.50|2.51|2.52|2.53|2.54|2.55|2.56|2.57|2.58|2.59|2.60|2.61|2.62|2.63|2.64|2.65|2.66|2.67|2.68|2.69|2.70|2.71|2.72|2.73|2.74|2.75|2.76|2.77|2.78|2.79|2.80|2.81|2.82|2.83|2.84|2.85|2.86|2.87|2.88|2.89|2.90|2.91|2.92|2.93|2.94|2.95|2.96|2.97|2.98|2.99|3.00|3.01|3.02|3.03|3.04|3.05|3.06|3.07|3.08|3.09|3.10|3.11|3.12|3.13|3.14|3.15|3.16|3.17|3.18|3.19|3.20|3.21|3.22|3.23|3.24|3.25|3.26|3.27|3.28|3.29|3.30|3.31|3.32|3.33|3.34|3.35|3.36|3.37|3.38|3.39|3.40|3.41|3.42|3.43|3.44|3.45|3.46|3.47|3.48|3.49|3.50|3.51|3.52|3.53|3.54|3.55|3.56|3.57|3.58|3.59|3.60|3.61|3.62|3.63|3.64|3.65|3.66|3.67|3.68|3.69|3.70|3.71|3.72|3.73|3.74|3.75|3.76|3.77|3.78|3.79|3.80|3.81|3.82|3.83|3.84|3.85|3.86|3.87|3.88|3.89|3.90|3.91|3.92|3.93|3.94|3.95|3.96|3.97|3.98|3.99|4.00|4.01|4.02|4.03|4.04|4.05|4.06|4.07|4.08|4.09|4.10|4.11|4.12|4.13|4.14|4.15|4.16|4.17|4.18|4.19|4.20|4.21|4.22|4.23|4.24|4.25|4.26|4.27|4.28|4.29|4.30|4.31|4.32|4.33|4.34|4.35|4.36|4.37|4.38|4.39|4.40|4.41|4.42|4.43|4.44|4.45|4.46|4.47|4.48|4.49|4.50|4.51|4.52|4.53|4.54|4.55|4.56|4.57|4.58|4.59|4.60|4.61|4.62|4.63|4.64|4.65|4.66|4.67|4.68|4.69|4.70|4.71|4.72|4.73|4.74|4.75|4.76|4.77|4.78|4.79|4.80|4.81|4.82|4.83|4.84|4.85|4.86|4.87|4.88|4.89|4.90|4.91|4.92|4.93|4.94|4.95|4.96|4.97|4.98|4.99|5.00|5.01|5.02|5.03|5.04|5.05|5.06|5.07|5.08|5.09|5.10|5.11|5.12|5.13|5.14|5.15|5.16|5.17|5.18|5.19|5.20|5.21|5.22|5.23|5.24|5.25|5.26|5.27|5.28|5.29|5.30|5.31|5.32|5.33|5.34|5.35|5.36|5.37|5.38|5.39|5.40|5.41|5.42|5.43|5.44|5.45|5.46|5.47|5.48|5.49|5.50|5.51|5.52|5.53|5.54|5.55|5.56|5.57|5.58|5.59|5.60|5.61|5.62|5.63|5.64|5.65|5.66|5.67|5.68|5.69|5.70|5.71|5.72|5.73|5.74|5.75|5.76|5.77|5.78|5.79|5.80|5.81|5.82|5.83|5.84|5.85|5.86|5.87|5.88|5.89|5.90|5.91|5.92|5.93|5.94|5.95|5.96|5.97|5.98|5.99|6.00", true, 3.5049999999999675d)] //string.Join("|", Enumerable.Range(1, 500).Select(i=>(1.0+0.01*i).ToString("0.00", CultureInfo.InvariantCulture)))        
    public void WalkingAverage(string text, bool expectedSuccess, double expectedResult)
    {
        var (success, result) = GetSequence(text).WalkingAverage(_doubleTransformer);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo(expectedResult));
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
    }

    [TestCase(@"", false, 0)]
    [TestCase(@"1|2|3|4|5|6|7|8|9", true, 45)]
    public void Aggregate(string text, bool success, double result)
    {
        var pair = GetSequence(text).Aggregate(_doubleTransformer, (a, b) => a + b);
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
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
        var pair = GetSequence(text).Sum(Int32Transformer.Instance);
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result));
        });
    }

    [TestCase(@"", false, 0)]
    [TestCase(@"10", true, 10)]
    [TestCase(@"1|-1|2|-2", true, 0)]
    [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
    [TestCase(@"1|2|3|4|5|6|7|8|9|10", true, 5.5)]
    [TestCase(@"1.01|1.02|1.03|1.04|1.05|1.06|1.07|1.08|1.09|1.10|1.11|1.12|1.13|1.14|1.15|1.16|1.17|1.18|1.19|1.20|1.21|1.22|1.23|1.24|1.25|1.26|1.27|1.28|1.29|1.30|1.31|1.32|1.33|1.34|1.35|1.36|1.37|1.38|1.39|1.40|1.41|1.42|1.43|1.44|1.45|1.46|1.47|1.48|1.49|1.50|1.51|1.52|1.53|1.54|1.55|1.56|1.57|1.58|1.59|1.60|1.61|1.62|1.63|1.64|1.65|1.66|1.67|1.68|1.69|1.70|1.71|1.72|1.73|1.74|1.75|1.76|1.77|1.78|1.79|1.80|1.81|1.82|1.83|1.84|1.85|1.86|1.87|1.88|1.89|1.90|1.91|1.92|1.93|1.94|1.95|1.96|1.97|1.98|1.99|2.00|2.01|2.02|2.03|2.04|2.05|2.06|2.07|2.08|2.09|2.10|2.11|2.12|2.13|2.14|2.15|2.16|2.17|2.18|2.19|2.20|2.21|2.22|2.23|2.24|2.25|2.26|2.27|2.28|2.29|2.30|2.31|2.32|2.33|2.34|2.35|2.36|2.37|2.38|2.39|2.40|2.41|2.42|2.43|2.44|2.45|2.46|2.47|2.48|2.49|2.50|2.51|2.52|2.53|2.54|2.55|2.56|2.57|2.58|2.59|2.60|2.61|2.62|2.63|2.64|2.65|2.66|2.67|2.68|2.69|2.70|2.71|2.72|2.73|2.74|2.75|2.76|2.77|2.78|2.79|2.80|2.81|2.82|2.83|2.84|2.85|2.86|2.87|2.88|2.89|2.90|2.91|2.92|2.93|2.94|2.95|2.96|2.97|2.98|2.99|3.00|3.01|3.02|3.03|3.04|3.05|3.06|3.07|3.08|3.09|3.10|3.11|3.12|3.13|3.14|3.15|3.16|3.17|3.18|3.19|3.20|3.21|3.22|3.23|3.24|3.25|3.26|3.27|3.28|3.29|3.30|3.31|3.32|3.33|3.34|3.35|3.36|3.37|3.38|3.39|3.40|3.41|3.42|3.43|3.44|3.45|3.46|3.47|3.48|3.49|3.50|3.51|3.52|3.53|3.54|3.55|3.56|3.57|3.58|3.59|3.60|3.61|3.62|3.63|3.64|3.65|3.66|3.67|3.68|3.69|3.70|3.71|3.72|3.73|3.74|3.75|3.76|3.77|3.78|3.79|3.80|3.81|3.82|3.83|3.84|3.85|3.86|3.87|3.88|3.89|3.90|3.91|3.92|3.93|3.94|3.95|3.96|3.97|3.98|3.99|4.00|4.01|4.02|4.03|4.04|4.05|4.06|4.07|4.08|4.09|4.10|4.11|4.12|4.13|4.14|4.15|4.16|4.17|4.18|4.19|4.20|4.21|4.22|4.23|4.24|4.25|4.26|4.27|4.28|4.29|4.30|4.31|4.32|4.33|4.34|4.35|4.36|4.37|4.38|4.39|4.40|4.41|4.42|4.43|4.44|4.45|4.46|4.47|4.48|4.49|4.50|4.51|4.52|4.53|4.54|4.55|4.56|4.57|4.58|4.59|4.60|4.61|4.62|4.63|4.64|4.65|4.66|4.67|4.68|4.69|4.70|4.71|4.72|4.73|4.74|4.75|4.76|4.77|4.78|4.79|4.80|4.81|4.82|4.83|4.84|4.85|4.86|4.87|4.88|4.89|4.90|4.91|4.92|4.93|4.94|4.95|4.96|4.97|4.98|4.99|5.00|5.01|5.02|5.03|5.04|5.05|5.06|5.07|5.08|5.09|5.10|5.11|5.12|5.13|5.14|5.15|5.16|5.17|5.18|5.19|5.20|5.21|5.22|5.23|5.24|5.25|5.26|5.27|5.28|5.29|5.30|5.31|5.32|5.33|5.34|5.35|5.36|5.37|5.38|5.39|5.40|5.41|5.42|5.43|5.44|5.45|5.46|5.47|5.48|5.49|5.50|5.51|5.52|5.53|5.54|5.55|5.56|5.57|5.58|5.59|5.60|5.61|5.62|5.63|5.64|5.65|5.66|5.67|5.68|5.69|5.70|5.71|5.72|5.73|5.74|5.75|5.76|5.77|5.78|5.79|5.80|5.81|5.82|5.83|5.84|5.85|5.86|5.87|5.88|5.89|5.90|5.91|5.92|5.93|5.94|5.95|5.96|5.97|5.98|5.99|6.00", true, 3.5050000000000008d)]
    public void AverageGeneric(string text, bool expectedSuccess, double expectedResult)
    {
        var (success, result) = GetSequence(text).Average<double, double, double>(_doubleTransformer);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo(expectedResult));
        });
    }

    [TestCase(@"", false, 0)]
    [TestCase(@"10", true, 10)]
    [TestCase(@"1|-1|2|-2", true, 0)]
    [TestCase(@"1|2|3|4|5|6|7|8|9", true, 5)]
    [TestCase(@"1|2|3|4|5|6|7|8|9|10", true, 5.5)]
    [TestCase(@"1.01|1.02|1.03|1.04|1.05|1.06|1.07|1.08|1.09|1.10|1.11|1.12|1.13|1.14|1.15|1.16|1.17|1.18|1.19|1.20|1.21|1.22|1.23|1.24|1.25|1.26|1.27|1.28|1.29|1.30|1.31|1.32|1.33|1.34|1.35|1.36|1.37|1.38|1.39|1.40|1.41|1.42|1.43|1.44|1.45|1.46|1.47|1.48|1.49|1.50|1.51|1.52|1.53|1.54|1.55|1.56|1.57|1.58|1.59|1.60|1.61|1.62|1.63|1.64|1.65|1.66|1.67|1.68|1.69|1.70|1.71|1.72|1.73|1.74|1.75|1.76|1.77|1.78|1.79|1.80|1.81|1.82|1.83|1.84|1.85|1.86|1.87|1.88|1.89|1.90|1.91|1.92|1.93|1.94|1.95|1.96|1.97|1.98|1.99|2.00|2.01|2.02|2.03|2.04|2.05|2.06|2.07|2.08|2.09|2.10|2.11|2.12|2.13|2.14|2.15|2.16|2.17|2.18|2.19|2.20|2.21|2.22|2.23|2.24|2.25|2.26|2.27|2.28|2.29|2.30|2.31|2.32|2.33|2.34|2.35|2.36|2.37|2.38|2.39|2.40|2.41|2.42|2.43|2.44|2.45|2.46|2.47|2.48|2.49|2.50|2.51|2.52|2.53|2.54|2.55|2.56|2.57|2.58|2.59|2.60|2.61|2.62|2.63|2.64|2.65|2.66|2.67|2.68|2.69|2.70|2.71|2.72|2.73|2.74|2.75|2.76|2.77|2.78|2.79|2.80|2.81|2.82|2.83|2.84|2.85|2.86|2.87|2.88|2.89|2.90|2.91|2.92|2.93|2.94|2.95|2.96|2.97|2.98|2.99|3.00|3.01|3.02|3.03|3.04|3.05|3.06|3.07|3.08|3.09|3.10|3.11|3.12|3.13|3.14|3.15|3.16|3.17|3.18|3.19|3.20|3.21|3.22|3.23|3.24|3.25|3.26|3.27|3.28|3.29|3.30|3.31|3.32|3.33|3.34|3.35|3.36|3.37|3.38|3.39|3.40|3.41|3.42|3.43|3.44|3.45|3.46|3.47|3.48|3.49|3.50|3.51|3.52|3.53|3.54|3.55|3.56|3.57|3.58|3.59|3.60|3.61|3.62|3.63|3.64|3.65|3.66|3.67|3.68|3.69|3.70|3.71|3.72|3.73|3.74|3.75|3.76|3.77|3.78|3.79|3.80|3.81|3.82|3.83|3.84|3.85|3.86|3.87|3.88|3.89|3.90|3.91|3.92|3.93|3.94|3.95|3.96|3.97|3.98|3.99|4.00|4.01|4.02|4.03|4.04|4.05|4.06|4.07|4.08|4.09|4.10|4.11|4.12|4.13|4.14|4.15|4.16|4.17|4.18|4.19|4.20|4.21|4.22|4.23|4.24|4.25|4.26|4.27|4.28|4.29|4.30|4.31|4.32|4.33|4.34|4.35|4.36|4.37|4.38|4.39|4.40|4.41|4.42|4.43|4.44|4.45|4.46|4.47|4.48|4.49|4.50|4.51|4.52|4.53|4.54|4.55|4.56|4.57|4.58|4.59|4.60|4.61|4.62|4.63|4.64|4.65|4.66|4.67|4.68|4.69|4.70|4.71|4.72|4.73|4.74|4.75|4.76|4.77|4.78|4.79|4.80|4.81|4.82|4.83|4.84|4.85|4.86|4.87|4.88|4.89|4.90|4.91|4.92|4.93|4.94|4.95|4.96|4.97|4.98|4.99|5.00|5.01|5.02|5.03|5.04|5.05|5.06|5.07|5.08|5.09|5.10|5.11|5.12|5.13|5.14|5.15|5.16|5.17|5.18|5.19|5.20|5.21|5.22|5.23|5.24|5.25|5.26|5.27|5.28|5.29|5.30|5.31|5.32|5.33|5.34|5.35|5.36|5.37|5.38|5.39|5.40|5.41|5.42|5.43|5.44|5.45|5.46|5.47|5.48|5.49|5.50|5.51|5.52|5.53|5.54|5.55|5.56|5.57|5.58|5.59|5.60|5.61|5.62|5.63|5.64|5.65|5.66|5.67|5.68|5.69|5.70|5.71|5.72|5.73|5.74|5.75|5.76|5.77|5.78|5.79|5.80|5.81|5.82|5.83|5.84|5.85|5.86|5.87|5.88|5.89|5.90|5.91|5.92|5.93|5.94|5.95|5.96|5.97|5.98|5.99|6.00", true, 3.562)]
    public void WalkingAverageGeneric(string text, bool expectedSuccess, double expectedResult)
    {
        var (success, result) = GetSequence(text).WalkingAverage<Half>(HalfTransformer.Instance);
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(result, Is.EqualTo((Half)expectedResult));
        });
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
        var pair = GetSequence(text).Variance<Half, float>(HalfTransformer.Instance);
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
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
        Assert.Multiple(() =>
        {
            Assert.That(pair.success, Is.EqualTo(success));
            Assert.That(pair.result, Is.EqualTo(result).Within(2).Ulps);
        });
    }
#endif
}
