using System.Runtime.InteropServices;

namespace Benchmarks.BestPractices;

/*
| Method      | Runtime  | Mean     | Error   | StdDev  | Ratio    | Allocated |
|------------ |--------- |---------:|--------:|--------:|---------:|----------:|-
| Get         | .NET 6.0 | 708.6 ns | 2.23 ns | 1.74 ns | baseline |         - |
| UnsafeGet   | .NET 6.0 | 940.6 ns | 3.42 ns | 3.20 ns |     +33% |         - |
| SpanGet     | .NET 6.0 | 432.0 ns | 5.55 ns | 5.19 ns |     -39% |         - |
| SpanListGet | .NET 6.0 | 426.9 ns | 1.05 ns | 0.93 ns |     -40% |         - |
| GetReverse  | .NET 6.0 | 709.4 ns | 3.26 ns | 2.72 ns |      +0% |         - |
|             |          |          |         |         |          |           |
| Get         | .NET 8.0 | 477.2 ns | 7.30 ns | 6.47 ns | baseline |         - |
| UnsafeGet   | .NET 8.0 | 427.1 ns | 0.80 ns | 0.62 ns |     -11% |         - |
| SpanGet     | .NET 8.0 | 297.4 ns | 2.26 ns | 1.89 ns |     -38% |         - |
| SpanListGet | .NET 8.0 | 299.0 ns | 3.41 ns | 3.02 ns |     -37% |         - |
| GetReverse  | .NET 8.0 | 355.5 ns | 1.50 ns | 1.25 ns |     -26% |         - |
  */
public class BoundsCheck
{
    private const int SIZE = 1000;
    private static readonly int[] _array = [.. Enumerable.Range(0, SIZE)];
    private static readonly List<int> _list = [.. Enumerable.Range(0, SIZE)];

    [Benchmark(Baseline = true)]
    public int Get()
    {
        int x = 0;
        for (int i = 0; i < SIZE; i++)
        {
            x = _array[i];
        }
        return x;
    }

    [Benchmark]
    public int UnsafeGet()
    {
        int x = 0;
        for (int i = 0; i < SIZE; i++)
        {
            x = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_array), i);
        }
        return x;
    }

    [Benchmark]
    public int SpanGet()
    {
        var span = _array.AsSpan();
        int x = 0;
        for (int i = 0; i < SIZE; i++)
        {
            x = span[i];
        }
        return x;
    }

    [Benchmark]
    public int SpanListGet()
    {
        var span = CollectionsMarshal.AsSpan(_list);
        int x = 0;
        for (int i = 0; i < SIZE; i++)
        {
            x = span[i];
        }
        return x;
    }

    [Benchmark]
    public int GetReverse()
    {
        int x = 0;
        for (int i = SIZE - 1; i >= 0; i--)
        {
            x = _array[i];
        }

        return x;
    }
}