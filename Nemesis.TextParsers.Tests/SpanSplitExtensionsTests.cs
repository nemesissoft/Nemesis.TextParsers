using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class SpanSplitExtensionsTests
    {
        [TestCase(",,", ',')]
        [TestCase("", 'a')]
        [TestCase(";;", ';')]
        [TestCase("a;;b", ';')]
        [TestCase("a", 'a')]
        [TestCase("aaaa", 'a')]
        [TestCase("abababab", 'a')]
        [TestCase("babababa", 'a')]
        [TestCase("aaaaab", 'a')]
        [TestCase("baaaaa", 'a')]
        [TestCase("zzzzzazzzzazzzzazzz", 'a')]
        [TestCase("123,456,", ',')]
        public void Split_EmptySequence_Test(string str, char separator)
        {
            var actual = new List<string>();
            foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator, true))
                actual.Add(part.ToString());

            string[] expected = str == "" ? new string[0] : str.Split(separator);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(",,", ',')]
        [TestCase("", 'a')]
        [TestCase("a", 'a')]
        [TestCase("aaaa", 'a')]
        [TestCase("abababab", 'a')]
        [TestCase("babababa", 'a')]
        [TestCase("aaaaab", 'a')]
        [TestCase("baaaaa", 'a')]
        [TestCase("zzzzzazzzzazzzzazzz", 'a')]
        [TestCase("123,456,", ',')]
        public void SplitTest(string str, char separator)
        {
            var actual = new List<string>();
            foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator))
                actual.Add(part.ToString());

            string[] expected = str.Split(separator);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(",,;,,", ',', ';')]
        [TestCase("", 'a', ';')]
        [TestCase("a", 'a', ';')]
        [TestCase("aaaa", 'a', ';')]
        [TestCase("aa;aa", 'a', ';')]
        [TestCase("abababab", 'a', ';')]
        [TestCase("aba;ba;bab", 'a', ';')]
        [TestCase("babababa", 'a', ';')]
        [TestCase("baba;baba", 'a', ';')]
        [TestCase("aaa;aab", 'a', ';')]
        [TestCase("baa;aaa", 'a', ';')]
        [TestCase("zzz;zzazzzzazzzzazzz", 'a', ';')]
        [TestCase("zzz;zzazzzzaz;zzzazzz", 'a', ';')]
        [TestCase("123,;456,", ',', ';')]
        public void Split2Test(string str, char separator1, char separator2)
        {
            var actual = new List<string>();
            foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator1, separator2))
                actual.Add(part.ToString());

            string[] expected = str.Split(separator1, separator2);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(",|,;,,", ',', ';', '|')]
        [TestCase("", 'a', ';', '|')]
        [TestCase("a", 'a', ';', '|')]
        [TestCase("aaaa", 'a', ';', '|')]
        [TestCase("aa;aa", 'a', ';', '|')]
        [TestCase("ab|ab||ab|ab", 'a', ';', '|')]
        [TestCase("aba;b|a;bab", 'a', ';', '|')]
        [TestCase("bab|ababa", 'a', ';', '|')]
        [TestCase("babababa", 'a', ';', '|')]
        [TestCase("baba;ba|ba", 'a', ';', '|')]
        [TestCase("aaa;aa|b", 'a', ';', '|')]
        [TestCase("baa;aaa", 'a', ';', '|')]
        [TestCase("zzz;zzazzzz;456|azzzzazzz|", 'a', ';', '|')]
        [TestCase("|zzz|;zzazzzzaz;|zzzazzz|", 'a', ';', '|')]
        [TestCase("123,;|456,", ',', ';', '|')]
        [TestCase(",;|,;|,;|", ',', ';', '|')]
        public void Split3Test(string str, char separator1, char separator2, char separator3)
        {
            var actual = new List<string>();
            foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator1, separator2, separator3))
                actual.Add(part.ToString());

            string[] expected = str.Split(separator1, separator2, separator3);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(",|/,;/,,", ',', ';', '|', '/')]
        [TestCase("", 'a', ';', '|', '/')]
        [TestCase("a", 'a', ';', '|', '/')]
        [TestCase("aaaa", 'a', ';', '|', '/')]
        [TestCase("aa;aa", 'a', ';', '|', '/')]
        [TestCase("ab|a/b||a/b|ab", 'a', ';', '|', '/')]
        [TestCase("ab/a;b|a;bab", 'a', ';', '|', '/')]
        [TestCase("bab|ab/aba", 'a', ';', '|', '/')]
        [TestCase("ba//b/ababa", 'a', ';', '|', '/')]
        [TestCase("ba//ba;ba|ba/", 'a', ';', '|', '/')]
        [TestCase("/aaa;aa|b/", 'a', ';', '|', '/')]
        [TestCase("/baa;aaa/", 'a', ';', '|', '/')]
        [TestCase("z/zz;zz/azzz/z;456|azz/zzazzz/|/", 'a', ';', '|', '/')]
        [TestCase("|/zzz|;zzazzzzaz;|zzz/azzz/|", 'a', ';', '|', '/')]
        [TestCase("1/23,;|45/6,/", ',', ';', '|', '/')]
        [TestCase("/,;|/,;|/,;|/", ',', ';', '|', '/')]
        public void SplitNTest(string str, char separator1, char separator2, char separator3, char separator4)
        {
            var actual = new List<string>();
            ReadOnlySpan<char> separators = stackalloc char[] { separator1, separator2, separator3, separator4 };
            foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separators))
                actual.Add(part.ToString());

            string[] expected = str.Split(separator1, separator2, separator3, separator4);
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
