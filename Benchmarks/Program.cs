using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Nemesis.TextParsers;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] _data;

        private readonly SHA256 _sha256 = SHA256.Create();
        private readonly MD5 _md5 = MD5.Create();

        public Md5VsSha256()
        {
            _data = new byte[N];
            new Random(42).NextBytes(_data);
        }

        [Benchmark]
        public byte[] Sha256() => _sha256.ComputeHash(_data);

        [Benchmark]
        public byte[] Md5() => _md5.ComputeHash(_data);
    }

    [MemoryDiagnoser]
    //[HardwareCounters(HardwareCounter.BranchMispredictions,HardwareCounter.BranchInstructions, HardwareCounter.Timer)]
    //[InliningDiagnoser]
    //[TailCallDiagnoser]
    //[EtwProfiler]
    //[ConcurrencyVisualizerProfiler]
    public class CollectionParserBench
    {
        public string Numbers { get; set; } =
            "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200|201|202|203|204|205|206|207|208|209|210|211|212|213|214|215|216|217|218|219|220|221|222|223|224|225|226|227|228|229|230|231|232|233|234|235|236|237|238|239|240|241|242|243|244|245|246|247|248|249|250|251|252|253|254|255|256|257|258|259|260|261|262|263|264|265|266|267|268|269|270|271|272|273|274|275|276|277|278|279|280|281|282|283|284|285|286|287|288|289|290|291|292|293|294|295|296|297|298|299|300|301|302|303|304|305|306|307|308|309|310|311|312|313|314|315|316|317|318|319|320|321|322|323|324|325|326|327|328|329|330|331|332|333|334|335|336|337|338|339|340|341|342|343|344|345|346|347|348|349|350|351|352|353|354|355|356|357|358|359|360|361|362|363|364|365|366|367|368|369|370|371|372|373|374|375|376|377|378|379|380|381|382|383|384|385|386|387|388|389|390|391|392|393|394|395|396|397|398|399|400|401|402|403|404|405|406|407|408|409|410|411|412|413|414|415|416|417|418|419|420|421|422|423|424|425|426|427|428|429|430|431|432|433|434|435|436|437|438|439|440|441|442|443|444|445|446|447|448|449|450|451|452|453|454|455|456|457|458|459|460|461|462|463|464|465|466|467|468|469|470|471|472|473|474|475|476|477|478|479|480|481|482|483|484|485|486|487|488|489|490|491|492|493|494|495|496|497|498|499|500|501|502|503|504|505|506|507|508|509|510|511|512|513|514|515|516|517|518|519|520|521|522|523|524|525|526|527|528|529|530|531|532|533|534|535|536|537|538|539|540|541|542|543|544|545|546|547|548|549|550|551|552|553|554|555|556|557|558|559|560|561|562|563|564|565|566|567|568|569|570|571|572|573|574|575|576|577|578|579|580|581|582|583|584|585|586|587|588|589|590|591|592|593|594|595|596|597|598|599|600|601|602|603|604|605|606|607|608|609|610|611|612|613|614|615|616|617|618|619|620|621|622|623|624|625|626|627|628|629|630|631|632|633|634|635|636|637|638|639|640|641|642|643|644|645|646|647|648|649|650|651|652|653|654|655|656|657|658|659|660|661|662|663|664|665|666|667|668|669|670|671|672|673|674|675|676|677|678|679|680|681|682|683|684|685|686|687|688|689|690|691|692|693|694|695|696|697|698|699|700|701|702|703|704|705|706|707|708|709|710|711|712|713|714|715|716|717|718|719|720|721|722|723|724|725|726|727|728|729|730|731|732|733|734|735|736|737|738|739|740|741|742|743|744|745|746|747|748|749|750|751|752|753|754|755|756|757|758|759|760|761|762|763|764|765|766|767|768|769|770|771|772|773|774|775|776|777|778|779|780|781|782|783|784|785|786|787|788|789|790|791|792|793|794|795|796|797|798|799|800|801|802|803|804|805|806|807|808|809|810|811|812|813|814|815|816|817|818|819|820|821|822|823|824|825|826|827|828|829|830|831|832|833|834|835|836|837|838|839|840|841|842|843|844|845|846|847|848|849|850|851|852|853|854|855|856|857|858|859|860|861|862|863|864|865|866|867|868|869|870|871|872|873|874|875|876|877|878|879|880|881|882|883|884|885|886|887|888|889|890|891|892|893|894|895|896|897|898|899|900|901|902|903|904|905|906|907|908|909|910|911|912|913|914|915|916|917|918|919|920|921|922|923|924|925|926|927|928|929|930|931|932|933|934|935|936|937|938|939|940|941|942|943|944|945|946|947|948|949|950|951|952|953|954|955|956|957|958|959|960|961|962|963|964|965|966|967|968|969|970|971|972|973|974|975|976|977|978|979|980|981|982|983|984|985|986|987|988|989|990|991|992|993|994|995|996|997|998|999|1000";

        private readonly CollectionSerializerSlow _collSer = CollectionSerializerSlow.DefaultInstance;

        public CollectionParserBench()
        {
            SpanSplit();
            SpanTokenize();
            TextTransformer.Default.GetTransformer<int[]>();
        }

        [Benchmark]
        public int CollectionSerializerTest()
        {
            int result = 0;
            var parsed = _collSer.ParseCollection<int>(Numbers);
            foreach (int num in parsed)
                result += num;

            return result;
        }

        [Benchmark]
        public int StringSplitTest()
        {
            int result = 0;
            var parsed = Numbers.Split('|');

            foreach (string text in parsed)
                result += int.Parse(text);

            return result;
        }

        [Benchmark]
        public int StringSplitTestNaïveWay()
        {
            int result = 0;
            var parsed = Numbers.Split('|');

            foreach (string text in parsed)
            {
                var conv = TypeDescriptor.GetConverter(typeof(int));
                if (conv.CanConvertFrom(typeof(string)))
                    result += (int)conv.ConvertFrom(text);
            }

            return result;
        }

        [Benchmark]
        public int StringSplitTestNaïveWayOpt()
        {
            int result = 0;
            var parsed = Numbers.Split('|');

            var conv = TypeDescriptor.GetConverter(typeof(int));

            foreach (string text in parsed)
                result += (int)conv.ConvertFrom(text);

            return result;
        }

        private static readonly ITransformer<int> _intParser = TextTransformer.Default.GetTransformer<int>();

        [Benchmark]
        public int SpanSplit()
        {
            int result = 0;

            var split = Numbers.AsSpan().Split('|');

            foreach (var text in split)
                result += _intParser.Parse(text);

            return result;
        }

        [Benchmark(Baseline = true)]
        public int SpanTokenize()
        {
            int result = 0;
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            foreach (int num in parsed) result += num;

            return result;
        }

        [Benchmark]
        public int SpanTokenize_Alloc()
        {
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;
            var array = new int[1000];
            int index = 0;

            int result = 0;
            foreach (int num in parsed)
            {
                array[index++] = num;
                result += num;
            }

            return result;
        }

        [Benchmark]
        public int SpanCollectionSerializer_Parse()
        {
            var parsed = SpanCollectionSerializer.DefaultInstance.ParseArray<int>(Numbers);
            int result = 0;
            foreach (int num in parsed)
                result += num;
            return result;
        }

        [Benchmark]
        public int SpanCollectionSerializer_ParseKnownLength()
        {
            var parsed = SpanCollectionSerializer.DefaultInstance.ParseArray<int>(Numbers, 1000);
            int result = 0;
            foreach (int num in parsed)
                result += num;
            return result;
        }

        [Benchmark]
        public int GetTransformer()
        {
            var trans = TextTransformer.Default.GetTransformer<int[]>();
            int[] parsed = trans.Parse(Numbers.AsSpan());
            int result = 0;
            foreach (int num in parsed)
                result += num;
            return result;
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IAggressionBased<out TValue>
    {
        TValue PassiveValue { get; }
        TValue NormalValue { get; }
        TValue AggressiveValue { get; }

        TValue GetValueFor(byte aggression);
    }

    public class AggressionBasedClass<TValue> : IAggressionBased<TValue>
    {
        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public TValue GetValueFor(byte aggression) => NormalValue;

        public AggressionBasedClass(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }
    }

    public readonly struct AggressionBasedStruct<TValue> : IAggressionBased<TValue>
    {
        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public TValue GetValueFor(byte aggression) => NormalValue;

        public AggressionBasedStruct(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }
    }

    [MemoryDiagnoser]
    public class AggressionBasedBench
    {
        [Benchmark]
        public int StructAllocTest()
        {
            int result = 0;

            for (int i = 0; i < 100; i++)
            {
                IAggressionBased<int> ab = new AggressionBasedStruct<int>(1, 2, 3);
                result += ab.NormalValue * ab.GetValueFor(10);
            }

            return result;
        }

        [Benchmark]
        public int ClassAllocTest()
        {
            int result = 0;

            for (int i = 0; i < 100; i++)
            {
                IAggressionBased<int> ab = new AggressionBasedClass<int>(1, 2, 3);
                result += ab.NormalValue * ab.GetValueFor(10);
            }

            return result;
        }
    }

    [MemoryDiagnoser]
    public class ArrayParserBench
    {
        public struct TestData
        {
            public ushort Capacity { get; }
            public string Text { get; }

            public TestData(ushort capacity, string text)
            {
                Capacity = capacity;
                Text = text;
            }

            public static TestData FromCapacity(ushort capacity) => new TestData(
                capacity,
                string.Join("|", Enumerable.Range(1, capacity).Select(i => i.ToString()))
            );

            public override string ToString() => Capacity.ToString();
        }

        [ParamsSource(nameof(ValuesForData))]
        public TestData Data;

        // public property
        public IEnumerable<TestData> ValuesForData => new[]
        {
            new TestData(005,"1|2|3|4|5"),
            new TestData(010,"1|2|3|4|5|6|7|8|9|10"),
            new TestData(050,"1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50"),
            new TestData(100,"1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100"),
            new TestData(200,"1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200"),
        };


        [Benchmark]
        public int ToArray_Test()
        {
            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).Parse<int>('\\', '∅');
            int[] parsed = stream.ToArray(Data.Capacity);
            return parsed[parsed.Length - 1];
        }

        [Benchmark(Baseline = true)]
        public int ToArrayStack_Test()
        {
            if (Data.Capacity > 128)
                throw new NotSupportedException("Not supported");

            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).Parse<int>('\\', '∅');

            Span<int> parsed = stackalloc int[Data.Capacity];
            int i = 0;
            foreach (var num in stream)
                parsed[i++] = num;

            return parsed[parsed.Length - 1];
        }

        [Benchmark]
        public int Mul_ToArray_Test()
        {
            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).Parse<int>('\\', '∅');
            int[] parsed = stream.ToArray(Data.Capacity);

            int result = 0;
            foreach (int num in parsed)
                result = unchecked(result * num);

            return result;
        }

        [Benchmark]
        public int Mul_ToArrayStack_Test()
        {
            if (Data.Capacity > 128)
                throw new NotSupportedException("Not supported");

            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).Parse<int>('\\', '∅');

            Span<int> parsed = stackalloc int[Data.Capacity];
            int i = 0;
            foreach (var num in stream)
                parsed[i++] = num;

            int result = 0;
            foreach (int num in parsed)
                result = unchecked(result * num);

            return result;
        }

        [Benchmark]
        public int Mul_Optimized()
        {
            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).Parse<int>('\\', '∅');

            int result = 0;
            foreach (int num in stream)
                result = unchecked(result * num);

            return result;
        }

    }

    [MemoryDiagnoser]
    public class EnumParserBench
    {
        [Flags]
        public enum DaysOfWeek : byte
        {
            None = 0,
            Monday
                = 0b0000_0001,
            Tuesday
                = 0b0000_0010,
            Wednesday
                = 0b0000_0100,
            Thursday
                = 0b0000_1000,
            Friday
                = 0b0001_0000,
            Saturday
                = 0b0010_0000,
            Sunday
                = 0b0100_0000,

            Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
            Weekends = Saturday | Sunday,
            All = Weekdays | Weekends
        }

        public static string[] AllEnums =
            //Enumerable.Range(0, 130).Select(i => i.ToString()).ToArray();
            Enumerable.Range(0, 130).Select(i => ((DaysOfWeek)i).ToString("G").Replace(" ", "")).ToArray();

        private static readonly EnumTransformer<DaysOfWeek, byte, ByteNumber> _parser = new EnumTransformer<DaysOfWeek, byte, ByteNumber>(new ByteNumber());

        static EnumParserBench() => _parser.Parse("10".AsSpan());

        [Benchmark(Baseline = true)]
        public DaysOfWeek EnumParser()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = _parser.Parse(text.AsSpan());
            }
            return current;
        }

        [Benchmark]
        public DaysOfWeek EnumParserCode()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = ParseDaysOfWeek(text.AsSpan());
            }
            return current;
        }

        private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;

            var enumStream = input.Split(',').GetEnumerator();

            if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {typeof(DaysOfWeek).Name} enum");
            byte currentValue = ParseDaysOfWeekElement(enumStream.Current);

            while (enumStream.MoveNext())
            {
                var element = ParseDaysOfWeekElement(enumStream.Current);

                currentValue |= element;
            }

            return (DaysOfWeek)currentValue;

        }

        private static byte ParseDaysOfWeekElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();

            return IsNumeric(input) && byte.TryParse(
#if NET48
                input.ToString()
#else
                input
#endif
                , out byte number) ? number : ParseDaysOfWeekByLabelOr(input);
        }

        private static bool IsNumeric(ReadOnlySpan<char> input)
        {
            char firstChar;
            return input.Length > 0 && (char.IsDigit(firstChar = input[0]) || firstChar == '-' || firstChar == '+');
        }

        private static byte ParseDaysOfWeekByLabelOr(ReadOnlySpan<char> input)
        {
            if (input.Length == 4 && (input[3] == 'E' || input[3] == 'e') && (input[2] == 'N' || input[2] == 'n') &&
                (input[1] == 'O' || input[1] == 'o') && (input[0] == 'N' || input[0] == 'n')
            )
                return 0;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') && (input[4] == 'A' || input[4] == 'a') &&
                (input[3] == 'D' || input[3] == 'd') && (input[2] == 'N' || input[2] == 'n') &&
                (input[1] == 'O' || input[1] == 'o') && (input[0] == 'M' || input[0] == 'm')
            )
                return 1;
            else if (
                input.Length == 7 && (input[6] == 'Y' || input[6] == 'y') && (input[5] == 'A' || input[5] == 'a') &&
                (input[4] == 'D' || input[4] == 'd') && (input[3] == 'S' || input[3] == 's') &&
                (input[2] == 'E' || input[2] == 'e') && (input[1] == 'U' || input[1] == 'u') &&
                (input[0] == 'T' || input[0] == 't')
            )
                return 2;
            else if (
                input.Length == 9 && (input[8] == 'Y' || input[8] == 'y') &&
                (input[7] == 'A' || input[7] == 'a') && (input[6] == 'D' || input[6] == 'd') &&
                (input[5] == 'S' || input[5] == 's') && (input[4] == 'E' || input[4] == 'e') &&
                (input[3] == 'N' || input[3] == 'n') && (input[2] == 'D' || input[2] == 'd') &&
                (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
            )
                return 4;
            else if (
                input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                (input[4] == 'S' || input[4] == 's') && (input[3] == 'R' || input[3] == 'r') &&
                (input[2] == 'U' || input[2] == 'u') && (input[1] == 'H' || input[1] == 'h') &&
                (input[0] == 'T' || input[0] == 't')
            )
                return 8;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                (input[2] == 'I' || input[2] == 'i') && (input[1] == 'R' || input[1] == 'r') &&
                (input[0] == 'F' || input[0] == 'f')
            )
                return 16;
            else if (
                input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                (input[4] == 'R' || input[4] == 'r') && (input[3] == 'U' || input[3] == 'u') &&
                (input[2] == 'T' || input[2] == 't') && (input[1] == 'A' || input[1] == 'a') &&
                (input[0] == 'S' || input[0] == 's')
            )
                return 32;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                (input[2] == 'N' || input[2] == 'n') && (input[1] == 'U' || input[1] == 'u') &&
                (input[0] == 'S' || input[0] == 's')
            )
                return 64;
            else if (
                input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                (input[6] == 'Y' || input[6] == 'y') &&
                (input[5] == 'A' || input[5] == 'a') &&
                (input[4] == 'D' || input[4] == 'd') &&
                (input[3] == 'K' || input[3] == 'k') &&
                (input[2] == 'E' || input[2] == 'e') &&
                (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
            )
                return 31;
            else if (
                input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                (input[6] == 'D' || input[6] == 'd') &&
                (input[5] == 'N' || input[5] == 'n') &&
                (input[4] == 'E' || input[4] == 'e') &&
                (input[3] == 'K' || input[3] == 'k') &&
                (input[2] == 'E' || input[2] == 'e') &&
                (input[1] == 'E' || input[1] == 'e') &&
                (input[0] == 'W' || input[0] == 'w')
            )
                return 96;
            else if (
                input.Length == 3 && (input[2] == 'L' || input[2] == 'l') &&
                (input[1] == 'L' || input[1] == 'l') &&
                (input[0] == 'A' || input[0] == 'a')
            )
                return 127;
            else
                throw new FormatException(
                    "Enum of type 'DaysOfWeek' cannot be parsed.Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");
        }

        [Benchmark]
        public DaysOfWeek NativeEnumIgnoreCaseGeneric()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);
            }
            return current;
        }

        [Benchmark]
        public DaysOfWeek NativeEnumIgnoreCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);
            }
            return current;
        }

