using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Tests.Utils;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;
using TCD = NUnit.Framework.TestCaseData;

namespace Nemesis.TextParsers.Tests.Collections
{
    [TestFixture]
    public class CollectionsTests
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        private static readonly ITransformerStore _store = Sut.DefaultStore;

        const string NULL_PLACEHOLDER = "维基百科";
        private static string NormalizeNullMarkers(string text) =>
            text.Replace(@"\∅", NULL_PLACEHOLDER).Replace(@"∅", NULL_PLACEHOLDER).Replace(NULL_PLACEHOLDER, @"\∅");

        private static IEnumerable<(string text, string[] collection)> ValidListData() => new[]
        {
            (null, null),
            ("", Array.Empty<string>()),
            //("", new []{""}), //not supported. Rare case 
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
            (@" A | \∅ | B ", new[]{" A ", " ∅ ", " B "}),



            (@"\|AAA\||\|BBB\||\|CCC\|", new[] {"|AAA|", "|BBB|", "|CCC|"}),
            (@"\\DDD\\|\\EEE\\|\\FFF\\", new[] {@"\DDD\", @"\EEE\", @"\FFF\"}),
            (@"\\GGG\||\|HHH\\|\|III\||\\JJJ\\", new[] {@"\GGG|", @"|HHH\", @"|III|", @"\JJJ\"}),
            (@"\|AAA\|| \∅ |\|CCC\|", new[] {"|AAA|", " ∅ ", "|CCC|"}),
            (@"\|AAA\||\∅|\|CCC\|", new[] {"|AAA|", "∅", "|CCC|"}),
            (@"\|AAA\||∅|\|CCC\|", new[] {"|AAA|", null, "|CCC|"}),
            (@"∅|\∅|∅|null| \∅ |\|\\\∅\|", new[] {null, "∅", null, "null", " ∅ ", @"|\∅|"}),
        };

        [TestCaseSource(nameof(ValidListData))]
        public void List_Parse_Test((string input, string[] expectedList) data)
        {
            var result = _store.GetTransformer<string[]>().Parse(data.input);

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
            //Console.WriteLine($@"'{result ?? "<null>"}'");
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
        public void List_Parse_NegativeTest(string _, string input, string expectedMessagePart)
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var ex = Assert.Throws<ArgumentException>(() => _store.GetTransformer<IList<string>>().Parse(input));
            Assert.That(ex.Message, Does.Contain(expectedMessagePart));
        }


        private static IEnumerable<(Type elementType, string input, Type expectedException)> Bad_ListParseData() => new[]
        {
            (typeof(IList<>), @"A|B|C", typeof(InvalidOperationException)),
            // ReSharper disable once StringLiteralTypo
            (typeof(bool), @"falsee", typeof(FormatException)),
            (typeof(bool), @"yes", typeof(FormatException)),
            (typeof(bool), @"no", typeof(FormatException)),
            (typeof(bool), @"0", typeof(FormatException)),


            (typeof(byte), @"abc", typeof(FormatException)),
            (typeof(byte), @"17| ", typeof(FormatException)),
            (typeof(byte), @"17abc", typeof(FormatException)),

            (typeof(sbyte), @"abc", typeof(FormatException)),
            (typeof(sbyte), @"17| ", typeof(FormatException)),
            (typeof(sbyte), @"17abc", typeof(FormatException)),

            (typeof(short), @"abc", typeof(FormatException)),
            (typeof(short), @"17| ", typeof(FormatException)),
            (typeof(short), @"17abc", typeof(FormatException)),

            (typeof(ushort), @"abc", typeof(FormatException)),
            (typeof(ushort), @"17| ", typeof(FormatException)),
            (typeof(ushort), @"17abc", typeof(FormatException)),

            (typeof(int), @"abc", typeof(FormatException)),
            (typeof(int), @"17| ", typeof(FormatException)),
            (typeof(int), @"17abc", typeof(FormatException)),

            (typeof(uint), @"abc", typeof(FormatException)),
            (typeof(uint), @"17| ", typeof(FormatException)),
            (typeof(uint), @"17abc", typeof(FormatException)),

            (typeof(long), @"abc", typeof(FormatException)),
            (typeof(long), @"17| ", typeof(FormatException)),
            (typeof(long), @"17abc", typeof(FormatException)),

            (typeof(ulong), @"abc", typeof(FormatException)),
            (typeof(ulong), @"17| ", typeof(FormatException)),
            (typeof(ulong), @"17abc", typeof(FormatException)),

            (typeof(float), @"abc", typeof(FormatException)),
            (typeof(float), @"17| ", typeof(FormatException)),
            (typeof(float), @"17abc", typeof(FormatException)),


            (typeof(byte), @"-1|0", typeof(OverflowException)),
            (typeof(byte), @"255|256", typeof(OverflowException)),

            (typeof(sbyte), @"-129|-128", typeof(OverflowException)),
            (typeof(sbyte), @"127|128", typeof(OverflowException)),

            (typeof(short), @"-32769|-32768", typeof(OverflowException)),
            (typeof(short), @"32767|32768", typeof(OverflowException)),

            (typeof(ushort), @"-1|0", typeof(OverflowException)),
            (typeof(ushort), @"65535|65536|65537", typeof(OverflowException)),

            (typeof(int), @"-2147483649|-2147483648", typeof(OverflowException)),
            (typeof(int), @"2147483647|2147483648", typeof(OverflowException)),

            (typeof(uint), @"-1|0", typeof(OverflowException)),
            (typeof(uint), @"4294967295|4294967296", typeof(OverflowException)),

            (typeof(long), @"-9223372036854775809|-9223372036854775808", typeof(OverflowException)),
            (typeof(long), @"9223372036854775807|9223372036854775808", typeof(OverflowException)),

            (typeof(ulong), @"-1|0", typeof(OverflowException)),
            (typeof(ulong), @"18446744073709551615|18446744073709551616", typeof(OverflowException)),

#if !NETCOREAPP3_1_OR_GREATER //core 3.1 removed overflow errors for float to be consistent with IEEE
            (typeof(float), @"-340282357000000000000000000000000000000|-340282347000000000000000000000000000000", typeof(OverflowException)),
            (typeof(float), @" 340282347000000000000000000000000000000|340283347000000000000000000000000000000", typeof(OverflowException)),
#endif
        };
        private static IReadOnlyCollection<TElement> ParseCollection<TElement>(string text) =>
            _store.GetTransformer<IReadOnlyCollection<TElement>>().Parse(text);

        [TestCaseSource(nameof(Bad_ListParseData))]
        public void List_Parse_NegativeCompoundTests((Type elementType, string input, Type expectedException) data)
        {
            var parseMethod = (GetType().GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                  mi.Name == nameof(ParseCollection))
                  ?? throw new MissingMethodException("Method ParseList does not exist"))
                .MakeGenericMethod(data.elementType);

            bool passed = false;
            IEnumerable parsed = null;
            try
            {
                parsed = (IEnumerable)parseMethod.Invoke(this, new object[] { data.input });
                passed = true;
            }
            catch (Exception e)
            {
                AssertException(e, data.expectedException, null);
            }
            if (passed)
                Assert.Fail($"'{data.input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, parsed?.Cast<object>().Select(r => $"'{r}'") ?? Array.Empty<string>())}");
        }

        [TestCaseSource(typeof(CollectionTestData), nameof(CollectionTestData.ListCompoundData))]
        public void List_CompoundTests((Type elementType, IEnumerable expectedOutput, string input) data)
        {
            var listCompound = MakeDelegate<Action<IEnumerable, string>>(
                (p1, p2) => List_CompoundTestsHelper<int>(p1, p2), data.elementType
            );

            listCompound(data.expectedOutput, data.input);
        }

        private static void List_CompoundTestsHelper<TElement>(IEnumerable expectedOutput, string input)
        {
            static void CheckEquivalency(IReadOnlyCollection<TElement> left, IReadOnlyCollection<TElement> right)
            {
                if (left is null)
                    Assert.That(right, Is.Null);
                else
                    Assert.That(left, Is.EqualTo(right));
            }

            var sut = _store.GetTransformer<List<TElement>>();

            var expectedList = expectedOutput?.Cast<TElement>().ToList();

            var trans = Sut.GetTransformer<IReadOnlyCollection<TElement>>();

            string textExpected = trans.Format(expectedList);

            var parsed1 = sut.Parse(input);
            CheckEquivalency(parsed1, expectedList);


            string text = trans.Format(parsed1);
            //Console.WriteLine($"EXP:{textExpected}");
            //Console.WriteLine($"INP:{input}");
            //Console.WriteLine($"TEX:{text}");


            var parsed2 = sut.Parse(text);
            CheckEquivalency(parsed2, expectedList);


            var parsed3 = sut.Parse(textExpected);
            CheckEquivalency(parsed3, expectedList);


            CheckEquivalency(parsed1, parsed2);
            CheckEquivalency(parsed1, parsed3);
        }

        [Test]
        public void List_CompoundTests_ComplexFlagEnum() //cannot attach this to ListCompoundData as test name becomes too long
        {
            const string ALL_DAYS_OF_WEEK = @"255|None|Monday|Tuesday|Monday, Tuesday|Wednesday|Monday, Wednesday|Tuesday, Wednesday|Monday, Tuesday, Wednesday|Thursday|Monday, Thursday|Tuesday, Thursday|Monday, Tuesday, Thursday|Wednesday, Thursday|Monday, Wednesday, Thursday|Tuesday, Wednesday, Thursday|Monday, Tuesday, Wednesday, Thursday|Friday|Monday, Friday|Tuesday, Friday|Monday, Tuesday, Friday|Wednesday, Friday|Monday, Wednesday, Friday|Tuesday, Wednesday, Friday|Monday, Tuesday, Wednesday, Friday|Thursday, Friday|Monday, Thursday, Friday|Tuesday, Thursday, Friday|Monday, Tuesday, Thursday, Friday|Wednesday, Thursday, Friday|Monday, Wednesday, Thursday, Friday|Tuesday, Wednesday, Thursday, Friday|Weekdays|Saturday|Monday, Saturday|Tuesday, Saturday|Monday, Tuesday, Saturday|Wednesday, Saturday|Monday, Wednesday, Saturday|Tuesday, Wednesday, Saturday|Monday, Tuesday, Wednesday, Saturday|Thursday, Saturday|Monday, Thursday, Saturday|Tuesday, Thursday, Saturday|Monday, Tuesday, Thursday, Saturday|Wednesday, Thursday, Saturday|Monday, Wednesday, Thursday, Saturday|Tuesday, Wednesday, Thursday, Saturday|Monday, Tuesday, Wednesday, Thursday, Saturday|Friday, Saturday|Monday, Friday, Saturday|Tuesday, Friday, Saturday|Monday, Tuesday, Friday, Saturday|Wednesday, Friday, Saturday|Monday, Wednesday, Friday, Saturday|Tuesday, Wednesday, Friday, Saturday|Monday, Tuesday, Wednesday, Friday, Saturday|Thursday, Friday, Saturday|Monday, Thursday, Friday, Saturday|Tuesday, Thursday, Friday, Saturday|Monday, Tuesday, Thursday, Friday, Saturday|Wednesday, Thursday, Friday, Saturday|Monday, Wednesday, Thursday, Friday, Saturday|Tuesday, Wednesday, Thursday, Friday, Saturday|Weekdays, Saturday|Sunday|Monday, Sunday|Tuesday, Sunday|Monday, Tuesday, Sunday|Wednesday, Sunday|Monday, Wednesday, Sunday|Tuesday, Wednesday, Sunday|Monday, Tuesday, Wednesday, Sunday|Thursday, Sunday|Monday, Thursday, Sunday|Tuesday, Thursday, Sunday|Monday, Tuesday, Thursday, Sunday|Wednesday, Thursday, Sunday|Monday, Wednesday, Thursday, Sunday|Tuesday, Wednesday, Thursday, Sunday|Monday, Tuesday, Wednesday, Thursday, Sunday|Friday, Sunday|Monday, Friday, Sunday|Tuesday, Friday, Sunday|Monday, Tuesday, Friday, Sunday|Wednesday, Friday, Sunday|Monday, Wednesday, Friday, Sunday|Tuesday, Wednesday, Friday, Sunday|Monday, Tuesday, Wednesday, Friday, Sunday|Thursday, Friday, Sunday|Monday, Thursday, Friday, Sunday|Tuesday, Thursday, Friday, Sunday|Monday, Tuesday, Thursday, Friday, Sunday|Wednesday, Thursday, Friday, Sunday|Monday, Wednesday, Thursday, Friday, Sunday|Tuesday, Wednesday, Thursday, Friday, Sunday|Weekdays, Sunday|Weekends|Monday, Weekends|Tuesday, Weekends|Monday, Tuesday, Weekends|Wednesday, Weekends|Monday, Wednesday, Weekends|Tuesday, Wednesday, Weekends|Monday, Tuesday, Wednesday, Weekends|Thursday, Weekends|Monday, Thursday, Weekends|Tuesday, Thursday, Weekends|Monday, Tuesday, Thursday, Weekends|Wednesday, Thursday, Weekends|Monday, Wednesday, Thursday, Weekends|Tuesday, Wednesday, Thursday, Weekends|Monday, Tuesday, Wednesday, Thursday, Weekends|Friday, Weekends|Monday, Friday, Weekends|Tuesday, Friday, Weekends|Monday, Tuesday, Friday, Weekends|Wednesday, Friday, Weekends|Monday, Wednesday, Friday, Weekends|Tuesday, Wednesday, Friday, Weekends|Monday, Tuesday, Wednesday, Friday, Weekends|Thursday, Friday, Weekends|Monday, Thursday, Friday, Weekends|Tuesday, Thursday, Friday, Weekends|Monday, Tuesday, Thursday, Friday, Weekends|Wednesday, Thursday, Friday, Weekends|Monday, Wednesday, Thursday, Friday, Weekends|Tuesday, Wednesday, Thursday, Friday, Weekends|All|128";

            List_CompoundTests((typeof(DaysOfWeek), Enumerable.Range(-1, 130).Select(i => (DaysOfWeek)i).ToList(), ALL_DAYS_OF_WEEK));
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

            var parsed2 = arrayTrans.Parse(@"300|||400");
            Assert.That(parsed2, Is.EqualTo(new int?[] { 300, null, null, 400 }));
        }


        private static IEnumerable<TCD> InnerCollectionsData() => new[]
        {
            new TCD("01", new List<string>{null}, @"[∅]"),//one null element
            new TCD("02", new List<string>(), @""),//empty list
            new TCD("03", new List<string>{""}, @"[]"),//one empty element

            new TCD("04", new List<string[]>
            {
                new[] {"A", "B", "C"},
                new[] {"D", "E", "F"},
            }, @"[[A\|B\|C]|[D\|E\|F]]"),
            new TCD("05", new List<string[]>
            {
                Array.Empty<string>(),
                Array.Empty<string>()
            }, @"[|]"),
            new TCD("06", new List<string[]>(), @""),


            new TCD("07", new[]
            {
                new List<string> {"A", "B", "C"},
                new List<string> {"D", "E", "F"},
            }, @"[[A\|B\|C]|[D\|E\|F]]"),
            new TCD("08", new[]
            {
                new List<string>(),
                new List<string>(),
            }, @"[|]"),
            new TCD("09", new[]
            {
                new List<string>(),
                new List<string>{"1","2","3"},
                new List<string>(),
            }, @"[|[1\|2\|3]|]"),
            new TCD("10", Array.Empty<List<string>>(), @""),

            new TCD("11", new List<string[]>{null, Array.Empty<string>(), new[] {""} }, @"[∅||[]]"),//null # empty # one empty element
        };

        [TestCaseSource(nameof(InnerCollectionsData))]
        public void Bordered_ShouldProperlyHandleBoundingMarkers(string _, object instance, string text)
        {
            var sut = GetBorderedSut();
            ParseAndFormatObject(instance, text, sut);
        }


        [TestCaseSource(typeof(CollectionTestData), nameof(CollectionTestData.ListCompoundData))]
        public void Bordered_Compound((Type _, IEnumerable expectedOutput, string text) data)
        {
            var borderedStore = GetBorderedSut();
            object instance = data.expectedOutput;
            RoundTrip(instance, borderedStore);


            var defaultTrans = _store.GetTransformer(instance.GetType());
            var borderedTrans = borderedStore.GetTransformer(instance.GetType());


            var parsedDefault = defaultTrans.ParseObject(data.text);

            var borderedText = borderedTrans.FormatObject(instance);
            var parsedBordered = borderedTrans.ParseObject(borderedText);


            IsMutuallyEquivalent(parsedDefault, instance);
            IsMutuallyEquivalent(parsedBordered, instance);
            IsMutuallyEquivalent(parsedDefault, parsedBordered);
        }

        private static ITransformerStore GetBorderedSut()
        {
            var borderedCollection = CollectionSettings.Default
                    .With(s => s.Start, '[')
                    .With(s => s.End, ']')
                ;
            var borderedArray = ArraySettings.Default
                    .With(s => s.Start, '[')
                    .With(s => s.End, ']')
                ;

            var borderedStore = SettingsStoreBuilder.GetDefault()
                .AddOrUpdate(borderedArray)
                .AddOrUpdate(borderedCollection)
                .Build();

            return TextTransformer.GetDefaultStoreWith(borderedStore);
        }
    }
}
