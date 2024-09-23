namespace Benchmarks.BestPractices;

/*
| Method         | Runtime  | Mean     | Error     | StdDev    | Ratio    | Gen0   | Allocated |
|--------------- |--------- |---------:|----------:|----------:|---------:|-------:|----------:|-
| UnBoxing       | .NET 6.0 | 2.931 us | 0.0568 us | 0.0584 us | baseline | 3.8261 |   24024 B |
| UnBoxingUnsafe | .NET 6.0 | 1.248 us | 0.0046 us | 0.0041 us |     -57% | 0.0038 |      24 B |
|                |          |          |           |           |          |        |           |
| UnBoxing       | .NET 8.0 | 2.961 us | 0.0547 us | 0.0427 us | baseline | 3.8261 |   24024 B |
| UnBoxingUnsafe | .NET 8.0 | 1.236 us | 0.0235 us | 0.0289 us |     -58% | 0.0038 |      24 B |
  */
public class BoxingUnsafe
{
    private const int SIZE = 1000;

    [Benchmark(Baseline = true)]
    public object UnBoxing()
    {
        object obj = 0;
        for (int i = 0; i < SIZE; i++)
            Inc((int)obj);

        return obj;

        static object Inc(int x) => ++x;
    }


    [Benchmark]
    public object UnBoxingUnsafe()
    {
        object obj = 0;
        for (int i = 0; i < SIZE; i++)
            Unsafe.Unbox<int>(obj)++;

        return obj;
    }
}