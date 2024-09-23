namespace Benchmarks.BestPractices;

/*
| Method   | Runtime  | Mean     | Error    | StdDev   | Ratio    | Allocated |
|--------- |--------- |---------:|---------:|---------:|---------:|----------:|-
| Nullable | .NET 6.0 | 493.3 ns |  9.84 ns |  9.67 ns | baseline |         - |
| Int      | .NET 6.0 | 244.6 ns |  4.08 ns |  3.62 ns |     -50% |         - |
|          |          |          |          |          |          |           |
| Nullable | .NET 8.0 | 730.6 ns | 13.59 ns | 25.52 ns | baseline |         - |
| Int      | .NET 8.0 | 243.7 ns |  3.84 ns |  4.72 ns |     -67% |         - |
  */
public class NullableBench
{
    private const int SIZE = 1000;

    [Benchmark(Baseline = true)]
    public int Nullable()
    {
        int? x = 0;
        for (int i = 0; i < SIZE; i++)
            x++;
        return x.Value;
    }


    [Benchmark]
    public int Int()
    {
        int x = 0;
        for (int i = 0; i < SIZE; i++)
            x++;
        return x;
    }
}