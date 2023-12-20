using Nemesis.TextParsers;

namespace Benchmarks.Collections;

/*
| Method                | Runtime  | Capacity | Mean        | Ratio    | Gen0   | Gen1   | Allocated | 
|---------------------- |--------- |--------- |------------:|---------:|-------:|-------:|----------:|-
| Mul_ToArray_Test      | .NET 6.0 | 5        |    377.9 ns | baseline | 0.0153 |      - |      48 B | 
| Mul_ToArrayStack_Test | .NET 6.0 | 5        |    287.9 ns |     -24% |      - |      - |         - | 
| Mul_Optimized         | .NET 6.0 | 5        |    274.3 ns |     -30% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 8.0 | 5        |    226.0 ns | baseline | 0.0153 |      - |      48 B | 
| Mul_ToArrayStack_Test | .NET 8.0 | 5        |    219.8 ns |      -3% |      - |      - |         - | 
| Mul_Optimized         | .NET 8.0 | 5        |    185.2 ns |     -19% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 6.0 | 10       |    678.9 ns | baseline | 0.0200 |      - |      64 B | 
| Mul_ToArrayStack_Test | .NET 6.0 | 10       |    604.6 ns |     -10% |      - |      - |         - | 
| Mul_Optimized         | .NET 6.0 | 10       |    549.1 ns |     -19% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 8.0 | 10       |    427.8 ns | baseline | 0.0200 |      - |      64 B | 
| Mul_ToArrayStack_Test | .NET 8.0 | 10       |    421.5 ns |      -4% |      - |      - |         - | 
| Mul_Optimized         | .NET 8.0 | 10       |    365.2 ns |     -16% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 6.0 | 50       |  3,247.1 ns | baseline | 0.0687 |      - |     224 B | 
| Mul_ToArrayStack_Test | .NET 6.0 | 50       |  2,942.5 ns |      -6% |      - |      - |         - | 
| Mul_Optimized         | .NET 6.0 | 50       |  3,130.3 ns |      +6% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 8.0 | 50       |  2,272.5 ns | baseline | 0.0687 |      - |     224 B | 
| Mul_ToArrayStack_Test | .NET 8.0 | 50       |  2,072.7 ns |     -20% |      - |      - |         - | 
| Mul_Optimized         | .NET 8.0 | 50       |  1,865.6 ns |     -17% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 6.0 | 100      |  6,235.5 ns | baseline | 0.1221 |      - |     424 B | 
| Mul_ToArrayStack_Test | .NET 6.0 | 100      |  6,053.0 ns |      +3% |      - |      - |         - | 
| Mul_Optimized         | .NET 6.0 | 100      |  5,786.0 ns |      -8% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 8.0 | 100      |  4,251.6 ns | baseline | 0.1297 |      - |     424 B | 
| Mul_ToArrayStack_Test | .NET 8.0 | 100      |  4,190.0 ns |      -2% |      - |      - |         - | 
| Mul_Optimized         | .NET 8.0 | 100      |  3,625.4 ns |     -15% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 6.0 | 200      | 13,076.4 ns | baseline | 0.2594 |      - |     824 B | 
| Mul_ToArrayStack_Test | .NET 6.0 | 200      |          NA |        ? |     NA |     NA |        NA | 
| Mul_Optimized         | .NET 6.0 | 200      | 11,879.5 ns |      -9% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| Mul_ToArray_Test      | .NET 8.0 | 200      |  9,204.0 ns | baseline | 0.2594 |      - |     824 B | 
| Mul_ToArrayStack_Test | .NET 8.0 | 200      |          NA |        ? |     NA |     NA |        NA | 
| Mul_Optimized         | .NET 8.0 | 200      |  8,695.3 ns |      -4% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 6.0 | 5        |    412.9 ns | baseline | 0.0153 | 0.0005 |      48 B | 
| ToArrayStack_Test     | .NET 6.0 | 5        |    324.7 ns |     -19% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 8.0 | 5        |    249.2 ns | baseline | 0.0153 |      - |      48 B | 
| ToArrayStack_Test     | .NET 8.0 | 5        |    222.8 ns |     -12% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 6.0 | 10       |    638.1 ns | baseline | 0.0200 |      - |      64 B | 
| ToArrayStack_Test     | .NET 6.0 | 10       |    606.9 ns |      -5% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 8.0 | 10       |    435.4 ns | baseline | 0.0200 |      - |      64 B | 
| ToArrayStack_Test     | .NET 8.0 | 10       |    445.0 ns |      +0% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 6.0 | 50       |  3,222.0 ns | baseline | 0.0687 |      - |     224 B | 
| ToArrayStack_Test     | .NET 6.0 | 50       |  2,788.1 ns |     -16% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 8.0 | 50       |  1,981.0 ns | baseline | 0.0687 |      - |     224 B | 
| ToArrayStack_Test     | .NET 8.0 | 50       |  1,980.6 ns |      -0% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 6.0 | 100      |  6,068.0 ns | baseline | 0.1297 |      - |     424 B | 
| ToArrayStack_Test     | .NET 6.0 | 100      |  5,628.6 ns |      -7% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 8.0 | 100      |  3,920.8 ns | baseline | 0.1297 |      - |     424 B | 
| ToArrayStack_Test     | .NET 8.0 | 100      |  4,047.7 ns |      +3% |      - |      - |         - | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 6.0 | 200      | 12,305.0 ns | baseline | 0.2594 |      - |     824 B | 
| ToArrayStack_Test     | .NET 6.0 | 200      |          NA |        ? |     NA |     NA |        NA | 
|                       |          |          |             |          |        |        |           | 
| ToArray_Test          | .NET 8.0 | 200      |  8,212.3 ns | baseline | 0.2594 |      - |     824 B | 
| ToArrayStack_Test     | .NET 8.0 | 200      |          NA |        ? |     NA |     NA |        NA | 
*/
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class ArrayParserBench
{
    private readonly ITransformer<int> _intTransformer = TextTransformer.Default.GetTransformer<int>();
    private readonly ITransformer<int[]> _intArrayTransformer = TextTransformer.Default.GetTransformer<int[]>();
    private string _text;

    [Params(5, 10, 50, 100, 200)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _text = string.Join("|",
            Enumerable.Range(1, Capacity)
            .Select(i => i.ToString()));
    }


    [Benchmark(Baseline = true), BenchmarkCategory("ToArray")]
    public int ToArray_Test()
    {
        int[] parsed = _intArrayTransformer.Parse(_text.AsSpan());
        return parsed[^1];
    }

    [Benchmark, BenchmarkCategory("ToArray")]
    public int ToArrayStack_Test()
    {
        if (Capacity > 128)
            throw new NotSupportedException("Not supported");

        var stream = _text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

        Span<int> parsed = stackalloc int[Capacity];
        int i = 0;
        foreach (var num in stream)
            parsed[i++] = num.ParseWith(_intTransformer);

        return parsed[^1];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Mul")]
    public int Mul_ToArray_Test()
    {
        int[] parsed = _intArrayTransformer.Parse(_text.AsSpan());

        int result = 0;
        foreach (int num in parsed)
            result = unchecked(result * num);

        return result;
    }

    [Benchmark, BenchmarkCategory("Mul")]
    public int Mul_ToArrayStack_Test()
    {
        if (Capacity > 128)
            throw new NotSupportedException("Not supported");

        var stream = _text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

        Span<int> parsed = stackalloc int[Capacity];
        int i = 0;
        foreach (var num in stream)
            parsed[i++] = num.ParseWith(_intTransformer);

        int result = 0;
        foreach (int num in parsed)
            result = unchecked(result * num);

        return result;
    }

    [Benchmark, BenchmarkCategory("Mul")]
    public int Mul_Optimized()
    {
        var stream = _text.AsSpan().Tokenize('|', '\\', true).PreParse('\\', '∅', '|');

        int result = 0;
        foreach (var num in stream)
            result = unchecked(result * num.ParseWith(_intTransformer));

        return result;
    }
}
