using System;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class SpanParserHelperTests
    {
        [TestCase(@"ABC", @"ABC", ';')]
        [TestCase(@"ABC\\;", @"ABC\\;", ';')]
        [TestCase(@"ABC\\\;", @"ABC\\;", ';')]
        [TestCase(@"\ABC\", @"\ABC\", ';')]
        [TestCase(@"\A\B\C", @"\A\B\C", ';')]
        [TestCase(@"ABC\;", @"ABC;", ';')]
        [TestCase(@"\\ABC\\\;", @"\\ABC\\;", ';')]

        //TODO more \ unescape tests
        [TestCase(@"\\ABC\\\;", @"\ABC\\;", '\\')]
        public void UnescapeCharacterTests(string input, string expectedOutput, char character)
        {
            var actual = input.AsSpan().UnescapeCharacter('\\', character)
                .ToString();

            Assert.That(actual, Is.EqualTo(expectedOutput));
        }
    }
}
