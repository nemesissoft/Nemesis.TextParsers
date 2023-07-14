using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Nemesis.TextParsers;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ArrayParserBench
    {
        private readonly ITransformer<int> _intTransformer = TextTransformer.Default.GetTransformer<int>();
        private readonly ITransformer<int[]> _intArrayTransformer = TextTransformer.Default.GetTransformer<int[]>();
        
        public readonly struct TestData
        {
            public ushort Capacity { get; }
            public string Text { get; }

            public TestData(ushort capacity, string text)
            {
                Capacity = capacity;
                Text = text;
            }

            public static TestData FromCapacity(ushort capacity) => new(
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
            int[] parsed = _intArrayTransformer.Parse(Data.Text.AsSpan());
            return parsed[^1];
        }

        [Benchmark(Baseline = true)]
        public int ToArrayStack_Test()
        {
            if (Data.Capacity > 128)
                throw new NotSupportedException("Not supported");

            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

            Span<int> parsed = stackalloc int[Data.Capacity];
            int i = 0;
            foreach (var num in stream)
                parsed[i++] = num.ParseWith(_intTransformer);

            return parsed[^1];
        }

        [Benchmark]
        public int Mul_ToArray_Test()
        {
            int[] parsed = _intArrayTransformer.Parse(Data.Text.AsSpan());

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

            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

            Span<int> parsed = stackalloc int[Data.Capacity];
            int i = 0;
            foreach (var num in stream)
                parsed[i++] = num.ParseWith(_intTransformer);

            int result = 0;
            foreach (int num in parsed)
                result = unchecked(result * num);

            return result;
        }

        [Benchmark]
        public int Mul_Optimized()
        {
            var stream = Data.Text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

            int result = 0;
            foreach (var num in stream)
                result = unchecked(result * num.ParseWith(_intTransformer));

            return result;
        }

    }
}
