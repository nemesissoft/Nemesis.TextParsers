using System;
using System.Collections.Generic;


namespace Nemesis.TextParsers.CodeGen.Tests
{
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
