using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Tests.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;

namespace Nemesis.TextParsers.Tests.Collections
{
    file class FailTest<T>(string argument2, Type argument3) : TestCaseData<T, string, Type>(default, argument2, argument3);

    [TestFixture]
    public class CollectionsTests
    {
        private static readonly ITransformerStore _store = Sut.DefaultStore;

        private const string NULL_PLACEHOLDER = "维基百科";

        private static string NormalizeNullMarkers(string text) =>
            text.Replace(@"\∅", NULL_PLACEHOLDER).Replace(@"∅", NULL_PLACEHOLDER).Replace(NULL_PLACEHOLDER, @"\∅");

        private static (string text, string[] collection)[] ValidListData() =>
        [
            (null, null),
            ("", []),
            //("", new []{""}), //not supported. Rare case 
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
            (@" A | \∅ | B ", [" A ", " ∅ ", " B "]),


            (@"\|AAA\||\|BBB\||\|CCC\|", ["|AAA|", "|BBB|", "|CCC|"]),
            (@"\\DDD\\|\\EEE\\|\\FFF\\", [@"\DDD\", @"\EEE\", @"\FFF\"]),
            (@"\\GGG\||\|HHH\\|\|III\||\\JJJ\\", [@"\GGG|", @"|HHH\", "|III|", @"\JJJ\"]),
            (@"\|AAA\|| \∅ |\|CCC\|", ["|AAA|", " ∅ ", "|CCC|"]),
            (@"\|AAA\||\∅|\|CCC\|", ["|AAA|", "∅", "|CCC|"]),
            (@"\|AAA\||∅|\|CCC\|", ["|AAA|", null, "|CCC|"]),
            (@"∅|\∅|∅|null| \∅ |\|\\\∅\|", [null, "∅", null, "null", " ∅ ", @"|\∅|"]),
        ];

        [TestCaseSource(nameof(ValidListData))]
        public void List_Parse_Test((string input, string[] expectedList) data)
        {
            var result = _store.GetTransformer<string[]>().Parse(data.input);

            if (data.expectedList == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EqualTo(data.expectedList));
        }

        [TestCaseSource(nameof(ValidListData))]
        public void List_Format_SymmetryTests((string expectedOutput, string[] inputList) data)
        {
            var trans = _store.GetTransformer<string[]>();

            var result = trans.Format(data.inputList);

            if (data.expectedOutput == null)
                Assert.That(result, Is.Null);
            else
            {
                result = NormalizeNullMarkers(result);
                var expectedOutput = NormalizeNullMarkers(data.expectedOutput);
                Assert.That(result, Is.EqualTo(expectedOutput));
            }
        }

        #region Negative tests

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
        [TestCase("11", @"\xAAA|BBB\n", "Illegal escape sequence found in input: 'x'")]

        #endregion

        public void List_Parse_NegativeTest(string _, string input, string expectedMessagePart) =>
            Assert.That(() => _store.GetTransformer<IList<string>>().Parse(input),
                Throws.ArgumentException.And.Message.Contains(expectedMessagePart));


        private static IEnumerable<TCD> Parse_ShouldFail_Data()
        {
            Type f = typeof(FormatException), o = typeof(OverflowException);

            return ((IEnumerable<TCD>)
            [
                new FailTest<int>("A|B|C", f),

                new FailTest<bool>("falsee", f), new FailTest<bool>("yes", f), new FailTest<bool>("no", f),
                new FailTest<bool>("0", f),
                new FailTest<byte>("abc", f), new FailTest<byte>("17| ", f), new FailTest<byte>("17abc", f),
                new FailTest<sbyte>("abc", f), new FailTest<sbyte>("17| ", f), new FailTest<sbyte>("17abc", f),
                new FailTest<short>("abc", f), new FailTest<short>("17| ", f), new FailTest<short>("17abc", f),
                new FailTest<ushort>("abc", f), new FailTest<ushort>("17| ", f), new FailTest<ushort>("17abc", f),
                new FailTest<int>("abc", f), new FailTest<int>("17| ", f), new FailTest<int>("17abc", f),
                new FailTest<uint>("abc", f), new FailTest<uint>("17| ", f), new FailTest<uint>("17abc", f),
                new FailTest<long>("abc", f), new FailTest<long>("17| ", f), new FailTest<long>("17abc", f),
                new FailTest<ulong>("abc", f), new FailTest<ulong>("17| ", f), new FailTest<ulong>("17abc", f),
                new FailTest<float>("abc", f), new FailTest<float>("17| ", f), new FailTest<float>("17abc", f),

                new FailTest<byte>("-1|0", o), new FailTest<byte>("255|256", o),
                new FailTest<sbyte>("-129|-128", o), new FailTest<sbyte>("127|128", o),
                new FailTest<short>("-32769|-32768", o), new FailTest<short>("32767|32768", o),
                new FailTest<ushort>("-1|0", o), new FailTest<ushort>("65535|65536|65537", o),
                new FailTest<int>("-2147483649|-2147483648", o), new FailTest<int>("2147483647|2147483648", o),
                new FailTest<uint>("-1|0", o), new FailTest<uint>("4294967295|4294967296", o),
                new FailTest<long>("-9223372036854775809|-9223372036854775808", o), new FailTest<long>("9223372036854775807|9223372036854775808", o),
                new FailTest<ulong>("-1|0", o), new FailTest<ulong>("18446744073709551615|18446744073709551616", o),
#if !NETCOREAPP3_1_OR_GREATER //core 3.1 removed overflow errors for float to be consistent with IEEE
                new FailTest<float>("-340282357000000000000000000000000000000|-340282347000000000000000000000000000000", o),
                new FailTest<float>(" 340282347000000000000000000000000000000|340283347000000000000000000000000000000", o),
#endif
            ]).Select((t, i) => t.SetName($"{i + 1:00}_{nameof(Parse_ShouldFail)}_{t.TypeArgs?[0].Name}"));
        }

        [TestCaseSource(nameof(Parse_ShouldFail_Data))]
        public void Parse_ShouldFail<TElement, TInput, TException>(TElement _, string input, Type expectedException)
        {
            var sut = _store.GetTransformer<IReadOnlyCollection<TElement>>();
            Assert.Throws(expectedException, () => sut.Parse(input));
        }

        [TestCaseSource(typeof(CollectionTestData), nameof(CollectionTestData.ListCompoundData))]
        public void List_CompoundTests<TElement, TExpectedEnumerable, TText>(TElement _, TExpectedEnumerable expectedOutput, string input)
            where TExpectedEnumerable : IEnumerable<TElement>
        {
            var sut = _store.GetTransformer<List<TElement>>();

            var expectedList = expectedOutput?.ToList();
            string textExpected = sut.Format(expectedList);


            var parsed1 = sut.Parse(input);
            CheckEquivalency(parsed1, expectedList);


            string text = sut.Format(parsed1);

            var parsed2 = sut.Parse(text);
            CheckEquivalency(parsed2, expectedList);


            var parsed3 = sut.Parse(textExpected);
            CheckEquivalency(parsed3, expectedList);


            CheckEquivalency(parsed1, parsed2);
            CheckEquivalency(parsed1, parsed3);


            var borderedStore = TextTransformer.GetDefaultStoreWith(SettingsStoreBuilder.GetDefault()
                .AddOrUpdateRange(
                    CollectionSettings.Default with { Start = '{', End = '}' },
                    ArraySettings.Default with { Start = '[', End = ']' },
                    DictionarySettings.Default with { Start = '<', End = '>' }
                )
                .Build());
            var borderedSut = borderedStore.GetTransformer<List<TElement>>();
            var borderedText = borderedSut.Format(expectedList);

            var parsedBordered = borderedSut.Parse(borderedText);
            CheckEquivalency(parsedBordered, expectedList);

            return;

            static void CheckEquivalency<T>(T left, T right) => Assert.That(left, Is.EqualTo(right));
        }

        [Test]
        public void Complex_List_Roundtrip_Test()
        {
            var arrayTrans = _store.GetTransformer<int?[]>();

            var array = new int?[] { 30, null, null, 40 };
            // (@"B|∅|A|∅", new []{"B",null,"A",null}),
            var text = arrayTrans.Format(array);

            Assert.That(text, Is.EqualTo("30|∅|∅|40"));

            var parsed = arrayTrans.Parse(text);
            Assert.That(parsed, Is.EqualTo(array));

            var parsed2 = arrayTrans.Parse("300|||400");
            Assert.That(parsed2, Is.EqualTo(new int?[] { 300, null, null, 400 }));
        }


        private static TCD[] InnerCollectionsData() =>
        [
            new("01", new List<string> { null }, @"[∅]"), //one null element
            new("02", new List<string>(), ""), //empty list
            new("03", new List<string> { "" }, "[]"), //one empty element

            new("04", (List<string[]>)[["A", "B", "C"], ["D", "E", "F"]],
                @"[[A\|B\|C]|[D\|E\|F]]"),
            new("05", (List<string[]>)[[], []], "[|]"),
            new("06", new List<string[]>(), ""),


            new("07", (List<string>[])[["A", "B", "C"], ["D", "E", "F"]],
                @"[[A\|B\|C]|[D\|E\|F]]"),
            new("08", (List<string>[])[[], []], "[|]"),
            new("09", new List<string>[]
            {
                [],
                ["1", "2", "3"],
                [],
            }, @"[|[1\|2\|3]|]"),
            new("10", Array.Empty<List<string>>(), ""),

            new("11", (List<string[]>)[null, [], [""]], @"[∅||[]]"), //null # empty # one empty element
        ];

        [TestCaseSource(nameof(InnerCollectionsData))]
        public void Bordered_ShouldProperlyHandleBoundingMarkers(string _, object instance, string text)
        {
            var sut = GetBorderedSut();
            ParseAndFormatObject(instance, text, sut);
        }

        private static ITransformerStore GetBorderedSut()
        {
            var borderedCollection = CollectionSettings.Default with { Start = '[', End = ']' };
            var borderedArray = ArraySettings.Default with { Start = '[', End = ']' };

            var borderedStoreBuilder = SettingsStoreBuilder.GetDefault()
                .AddOrUpdateRange([borderedArray, borderedCollection]);

            return TextTransformer.GetDefaultStoreWith(borderedStoreBuilder.Build());
        }
    }
}