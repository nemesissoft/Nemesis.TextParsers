namespace Benchmarks.BestPractices;

/*
| Method | Runtime  | Mean       | Error    | StdDev    | Ratio    | Gen0   | Allocated |
|------- |--------- |-----------:|---------:|----------:|---------:|-------:|----------:|
| Boxing | .NET 6.0 | 2,881.4 ns | 55.29 ns |  71.89 ns | baseline | 3.8261 |   24024 B |
| Int    | .NET 6.0 |   244.8 ns |  3.10 ns |   2.75 ns |     -92% | 0.0038 |      24 B |
|        |          |            |          |           |          |        |           |
| Boxing | .NET 8.0 | 2,946.5 ns | 56.35 ns | 155.20 ns | baseline | 3.8261 |   24024 B |
| Int    | .NET 8.0 |   248.8 ns |  4.66 ns |   4.13 ns |     -92% | 0.0038 |      24 B |
  */
public class BoxingBench
{
    private const int SIZE = 1000;

    [Benchmark(Baseline = true)]
    public object Boxing()
    {
        int x = 0;
        object obj = x;
        for (int i = 0; i < SIZE; i++)
        {
            x++;
            obj = x;
        }
        return obj;
    }


    [Benchmark]
    public object Int()
    {
        int x = 0;
        object obj = x;
        for (int i = 0; i < SIZE; i++)
        {
            x++;
        }
        return obj;
    }
}