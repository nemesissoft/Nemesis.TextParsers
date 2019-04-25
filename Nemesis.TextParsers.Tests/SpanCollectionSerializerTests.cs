using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Dss = System.Collections.Generic.SortedDictionary<string, string>;

namespace Nemesis.TextParsers.Tests
{
    //TODO negative tests for generic type definition
    [TestFixture]
    public class SpanCollectionSerializerTests
    {
        private readonly SpanCollectionSerializer _sut = SpanCollectionSerializer.DefaultInstance;

        private static string NormalizeNullMarkers(string text)
        {
            const string NULL = "维基百科";
            return text.Replace(@"\∅", NULL).Replace(@"∅", NULL).Replace(NULL, @"\∅");
        }

        #region List
        internal static IEnumerable<(string, string[])> ValidListData() => new[]
        {
            (null, null),
            ("", new string[0]),
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
            var result = _sut.ParseCollection<string>(data.input);

            if (data.expectedList == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EquivalentTo(data.expectedList));


            if (data.expectedList == null)
                Console.WriteLine(@"NULL list");
            else if (!data.expectedList.Any())
                Console.WriteLine(@"Empty list");
            else
                foreach (string elem in data.expectedList)
                    Console.WriteLine($@"'{elem ?? "<null>"}'");
        }

        [TestCaseSource(nameof(ValidListData))]
        public void List_Format_SymmetryTests((string expectedOutput, string[] inputList) data)
        {
            var result = _sut.FormatCollection(data.inputList);

            if (data.expectedOutput == null)
                Assert.That(result, Is.Null);
            else
            {
                result = NormalizeNullMarkers(result);
                var expectedOutput = NormalizeNullMarkers(data.expectedOutput);
                Assert.That(result, Is.EqualTo(expectedOutput));
            }

            Console.WriteLine($@"'{result ?? "<null>"}'");

        }

