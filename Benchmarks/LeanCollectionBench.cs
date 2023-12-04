using BenchmarkDotNet.Attributes;

using Nemesis.TextParsers.Utils;

using Input = Benchmarks.BenchmarkInput<int[]>;
// ReSharper disable CommentTypo

namespace Benchmarks
{
    /*
|       Method |               Source |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |--------------------- |----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|      LeanSum |                 [10] |  32.16 ns | 0.219 ns | 0.205 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |                 [10] |  40.82 ns | 0.261 ns | 0.218 ns |  1.27 |    0.01 | 0.0102 |     - |     - |      64 B |
| LINQ_LeanSum |                 [10] |  50.80 ns | 0.443 ns | 0.414 ns |  1.58 |    0.02 | 0.0140 |     - |     - |      88 B |
| LINQ_ListSum |                 [10] |  58.70 ns | 0.482 ns | 0.427 ns |  1.83 |    0.02 | 0.0166 |     - |     - |     104 B |
|              |                      |           |          |          |       |         |        |       |       |           |
|      LeanSum |             [20, 10] |  34.67 ns | 0.257 ns | 0.201 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |             [20, 10] |  44.24 ns | 0.479 ns | 0.424 ns |  1.28 |    0.01 | 0.0102 |     - |     - |      64 B |
| LINQ_LeanSum |             [20, 10] |  58.62 ns | 0.858 ns | 0.802 ns |  1.68 |    0.02 | 0.0139 |     - |     - |      88 B |
| LINQ_ListSum |             [20, 10] |  66.64 ns | 1.145 ns | 1.071 ns |  1.93 |    0.04 | 0.0166 |     - |     - |     104 B |
|              |                      |           |          |          |       |         |        |       |       |           |
|      LeanSum |         [30, 20, 10] |  36.45 ns | 0.413 ns | 0.366 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |         [30, 20, 10] |  47.11 ns | 0.506 ns | 0.473 ns |  1.29 |    0.02 | 0.0114 |     - |     - |      72 B |
| LINQ_LeanSum |         [30, 20, 10] |  66.80 ns | 1.264 ns | 1.242 ns |  1.83 |    0.05 | 0.0139 |     - |     - |      88 B |
| LINQ_ListSum |         [30, 20, 10] |  72.76 ns | 0.663 ns | 0.620 ns |  2.00 |    0.04 | 0.0178 |     - |     - |     112 B |
|              |                      |           |          |          |       |         |        |       |       |           |
|      LeanSum | [90, (...), 10] [36] |  48.24 ns | 0.505 ns | 0.447 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum | [90, (...), 10] [36] |  63.90 ns | 0.729 ns | 0.682 ns |  1.33 |    0.01 | 0.0153 |     - |     - |      96 B |
| LINQ_LeanSum | [90, (...), 10] [36] | 111.62 ns | 1.157 ns | 1.082 ns |  2.31 |    0.02 | 0.0139 |     - |     - |      88 B |
| LINQ_ListSum | [90, (...), 10] [36] | 119.91 ns | 0.479 ns | 0.424 ns |  2.49 |    0.02 | 0.0216 |     - |     - |     136 B |
|              |                      |           |          |          |       |         |        |       |       |           |
|      LeanSum | [140,(...), 10] [61] |  58.87 ns | 0.195 ns | 0.163 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum | [140,(...), 10] [61] |  68.80 ns | 0.464 ns | 0.434 ns |  1.17 |    0.01 | 0.0178 |     - |     - |     112 B |
| LINQ_LeanSum | [140,(...), 10] [61] | 135.45 ns | 1.977 ns | 1.544 ns |  2.30 |    0.03 | 0.0138 |     - |     - |      88 B |
| LINQ_ListSum | [140,(...), 10] [61] | 153.95 ns | 2.360 ns | 2.208 ns |  2.62 |    0.04 | 0.0241 |     - |     - |     152 B |
*/
    [MemoryDiagnoser/*, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)*/]
    public class LeanCollectionSum
    {
        [ParamsSource(nameof(Sources))]
        public Input Source { get; set; }

        public IEnumerable<Input> Sources => new[]
        {
            new Input([10]),
            new Input([20, 10]),
            new Input([30, 20, 10]),
            new Input([90, 80, 70, 60, 50, 40, 30, 20, 10]),
            new Input([140, 130, 120, 110, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10]),
        };


