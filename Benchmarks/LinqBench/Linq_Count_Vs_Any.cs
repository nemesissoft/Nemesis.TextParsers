namespace Benchmarks;

/*BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.19044.2486/21H2/November2021Update)
Intel Core i7-8700 CPU 3.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2

|           Method | Size |       Mean |     Error |    StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|----------------- |----- |-----------:|----------:|----------:|---------:|--------:|-------:|----------:|------------:|
|      Count_Array |   10 |   8.012 ns | 0.0932 ns | 0.0872 ns | baseline |         |      - |         - |          NA |
|        Any_Array |   10 |   8.714 ns | 0.0505 ns | 0.0472 ns |      +9% |    1.2% |      - |         - |          NA |
|                  |      |            |           |           |          |         |        |           |             |
| Count_Enumerable |   10 |  49.501 ns | 0.3702 ns | 0.3282 ns | baseline |         | 0.0063 |      40 B |             |
|   Any_Enumerable |   10 |  19.042 ns | 0.0860 ns | 0.0763 ns |     -62% |    0.7% | 0.0063 |      40 B |         +0% |
|                  |      |            |           |           |          |         |        |           |             |
|                  |      |            |           |           |          |         |        |           |             |
|      Count_Array |  100 |   8.632 ns | 0.0676 ns | 0.0633 ns | baseline |         |      - |         - |          NA |
|        Any_Array |  100 |   8.064 ns | 0.0697 ns | 0.0582 ns |      -7% |    0.7% |      - |         - |          NA |
|                  |      |            |           |           |          |         |        |           |             |
| Count_Enumerable |  100 | 289.673 ns | 4.8053 ns | 4.2598 ns | baseline |         | 0.0062 |      40 B |             |
|   Any_Enumerable |  100 |  19.083 ns | 0.1910 ns | 0.1693 ns |     -93% |    1.7% | 0.0063 |      40 B |         +0% |
*/
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class Linq_Count_Vs_Any
{
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
    public bool Length_Array() => _data.Length > 0;

    [BenchmarkCategory("Array"), Benchmark]
    public bool Count_Array() => _data.Count() > 0;

    [BenchmarkCategory("Array"), Benchmark]
    public bool Any_Array() => _data.Any();



    [BenchmarkCategory("Enumerable"), Benchmark(Baseline = true)]
    public bool Count_Enumerable() => GetEnumerable().Count() > 0;

    [BenchmarkCategory("Enumerable"), Benchmark]
    public bool Any_Enumerable() => GetEnumerable().Any();
}


/*
|             Method | Size |       Mean |     Error |    StdDev |    Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|------------------- |----- |-----------:|----------:|----------:|---------:|--------:|-------:|----------:|------------:|
|      Iterate_Array |   10 |   3.764 ns | 0.0494 ns | 0.0438 ns | baseline |         |      - |         - |          NA |
|        Count_Array |   10 |  75.808 ns | 0.5585 ns | 0.5224 ns |  +1,912% |    1.0% | 0.0191 |     120 B |          NA |
|          Any_Array |   10 |  56.647 ns | 0.6287 ns | 0.4909 ns |  +1,405% |    1.3% | 0.0191 |     120 B |          NA |
|                    |      |            |           |           |          |         |        |           |             |
| Iterate_Enumerable |   10 |  41.854 ns | 0.3089 ns | 0.2739 ns | baseline |         | 0.0063 |      40 B |             |
|   Count_Enumerable |   10 |  82.023 ns | 0.3597 ns | 0.3365 ns |     +96% |    0.7% | 0.0204 |     128 B |       +220% |
|     Any_Enumerable |   10 |  58.203 ns | 0.3344 ns | 0.2964 ns |     +39% |    0.8% | 0.0204 |     128 B |       +220% |
|                    |      |            |           |           |          |         |        |           |             |
|                    |      |            |           |           |          |         |        |           |             |
|      Iterate_Array |  100 |  25.661 ns | 0.1365 ns | 0.1277 ns | baseline |         |      - |         - |          NA |
|        Count_Array |  100 | 559.625 ns | 3.3309 ns | 2.9528 ns |  +2,082% |    0.7% | 0.0191 |     120 B |          NA |
|          Any_Array |  100 | 309.281 ns | 1.5064 ns | 1.2579 ns |  +1,107% |    0.6% | 0.0191 |     120 B |          NA |
|                    |      |            |           |           |          |         |        |           |             |
| Iterate_Enumerable |  100 | 240.658 ns | 1.6646 ns | 1.4757 ns | baseline |         | 0.0062 |      40 B |             |
|   Count_Enumerable |  100 | 608.032 ns | 3.2091 ns | 3.0018 ns |    +153% |    0.7% | 0.0200 |     128 B |       +220% |
|     Any_Enumerable |  100 | 309.636 ns | 2.7584 ns | 2.5802 ns |     +29% |    0.9% | 0.0200 |     128 B |       +220% |
*/
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class Linq_Count_Vs_Any_WithPredicate
{
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
    public bool Iterate_Array()
    {
        var midPoint = Size / 2;
        for (int i = 0; i < _data.Length; i++)
        {
            if (_data[i] > midPoint)
                return true;
        }
        return false;
    }

    [BenchmarkCategory("Array"), Benchmark]
    public bool Count_Array()
    {
        var midPoint = Size / 2;
        return _data.Count(i => i > midPoint) > 0;
    }

    [BenchmarkCategory("Array"), Benchmark]
    public bool Any_Array()
    {
        var midPoint = Size / 2;
        return _data.Any(i => i > midPoint);
    }




    [BenchmarkCategory("Enumerable"), Benchmark(Baseline = true)]
    public bool Iterate_Enumerable()
    {
        var midPoint = Size / 2;
        foreach (var item in GetEnumerable())
        {
            if (item > midPoint)
                return true;
        }
        return false;
    }
    [BenchmarkCategory("Enumerable"), Benchmark]
    public bool Count_Enumerable()
    {
        var midPoint = Size / 2;
        return GetEnumerable().Count(i => i > midPoint) > 0;
    }

    [BenchmarkCategory("Enumerable"), Benchmark]
    public bool Any_Enumerable()
    {
        var midPoint = Size / 2;
        return GetEnumerable().Any(i => i > midPoint);
    }
}
