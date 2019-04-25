using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    //TODO test for FindUnescapedSeparator for | at the end 
    [TestFixture]
    public class TokenSequenceTests
    {
        private static IEnumerable<(string, IEnumerable<string>)> ValidListData() => new (string, IEnumerable<string>)[]
        {
            ("", new []{""}),
            (@"AAA|BBB|CCC", new []{"AAA","BBB","CCC"}),
            (@"|BBB||CCC", new []{"","BBB","","CCC"}),
            (@"|BBB|\|CCC", new []{"","BBB",@"\|CCC"}),
            (@"|B\\BB|\|CCC", new []{"",@"B\\BB",@"\|CCC"}),
            (@"|BBB|", new []{"","BBB",""}),
            (@"|BBB\|", new []{"",@"BBB\|"}),
            (@"\|BBB|", new []{@"\|BBB",""}),


            (@"\|\\\|", new []{ @"\|\\\|"}),
            (@"\|\|\\|", new []{ @"\|\|\\", ""}),
            (@"ABC\|\|\\|DEF", new []{ @"ABC\|\|\\", "DEF"}),

            (@"\\1\\|ABC|\\2\\", new []{@"\\1\\","ABC", @"\\2\\"}),
            (@"\\3\\|ABC|\|", new []{@"\\3\\","ABC", @"\|"}),
            (@"\||ABC|\\4\\", new []{@"\|","ABC", @"\\4\\"}),

            (@"\|", new []{@"\|"}),
            (@"\\|", new []{@"\\", ""}),
            (@"|", new []{@"", ""}),
            (@" |", new []{@" ", ""}),

            (@" |\\", new []{@" ", @"\\"}),
            (@" |\", new []{@" ", @"\"}),
            (@" |\|", new []{@" ", @"\|"}),
        };

        [TestCaseSource(nameof(ValidListData))]
        public void TokenizeTest((string input, IEnumerable<string> expectedList) data)
        {
            var tokens = data.input.AsSpan().Tokenize('|', '\\', false);

            var result = new List<string>();

            foreach (var part in tokens)
                result.Add(part.ToString());


            if (data.expectedList == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EquivalentTo(data.expectedList));
        }

        [TestCase((string)null)]
        [TestCase("")]
        public void TokenizeTest_CheckEmpty(string input)
        {
            var tokens = input.AsSpan().Tokenize('|', '\\', true);

            var result = new List<string>();

            foreach (var part in tokens)
                result.Add(part.ToString());

            Assert.That(result, Is.Empty);
        }
    }
}
