namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class SpanParserHelperTests
    {
        private static IEnumerable<TCD> ValidValuesFor1() =>
        [
            new TCD(@"", @"", ';'),
            new TCD(@";", @";", ';'),
            new TCD(@"\;", @";", ';'),
            new TCD(@"XYZ\", @"XYZ\", ';'),
            new TCD(@"ABC", @"ABC", ';'),
            new TCD(@"ABC\\;", @"ABC\\;", ';'),
            new TCD(@"ABC\\\;", @"ABC\\;", ';'),

            new TCD(@"ABC\\;DEF", @"ABC\\;DEF", ';'),
            new TCD(@"ABC\\\;DEF", @"ABC\\;DEF", ';'),

            new TCD(@"ABC\\;DE\F", @"ABC\\;DE\F", ';'),
            new TCD(@"ABC\\\;DE\F", @"ABC\\;DE\F", ';'),
            new TCD(@"ABC\\\;DE\F\;", @"ABC\\;DE\F;", ';'),
            new TCD(@"ABC\\\;DE\F\;", @"ABC\\;DE\F\;", '\\'),

            new TCD(@"\ABC\", @"\ABC\", ';'),
            new TCD(@"ABC\", @"ABC\", ';'),
            new TCD(@"\ABC", @"\ABC", ';'),

            new TCD(@";ABC;", @";ABC;", ';'),
            new TCD(@"ABC;", @"ABC;", ';'),
            new TCD(@";ABC", @";ABC", ';'),

            new TCD(@"\;ABC\;", @";ABC;", ';'),
            new TCD(@"ABC\;", @"ABC;", ';'),
            new TCD(@"\;ABC", @";ABC", ';'),

            new TCD(@"\\ABC\\", @"\\ABC\\", ';'),
            new TCD(@"ABC\\", @"ABC\\", ';'),
            new TCD(@"\\ABC", @"\\ABC", ';'),

            new TCD(@"\A\B\C", @"\A\B\C", ';'),
            new TCD(@"\A\B\C", @"A\B\C", 'A'),
            new TCD(@"\A\B\C", @"\AB\C", 'B'),
            new TCD(@"\A\B\C", @"\A\BC", 'C'),

            new TCD(@"ABC\;", @"ABC;", ';'),
            new TCD(@"\\ABC\\\;", @"\\ABC\\;", ';'),

            new TCD(@"\\ABC\;", @"\ABC\;", '\\'),
            new TCD(@"\\ABC\\;", @"\ABC\;", '\\'),
            new TCD(@"\\ABC\\\;", @"\ABC\\;", '\\'),
            new TCD(@"\\ABC\\\\;", @"\ABC\\;", '\\'),

            new TCD(@"\\ABC\", @"\ABC\", '\\'),
            new TCD(@"\\ABC\\", @"\ABC\", '\\'),
            new TCD(@"\\ABC\\\", @"\ABC\\", '\\'),
            new TCD(@"\\ABC\\\\", @"\ABC\\", '\\'),
        ];

        [TestCaseSource(nameof(ValidValuesFor1))]
        public void UnescapeCharacter1Tests(string input, string expectedOutput, char character)
        {
            var actual = input.AsSpan().UnescapeCharacter('\\', character)
                .ToString();

            Assert.That(actual, Is.EqualTo(expectedOutput));
        }

        private static IEnumerable<TCD> ValidValuesFor2() => ValidValuesFor1().Select(
            tcd => new TCD(tcd.Arguments[0], tcd.Arguments[1], tcd.Arguments[2], '=')
        ).Concat(
        [
            new TCD(@"", @"", ';', ','),
            new TCD(@";,", @";,", ';', ','),
            new TCD(@"\;\,", @";,", ';', ','),
            new TCD(@"\,\;\,", @",;,", ';', ','),
            new TCD(@"\;\,\;\,", @";,;,", ';', ','),
            new TCD(@"\;\,\;\,\;", @";,;,;", ';', ','),
            new TCD(@"XYZ\ ,", @"XYZ\ ,", ';', ','),
            new TCD(@"ABC\ ;", @"ABC\ ;", ';', ','),
            new TCD(@"ABC\\;", @"ABC\\;", ';', ','),
            new TCD(@"ABC\\,", @"ABC\\,", ';', ','),
            new TCD(@"ABC\\\;", @"ABC\\;", ';', ','),
            new TCD(@"ABC\\\,", @"ABC\\,", ';', ','),

            new TCD(@"ABC\\,;DEF", @"ABC\\,;DEF", ';', ','),
            new TCD(@"ABC\\\,;DEF", @"ABC\\,;DEF", ';', ','),

            new TCD(@"ABC\\,;DE\,F", @"ABC\\,;DE,F", ';', ','),
            new TCD(@"ABC\\\,;DE\,F", @"ABC\\,;DE,F", ';', ','),
            new TCD(@"ABC\\\;DE\F\,;", @"ABC\\;DE\F,;", ';', ','),
            new TCD(@"ABC\\\;DE\F,\,", @"ABC\\;DE\F,,", '\\', ','),

            new TCD(@"\ABC\", @"\ABC\", ';', ','),
            new TCD(@"ABC\", @"ABC\", ';', ','),
            new TCD(@"\ABC", @"\ABC", ';', ','),

            new TCD(@";,ABC,;", @";,ABC,;", ';', ','),
            new TCD(@"ABC,;", @"ABC,;", ';', ','),
            new TCD(@",;ABC", @",;ABC", ';', ','),

            new TCD(@",;ABC;,", @",;ABC;,", ';', ','),
            new TCD(@"ABC;,", @"ABC;,", ';', ','),
            new TCD(@";,ABC", @";,ABC", ';', ','),


            new TCD(@"\;\,ABC\,\;", @";,ABC,;", ';', ','),
            new TCD(@"ABC\,\;", @"ABC,;", ';', ','),
            new TCD(@"\,\;ABC", @",;ABC", ';', ','),

            new TCD(@"\,\;ABC\;\,", @",;ABC;,", ';', ','),
            new TCD(@"ABC\;\,", @"ABC;,", ';', ','),
            new TCD(@"\;\,ABC", @";,ABC", ';', ','),

            new TCD(@"\\ABC\\", @"\\ABC\\", ';', ','),
            new TCD(@"ABC\\", @"ABC\\", ';', ','),
            new TCD(@"\\ABC", @"\\ABC", ';', ','),

            new TCD(@"\\ABC\\", @"\ABC\", '\\', ','),
            new TCD(@"ABC\\", @"ABC\", '\\', ','),
            new TCD(@"\\ABC", @"\ABC", '\\', ','),

            new TCD(@"\A\B\C", @"\A\B\C", ';', ','),
            new TCD(@"\A\B\C", @"AB\C", 'A', 'B'),
            new TCD(@"\A\B\C", @"\ABC", 'C', 'B'),
            new TCD(@"\A\B\C", @"A\BC", 'A', 'C'),


            new TCD(@"\,ABC\;", @",ABC;", ';', ','),
            new TCD(@"\\,ABC\\;", @"\\,ABC\\;", ';', ','),
            new TCD(@"\\\,ABC\\\;", @"\\,ABC\\;", ';', ','),
            new TCD(@"\\\,ABC\\\;", @"\,ABC\\;", '\\', ','),
            new TCD(@"\\\,ABC\\\;", @"\\,ABC\;", '\\', ';'),

            new TCD(@"\\ABC\\\;", @"\ABC\;", '\\', ';'),
        ]);


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

            Assert.Multiple(() =>
            {
                Assert.That(actual1, Is.EqualTo(expectedOutput));
                Assert.That(actual2, Is.EqualTo(expectedOutput));
            });
        }
    }
}
