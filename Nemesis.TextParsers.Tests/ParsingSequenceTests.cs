using Nemesis.TextParsers.Tests.Collections;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests;

[TestFixture]
internal class ParsingSequenceTests
{
    private static IReadOnlyList<T> ParseCollection<T>(string text)
    {
        if (text == null) return null;

        var tokens = text.AsSpan().Tokenize('|', '\\', false);
        var parsed = new ParsingSequence(tokens, '\\', '∅', '|');

        var result = new List<T>();
        var elementTransformer = Sut.GetTransformer<T>();
        foreach (var part in parsed)
            result.Add(part.ParseWith(elementTransformer));

        return result;
    }

    private static IEnumerable<(string, IEnumerable<string>)> ValidListData() =>
    [
        ("", [""]),
        ("AAA|BBB|CCC", ["AAA", "BBB", "CCC"]),
        ("|BBB||CCC", ["", "BBB", "", "CCC"]),
        (@"|BBB|\|CCC", ["", "BBB", "|CCC"]),
        (@"|B\\BB|\|CCC", ["", @"B\BB", "|CCC"]),
        ("|BBB|", ["", "BBB", ""]),
        (@"|BBB\|", ["", "BBB|"]),
        (@"\|BBB|", ["|BBB", ""]),
        (@"|∅||∅", ["", null, "", null]),
        (@"∅", [null]),
        (@"B|∅|A|∅", ["B", null, "A", null]),
        ("|||", ["", "", "", ""]),
        (@"|\||", ["", "|", ""]),
        (@"|\\\\\||", ["", @"\\|", ""]),
        (@"|\\\|\\\||", ["", @"\|\|", ""]),
        (@"\\\|\\\||", [@"\|\|", ""]),
        (@"\\|ABC|\\", [@"\", "ABC", @"\"]),
        (@"\\|ABC|\|", [@"\", "ABC", "|"]),
        (@"\||ABC|\\", ["|", "ABC", @"\"]),

        (@"\\\\|ABC|\\\\", [@"\\", "ABC", @"\\"]),
        (@"\\\\|ABC|\|", [@"\\", "ABC", "|"]),
        (@"\||ABC|\\\\", ["|", "ABC", @"\\"]),

        (@"\\1\\|ABC|\\2\\", [@"\1\", "ABC", @"\2\"]),
        (@"\\3\\|ABC|\|", [@"\3\", "ABC", "|"]),
        (@"\||ABC|\\4\\", ["|", "ABC", @"\4\"]),

        (@"\|", ["|"]),
        ("|", ["", ""]),
        (" |", [" ", ""]),
        (@"\\", [@"\"]),
        (@"\∅", [@"∅"]),
        (@"∅", [null]),
        (@" ∅", [" ∅"]),
        (@" ∅ ", [" ∅ "]),
        (@" \∅ ", [" ∅ "]),
        (@"∅ ", [@"∅ "]),
        (@"\∅ ", [@"∅ "]),
        (@"A|∅|B", ["A", null, "B"]),
        (@"A| ∅ |B", ["A", " ∅ ", "B"]),
        (@"∅|B", [null, "B"]),
        (@"∅ |B", ["∅ ", "B"]),
        (@"\∅ |B", ["∅ ", "B"]),
        (@"A| ∅ |B", ["A", " ∅ ", "B"]),
        (@"A| \∅ |B", ["A", " ∅ ", "B"]),

        (@" |\\", [" ", @"\"]),
        (@" |\\\|", [" ", @"\|"]),
        (@" |\\\||A", [" ", @"\|", "A"]),
        (@" |\|", [" ", "|"]),
    ];

    [TestCaseSource(nameof(ValidListData))]
    public void List_Parse_Test((string input, IEnumerable<string> expectedList) data)
    {
        IEnumerable<string> result = ParseCollection<string>(data.input);

        if (data.expectedList == null)
            Assert.That(result, Is.Null);
        else
            Assert.That(result, Is.EqualTo(data.expectedList));

        /*if (data.expectedList == null)
            Console.WriteLine(@"NULL list");
        else if (!data.expectedList.Any())
            Console.WriteLine(@"Empty list");
        else
            foreach (string elem in data.expectedList)
                Console.WriteLine($@"'{elem ?? "<null>"}'");*/
    }

    //unfinished escaping sequence
    [TestCase("01", @"\", "Unfinished escaping sequence detected at the end of input")]
    [TestCase("02", @"\\\", "Unfinished escaping sequence detected at the end of input")]
    [TestCase("03", @"\\\\\", "Unfinished escaping sequence detected at the end of input")]
    [TestCase("04", @"\|\|\|\|\", "Unfinished escaping sequence detected at the end of input")]
    [TestCase("05", @"AAA|BBB\", "Unfinished escaping sequence detected at the end of input")]
    //illegal escaping sequence
    [TestCase("06", @"AAA|BBB\n", "Illegal escape sequence found in input: 'n'")]
    [TestCase("07", @"\aAAA|BBB\r", "Illegal escape sequence found in input: 'a'")]
    [TestCase("08", @"AAA|BB\\\B", "Illegal escape sequence found in input: 'B'")]
    [TestCase("09", @"\AAA|BB\\\B", "Illegal escape sequence found in input: 'A'")]
    [TestCase("10", @"\r", "Illegal escape sequence found in input: 'r'")]
    public void List_Parse_NegativeTest(string _, string input, string expectedMessagePart) =>
        Assert.That(() => ParseCollection<string>(input).ToList(),
            Throws.ArgumentException.And.Message.Contains(expectedMessagePart));


    [TestCaseSource(typeof(CollectionTestData), nameof(CollectionTestData.ListCompoundData))]
    public void List_Parse_CompoundTests<TElement, TExpectedEnumerable, TText>(TElement _, TExpectedEnumerable expectedOutput, string input)
        where TExpectedEnumerable : IEnumerable<TElement>
    {
        var deserialized = ParseCollection<TElement>(input);
        Assert.That(deserialized, Is.EqualTo(expectedOutput));
    }
}