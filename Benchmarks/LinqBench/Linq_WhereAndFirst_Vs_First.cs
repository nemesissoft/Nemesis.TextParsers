using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;

namespace Benchmarks;

/*BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.19044.2486/21H2/November2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2


|                   Method | Size |      Mean |    Error |   StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|------------------------- |----- |----------:|---------:|---------:|---------:|--------:|-------:|----------:|------------:|
|      WhereAndFirst_Array |   10 |  47.85 ns | 0.975 ns | 1.301 ns | baseline |         | 0.0216 |     136 B |             |
|              First_Array |   10 |  54.37 ns | 0.243 ns | 0.203 ns |     +15% |    2.7% | 0.0191 |     120 B |        -12% |
|                          |      |           |          |          |          |         |        |           |             |
| WhereAndFirst_Enumerable |   10 |  93.61 ns | 1.621 ns | 1.437 ns | baseline |         | 0.0293 |     184 B |             |
|         First_Enumerable |   10 |  63.95 ns | 1.240 ns | 1.273 ns |     -32% |    3.0% | 0.0204 |     128 B |        -30% |
|                          |      |           |          |          |          |         |        |           |             |
|                          |      |           |          |          |          |         |        |           |             |
|      WhereAndFirst_Array |  100 | 120.60 ns | 1.067 ns | 0.945 ns | baseline |         | 0.0215 |     136 B |             |
|              First_Array |  100 | 293.09 ns | 1.261 ns | 1.180 ns |    +143% |    0.8% | 0.0191 |     120 B |        -12% |
|                          |      |           |          |          |          |         |        |           |             |
| WhereAndFirst_Enumerable |  100 | 354.49 ns | 4.052 ns | 3.790 ns | baseline |         | 0.0291 |     184 B |             |
|         First_Enumerable |  100 | 332.65 ns | 1.550 ns | 1.294 ns |      -6% |    1.1% | 0.0200 |     128 B |        -30% |*/
[MemoryDiagnoser]
//[SimpleJob(RuntimeMoniker.Net70)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Config(typeof(Config))]
public class Linq_WhereAndFirst_Vs_First
{
    private class Config : ManualConfig
    {
        public Config() =>
            SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
    }

    private int[] _data;

    [Params(10, 100)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup() => _data = Enumerable.Range(0, Size).ToArray();

    private IEnumerable<int> GetEnumerable()
    {
        for (int i = 0; i < _data.Length; i++)
            yield return _data[i];
    }

    [BenchmarkCategory("Array"), Benchmark(Baseline = true)]
    public int WhereAndFirst_Array()
    {
        var midPoint = Size / 2;
        return _data.Where(i => i > midPoint).First();
    }

    [BenchmarkCategory("Array"), Benchmark]
    public int First_Array()
    {
        var midPoint = Size / 2;
        return _data.First(i => i > midPoint);
    }

    [BenchmarkCategory("Enumerable"), Benchmark(Baseline = true)]
    public int WhereAndFirst_Enumerable()
    {
        var midPoint = Size / 2;
        return GetEnumerable().Where(i => i > midPoint).First();
    }

    [BenchmarkCategory("Enumerable"), Benchmark]
    public int First_Enumerable()
    {
        var midPoint = Size / 2;
        return GetEnumerable().First(i => i > midPoint);
    }
}

