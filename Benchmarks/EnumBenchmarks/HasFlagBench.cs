using System.Reflection.Emit;

namespace Benchmarks.EnumBenchmarks;

/*
| Method           | Job      | Mean      | Ratio    | Allocated |
|----------------- |--------- |----------:|---------:|----------:|-
| HasFlag_Native   | .NET 6.0 | 0.2381 ns |      -2% |         - |
| HasFlags_Dynamic | .NET 6.0 | 3.3180 ns |  +1,261% |         - |
| HasFlags_Bitwise | .NET 6.0 | 0.2447 ns | baseline |         - |
|                  |          |           |          |           |
| HasFlag_Native   | .NET 8.0 | 0.2403 ns |      +2% |         - |
| HasFlags_Dynamic | .NET 8.0 | 3.3305 ns |  +1,313% |         - |
| HasFlags_Bitwise | .NET 8.0 | 0.2368 ns | baseline |         - |
*/
public class HasFlagBench
{
    private const int OPERATIONS_PER_INVOKE = 10_000;

    public static Func<T, long> CreateConvertToLong<T>() where T : struct, Enum
    {
        var generator = DynamicMethodGenerator.Create<Func<T, long>>("ConvertEnumToLong");

        var ilGen = generator.GetMsilGenerator();

        ilGen.Emit(OpCodes.Ldarg_0);
        ilGen.Emit(OpCodes.Conv_I8);
        ilGen.Emit(OpCodes.Ret);
        return generator.Generate();
    }

    public static readonly Func<FileAccess, long> ConverterFunc = CreateConvertToLong<FileAccess>();

    public static bool HasFlags(FileAccess left, FileAccess right)
    {
        var fn = ConverterFunc;
        return (fn(left) & fn(right)) != 0;
    }

    [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
    public bool HasFlag_Native()
    {
        const FileAccess VALUE = FileAccess.ReadWrite;
        var result = true;

        for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
            result &= VALUE.HasFlag(FileAccess.Read);

        return result;
    }

    [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
    public bool HasFlags_Dynamic()
    {
        const FileAccess VALUE = FileAccess.ReadWrite;
        var result = true;

        for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
            result &= HasFlags(VALUE, FileAccess.Read);

        return result;
    }

    [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE, Baseline = true)]
    public bool HasFlags_Bitwise()
    {
        const FileAccess VALUE = FileAccess.ReadWrite;
        var result = true;

        for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
            result &= ((VALUE & FileAccess.Read) != 0);

        return result;
    }
}