#if !NET48
        [Benchmark]
        public DaysOfWeek NativeEnumObserveCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = Enum.Parse<DaysOfWeek>(text, false);
            }
            return current;
        }
#endif
    }

    [MemoryDiagnoser]
    public class ToEnumBench
    {
        [Flags]
        public enum DaysOfWeek : byte
        {
            None = 0,
            Monday
                = 0b0000_0001,
            Tuesday
                = 0b0000_0010,
            Wednesday
                = 0b0000_0100,
            Thursday
                = 0b0000_1000,
            Friday
                = 0b0001_0000,
            Saturday
                = 0b0010_0000,
            Sunday
                = 0b0100_0000,

            Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
            Weekends = Saturday | Sunday,
            All = Weekdays | Weekends
        }

        public static byte[] AllEnumValues = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();

        internal static Func<byte, DaysOfWeek> GetNumberConverterDynamicMethod()
        {
            var method = new DynamicMethod("Convert", typeof(DaysOfWeek), new[] { typeof(byte) }, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            return (Func<byte, DaysOfWeek>)method.CreateDelegate(typeof(Func<byte, DaysOfWeek>));
        }

        private static readonly Func<byte, DaysOfWeek> _expressionFunc = EnumTransformerHelper.GetNumberConverter<DaysOfWeek, byte>();
        private static readonly Func<byte, DaysOfWeek> _dynamicMethodFunc = GetNumberConverterDynamicMethod();


        [Benchmark(Baseline = true)]
        public DaysOfWeek NativeTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= (DaysOfWeek)AllEnumValues[i];
            return current;
        }

        private static TEnum ToEnumCast<TEnum, TUnderlying>(TUnderlying number)
            where TEnum : Enum
            where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>,
            IFormattable =>
            (TEnum)(object)number;

        [Benchmark]
        public DaysOfWeek CastTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= ToEnumCast<DaysOfWeek, byte>(AllEnumValues[i]);
            return current;
        }

        [Benchmark]
        public DaysOfWeek ExpressionTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= _expressionFunc(AllEnumValues[i]);
            return current;
        }

        [Benchmark]
        public DaysOfWeek DynamicMethod()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            {
                current |= _dynamicMethodFunc(AllEnumValues[i]);
            }
            return current;
        }

        [Benchmark]
        public DaysOfWeek UnsafeAsTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            {
                byte value = AllEnumValues[i];

                current |= Unsafe.As<byte, DaysOfWeek>(ref value);
            }
            return current;
        }

        [Benchmark]
        public DaysOfWeek UnsafeAsRefTest()
        {
            Span<DaysOfWeek> enums = MemoryMarshal.Cast<byte, DaysOfWeek>(AllEnumValues.AsSpan());

            DaysOfWeek current = default;
            for (int i = enums.Length - 1; i >= 0; i--)
                current |= enums[i];
            return current;
        }

        [Benchmark]
        public DaysOfWeek IlGenericTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= EnumConverter.Convert.ToEnumIl<DaysOfWeek, byte>(AllEnumValues[i]);

            return current;
        }

        [Benchmark]
        public DayOfWeek IlDedicatedTest()
        {
            DayOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                current |= EnumConverter.Convert.ToEnumIlConcrete(AllEnumValues[i]);

            return current;
        }

        private static DaysOfWeek ToEnum(byte value) => EnumTransformer<DaysOfWeek, byte, ByteNumber>.ToEnum(value);

        [Benchmark]
        public DaysOfWeek SelectedSolution()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= ToEnum(AllEnumValues[i]);
            return current;
        }

        /*[MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern TEnum ToEnumIl<TEnum, TUnderlying>(TUnderlying number)
            where TEnum : Enum
            where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable;

        [Benchmark]
        public DaysOfWeek ExternTest()
        {
        }*/
    }

    /*|    Method |         Source |        Mean |     Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
      |---------- |--------------- |------------:|----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
      | NativeSum | System.Int32[] |   0.4192 ns | 0.0347 ns | 0.0325 ns |   1.00 |    0.00 |      - |     - |     - |         - |
      | NativeSum | System.Int32[] |   1.3393 ns | 0.0255 ns | 0.0238 ns |   3.21 |    0.23 |      - |     - |     - |         - |
      | NativeSum | System.Int32[] |   1.9145 ns | 0.0265 ns | 0.0235 ns |   4.61 |    0.34 |      - |     - |     - |         - |
      | NativeSum | System.Int32[] |   6.7011 ns | 0.1056 ns | 0.0988 ns |  16.08 |    1.29 |      - |     - |     - |         - |

      |   LeanSum | System.Int32[] |  38.2319 ns | 0.4710 ns | 0.4406 ns |  91.68 |    6.75 |      - |     - |     - |         - |
      |   LeanSum | System.Int32[] |  40.9513 ns | 0.3505 ns | 0.3278 ns |  98.23 |    7.58 |      - |     - |     - |         - |
      |   LeanSum | System.Int32[] |  43.5412 ns | 0.5777 ns | 0.4824 ns | 104.33 |    8.11 |      - |     - |     - |         - |
      |   LeanSum | System.Int32[] |  54.4123 ns | 0.3606 ns | 0.3373 ns | 130.54 |   10.32 |      - |     - |     - |         - |

      |   ListSum | System.Int32[] |  54.2327 ns | 0.8438 ns | 0.7481 ns | 130.62 |   10.27 | 0.0171 |     - |     - |      72 B |
      |   ListSum | System.Int32[] |  56.5301 ns | 0.9924 ns | 0.8797 ns | 136.10 |   10.05 | 0.0171 |     - |     - |      72 B |
      |   ListSum | System.Int32[] |  59.9531 ns | 1.0717 ns | 1.0025 ns | 143.68 |    9.44 | 0.0190 |     - |     - |      80 B |
      |   ListSum | System.Int32[] |  74.7951 ns | 0.8450 ns | 0.7057 ns | 179.22 |   13.78 | 0.0247 |     - |     - |     104 B |


      |  LeanSort | System.Int32[] |  37.6782 ns | 0.7066 ns | 0.6263 ns |  90.73 |    6.93 |      - |     - |     - |         - |
      |  LeanSort | System.Int32[] |  42.8604 ns | 0.6790 ns | 0.6019 ns | 103.27 |    8.63 |      - |     - |     - |         - |
      |  LeanSort | System.Int32[] |  51.7633 ns | 0.9994 ns | 1.0263 ns | 124.17 |    9.12 |      - |     - |     - |         - |
      |  LeanSort | System.Int32[] |  89.5636 ns | 1.4138 ns | 1.3224 ns | 214.91 |   17.83 |      - |     - |     - |         - |

      |  ListSort | System.Int32[] |  45.3207 ns | 0.9718 ns | 1.1191 ns | 108.48 |    9.15 | 0.0171 |     - |     - |      72 B |
      |  ListSort | System.Int32[] |  72.7966 ns | 1.4664 ns | 1.8009 ns | 174.16 |   14.27 | 0.0170 |     - |     - |      72 B |
      |  ListSort | System.Int32[] |  76.2438 ns | 1.4923 ns | 1.7185 ns | 182.79 |   16.10 | 0.0190 |     - |     - |      80 B |
      |  ListSort | System.Int32[] | 122.7672 ns | 2.4729 ns | 2.6460 ns | 294.28 |   17.75 | 0.0246 |     - |     - |     104 B |*/
    [MemoryDiagnoser]
    public class LeanCollection
    {
        //  [BenchmarkCategory("Slow"), Benchmark(Baseline = true)]
        // alloc+enumeration+operation, Sort
        [ParamsSource(nameof(Sources))]
        public int[] Source { get; set; }

        public IEnumerable<int[]> Sources => new[]
        {
            new[] {10},
            new[] {20, 10},
            new[] {30, 20, 10},
            new[] {90, 80, 70, 60, 50, 40, 30, 20, 10},
        };


        [BenchmarkCategory("Sum"), Benchmark(Baseline = true)]
        public int NativeSum()
        {
            int sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < Source.Length; i++)
                sum += Source[i];
            return sum;
        }

        [BenchmarkCategory("Sum"), Benchmark]
        public int LeanSum()
        {
            var coll = LeanCollection<int>.FromArray(Source);

            var enumerator = coll.GetEnumerator();
            if (!enumerator.MoveNext()) return 0;

            int sum = 0;
            do
                sum += enumerator.Current;
            while (enumerator.MoveNext());

            return sum;
        }

        [BenchmarkCategory("Sum"), Benchmark]
        public int ListSum()
        {
            var coll = new List<int>(Source);

            int sum = 0;
            foreach (int i in coll)
                sum += i;

            return sum;
        }

        [BenchmarkCategory("Sort"), Benchmark]
        public int LeanSort()
        {
            var coll = LeanCollection<int>.FromArray(Source);

            coll.Sort();

            return coll.Size;
        }

        [BenchmarkCategory("Sort"), Benchmark]
        public int ListSort()
        {
            var coll = new List<int>(Source);

            coll.Sort();

            return coll.Count;
        }
    }

    [MemoryDiagnoser]
    [ClrJob, CoreJob]
    public class HasFlagBench
    {
        private const int OPERATIONS_PER_INVOKE = 10_000;

        public static Func<T, long> CreateConvertToLong<T>() where T : struct, Enum
        {
            var generator = DynamicMethodGenerator.Create<Func<T, long>>("ConvertEnumToLong");

            var ilGen = generator.GetMsilGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Conv_I8);
            ilGen.Emit(OpCodes.Ret);
            return generator.Generate();
        }

        public static readonly Func<FileAccess, long> ConverterFunc = CreateConvertToLong<FileAccess>();

        public static bool HasFlags(FileAccess left, FileAccess right)
        {
            var fn = ConverterFunc;
            return (fn(left) & fn(right)) != 0;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public bool HasFlag_Native()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= VALUE.HasFlag(FileAccess.Read);

            return result;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public bool HasFlags_Dynamic()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= HasFlags(VALUE, FileAccess.Read);

            return result;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE, Baseline = true)]
        public bool HasFlags_Bitwise()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= ((VALUE & FileAccess.Read) != 0);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return result;
        }


    }

    [MemoryDiagnoser]
    public class StringConcatBench
    {
        private const int ITERATIONS = 500;

        [Benchmark]
        public string StringConcat()
        {
            string s = "";
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                s += c.ToString();
            return s;
        }

        [Benchmark(Baseline = true)]
        public string StringBuilderNew()
        {
            var sb = new StringBuilder();
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCache = new StringBuilder();
        [Benchmark]
        public string StringBuilderPool()
        {
            var sb = _builderCache;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCacheLarge = new StringBuilder(1000);
        [Benchmark]
        public string StringBuilderPoolLarge()
        {
            var sb = _builderCacheLarge;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        [Benchmark]
        public string ValueStringBuilder()
        {
            Span<char> initialBuffer = stackalloc char[1000];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                accumulator.Append(c);

            var text = accumulator.AsSpan().ToString();
            accumulator.Dispose();
            return text;
        }
    }

    [MemoryDiagnoser]
    public class MultiKeyDictionaryBench
    {
        private const int COUNT = 500;

        private static readonly Dictionary<string, int> _string = GetStringDict();
        private static readonly Dictionary<Tuple<int, int>, int> _tuple = GetTupleDict();
        private static readonly Dictionary<(int, int), int> _valueTuple = GetValueTupleDict();

        private static Dictionary<string, int> GetStringDict()
        {
            var dict = new Dictionary<string, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10);
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<Tuple<int, int>, int> GetTupleDict()
        {
            var dict = new Dictionary<Tuple<int, int>, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = new Tuple<int, int>(i, i * 10);
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<(int, int), int> GetValueTupleDict()
        {
            var dict = new Dictionary<(int, int), int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = (i, i * 10);
                dict[key] = i;
            }

            return dict;
        }

        [BenchmarkCategory("Build"), Benchmark(Baseline = true)]
        public int MakeStringDictionary()
        {
            var dict = GetStringDict();
            return dict.Count;
        }

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeTupleDictionary()
        {
            var dict = GetTupleDict();
            return dict.Count;
        }

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeValueTupleDictionary()
        {
            var dict = GetValueTupleDict();
            return dict.Count;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromStringDictionary()
        {
            var dict = _string;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10);
                value = dict[key];
            }

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromTupleDictionary()
        {
            var dict = _tuple;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = new Tuple<int, int>(i, i * 10);
                value = dict[key];
            }

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromValueTupleDictionary()
        {
            var dict = _valueTuple;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = (i, i * 10);
                value = dict[key];
            }

            return value;
        }
    }

    //dotnet run -c Release --framework net472 -- --runtimes net472 netcoreapp2.2
    internal class Program
    {
        private static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
