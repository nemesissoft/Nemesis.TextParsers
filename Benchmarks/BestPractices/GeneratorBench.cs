namespace Benchmarks.BestPractices;

/*
| Method           | Runtime  | Mean       | Ratio    |
|----------------- |--------- |-----------:|---------:|
| Yield            | .NET 6.0 | 3,778.5 ns | baseline |
| YieldLocalMethod | .NET 6.0 | 3,760.8 ns |      -0% |
| EnumerableRange  | .NET 6.0 | 3,536.0 ns |      -6% |
| ArrayBench       | .NET 6.0 |   850.7 ns |     -77% |
|                  |          |            |          |
| Yield            | .NET 8.0 | 1,452.5 ns | baseline |
| YieldLocalMethod | .NET 8.0 | 1,440.6 ns |      -1% |
| EnumerableRange  | .NET 8.0 | 1,432.5 ns |      -1% |
| ArrayBench       | .NET 8.0 |   753.2 ns |     -48% |
  */
public class GeneratorBench
{
    private const int SIZE = 1000;

    [Benchmark(Baseline = true)]
    public int Yield()
    {
        int sum = 0;
        foreach (var i in Sequence())
            sum += i;
        return sum;
    }

    static IEnumerable<int> Sequence()
    {
        for (int i = 0; i < SIZE; i++)
            yield return i;
    }

    [Benchmark]
    public int YieldLocalMethod()
    {
        int sum = 0;
        foreach (var i in SequenceLocal())
            sum += i;
        return sum;

        static IEnumerable<int> SequenceLocal()
        {
            for (int i = 0; i < SIZE; i++)
                yield return i;
        }
    }

    [Benchmark]
    public int EnumerableRange()
    {
        int sum = 0;
        foreach (var i in Enumerable.Range(0, SIZE))
            sum += i;
        return sum;
    }

    [Benchmark]
    public int ArrayBench()
    {
        int sum = 0;
        foreach (var i in ArrayGen())
            sum += i;
        return sum;
    }


    static int[] ArrayGen()
    {
        var arr = new int[SIZE];
        for (int i = 0; i < SIZE; i++)
            arr[i] = i;
        return arr;
    }
}





















































/*
| Method           | Runtime  | Mean       | Ratio    | Gen0   | Allocated |
|----------------- |--------- |-----------:|---------:|-------:|----------:|-
| Yield            | .NET 6.0 | 3,778.5 ns | baseline |      - |      32 B |
| YieldLocalMethod | .NET 6.0 | 3,760.8 ns |      -0% | 0.0038 |      32 B |
| EnumerableRange  | .NET 6.0 | 3,536.0 ns |      -6% | 0.0038 |      40 B |
| ArrayBench       | .NET 6.0 |   850.7 ns |     -77% | 0.6409 |    4024 B |
|                  |          |            |          |        |           |
| Yield            | .NET 8.0 | 1,452.5 ns | baseline | 0.0038 |      32 B |
| YieldLocalMethod | .NET 8.0 | 1,440.6 ns |      -1% | 0.0038 |      32 B |
| EnumerableRange  | .NET 8.0 | 1,432.5 ns |      -1% | 0.0057 |      40 B |
| ArrayBench       | .NET 8.0 |   753.2 ns |     -48% | 0.6409 |    4024 B |
*/