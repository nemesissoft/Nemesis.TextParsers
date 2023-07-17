using System.Collections;
using Nemesis.TextParsers.Tests.Collections;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests
{
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

        #region List

        private static IEnumerable<(string, IEnumerable<string>)> ValidListData() => new (string, IEnumerable<string>)[]
        {
            ("", new[]{""}),
            (@"AAA|BBB|CCC", new []{"AAA","BBB","CCC"}),
            (@"|BBB||CCC", new []{"","BBB","","CCC"}),
            (@"|BBB|\|CCC", new []{"","BBB","|CCC"}),
            (@"|B\\BB|\|CCC", new []{"",@"B\BB","|CCC"}),
            (@"|BBB|", new []{"","BBB",""}),
            (@"|BBB\|", new []{"","BBB|"}),
            (@"\|BBB|", new []{"|BBB",""}),
            (@"|∅||∅", new []{"",null,"",null}),
            (@"∅", new []{(string)null}),
            (@"B|∅|A|∅", new []{"B",null,"A",null}),
            (@"|||", new []{"","","",""}),
            (@"|\||", new []{"","|",""}),
            (@"|\\\\\||", new []{"",@"\\|",""}),
            (@"|\\\|\\\||", new []{"",@"\|\|",""}),
            (@"\\\|\\\||", new []{@"\|\|",""}),
            (@"\\|ABC|\\", new []{@"\","ABC", @"\"}),
            (@"\\|ABC|\|", new []{@"\","ABC", @"|"}),
            (@"\||ABC|\\", new []{@"|","ABC", @"\"}),

            (@"\\\\|ABC|\\\\", new []{@"\\","ABC", @"\\"}),
            (@"\\\\|ABC|\|", new []{@"\\","ABC", @"|"}),
            (@"\||ABC|\\\\", new []{@"|","ABC", @"\\"}),

            (@"\\1\\|ABC|\\2\\", new []{@"\1\","ABC", @"\2\"}),
            (@"\\3\\|ABC|\|", new []{@"\3\","ABC", @"|"}),
            (@"\||ABC|\\4\\", new []{@"|","ABC", @"\4\"}),

            (@"\|", new []{@"|"}),
            (@"|", new []{@"", ""}),
            (@" |", new []{@" ", ""}),
            (@"\\", new []{@"\"}),
            (@"\∅", new []{@"∅"}),
            (@"∅", new string[]{null}),
            (@" ∅", new[]{" ∅"}),
            (@" ∅ ", new[]{" ∅ "}),
            (@" \∅ ", new[]{" ∅ "}),
            (@"∅ ", new[]{@"∅ "}),
            (@"\∅ ", new[]{@"∅ "}),
            (@"A|∅|B", new[]{"A",null,"B"}),
            (@"A| ∅ |B", new[]{"A", " ∅ ", "B"}),
            (@"∅|B", new[]{null,"B"}),
            (@"∅ |B", new[]{ "∅ ", "B"}),
            (@"\∅ |B", new[]{ "∅ ", "B"}),
            (@"A| ∅ |B", new[]{"A", " ∅ ", "B"}),
            (@"A| \∅ |B", new[]{"A", " ∅ ", "B"}),

            (@" |\\", new []{@" ", @"\"}),
            (@" |\\\|", new []{@" ", @"\|"}),
            (@" |\\\||A", new []{@" ", @"\|", "A"}),
            (@" |\|", new []{@" ", @"|"}),
        };

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
        public void List_Parse_NegativeTest(string _, string input, string expectedMessagePart)
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var ex = Assert.Throws<ArgumentException>(() => ParseCollection<string>(input).ToList());
            Assert.That(ex.Message, Does.Contain(expectedMessagePart));
        }

        [TestCaseSource(typeof(CollectionTestData), nameof(CollectionTestData.ListCompoundData))]
        public void List_Parse_CompoundTests((Type elementType, IEnumerable expectedList, string input) data)
        {
            static void CheckEquivalency(IEnumerable left, IEnumerable right)
            {
                if (left is null) Assert.That(right, Is.Null);
                else Assert.That(left, Is.EqualTo(right));
            }

            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            var parseMethod = (typeof(ParsingSequenceTests).GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                    mi.Name == nameof(ParseCollection) && mi.IsGenericMethod)
                    ?? throw new MissingMethodException("Method ParseCollection does not exist"))
                .MakeGenericMethod(data.elementType);

            var deser = (IEnumerable)parseMethod.Invoke(null, new object[] { data.input });
            CheckEquivalency(deser, data.expectedList);

            /*if (data.expectedList == null)
                Console.WriteLine(@"NULL list");
            else if (!data.expectedList.Cast<object>().Any())
                Console.WriteLine(@"Empty list");
            else
                foreach (object elem in data.expectedList)
                    Console.WriteLine(FormattableString.Invariant($"{elem}"));*/
        }

        #endregion
    }
}
