using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    internal class TestHelper
    {
        public static string AssertException(Exception actual, Type expectedException, string expectedErrorMessagePart, bool logMessage = false)
        {
            if (actual is TargetInvocationException tie && tie.InnerException is { } inner)
                actual = inner;

            Assert.That(actual, Is.TypeOf(expectedException), () => $@"Unexpected external exception: {actual}");

            if (expectedErrorMessagePart != null)
                Assert.That(
                    IgnoreNewLinesComparer.NormalizeNewLines(actual?.Message),
                    Does.Contain(IgnoreNewLinesComparer.NormalizeNewLines(expectedErrorMessagePart))
                );

            if (!logMessage) return "";

            string message = actual switch
            {
                OverflowException oe => $"Expected overflow: {oe.Message}",
                FormatException fe => $"Expected bad format: {fe.Message}",
                ArgumentException ae => $"Expected argument exception: {ae.Message}",
                InvalidOperationException ioe => $"Expected invalid operation: {ioe.Message}",
                _ => $"Expected other: {actual?.Message}"
            };

            return message;
        }

        public static void IsMutuallyEquivalent(object o1, object o2, string because = "")
        {
            o1.Should().BeEquivalentTo(o2, because);
            o2.Should().BeEquivalentTo(o1, because);
        }

        public static TDelegate MakeDelegate<TDelegate>(Expression<TDelegate> expression, params Type[] typeArguments)
            where TDelegate : Delegate
        {
            var method = Method.OfExpression(expression);
            if (method.IsGenericMethod)
            {
                method = method.GetGenericMethodDefinition();
                method = method.MakeGenericMethod(typeArguments);
            }

            var parameters = method.GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToList();
            var call = method.IsStatic
                ? Expression.Call(method, parameters)
                : Expression.Call(parameters[0], method, parameters.Skip(1));

            return Expression.Lambda<TDelegate>(call, parameters).Compile();
        }


    }

    internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
    {
        [PublicAPI]
        public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();
        [PublicAPI]
        public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

        public int Compare(string x, string y) => string.CompareOrdinal(NormalizeNewLines(x), NormalizeNewLines(y));

        public bool Equals(string x, string y) => NormalizeNewLines(x) == NormalizeNewLines(y);

        public int GetHashCode(string s) => NormalizeNewLines(s)?.GetHashCode() ?? 0;

        public static string NormalizeNewLines(string s) => s?
            .Replace(Environment.NewLine, "")
            .Replace("\n", "")
            .Replace("\r", "");
    }

    [PublicAPI]
    internal class RandomSource
    {
        private int _seed;
        private Random _rand;

        public RandomSource() => UseNewSeed();

        public RandomSource(int seed) => UseNewSeed(seed);

        public int UseNewSeed()
        {
            _seed = Environment.TickCount;
            _rand = new Random(_seed);
            return _seed;
        }

        public void UseNewSeed(int newSeed)
        {
            _seed = newSeed;
            _rand = new Random(_seed);
        }

        public int Next() => _rand.Next();
        public int Next(int maxValue) => _rand.Next(maxValue);
        public int Next(int minValue, int maxValue) => _rand.Next(minValue, maxValue);
        public double NextDouble() => _rand.NextDouble();

        public TElement NextElement<TElement>(IReadOnlyList<TElement> list) => list[Next(list.Count)];

        public string NextString(char start, char end, int length = 10)
        {
            var chars = new char[length];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)_rand.Next(start, end + 1);
            return new string(chars);
        }

        public double NextFloatingNumber(int magnitude = 1000, bool generateSpecialValues = true)
        {
            if (generateSpecialValues && _rand.NextDouble() is { } chance && chance < 0.1)
            {
                if (chance < 0.045) return double.PositiveInfinity;
                else if (chance < 0.09) return double.NegativeInfinity;
                else return double.NaN;
            }
            else
                return Math.Round((_rand.NextDouble() - 0.5) * 2 * magnitude, 3);
        }
    }
}
