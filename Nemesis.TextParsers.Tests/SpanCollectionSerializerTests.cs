using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;
using Dss = System.Collections.Generic.SortedDictionary<string, string>;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class SpanCollectionSerializerTests
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        private readonly SpanCollectionSerializer _sut = SpanCollectionSerializer.DefaultInstance;

        const string NULL_PLACEHOLDER = "维基百科";
        private static string NormalizeNullMarkers(string text) =>
            text.Replace(@"\∅", NULL_PLACEHOLDER).Replace(@"∅", NULL_PLACEHOLDER).Replace(NULL_PLACEHOLDER, @"\∅");

        #region List
        internal static IEnumerable<(string text, string[] collection)> ValidListData() => new[]
        {
            (null, null),
            ("", new string[0]),
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
            var result = _sut.ParseCollection<string>(data.input);

            if (data.expectedList == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EqualTo(data.expectedList));


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


        internal static IEnumerable<(Type elementType, string input, Type expectedException)> Bad_ListParseData() => new[]
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

#if NETCOREAPP3_0 == false
            (typeof(float), @"-340282357000000000000000000000000000000|-340282347000000000000000000000000000000", typeof(OverflowException)),
            (typeof(float), @" 340282347000000000000000000000000000000|340283347000000000000000000000000000000", typeof(OverflowException)),
#endif
        };
        private IReadOnlyCollection<TElement> ParseCollection<TElement>(string text) => _sut.ParseCollection<TElement>(text);
        [TestCaseSource(nameof(Bad_ListParseData))]
        public void List_Parse_NegativeCompoundTests((Type elementType, string input, Type expectedException) data)
        {
            var parseMethod = (typeof(SpanCollectionSerializerTests).GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
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
                TestHelper.AssertException(e, data.expectedException, null);
            }
            if (passed)
                Assert.Fail($"'{data.input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, parsed?.Cast<object>().Select(r => $"'{r}'") ?? new string[0])}");
        }

        private static IEnumerable<TNumber> GetTestNumbers<TNumber>(TNumber from, TNumber to, TNumber increment, Func<TNumber, TNumber, TNumber> addFunc)
            where TNumber : struct, IComparable, IComparable<TNumber>, IEquatable<TNumber>, IFormattable
        {
            var result = new List<TNumber>();
            for (var i = from; i.CompareTo(to) <= 0; i = addFunc(i, increment))
                result.Add(i);

            return result;

        }

        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        [SuppressMessage("ReSharper", "RedundantCast")]
        internal static IEnumerable<(Type elementType, IEnumerable expectedOutput, string input)> ListCompoundData() => new (Type, IEnumerable, string)[]
        {
            (typeof(byte), null, null),
            (typeof(string), null, null),

            (typeof(byte), GetTestNumbers<byte>(byte.MinValue, byte.MaxValue-1, 1, (n1, n2) => (byte)(n1+n2)),
                @"∅|  1 | 2 |3 | 4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200|201|202|203|204|205|206|207|208|209|210|211|212|213|214|215|216|217|218|219|220|221|222|223|224|225|226|227|228|229|230|231|232|233|234|235|236|237|238|239|240|241|242|243|244|245|246|247|248|249|250|251|252|253|254" ),

            (typeof(sbyte), GetTestNumbers<sbyte>(sbyte.MinValue, sbyte.MaxValue-1, 1, (n1, n2) => (sbyte)(n1+n2)),
                @"-128|  -127 | -126 |-125 | -124|-123|-122|-121|-120|-119|-118|-117|-116|-115|-114|-113|-112|-111|-110|-109|-108|-107|-106|-105|-104|-103|-102|-101|-100|-99|-98|-97|-96|-95|-94|-93|-92|-91|-90|-89|-88|-87|-86|-85|-84|-83|-82|-81|-80|-79|-78|-77|-76|-75|-74|-73|-72|-71|-70|-69|-68|-67|-66|-65|-64|-63|-62|-61|-60|-59|-58|-57|-56|-55|-54|-53|-52|-51|-50|-49|-48|-47|-46|-45|-44|-43|-42|-41|-40|-39|-38|-37|-36|-35|-34|-33|-32|-31|-30|-29|-28|-27|-26|-25|-24|-23|-22|-21|-20|-19|-18|-17|-16|-15|-14|-13|-12|-11|-10|-9|-8|-7|-6|-5|-4|-3|-2|-1|0|1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126" ),

            (typeof(short), GetTestNumbers<short>(short.MinValue, short.MaxValue-short.MaxValue/100, short.MaxValue/100, (n1, n2) => (short)(n1+n2)),
                @"-32768|  -32441 | -32114 | -31787|-31460 |-31133|-30806|-30479|-30152|-29825|-29498|-29171|-28844|-28517|-28190|-27863|-27536|-27209|-26882|-26555|-26228|-25901|-25574|-25247|-24920|-24593|-24266|-23939|-23612|-23285|-22958|-22631|-22304|-21977|-21650|-21323|-20996|-20669|-20342|-20015|-19688|-19361|-19034|-18707|-18380|-18053|-17726|-17399|-17072|-16745|-16418|-16091|-15764|-15437|-15110|-14783|-14456|-14129|-13802|-13475|-13148|-12821|-12494|-12167|-11840|-11513|-11186|-10859|-10532|-10205|-9878|-9551|-9224|-8897|-8570|-8243|-7916|-7589|-7262|-6935|-6608|-6281|-5954|-5627|-5300|-4973|-4646|-4319|-3992|-3665|-3338|-3011|-2684|-2357|-2030|-1703|-1376|-1049|-722|-395|-68|259|586|913|1240|1567|1894|2221|2548|2875|3202|3529|3856|4183|4510|4837|5164|5491|5818|6145|6472|6799|7126|7453|7780|8107|8434|8761|9088|9415|9742|10069|10396|10723|11050|11377|11704|12031|12358|12685|13012|13339|13666|13993|14320|14647|14974|15301|15628|15955|16282|16609|16936|17263|17590|17917|18244|18571|18898|19225|19552|19879|20206|20533|20860|21187|21514|21841|22168|22495|22822|23149|23476|23803|24130|24457|24784|25111|25438|25765|26092|26419|26746|27073|27400|27727|28054|28381|28708|29035|29362|29689|30016|30343|30670|30997|31324|31651|31978|32305" ),

            (typeof(ushort), GetTestNumbers<ushort>(ushort.MinValue, ushort.MaxValue-ushort.MaxValue/100, ushort.MaxValue/100, (n1, n2) => (ushort)(n1+n2)),
                @"0|  655 | 1310 | 1965|2620 |3275|3930|4585|5240|5895|6550|7205|7860|8515|9170|9825|10480|11135|11790|12445|13100|13755|14410|15065|15720|16375|17030|17685|18340|18995|19650|20305|20960|21615|22270|22925|23580|24235|24890|25545|26200|26855|27510|28165|28820|29475|30130|30785|31440|32095|32750|33405|34060|34715|35370|36025|36680|37335|37990|38645|39300|39955|40610|41265|41920|42575|43230|43885|44540|45195|45850|46505|47160|47815|48470|49125|49780|50435|51090|51745|52400|53055|53710|54365|55020|55675|56330|56985|57640|58295|58950|59605|60260|60915|61570|62225|62880|63535|64190|64845" ),

            (typeof(int), GetTestNumbers<int>(int.MinValue, int.MaxValue-int.MaxValue/10, int.MaxValue/10, (n1, n2) => (int)(n1+n2)),
                @"-2147483648|  -1932735284 | -1717986920 | -1503238556|-1288490192 |-1073741828|-858993464|-644245100|-429496736|-214748372|-8|214748356|429496720|644245084|858993448|1073741812|1288490176|1503238540|1717986904|1932735268" ),

            (typeof(uint), GetTestNumbers<uint>(uint.MinValue, uint.MaxValue-uint.MaxValue/10, uint.MaxValue/10, (n1, n2) => (uint)(n1+n2)),
                @"0|  429496729 | 858993458 | 1288490187|1717986916 |2147483645|2576980374|3006477103|3435973832|3865470561" ),

            (typeof(long), GetTestNumbers<long>(long.MinValue, long.MaxValue-long.MaxValue/10, long.MaxValue/10, (n1, n2) => (long)(n1+n2)),
                @"-9223372036854775808|  -8301034833169298228 | -7378697629483820648 | -6456360425798343068|-5534023222112865488 |-4611686018427387908|-3689348814741910328|-2767011611056432748|-1844674407370955168|-922337203685477588|-8|922337203685477572|1844674407370955152|2767011611056432732|3689348814741910312|4611686018427387892|5534023222112865472|6456360425798343052|7378697629483820632|8301034833169298212" ),

            (typeof(ulong), GetTestNumbers<ulong>(ulong.MinValue, ulong.MaxValue-ulong.MaxValue/10, ulong.MaxValue/10, (n1, n2) => (ulong)(n1+n2)),
                @"0|  1844674407370955161 | 3689348814741910322 | 5534023222112865483|7378697629483820644 |9223372036854775805|11068046444225730966|12912720851596686127|14757395258967641288|16602069666338596449" ),

            (typeof(float), GetTestNumbers<float>(float.MinValue+float.MaxValue/10, float.MaxValue-float.MaxValue/10, float.MaxValue/10, (n1, n2) => (float)(n1+n2)),
                @"-3.06254122E+38|-2.72225877E+38|-2.38197633E+38|-2.04169388E+38|-1.70141153E+38|-1.36112918E+38|-1.02084684E+38|-6.8056449E+37|-3.40282144E+37|2.02824096E+31|3.40282549E+37|6.80564896E+37|1.02084724E+38|1.36112959E+38|1.70141183E+38|2.04169428E+38|2.38197673E+38|2.72225918E+38" ),

            (typeof(double), GetTestNumbers<double>(double.MinValue+double.MaxValue/10, double.MaxValue-double.MaxValue/10, double.MaxValue/10, (n1, n2) => (double)(n1+n2)),
                @"-1.6179238213760842E+308|-1.4381545078898526E+308|-1.2583851944036211E+308|-1.0786158809173895E+308|-8.9884656743115795E+307|-7.190772539449264E+307|-5.3930794045869485E+307|-3.595386269724633E+307|-1.7976931348623173E+307|-1.4968802321510399E+292|1.7976931348623143E+307|3.59538626972463E+307|5.3930794045869455E+307|7.190772539449261E+307|8.9884656743115765E+307|1.0786158809173893E+308|1.2583851944036209E+308|1.4381545078898524E+308|1.617923821376084E+308" ),

            (typeof(decimal), GetTestNumbers<decimal>(decimal.MinValue+(decimal)0.123456789, decimal.MaxValue-decimal.MaxValue/10, decimal.MaxValue/5, (n1, n2) => (decimal)(n1+n2)),
                @"-79228162514264337593543950335|-63382530011411470074835160268|-47536897508558602556126370201|-31691265005705735037417580134|-15845632502852867518708790067|0|15845632502852867518708790067|31691265005705735037417580134|47536897508558602556126370201|63382530011411470074835160268" ),

            (typeof(BigInteger), GetTestNumbers<BigInteger>(BigInteger.Parse("-12345678901234567890"), BigInteger.Parse("12345678901234567890"), BigInteger.Parse("2469135780246913578"), (n1, n2) => (BigInteger)(n1+n2)),
                @"-12345678901234567890|-9876543120987654312|-7407407340740740734|-4938271560493827156|-2469135780246913578|0|2469135780246913578|4938271560493827156|7407407340740740734|9876543120987654312|12345678901234567890" ),

            (typeof(TimeSpan), GetTestNumbers<TimeSpan>(TimeSpan.MinValue, TimeSpan.MaxValue-Divide(TimeSpan.MaxValue,10), Divide(TimeSpan.MaxValue,10), (n1, n2) => (TimeSpan)(n1+n2)),
                @"-10675199.02:48:05.4775808|-9607679.04:55:16.9298176|-8540159.07:02:28.3820544|-7472639.09:09:39.8342912|-6405119.11:16:51.2865280|-5337599.13:24:02.7387648|-4270079.15:31:14.1910016|-3202559.17:38:25.6432384|-2135039.19:45:37.0954752|-1067519.21:52:48.5477120|00:00:00.0000512|1067519.21:52:48.5478144|2135039.19:45:37.0955776|3202559.17:38:25.6433408|4270079.15:31:14.1911040|5337599.13:24:02.7388672|6405119.11:16:51.2866304|7472639.09:09:39.8343936|8540159.07:02:28.3821568" ),

            (typeof(TimeSpan), Enumerable.Range(1, 7).Select(i => new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|2.03:04:05|3.04:05:06|4.05:06:07|5.06:07:08|6.07:08:09|7.08:09:10" ),

            (typeof(TimeSpan),
                Enumerable.Range(1, 7).Select(i => i % 2 == 0 ? TimeSpan.Zero : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|∅|3.04:05:06|∅|5.06:07:08|∅|7.08:09:10"),

            (typeof(TimeSpan?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (TimeSpan?) null : new TimeSpan(i, i + 1, i + 2, i + 3)).ToList(),
            @"1.02:03:04|2.03:04:05|∅|4.05:06:07|5.06:07:08|∅|7.08:09:10" ),

            (typeof(Uri), Enumerable.Range(1, 7).Select(i => new Uri($"http://www.google{i}.com")).ToList(),
                @"http://www.google1.com|http://www.google2.com|http://www.google3.com|http://www.google4.com|http://www.google5.com|http://www.google6.com|http://www.google7.com" ),

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

            (typeof(Complex), Enumerable.Range(1, 7).Select(i => new Complex(i*1.1, -i*2.2)).ToList(),
                @"(1.1; -2.2)|(2.2; -4.4)|(3.3000000000000003; -6.6000000000000005)|(4.4; -8.8)|(5.5; -11)|(6.6000000000000005; -13.200000000000001)|(7.7000000000000011; -15.400000000000002)" ),

            (typeof(Complex?), Enumerable.Range(1, 7).Select(i => i % 3 == 0 ? (Complex?)null :new Complex(i*1.1, -i*2.2)).ToList(),
                @"(1.1; -2.2)|(2.2; -4.4)|∅|(4.4; -8.8)|(5.5; -11)|∅|(7.7000000000000011; -15.400000000000002)" ),

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

            (typeof(HashSet<float>),
                Enumerable.Range(1, 3).Select(i =>
                    new HashSet<float>
                    {
                        i*30 + 0.5f,
                        i*60 + 0.5f,
                    }
                ).ToList(),
                @"30.5\|60.5|60.5\|120.5|90.5\|180.5"),

            (typeof(SortedDictionary<int, float>),
                Enumerable.Range(1, 3).Select(i =>
                    new SortedDictionary<int, float>
                    {
                        [i*10+0]=i*10 + 0.5f,
                        [i*20+0]=i*20 + 0.5f,
                    }
                ).ToList(),
                @"10=10.5;20=20.5|20=20.5;40=40.5|30=30.5;60=60.5"),

            (typeof(ReadOnlyDictionary<int, float>),
                Enumerable.Range(1, 4).Select(i => new ReadOnlyDictionary<int, float>(
                    new Dictionary<int, float>()
                    {
                        [i*100+0]=i*10 + 0.5f,
                        [i*200+0]=i*20 + 0.5f,
                    })
                ).ToList(),
                @"100=10.5;200=20.5|200=20.5;400=40.5|300=30.5;600=60.5|400=40.5;800=80.5"),

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

            (typeof(KeyValuePair<string, float?>),
                new[]
                {
                    new KeyValuePair<string, float?>("PI", 3.14f),
                    new KeyValuePair<string, float?>("PI", null),
                    new KeyValuePair<string, float?>("", 3.14f),
                    new KeyValuePair<string, float?>(null, 3.14f),

                    new KeyValuePair<string, float?>("", null),
                    new KeyValuePair<string, float?>(null, null),
                    new KeyValuePair<string, float?>("", null),

                    default,
                    new KeyValuePair<string, float?>("", null),
                    new KeyValuePair<string, float?>("", 0),
                    default
                }.ToList(),
                @"PI=3.14|PI=∅|=3.14|∅=3.14||∅=∅||∅=∅|=∅|=0|∅"),

            (typeof( (TimeSpan, int, float, string, decimal?) ),
                new[]
                {
                    (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m),
                    (new TimeSpan(31,14,15,9), 3, 3.14f, "Pi", (decimal?)null),
                    (TimeSpan.MinValue, 3, 3.14f, "Pi", (decimal?)null),

                    (TimeSpan.Zero, 0, 0f, "", 0m),
                    (TimeSpan.Zero, 0, 0f, (string)null, (decimal?)null),
                    default,
                    default,
                    default,
                }.ToList(),
                @"(3.14:15:09,3,3.1400001,Pi,3.14)|(31.14:15:09,3,3.1400001,Pi,\∅)|(-10675199.02:48:05.4775808,3,3.1400001,Pi,\∅)|(00:00:00,0,0,,0)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)"),

            (typeof( (TimeSpan, int, float, string, decimal?) ),
                new[]
                {
                    (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m),
                    (new TimeSpan(31,14,15,9), 3, 3.14f, "Pi", (decimal?)null),
                    (TimeSpan.MinValue, 3, 3.14f, "Pi", (decimal?)null),

                    (TimeSpan.Zero, 0, 0f, "", 0m),
                    (TimeSpan.Zero, 0, 0f, (string)null, (decimal?)null),
                    default,
                    default,
                    (TimeSpan.Zero, 0, 0f, "", (decimal?)null),
                }.ToList(),
                @"(3.14:15:09,3,3.1400001,Pi,3.14)|(31.14:15:09,3,3.1400001,Pi,\∅)|(-10675199.02:48:05.4775808,3,3.1400001,Pi,\∅)|(00:00:00,0,0,,0)|(00:00:00,0,0,\∅,\∅)|(00:00:00,0,0,\∅,\∅)|∅|"),
        };

        static TimeSpan Divide(TimeSpan dividend, long divisor) => TimeSpan.FromTicks((long)(dividend.Ticks / (double)divisor));

        [TestCaseSource(nameof(ListCompoundData))]
        public void List_CompoundTests((Type elementType, IEnumerable expectedOutput, string input) data)
        {
            var tester = (
                GetType().GetMethod(nameof(List_CompoundTestsHelper), ALL_FLAGS) ??
                throw new MissingMethodException(GetType().FullName, nameof(List_CompoundTestsHelper))
            ).MakeGenericMethod(data.elementType);

            tester.Invoke(null, new object[] { data.expectedOutput, data.input });
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

            var sut = SpanCollectionSerializer.DefaultInstance;

            var expectedList = expectedOutput?.Cast<TElement>().ToList();

            string textExpected = sut.FormatCollection(expectedList);

            var parsed1 = sut.ParseCollection<TElement>(input);
            CheckEquivalency(parsed1, expectedList);


            string text = sut.FormatCollection(parsed1);
            //Console.WriteLine($"EXP:{textExpected}");
            //Console.WriteLine($"INP:{input}");
            //Console.WriteLine($"TEX:{text}");


            var parsed2 = sut.ParseCollection<TElement>(text);
            CheckEquivalency(parsed2, expectedList);


            var parsed3 = sut.ParseCollection<TElement>(textExpected);
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
        public void AggressionBased_OfList_Tests()
        {
            var input = AggressionBasedFactory<List<float?>>.FromPassiveNormalAggressive(
                        Enumerable.Range(1, 3).Select(i => i == 2 ? (float?)null : 10 * i).ToList(),
                        null,
                        Enumerable.Range(10, 6).Select(i => i % 2 == 0 ? (float?)null : 10 * i).ToList()
                );

            string text = input.ToString();
            Assert.That(text, Is.EqualTo(@"10|\∅|30#∅#\∅|110|\∅|130|\∅|150"));
            var deser = AggressionBasedFactoryChecked<List<float?>>.FromText(text.AsSpan());
            Assert.That(deser, Is.EqualTo(input));
        }

        [Test]
        public void Complex_List_Roundtrip_Test()
        {
            var array = new int?[] { 30, null, null, 40 };
            // (@"B|∅|A|∅", new []{"B",null,"A",null}),
            var text = _sut.FormatCollection(array);

            Assert.That(text, Is.EqualTo("30|∅|∅|40"));

            var parsed = _sut.ParseArray<int?>(text);
            Assert.That(parsed, Is.EqualTo(array));

            var parsed2 = _sut.ParseArray<int?>(@"300|||400");
            Assert.That(parsed2, Is.EqualTo(new int?[] { 300, null, null, 400 }));


            var trans = TextTransformer.Default.GetTransformer<IAggressionBased<int?[]>>();
            var parsed3 = trans.Parse(@"3000|\∅|\∅|4000");
            Assert.That(
                ((IAggressionValuesProvider<int?[]>)parsed3).Values.SingleOrDefault(),
                Is.EqualTo(new int?[] { 3000, null, null, 4000 }));
        }

        #endregion

        #region Dict

        private static IEnumerable<(string text, Dss dictionary)> ValidDictData() => new[]
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

            (@"Key=Text\;Text", new Dss{{ "Key", @"Text;Text" }})
        };

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Parse_Test((string input, Dss expectedDict) data)
        {
            IDictionary<string, string> result = _sut.ParseDictionary<string, string>(data.input, DictionaryKind.SortedDictionary);

            if (data.expectedDict == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EqualTo(data.expectedDict));


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
        [TestCase(@"key1", typeof(ArgumentException), "'key1' has no matching value")]//no value
        [TestCase(@";", typeof(ArgumentException), "Key=Value part was not found")]//no pairs
        [TestCase(@"key1 ; key2", typeof(ArgumentException), "'key1 ' has no matching value")]//no values
        [TestCase(@"key1=value1;", typeof(ArgumentException), "Key=Value part was not found")]//non terminated sequence
        [TestCase(@"ke=y1=value1", typeof(ArgumentException), "ke=y1 pair cannot have more than 2 elements: 'value1'")]//too many separators
        [TestCase(@"SameKey=value1;SameKey=value2", typeof(ArgumentException), "The key 'SameKey' has already been added")] //An item with the same key has already been added. (DictionaryBehaviour.ThrowOnDuplicate)
        [TestCase(@"∅=value", typeof(ArgumentException), "Key equal to NULL is not supported")]//Key element in dictionary cannot be null
        [TestCase(@"∅", typeof(ArgumentException), "'' has no matching value")]//null dictionary can only be mapped as null string  
        #endregion
        public void Dict_Parse_NegativeTest(string input, Type expectedException, string expectedErrorMessagePart)
        {
            IDictionary<string, string> result = null;
            bool passed = false;
            try
            {
                result = _sut.ParseDictionary<string, string>(input, DictionaryKind.Dictionary, DictionaryBehaviour.ThrowOnDuplicate);
                passed = true;
            }
            catch (Exception actual)
            {
                TestHelper.AssertException(actual, expectedException, expectedErrorMessagePart);

                if (actual.TargetSite?.Name == nameof(_sut.ParseDictionary))
                    Console.WriteLine($@"Expected exception from implementation: {actual.GetType().Name}=>{actual.Message}");
            }

            if (passed)
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(kvp => $"[{kvp.Key}] = '{kvp.Value}'"))}");
        }

        [Test]
        public void Dict_CompoundTests()
        {
            var dict = Enumerable.Range(1, 5).ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var text = _sut.FormatDictionary(dict);
            var dict2 = _sut.ParseDictionary<int, TimeSpan>(text);

            Assert.That(text, Is.EqualTo("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08"));
            Assert.That(dict2, Is.EqualTo(dict));
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
            Assert.That(deser, Is.EqualTo(dict));
        }

        #endregion
    }
}
