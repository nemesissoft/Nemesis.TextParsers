using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Nemesis.TextParsers.Utils;

// ReSharper disable CommentTypo

namespace Benchmarks
{
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
    public class LeanCollectionBench
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
            var coll = LeanCollectionFactory.FromArray(Source);

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
            var coll = LeanCollectionFactory.FromArray(Source);

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
}
