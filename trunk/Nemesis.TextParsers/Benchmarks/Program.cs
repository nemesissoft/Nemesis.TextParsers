using System;
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Nemesis.TextParsers;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    //[HardwareCounters(HardwareCounter.BranchMispredictions,HardwareCounter.BranchInstructions, HardwareCounter.Timer)]
    //[InliningDiagnoser]
    //[TailCallDiagnoser]
    //[EtwProfiler]
    //[ConcurrencyVisualizerProfiler]
    public class CollectionParserBench
    {
        /*[Params(
            "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20",
            "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200|201|202|203|204|205|206|207|208|209|210|211|212|213|214|215|216|217|218|219|220|221|222|223|224|225|226|227|228|229|230|231|232|233|234|235|236|237|238|239|240|241|242|243|244|245|246|247|248|249|250|251|252|253|254|255|256|257|258|259|260|261|262|263|264|265|266|267|268|269|270|271|272|273|274|275|276|277|278|279|280|281|282|283|284|285|286|287|288|289|290|291|292|293|294|295|296|297|298|299|300|301|302|303|304|305|306|307|308|309|310|311|312|313|314|315|316|317|318|319|320|321|322|323|324|325|326|327|328|329|330|331|332|333|334|335|336|337|338|339|340|341|342|343|344|345|346|347|348|349|350|351|352|353|354|355|356|357|358|359|360|361|362|363|364|365|366|367|368|369|370|371|372|373|374|375|376|377|378|379|380|381|382|383|384|385|386|387|388|389|390|391|392|393|394|395|396|397|398|399|400|401|402|403|404|405|406|407|408|409|410|411|412|413|414|415|416|417|418|419|420|421|422|423|424|425|426|427|428|429|430|431|432|433|434|435|436|437|438|439|440|441|442|443|444|445|446|447|448|449|450|451|452|453|454|455|456|457|458|459|460|461|462|463|464|465|466|467|468|469|470|471|472|473|474|475|476|477|478|479|480|481|482|483|484|485|486|487|488|489|490|491|492|493|494|495|496|497|498|499|500|501|502|503|504|505|506|507|508|509|510|511|512|513|514|515|516|517|518|519|520|521|522|523|524|525|526|527|528|529|530|531|532|533|534|535|536|537|538|539|540|541|542|543|544|545|546|547|548|549|550|551|552|553|554|555|556|557|558|559|560|561|562|563|564|565|566|567|568|569|570|571|572|573|574|575|576|577|578|579|580|581|582|583|584|585|586|587|588|589|590|591|592|593|594|595|596|597|598|599|600|601|602|603|604|605|606|607|608|609|610|611|612|613|614|615|616|617|618|619|620|621|622|623|624|625|626|627|628|629|630|631|632|633|634|635|636|637|638|639|640|641|642|643|644|645|646|647|648|649|650|651|652|653|654|655|656|657|658|659|660|661|662|663|664|665|666|667|668|669|670|671|672|673|674|675|676|677|678|679|680|681|682|683|684|685|686|687|688|689|690|691|692|693|694|695|696|697|698|699|700|701|702|703|704|705|706|707|708|709|710|711|712|713|714|715|716|717|718|719|720|721|722|723|724|725|726|727|728|729|730|731|732|733|734|735|736|737|738|739|740|741|742|743|744|745|746|747|748|749|750|751|752|753|754|755|756|757|758|759|760|761|762|763|764|765|766|767|768|769|770|771|772|773|774|775|776|777|778|779|780|781|782|783|784|785|786|787|788|789|790|791|792|793|794|795|796|797|798|799|800|801|802|803|804|805|806|807|808|809|810|811|812|813|814|815|816|817|818|819|820|821|822|823|824|825|826|827|828|829|830|831|832|833|834|835|836|837|838|839|840|841|842|843|844|845|846|847|848|849|850|851|852|853|854|855|856|857|858|859|860|861|862|863|864|865|866|867|868|869|870|871|872|873|874|875|876|877|878|879|880|881|882|883|884|885|886|887|888|889|890|891|892|893|894|895|896|897|898|899|900|901|902|903|904|905|906|907|908|909|910|911|912|913|914|915|916|917|918|919|920|921|922|923|924|925|926|927|928|929|930|931|932|933|934|935|936|937|938|939|940|941|942|943|944|945|946|947|948|949|950|951|952|953|954|955|956|957|958|959|960|961|962|963|964|965|966|967|968|969|970|971|972|973|974|975|976|977|978|979|980|981|982|983|984|985|986|987|988|989|990|991|992|993|994|995|996|997|998|999|1000"
        )]*/
        public string Numbers { get; set; } =
            "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50|51|52|53|54|55|56|57|58|59|60|61|62|63|64|65|66|67|68|69|70|71|72|73|74|75|76|77|78|79|80|81|82|83|84|85|86|87|88|89|90|91|92|93|94|95|96|97|98|99|100|101|102|103|104|105|106|107|108|109|110|111|112|113|114|115|116|117|118|119|120|121|122|123|124|125|126|127|128|129|130|131|132|133|134|135|136|137|138|139|140|141|142|143|144|145|146|147|148|149|150|151|152|153|154|155|156|157|158|159|160|161|162|163|164|165|166|167|168|169|170|171|172|173|174|175|176|177|178|179|180|181|182|183|184|185|186|187|188|189|190|191|192|193|194|195|196|197|198|199|200|201|202|203|204|205|206|207|208|209|210|211|212|213|214|215|216|217|218|219|220|221|222|223|224|225|226|227|228|229|230|231|232|233|234|235|236|237|238|239|240|241|242|243|244|245|246|247|248|249|250|251|252|253|254|255|256|257|258|259|260|261|262|263|264|265|266|267|268|269|270|271|272|273|274|275|276|277|278|279|280|281|282|283|284|285|286|287|288|289|290|291|292|293|294|295|296|297|298|299|300|301|302|303|304|305|306|307|308|309|310|311|312|313|314|315|316|317|318|319|320|321|322|323|324|325|326|327|328|329|330|331|332|333|334|335|336|337|338|339|340|341|342|343|344|345|346|347|348|349|350|351|352|353|354|355|356|357|358|359|360|361|362|363|364|365|366|367|368|369|370|371|372|373|374|375|376|377|378|379|380|381|382|383|384|385|386|387|388|389|390|391|392|393|394|395|396|397|398|399|400|401|402|403|404|405|406|407|408|409|410|411|412|413|414|415|416|417|418|419|420|421|422|423|424|425|426|427|428|429|430|431|432|433|434|435|436|437|438|439|440|441|442|443|444|445|446|447|448|449|450|451|452|453|454|455|456|457|458|459|460|461|462|463|464|465|466|467|468|469|470|471|472|473|474|475|476|477|478|479|480|481|482|483|484|485|486|487|488|489|490|491|492|493|494|495|496|497|498|499|500|501|502|503|504|505|506|507|508|509|510|511|512|513|514|515|516|517|518|519|520|521|522|523|524|525|526|527|528|529|530|531|532|533|534|535|536|537|538|539|540|541|542|543|544|545|546|547|548|549|550|551|552|553|554|555|556|557|558|559|560|561|562|563|564|565|566|567|568|569|570|571|572|573|574|575|576|577|578|579|580|581|582|583|584|585|586|587|588|589|590|591|592|593|594|595|596|597|598|599|600|601|602|603|604|605|606|607|608|609|610|611|612|613|614|615|616|617|618|619|620|621|622|623|624|625|626|627|628|629|630|631|632|633|634|635|636|637|638|639|640|641|642|643|644|645|646|647|648|649|650|651|652|653|654|655|656|657|658|659|660|661|662|663|664|665|666|667|668|669|670|671|672|673|674|675|676|677|678|679|680|681|682|683|684|685|686|687|688|689|690|691|692|693|694|695|696|697|698|699|700|701|702|703|704|705|706|707|708|709|710|711|712|713|714|715|716|717|718|719|720|721|722|723|724|725|726|727|728|729|730|731|732|733|734|735|736|737|738|739|740|741|742|743|744|745|746|747|748|749|750|751|752|753|754|755|756|757|758|759|760|761|762|763|764|765|766|767|768|769|770|771|772|773|774|775|776|777|778|779|780|781|782|783|784|785|786|787|788|789|790|791|792|793|794|795|796|797|798|799|800|801|802|803|804|805|806|807|808|809|810|811|812|813|814|815|816|817|818|819|820|821|822|823|824|825|826|827|828|829|830|831|832|833|834|835|836|837|838|839|840|841|842|843|844|845|846|847|848|849|850|851|852|853|854|855|856|857|858|859|860|861|862|863|864|865|866|867|868|869|870|871|872|873|874|875|876|877|878|879|880|881|882|883|884|885|886|887|888|889|890|891|892|893|894|895|896|897|898|899|900|901|902|903|904|905|906|907|908|909|910|911|912|913|914|915|916|917|918|919|920|921|922|923|924|925|926|927|928|929|930|931|932|933|934|935|936|937|938|939|940|941|942|943|944|945|946|947|948|949|950|951|952|953|954|955|956|957|958|959|960|961|962|963|964|965|966|967|968|969|970|971|972|973|974|975|976|977|978|979|980|981|982|983|984|985|986|987|988|989|990|991|992|993|994|995|996|997|998|999|1000";

        private readonly CollectionSerializerSlow _collSer = CollectionSerializerSlow.DefaultInstance;

        public CollectionParserBench() => SpanSplitTest();

        [Benchmark]
        public int CollectionSerializerTest()
        {
            int result = 0;
            var parsed = _collSer.ParseCollection<int>(Numbers);
            foreach (int num in parsed)
                result += (int)num;

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
        public int StringSplitTestAesWay()
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
        public int StringSplitTestAesWayOpt()
        {
            int result = 0;
            var parsed = Numbers.Split('|');

            var conv = TypeDescriptor.GetConverter(typeof(int));

            foreach (string text in parsed)
                result += (int)conv.ConvertFrom(text);

            return result;
        }

        [Benchmark(Baseline = true)]
        public int SpanSplitTest()
        {
            int result = 0;
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            foreach (int num in parsed) result += num;

            return result;
        }

        [Benchmark]
        public int SpanSplitTest_Alloc()
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
    }

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
        private const int POTENTIAL_LENGTH = 50;

        public string Numbers { get; set; } =
            "1|2|3|4|5|6|7|8|9|10|11|12|13|14|15|16|17|18|19|20|21|22|23|24|25|26|27|28|29|30|31|32|33|34|35|36|37|38|39|40|41|42|43|44|45|46|47|48|49|50";

        [Benchmark]
        public int[] ToArray_Test()
        {
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            return parsed.ToArray(POTENTIAL_LENGTH);
        }

        [Benchmark(Baseline = true)]
        public int[] ToArrayUnmanaged_Test()
        {
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            return parsed.ToArrayUnmanaged(POTENTIAL_LENGTH);
        }

        [Benchmark]
        public int Sum_ToArray_Test()
        {
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            var array = parsed.ToArray(POTENTIAL_LENGTH);

            int result = 0;
            foreach (int num in array)
                result = unchecked(result * num);

            return result;
        }

        [Benchmark]
        public int Sum_ToArrayUnmanaged_Test()
        {
            var parsed = Numbers.AsSpan().Tokenize('|', '\\', true)
                    .Parse<int>('\\', '∅')
                ;

            var array = parsed.ToArrayUnmanaged(POTENTIAL_LENGTH);

            int result = 0;
            foreach (int num in array)
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
            Enumerable.Range(0, 130).Select(i => ((DaysOfWeek)i).ToString("G").Replace(" ","")).ToArray();

        private static readonly EnumTransformer<DaysOfWeek, byte, ByteNumber> _parser = new EnumTransformer<DaysOfWeek, byte, ByteNumber>(new ByteNumber());

        static EnumParserBench()
        {
            var tt = _parser.Parse("10".AsSpan());
        }

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

            var enumStream = input.Split(',', false).GetEnumerator();

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

            return IsNumeric(input)&& byte.TryParse(
#if NET472
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

            ;
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

        /*[Benchmark]
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
        }*/
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            /*new BenchmarkSwitcher(new[]
            {
                typeof(EnumParserBench),
                typeof(ArrayParserBench),
                typeof(CollectionParserBench),
                typeof(AggressionBasedBench),
            }).Run(new[] { "*" });*/
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
