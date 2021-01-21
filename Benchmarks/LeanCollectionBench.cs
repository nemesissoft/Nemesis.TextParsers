using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

using Nemesis.TextParsers.Utils;

using Input = Benchmarks.BenchmarkInput<int[]>;
// ReSharper disable CommentTypo

namespace Benchmarks
{
    /*
|       Method |               Source |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |--------------------- |----------:|---------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|      LeanSum |                 [10] |  31.98 ns | 0.236 ns | 0.221 ns |  31.94 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |                 [10] |  41.58 ns | 0.378 ns | 0.353 ns |  41.69 ns |  1.30 |    0.01 | 0.0102 |     - |     - |      64 B |
| LINQ_LeanSum |                 [10] |  43.40 ns | 0.425 ns | 0.377 ns |  43.40 ns |  1.36 |    0.01 | 0.0166 |     - |     - |     104 B |
| LINQ_ListSum |                 [10] |  62.02 ns | 0.481 ns | 0.449 ns |  62.02 ns |  1.94 |    0.02 | 0.0166 |     - |     - |     104 B |
|              |                      |           |          |          |           |       |         |        |       |       |           |
|      LeanSum |             [20, 10] |  36.08 ns | 0.741 ns | 1.580 ns |  35.14 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |             [20, 10] |  43.50 ns | 0.523 ns | 0.489 ns |  43.55 ns |  1.21 |    0.05 | 0.0102 |     - |     - |      64 B |
| LINQ_LeanSum |             [20, 10] |  48.90 ns | 0.743 ns | 0.658 ns |  48.98 ns |  1.36 |    0.06 | 0.0166 |     - |     - |     104 B |
| LINQ_ListSum |             [20, 10] |  66.69 ns | 0.956 ns | 0.894 ns |  66.37 ns |  1.86 |    0.07 | 0.0166 |     - |     - |     104 B |
|              |                      |           |          |          |           |       |         |        |       |       |           |
|      LeanSum |         [30, 20, 10] |  36.84 ns | 0.373 ns | 0.349 ns |  36.73 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum |         [30, 20, 10] |  45.82 ns | 0.260 ns | 0.231 ns |  45.78 ns |  1.24 |    0.01 | 0.0114 |     - |     - |      72 B |
| LINQ_LeanSum |         [30, 20, 10] |  54.24 ns | 0.626 ns | 0.522 ns |  54.32 ns |  1.47 |    0.02 | 0.0166 |     - |     - |     104 B |
| LINQ_ListSum |         [30, 20, 10] |  77.78 ns | 1.558 ns | 1.457 ns |  77.47 ns |  2.11 |    0.04 | 0.0178 |     - |     - |     112 B |
|              |                      |           |          |          |           |       |         |        |       |       |           |
|      LeanSum | [90, (...), 10] [36] |  48.62 ns | 0.417 ns | 0.370 ns |  48.60 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum | [90, (...), 10] [36] |  64.53 ns | 0.833 ns | 0.738 ns |  64.57 ns |  1.33 |    0.02 | 0.0153 |     - |     - |      96 B |
| LINQ_LeanSum | [90, (...), 10] [36] |  84.96 ns | 1.711 ns | 4.196 ns |  83.00 ns |  1.78 |    0.09 | 0.0166 |     - |     - |     104 B |
| LINQ_ListSum | [90, (...), 10] [36] | 123.98 ns | 2.495 ns | 6.829 ns | 122.07 ns |  2.64 |    0.15 | 0.0215 |     - |     - |     136 B |
|              |                      |           |          |          |           |       |         |        |       |       |           |
|      LeanSum | [140,(...), 10] [61] |  60.61 ns | 1.226 ns | 1.412 ns |  60.02 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|      ListSum | [140,(...), 10] [61] |  76.78 ns | 0.737 ns | 0.653 ns |  76.70 ns |  1.26 |    0.03 | 0.0178 |     - |     - |     112 B |
| LINQ_LeanSum | [140,(...), 10] [61] | 109.93 ns | 1.053 ns | 0.985 ns | 109.40 ns |  1.81 |    0.05 | 0.0166 |     - |     - |     104 B |
| LINQ_ListSum | [140,(...), 10] [61] | 151.92 ns | 1.581 ns | 1.320 ns | 151.91 ns |  2.49 |    0.07 | 0.0241 |     - |     - |     152 B |
*/
    [MemoryDiagnoser/*, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)*/]
    public class LeanCollectionSum
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

            /*var enumerator = coll.GetEnumerator();
            if (!enumerator.MoveNext()) return 0;

            int sum = 0;
            do
                sum += enumerator.Current;
            while (enumerator.MoveNext());*/

            return sum;
        }

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

            coll = coll.Sort();

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
