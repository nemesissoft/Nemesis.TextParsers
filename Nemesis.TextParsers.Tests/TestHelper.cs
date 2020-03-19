using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    internal class TestHelper
    {
        public static void AssertException(Exception actual, Type expectedException, string expectedErrorMessagePart, bool logMessage = false)
        {
            if (actual is TargetInvocationException tie && tie.InnerException is { } inner)
                actual = inner;

            Assert.That(actual, Is.TypeOf(expectedException), () => $@"Unexpected external exception: {actual}");

            if (expectedErrorMessagePart != null)
                Assert.That(
                    IgnoreNewLinesComparer.NormalizeNewLines(actual?.Message),
                    Does.Contain(IgnoreNewLinesComparer.NormalizeNewLines(expectedErrorMessagePart))
                );

            if (!logMessage) return;

            string message = actual switch
            {
                OverflowException oe => $"Expected overflow: {oe.Message}",
                FormatException fe => $"Expected bad format: {fe.Message}",
                ArgumentException ae => $"Expected argument exception: {ae.Message}",
                InvalidOperationException ioe => $"Expected invalid operation: {ioe.Message}",
                _ => $"Expected other: {actual?.Message}"
            };

            Console.WriteLine(message);
        }
    }

    internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
    {
        public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();
        public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

        public int Compare(string x, string y) => string.CompareOrdinal(NormalizeNewLines(x), NormalizeNewLines(y));

        public bool Equals(string x, string y) => NormalizeNewLines(x) == NormalizeNewLines(y);

        public int GetHashCode(string s) => NormalizeNewLines(s)?.GetHashCode() ?? 0;

        public static string NormalizeNewLines(string s) => s?
            .Replace(Environment.NewLine, "")
            .Replace("\n", "")
            .Replace("\r", "");
    }
}