        /*[BenchmarkCategory("Sum"), Benchmark]
        public int NativeSum()
        {
            int sum = 0;
            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < Source.Value.Length; i++)
                sum += Source[i];
            return sum;
        }*/

        [BenchmarkCategory("Sum"), Benchmark(Baseline = true)]
        public int LeanSum()
        {
            var coll = LeanCollectionFactory.FromArray(Source.Value);

            var sum = 0;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var i in coll)
                sum += i;

            return sum;
        }

        /*[BenchmarkCategory("Sum"), Benchmark]
        public int LeanSumIEnumerable()
        {
            var coll = LeanCollectionFactory.FromArray(Source.Value);

            using var enumerator = ((IEnumerable<int>)coll).GetEnumerator();
            if (!enumerator.MoveNext()) return 0;

            int sum = 0;
            do
                sum += enumerator.Current;
            while (enumerator.MoveNext());

            return sum;
        }*/

        [BenchmarkCategory("Sum"), Benchmark]
        public int ListSum()
        {
            var coll = new List<int>(Source.Value);

            int sum = 0;
            foreach (int i in coll)
                sum += i;

            return sum;
        }

        [BenchmarkCategory("Sum"), Benchmark]
        public int LINQ_LeanSum()
        {
            var coll = LeanCollectionFactory.FromArray(Source.Value);

            return coll.Sum();
        }

        [BenchmarkCategory("Sum"), Benchmark]
        public int LINQ_ListSum()
        {
            var coll = new List<int>(Source.Value);

            return coll.Sum();
        }
    }

    /*
|   Method |               Source |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------- |--------------------- |----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
| LeanSort |                 [10] |  24.58 ns | 0.148 ns | 0.138 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| ListSort |                 [10] |  37.50 ns | 0.327 ns | 0.306 ns |  1.53 |    0.01 | 0.0102 |     - |     - |      64 B |
|          |                      |           |          |          |       |         |        |       |       |           |
| LeanSort |             [20, 10] |  28.85 ns | 0.138 ns | 0.129 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| ListSort |             [20, 10] |  56.42 ns | 0.252 ns | 0.210 ns |  1.96 |    0.01 | 0.0102 |     - |     - |      64 B |
|          |                      |           |          |          |       |         |        |       |       |           |
| LeanSort |         [30, 20, 10] |  34.90 ns | 0.158 ns | 0.132 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| ListSort |         [30, 20, 10] |  60.11 ns | 0.407 ns | 0.381 ns |  1.72 |    0.01 | 0.0114 |     - |     - |      72 B |
|          |                      |           |          |          |       |         |        |       |       |           |
| LeanSort | [90, (...), 10] [36] |  64.10 ns | 0.938 ns | 1.042 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| ListSort | [90, (...), 10] [36] | 108.40 ns | 2.187 ns | 2.246 ns |  1.69 |    0.05 | 0.0153 |     - |     - |      96 B |
|          |                      |           |          |          |       |         |        |       |       |           |
| LeanSort | [140,(...), 10] [61] |  70.24 ns | 0.662 ns | 0.587 ns |  1.00 |    0.00 |      - |     - |     - |         - |
| ListSort | [140,(...), 10] [61] | 150.32 ns | 1.558 ns | 1.458 ns |  2.14 |    0.03 | 0.0176 |     - |     - |     112 B |
     */
    [MemoryDiagnoser]
    public class LeanCollectionSort
    {
        [ParamsSource(nameof(Sources))]
        public Input Source { get; set; }

        public IEnumerable<Input> Sources => new[]
        {
            new Input(new[] {10}),
            new Input(new[] {20, 10}),
            new Input(new[] {30, 20, 10}),
            new Input(new[] {90, 80, 70, 60, 50, 40, 30, 20, 10}),
            new Input(new[] {140, 130, 120, 110, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10}),
        };


        [BenchmarkCategory("Sort"), Benchmark(Baseline = true)]
        public int LeanSort()
        {
            var coll = LeanCollectionFactory.FromArray(Source.Value);

            coll = ((IListOperations<int>)coll).Sort();

            return coll.Size;
        }

        [BenchmarkCategory("Sort"), Benchmark]
        public int ListSort()
        {
            var coll = new List<int>(Source.Value);

            coll.Sort();

            return coll.Count;
        }
    }
}