        #region Negative tests
        [TestCase(@"AAA|BBB\")]//not finished escape sequence
        [TestCase(@"AAA|BBB\n")]//illegal escape sequence
        [TestCase(@"\aAAA|BBB\n")]//illegal escape sequence
        [TestCase(@"AAA|BB\\\B")]//illegal escape sequence
        [TestCase(@"\AAA|BB\\\B")]//illegal escape sequence
        [TestCase(@"\")]//not finished escape sequence
        #endregion
        public void List_Parse_NegativeTest(string input)
        {
            try
            {
                var result = _sut.ParseCollection<string>(input);
                //Console.WriteLine(string.Join(Environment.NewLine, result));
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(r => $"'{r}'"))}");
            }
            catch (ArgumentException ae) when (ae.TargetSite?.Name == nameof(_sut.ParseCollection) || ae.TargetSite?.Name == nameof(_sut.ParseStream))
            {
                Console.WriteLine($@"Expected exception from implementation: {ae.Message}");
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine($@"Expected external exception: {ae.Message}");
            }
            catch (Exception e)
            {
                Assert.Fail($@"Unexpected external exception: {e.Message}");
            }
        }


        //TODO add various list, dict, array etc
        internal static IEnumerable<(Type, IEnumerable, string)> ListCompoundData() => new (Type, IEnumerable, string)[]
        {
            (typeof(TimeSpan), Enumerable.Range(1, 7).Select(i => new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|2.03:04:05|3.04:05:06|4.05:06:07|5.06:07:08|6.07:08:09|7.08:09:10" ),

            (typeof(TimeSpan),
                Enumerable.Range(1, 7).Select(i => i % 2 == 0 ? TimeSpan.Zero : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|∅|3.04:05:06|∅|5.06:07:08|∅|7.08:09:10"),

            (typeof(TimeSpan?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (TimeSpan?) null : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|2.03:04:05|∅|4.05:06:07|5.06:07:08|∅|7.08:09:10" ),

            (typeof(decimal[]), Enumerable.Range(1, 7).Select(
                    i => i % 3 == 0 ? null : new decimal[] {10 * i, 10 * i + 1}).ToList(),
            @"10\|11|20\|21|∅|40\|41|50\|51|∅|70\|71" ),

            (typeof(float[]), Enumerable.Range(1, 5).Select(
                    i => new float[] {10 * i, 10 * i + 1, 10 * i + 2}).ToList(),
            @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

            (typeof(float[][]), Enumerable.Range(1, 1).Select(
                    i => new[] {new float[]{ 10 * i, 10 * i + 1 }, new float[] { 100 * i, 100 * i + 1 } }
            ).ToList(),
            @"10\\\|11\|100\\\|101" ),

            (typeof(float[][]), Enumerable.Range(1, 3).Select(
                    i => new[] {new float[]{ 10 * i, 10 * i + 1 }, new float[] { 100 * i, 100 * i + 1 } }
            ).ToList(),
            @"10\\\|11\|100\\\|101|20\\\|21\|200\\\|201|30\\\|31\|300\\\|301" ),

            (typeof(List<float>), Enumerable.Range(1, 5).Select(
                i => new List<float> {10 * i, 10 * i + 1, 10 * i + 2}).ToList(),
            @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

            (typeof(IList<float>), Enumerable.Range(1, 5).Select(
                i => (IList<float>)new List<float> {10 * i, 10 * i + 1, 10 * i + 2}).ToList(),
            @"10\|11\|12|20\|21\|22|30\|31\|32|40\|41\|42|50\|51\|52" ),

            (typeof(bool), Enumerable.Range(1, 8).Select(i => i % 2 == 0).ToList(),
                @"False|True|False|True|False|True|False|True"),

            (typeof(bool), Enumerable.Range(1, 10).Select(i => i % 2 == 0).ToList(),
                @"∅|True|False|True|False|True|False|True|False|True"),

            (typeof(bool?), Enumerable.Range(1, 10).Select(i => i % 3 == 0 ? (bool?)null : (i % 2 == 0)).ToList(),
                @"False|True|∅|True|False|∅|False|True|∅|True"),

            (typeof(Point), Enumerable.Range(0, 6).Select(i => new Point(i * 10, i * 20)).ToList(),
                @"∅|10;20|20;40|30;60|40;80|50;100"),

            (typeof(Point?),
                Enumerable.Range(1, 6).Select(i => i % 2 == 0 ? (Point?) null : new Point(i * 10, i * 20)).ToList(),
                @"10;20|∅|30;60|∅|50;100|∅"),

            (typeof(Color), Enumerable.Range(1, 5).Select(i => (Color) i).ToList(),
                @"Red|Blue|Green|4|5"),

            (typeof(Colors), Enumerable.Range(0, 9).Select(i => (Colors) i).ToList(),
                @"None|Red|Blue|RedAndBlue|Green|Red,Green|Blue,  Green|RedAndBlue,Green|8"),

            (typeof(Color?), Enumerable.Range(1, 8).Select(i => i % 2 == 0 ? (Color?) null : (Color) i).ToList(),
                @" Red |∅|Green|∅|5||7|∅"),

            (typeof(Rect),
                Enumerable.Range(0, 4).Select(i => new Rect(i * 10 + 1, i * 10 + 2, i * 10 + 3, i * 10 + 4)).ToList(),
                @"1;2;3;4|11;12;13;14|21;22;23;24|31;32;33;34"),

            (typeof(ThreeLetters),
                Enumerable.Range(-2, 6).Select(i => i < 0
                    ? default
                    : new ThreeLetters((char) (65 + i + 0), (char) (65 + i + 1), (char) (65 + i + 2))).ToList(),
                @"|∅|ABC|BCD|CDE|DEF"), //∅ == "" == "\0\0\0"

            (typeof(ThreeLetters?),
                Enumerable.Range(0, 7).Select(i =>
                    i>0 && i % 3 == 0
                        ? (ThreeLetters?) null
                        : new ThreeLetters((char) (65 + i + 0), (char) (65 + i + 1), (char) (65 + i + 2))).ToList(),
                @"ABC|BCD|CDE|∅|EFG|FGH|∅"),

            (typeof(ThreeElements<float>),
                Enumerable.Range(0, 3).Select(i => new ThreeElements<float>(i+0.5f,i+1.5f,i+2.5f)).ToList(),
                @"0.5,1.5,2.5|1.5,2.5,3.5|2.5,3.5,4.5"),

            (typeof(PairWithFactory<float>),
                Enumerable.Range(0, 3).Select(i => new PairWithFactory<float>(i+0.5f,i+1.5f)).ToList(),
                @"0.5,1.5|1.5,2.5|2.5,3.5"),

            (typeof(SortedDictionary<int, float>),
                Enumerable.Range(1, 3).Select(i =>
                    new SortedDictionary<int, float>()
                    {
                        [i*10+0]=i*10 + 0.5f,
                        [i*20+0]=i*20 + 0.5f,
                    }
                ).ToList(),
                @"10=10.5;20=20.5|20=20.5;40=40.5|30=30.5;60=60.5"),

            (typeof(Option),
                Enumerable.Range(0, 5).Select(i => new Option((OptionEnum)i)).ToList(),
                @" None|Option1 | Option2 |Option3  | 4"),

            (typeof(Option),
                Enumerable.Range(0, 10).Select(i => new Option((OptionEnum)(i *9 % 10))).ToList(),
                @" None |  9 |  8 |  7 |  6 |  5 |  4 |  Option3 |  Option2 |  Option1"),

            (typeof(IAggressionBased<int>), Enumerable.Range(1, 5).Select(i => AggressionBasedFactory<int>.FromPassiveNormalAggressive(i, i * 10 + 1, i * 20)).ToList(),
                @"1#11#20|2#21#40|3#31#60|4#41#80|5#51#100" ),

            (typeof(IAggressionBased<List<float>>), Enumerable.Range(1, 5).Select(
                    i => i == 2 ? null
                        : AggressionBasedFactory<List<float>>.FromPassiveNormalAggressive(
                            new List<float> { 10 * i, 10 * i + 1, 10 * i + 2, 10 * i + 3 },
                            null,
                            new List<float> { 100 * i, 100 * i + 1, 100 * i + 2 })).ToList(),
                @"10\|11\|12\|13#\∅#100\|101\|102|∅|30\|31\|32\|33#\∅#300\|301\|302|40\|41\|42\|43#\∅#400\|401\|402|50\|51\|52\|53#\∅#500\|501\|502" ),

            (typeof(SortedDictionary<char, IAggressionBased<float[]>>),
                Enumerable.Range(1, 3).Select(i =>
                    new SortedDictionary<char, IAggressionBased<float[]>>()
                    {
                        [(char)(65+(i-1)*3+0)]=AggressionBasedFactory<float[]>.FromPassiveNormalAggressive(
                            new[]{i*10 + 0.5f,i*10 + 1.5f},
                            new[]{i*10 + 2.5f},
                            new[]{i*10 + 3.5f}),
                        [(char)(65+(i-1)*3+1)]=AggressionBasedFactory<float[]>.FromPassiveNormalAggressive(
                            new[]{i*100 + 0.5f,i*100 + 1.5f},
                            new[]{i*100 + 2.5f},
                            new[]{i*100 + 3.5f}),
                        
                    }
                ).ToList(),
                @"A=10.5\|11.5#12.5#13.5;B=100.5\|101.5#102.5#103.5|D=20.5\|21.5#22.5#23.5;E=200.5\|201.5#202.5#203.5|G=30.5\|31.5#32.5#33.5;H=300.5\|301.5#302.5#303.5"),
        };

        [TestCaseSource(nameof(ListCompoundData))]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void List_CompoundTests((Type elementType, IEnumerable expectedOutput, string input) data)
        {
            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            var castMethod = (typeof(Enumerable).GetMethod(nameof(Enumerable.Cast), ALL_FLAGS)
                    ?? throw new MissingMethodException("Method Enumerable.Cast does not exist"))
                .MakeGenericMethod(data.elementType);

            var formatMethod = (typeof(SpanCollectionSerializer).GetMethods(ALL_FLAGS)
                                    .SingleOrDefault(mi => mi.Name == nameof(SpanCollectionSerializer.FormatCollection) && mi.IsGenericMethod)
                    ?? throw new MissingMethodException("Method FormatCollection does not exist"))
                .MakeGenericMethod(data.elementType);

            var parseMethod = (typeof(SpanCollectionSerializerTests).GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                  mi.Name == nameof(ParseCollection))
                  ?? throw new MissingMethodException("Method ParseList does not exist"))
                .MakeGenericMethod(data.elementType);

            var genericListExpected = castMethod.Invoke(null, new object[] { data.expectedOutput });
            string textExpected = (string)formatMethod.Invoke(_sut, new[] { genericListExpected });


            var parsed = (IEnumerable)parseMethod.Invoke(this, new object[] { data.input });
            Assert.That(parsed, Is.EquivalentTo(data.expectedOutput));

            var genericList = castMethod.Invoke(null, new object[] { parsed });

            string text = (string)formatMethod.Invoke(_sut, new[] { genericList });
            Console.WriteLine(data.input);
            Console.WriteLine(text);

            var parsed2 = (IEnumerable)parseMethod.Invoke(this, new object[] { text });
            Assert.That(parsed2, Is.EquivalentTo(data.expectedOutput));


            var parsed3 = (IEnumerable)parseMethod.Invoke(this, new object[] { textExpected });
            Assert.That(parsed3, Is.EquivalentTo(data.expectedOutput));


            Assert.That(parsed, Is.EquivalentTo(parsed2));
            Assert.That(parsed, Is.EquivalentTo(parsed3));
        }

        private ICollection<TElement> ParseCollection<TElement>(string text) => _sut.ParseCollection<TElement>(text);

        [Test]
        public void List_CompoundTests_ComplexFlagEnum() //cannot attach this to ListCompoundData as test name becomes too long
        {
            const string ALL_DAYS_OF_WEEK = @"255|None|Monday|Tuesday|Monday, Tuesday|Wednesday|Monday, Wednesday|Tuesday, Wednesday|Monday, Tuesday, Wednesday|Thursday|Monday, Thursday|Tuesday, Thursday|Monday, Tuesday, Thursday|Wednesday, Thursday|Monday, Wednesday, Thursday|Tuesday, Wednesday, Thursday|Monday, Tuesday, Wednesday, Thursday|Friday|Monday, Friday|Tuesday, Friday|Monday, Tuesday, Friday|Wednesday, Friday|Monday, Wednesday, Friday|Tuesday, Wednesday, Friday|Monday, Tuesday, Wednesday, Friday|Thursday, Friday|Monday, Thursday, Friday|Tuesday, Thursday, Friday|Monday, Tuesday, Thursday, Friday|Wednesday, Thursday, Friday|Monday, Wednesday, Thursday, Friday|Tuesday, Wednesday, Thursday, Friday|Weekdays|Saturday|Monday, Saturday|Tuesday, Saturday|Monday, Tuesday, Saturday|Wednesday, Saturday|Monday, Wednesday, Saturday|Tuesday, Wednesday, Saturday|Monday, Tuesday, Wednesday, Saturday|Thursday, Saturday|Monday, Thursday, Saturday|Tuesday, Thursday, Saturday|Monday, Tuesday, Thursday, Saturday|Wednesday, Thursday, Saturday|Monday, Wednesday, Thursday, Saturday|Tuesday, Wednesday, Thursday, Saturday|Monday, Tuesday, Wednesday, Thursday, Saturday|Friday, Saturday|Monday, Friday, Saturday|Tuesday, Friday, Saturday|Monday, Tuesday, Friday, Saturday|Wednesday, Friday, Saturday|Monday, Wednesday, Friday, Saturday|Tuesday, Wednesday, Friday, Saturday|Monday, Tuesday, Wednesday, Friday, Saturday|Thursday, Friday, Saturday|Monday, Thursday, Friday, Saturday|Tuesday, Thursday, Friday, Saturday|Monday, Tuesday, Thursday, Friday, Saturday|Wednesday, Thursday, Friday, Saturday|Monday, Wednesday, Thursday, Friday, Saturday|Tuesday, Wednesday, Thursday, Friday, Saturday|Weekdays, Saturday|Sunday|Monday, Sunday|Tuesday, Sunday|Monday, Tuesday, Sunday|Wednesday, Sunday|Monday, Wednesday, Sunday|Tuesday, Wednesday, Sunday|Monday, Tuesday, Wednesday, Sunday|Thursday, Sunday|Monday, Thursday, Sunday|Tuesday, Thursday, Sunday|Monday, Tuesday, Thursday, Sunday|Wednesday, Thursday, Sunday|Monday, Wednesday, Thursday, Sunday|Tuesday, Wednesday, Thursday, Sunday|Monday, Tuesday, Wednesday, Thursday, Sunday|Friday, Sunday|Monday, Friday, Sunday|Tuesday, Friday, Sunday|Monday, Tuesday, Friday, Sunday|Wednesday, Friday, Sunday|Monday, Wednesday, Friday, Sunday|Tuesday, Wednesday, Friday, Sunday|Monday, Tuesday, Wednesday, Friday, Sunday|Thursday, Friday, Sunday|Monday, Thursday, Friday, Sunday|Tuesday, Thursday, Friday, Sunday|Monday, Tuesday, Thursday, Friday, Sunday|Wednesday, Thursday, Friday, Sunday|Monday, Wednesday, Thursday, Friday, Sunday|Tuesday, Wednesday, Thursday, Friday, Sunday|Weekdays, Sunday|Weekends|Monday, Weekends|Tuesday, Weekends|Monday, Tuesday, Weekends|Wednesday, Weekends|Monday, Wednesday, Weekends|Tuesday, Wednesday, Weekends|Monday, Tuesday, Wednesday, Weekends|Thursday, Weekends|Monday, Thursday, Weekends|Tuesday, Thursday, Weekends|Monday, Tuesday, Thursday, Weekends|Wednesday, Thursday, Weekends|Monday, Wednesday, Thursday, Weekends|Tuesday, Wednesday, Thursday, Weekends|Monday, Tuesday, Wednesday, Thursday, Weekends|Friday, Weekends|Monday, Friday, Weekends|Tuesday, Friday, Weekends|Monday, Tuesday, Friday, Weekends|Wednesday, Friday, Weekends|Monday, Wednesday, Friday, Weekends|Tuesday, Wednesday, Friday, Weekends|Monday, Tuesday, Wednesday, Friday, Weekends|Thursday, Friday, Weekends|Monday, Thursday, Friday, Weekends|Tuesday, Thursday, Friday, Weekends|Monday, Tuesday, Thursday, Friday, Weekends|Wednesday, Thursday, Friday, Weekends|Monday, Wednesday, Thursday, Friday, Weekends|Tuesday, Wednesday, Thursday, Friday, Weekends|All|128";

            List_CompoundTests((typeof(DaysOfWeek), Enumerable.Range(-1, 130).Select(i => (DaysOfWeek)i).ToList(), ALL_DAYS_OF_WEEK));
        }

        [Test]
        public void AggressionBased_OfList_Tests()
        {
            var input = AggressionBasedFactory<List<float?>>.FromPassiveNormalAggressive(
                        Enumerable.Range(1, 3).Select(i => i == 2 ? (float?)null : 10 * i).ToList(),
                        null,
                        Enumerable.Range(10, 6).Select(i => i % 2 == 0 ? (float?)null : 10 * i).ToList()
                );

            string text = input.ToString();
            Assert.That(text, Is.EqualTo(@"10|\∅|30#∅#\∅|110|\∅|130|\∅|150"));
            var deser = AggressionBasedFactory<List<float?>>.FromText(text);
            Assert.That(deser, Is.EqualTo(input));
        }
        #endregion

        #region Dict

        private static IEnumerable<(string, Dss)> ValidDictData() => new[]
        {
            (null, null),
            (@"", new Dss()),
            (@"=", new Dss{[""]=""}),
            (@"key1=", new Dss{["key1"]=""}),
            (@"key1=∅", new Dss{["key1"]=null}),
            (@"key1= ∅", new Dss{["key1"]=" ∅"}),
            (@"key1= ∅ ", new Dss{["key1"]=" ∅ "}),
            (@"key1=\∅", new Dss{["key1"]="∅"}),
            (@"key1=\∅ ", new Dss{["key1"]="∅ "}),
            (@"key1=\∅;key2=\∅", new Dss{["key1"]="∅",["key2"]="∅"}),
            (@"key1=∅;key2=\∅", new Dss{["key1"]=null,["key2"]="∅"}),
            (@"key1=value1", new Dss{["key1"]="value1"}),
            (@"key\=1=value\=1", new Dss{["key=1"]="value=1"}),
            (@"\=1=\=2", new Dss{["=1"]="=2"}),
            (@"1\==2\=", new Dss{["1="]="2="}),
            (@"key1=value1;key2=value2", new Dss{["key1"]="value1",["key2"]="value2"}),
            (@"\;key1\;=\;value1\;;\;key2\;=\;value2\;", new Dss{[";key1;"]=";value1;",[";key2;"]=";value2;"}),
            (@"\;key1\=\;=\;val\=ue1\;;\;key2\;=\;value\=2\;", new Dss{[";key1=;"]=";val=ue1;",[";key2;"]=";value=2;"}),


            (@"key\∅= \∅ ;key\∅2= \\\∅ ;key1=\∅;key2=∅", new Dss{["key1"]="∅",["key2"]=null,["key∅"]=" ∅ ",["key∅2"]=@" \∅ ", }),
            (@"key1=\=\;", new Dss{["key1"]="=;"} ),
            (@"key\;\=1=\=\;", new Dss{["key;=1"]="=;"} ),
            (@"\\\;\\\==\\\;\\\=;k\\\=ey2=\;\\\=A\\BC;key\;\=1=\=\;", new Dss{[@"\;\="]=@"\;\=", [@"k\=ey2"]=@";\=A\BC", ["key;=1"]="=;"}),

        };

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Parse_Test((string input, Dss expectedDict) data)
        {
            IDictionary<string, string> result = _sut.ParseDictionary<string, string>(data.input, DictionaryKind.SortedDictionary);

            if (data.expectedDict == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EquivalentTo(data.expectedDict));


            if (data.expectedDict == null)
                Console.WriteLine(@"NULL dictionary");
            else if (!data.expectedDict.Any())
                Console.WriteLine(@"Empty dictionary");
            else
                foreach (var kvp in data.expectedDict)
                    Console.WriteLine($@"[{kvp.Key}] = '{kvp.Value ?? "<null>"}'");
        }

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Format_SymmetryTests((string expectedOutput, Dss inputDict) data)
        {
            var result = _sut.FormatDictionary(data.inputDict);

            if (data.expectedOutput == null)
                Assert.That(result, Is.Null);
            else
            {
                string expectedOutput = data.expectedOutput;

                result = NormalizeNullMarkers(result);
                expectedOutput = NormalizeNullMarkers(expectedOutput);
                Assert.That(result, Is.EqualTo(expectedOutput));
            }

            Console.WriteLine($@"'{result ?? "<null>"}'");
        }

        #region Negative tests
        [TestCase(@"key1")]//no value
        [TestCase(@";")]//no values
        [TestCase(@"key1 ; key2")]

        //TODO check below if that's ok
        [TestCase(@"key1=value1;")]//non terminated sequence
        [TestCase(@"ke=y1=value1")]//too many separators
        [TestCase(@"key1=value1;key1=value2")] //An item with the same key has already been added.
        [TestCase(@"∅=value")]//Key element in dictionary cannot be null
        [TestCase(@"∅")]//null dictionary can only be mapped as null string  
        #endregion
        public void Dict_Parse_NegativeTest(string input)
        {
            try
            {
                var result = _sut.ParseDictionary<string, string>(input, DictionaryKind.Dictionary, DictionaryBehaviour.ThrowOnDuplicate).ToList();
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(kvp => $"[{kvp.Key}] = '{kvp.Value}'"))}");
            }
            catch (ArgumentException ae) when (ae.TargetSite?.Name == "ParseDictionary")
            {
                Console.WriteLine($@"Expected exception from implementation: {ae.Message}");
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine($@"Expected external exception: {ae.Message}");
            }
            catch (Exception e)
            {
                Assert.Fail($@"Unexpected external exception: {e.Message}");
            }

        }

        [Test]
        public void Dict_CompoundTests()
        {
            var dict = Enumerable.Range(1, 5).ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var text = _sut.FormatDictionary(dict);
            var dict2 = _sut.ParseDictionary<int, TimeSpan>(text);

            Assert.That(text, Is.EqualTo("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08"));
            Assert.That(dict2, Is.EquivalentTo(dict));
        }

        [Test]
        public void Dict_CompoundTestsAggBasedAndList()
        {
            var dict = Enumerable.Range(0, 4).ToDictionary(
                i => AggressionBasedFactory<float>.FromPassiveNormalAggressive(10 * i, 10 * i + 1, 10 * i + 2),
                i => new List<TimeSpan> { new TimeSpan(i, i + 1, i + 2, i + 3), new TimeSpan(10 * i, 10 * i + 1, 10 * i + 2, 10 * i + 3) });

            var text = _sut.FormatDictionary(dict);
            Assert.That(text, Is.EqualTo("0#1#2=01:02:03|01:02:03;10#11#12=1.02:03:04|10.11:12:13;20#21#22=2.03:04:05|20.21:22:23;30#31#32=3.04:05:06|31.07:32:33"));

            //dict.Remove(dict.First().Key);

            var deser = _sut.ParseDictionary<IAggressionBased<float>, List<TimeSpan>>(text);
            Assert.That(deser, Is.EquivalentTo(dict));
        }

        #endregion
    }
}
