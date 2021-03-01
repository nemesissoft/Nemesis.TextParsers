using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class SpanParserHelperTests
    {
        private static IEnumerable<TestCaseData> ValidValuesFor1() => new[]
        {
            new TestCaseData(@"", @"", ';'),
            new TestCaseData(@";", @";", ';'),
            new TestCaseData(@"\;", @";", ';'),
            new TestCaseData(@"XYZ\", @"XYZ\", ';'),
            new TestCaseData(@"ABC", @"ABC", ';'),
            new TestCaseData(@"ABC\\;", @"ABC\\;", ';'),
            new TestCaseData(@"ABC\\\;", @"ABC\\;", ';'),

            new TestCaseData(@"ABC\\;DEF", @"ABC\\;DEF", ';'),
            new TestCaseData(@"ABC\\\;DEF", @"ABC\\;DEF", ';'),

            new TestCaseData(@"ABC\\;DE\F", @"ABC\\;DE\F", ';'),
            new TestCaseData(@"ABC\\\;DE\F", @"ABC\\;DE\F", ';'),
            new TestCaseData(@"ABC\\\;DE\F\;", @"ABC\\;DE\F;", ';'),
            new TestCaseData(@"ABC\\\;DE\F\;", @"ABC\\;DE\F\;", '\\'),

            new TestCaseData(@"\ABC\", @"\ABC\", ';'),
            new TestCaseData(@"ABC\", @"ABC\", ';'),
            new TestCaseData(@"\ABC", @"\ABC", ';'),

            new TestCaseData(@";ABC;", @";ABC;", ';'),
            new TestCaseData(@"ABC;", @"ABC;", ';'),
            new TestCaseData(@";ABC", @";ABC", ';'),

            new TestCaseData(@"\;ABC\;", @";ABC;", ';'),
            new TestCaseData(@"ABC\;", @"ABC;", ';'),
            new TestCaseData(@"\;ABC", @";ABC", ';'),

            new TestCaseData(@"\\ABC\\", @"\\ABC\\", ';'),
            new TestCaseData(@"ABC\\", @"ABC\\", ';'),
            new TestCaseData(@"\\ABC", @"\\ABC", ';'),

            new TestCaseData(@"\A\B\C", @"\A\B\C", ';'),
            new TestCaseData(@"\A\B\C", @"A\B\C", 'A'),
            new TestCaseData(@"\A\B\C", @"\AB\C", 'B'),
            new TestCaseData(@"\A\B\C", @"\A\BC", 'C'),

            new TestCaseData(@"ABC\;", @"ABC;", ';'),
            new TestCaseData(@"\\ABC\\\;", @"\\ABC\\;", ';'),

            new TestCaseData(@"\\ABC\;", @"\ABC\;", '\\'),
            new TestCaseData(@"\\ABC\\;", @"\ABC\;", '\\'),
            new TestCaseData(@"\\ABC\\\;", @"\ABC\\;", '\\'),
            new TestCaseData(@"\\ABC\\\\;", @"\ABC\\;", '\\'),

            new TestCaseData(@"\\ABC\", @"\ABC\", '\\'),
            new TestCaseData(@"\\ABC\\", @"\ABC\", '\\'),
            new TestCaseData(@"\\ABC\\\", @"\ABC\\", '\\'),
            new TestCaseData(@"\\ABC\\\\", @"\ABC\\", '\\'),
        };

        [TestCaseSource(nameof(ValidValuesFor1))]
        public void UnescapeCharacter1Tests(string input, string expectedOutput, char character)
        {
            var actual = input.AsSpan().UnescapeCharacter('\\', character)
                .ToString();

            Assert.That(actual, Is.EqualTo(expectedOutput));
        }

        private static IEnumerable<TestCaseData> ValidValuesFor2() => ValidValuesFor1().Select(
            tcd => new TestCaseData(tcd.Arguments[0], tcd.Arguments[1], tcd.Arguments[2], '=')
        ).Concat(new[]
        {
            new TestCaseData(@"", @"", ';', ','),
            new TestCaseData(@";,", @";,", ';', ','),
            new TestCaseData(@"\;\,", @";,", ';', ','),
            new TestCaseData(@"\,\;\,", @",;,", ';', ','),
            new TestCaseData(@"\;\,\;\,", @";,;,", ';', ','),
            new TestCaseData(@"\;\,\;\,\;", @";,;,;", ';', ','),
            new TestCaseData(@"XYZ\ ,", @"XYZ\ ,", ';', ','),
            new TestCaseData(@"ABC\ ;", @"ABC\ ;", ';', ','),
            new TestCaseData(@"ABC\\;", @"ABC\\;", ';', ','),
            new TestCaseData(@"ABC\\,", @"ABC\\,", ';', ','),
            new TestCaseData(@"ABC\\\;", @"ABC\\;", ';', ','),
            new TestCaseData(@"ABC\\\,", @"ABC\\,", ';', ','),

            new TestCaseData(@"ABC\\,;DEF", @"ABC\\,;DEF", ';', ','),
            new TestCaseData(@"ABC\\\,;DEF", @"ABC\\,;DEF", ';', ','),

            new TestCaseData(@"ABC\\,;DE\,F", @"ABC\\,;DE,F", ';', ','),
            new TestCaseData(@"ABC\\\,;DE\,F", @"ABC\\,;DE,F", ';', ','),
            new TestCaseData(@"ABC\\\;DE\F\,;", @"ABC\\;DE\F,;", ';', ','),
            new TestCaseData(@"ABC\\\;DE\F,\,", @"ABC\\;DE\F,,", '\\', ','),

            new TestCaseData(@"\ABC\", @"\ABC\", ';', ','),
            new TestCaseData(@"ABC\", @"ABC\", ';', ','),
            new TestCaseData(@"\ABC", @"\ABC", ';', ','),

            new TestCaseData(@";,ABC,;", @";,ABC,;", ';', ','),
            new TestCaseData(@"ABC,;", @"ABC,;", ';', ','),
            new TestCaseData(@",;ABC", @",;ABC", ';', ','),

            new TestCaseData(@",;ABC;,", @",;ABC;,", ';', ','),
            new TestCaseData(@"ABC;,", @"ABC;,", ';', ','),
            new TestCaseData(@";,ABC", @";,ABC", ';', ','),


            new TestCaseData(@"\;\,ABC\,\;", @";,ABC,;", ';', ','),
            new TestCaseData(@"ABC\,\;", @"ABC,;", ';', ','),
            new TestCaseData(@"\,\;ABC", @",;ABC", ';', ','),

            new TestCaseData(@"\,\;ABC\;\,", @",;ABC;,", ';', ','),
            new TestCaseData(@"ABC\;\,", @"ABC;,", ';', ','),
            new TestCaseData(@"\;\,ABC", @";,ABC", ';', ','),

            new TestCaseData(@"\\ABC\\", @"\\ABC\\", ';', ','),
            new TestCaseData(@"ABC\\", @"ABC\\", ';', ','),
            new TestCaseData(@"\\ABC", @"\\ABC", ';', ','),

            new TestCaseData(@"\\ABC\\", @"\ABC\", '\\', ','),
            new TestCaseData(@"ABC\\", @"ABC\", '\\', ','),
            new TestCaseData(@"\\ABC", @"\ABC", '\\', ','),

            new TestCaseData(@"\A\B\C", @"\A\B\C", ';', ','),
            new TestCaseData(@"\A\B\C", @"AB\C", 'A', 'B'),
            new TestCaseData(@"\A\B\C", @"\ABC", 'C', 'B'),
            new TestCaseData(@"\A\B\C", @"A\BC", 'A', 'C'),


            new TestCaseData(@"\,ABC\;", @",ABC;", ';', ','),
            new TestCaseData(@"\\,ABC\\;", @"\\,ABC\\;", ';', ','),
            new TestCaseData(@"\\\,ABC\\\;", @"\\,ABC\\;", ';', ','),
            new TestCaseData(@"\\\,ABC\\\;", @"\,ABC\\;", '\\', ','),
            new TestCaseData(@"\\\,ABC\\\;", @"\\,ABC\;", '\\', ';'),

            new TestCaseData(@"\\ABC\\\;", @"\ABC\;", '\\', ';'),
        });


        [TestCaseSource(nameof(ValidValuesFor2))]
        public void UnescapeCharacter2Tests(string input, string expectedOutput, char character1, char character2)
        {
            var actual = input.AsSpan().UnescapeCharacter('\\', character1, character2)
                .ToString();

            Assert.That(actual, Is.EqualTo(expectedOutput));
        }

        [TestCaseSource(nameof(ValidValuesFor2))]
        public void UnescapeCharacter2_SymmetryTests(string input, string expectedOutput, char character1, char character2)
        {
            var actual1 = input.AsSpan().UnescapeCharacter('\\', character1, character2)
                .ToString();
            var actual2 = input.AsSpan().UnescapeCharacter('\\', character2, character1)
                .ToString();

            Assert.That(actual1, Is.EqualTo(expectedOutput));
            Assert.That(actual2, Is.EqualTo(expectedOutput));
        }
    }
}
