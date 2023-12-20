using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Benchmarks.EnumBenchmarks;

public class ConvertEnumBench
{
    [Flags]
    public enum DaysOfWeek : byte
    {
        None = 0,
        Monday
            = 0b0000_0001,
        Tuesday
            = 0b0000_0010,
        Wednesday
            = 0b0000_0100,
        Thursday
            = 0b0000_1000,
        Friday
            = 0b0001_0000,
        Saturday
            = 0b0010_0000,
        Sunday
            = 0b0100_0000,

        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekends = Saturday | Sunday,
        All = Weekdays | Weekends
    }

    public static readonly byte[] AllEnumValues = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();

    internal static Func<byte, DaysOfWeek> GetNumberConverterDynamicMethod()
    {
        var method = new DynamicMethod("Convert", typeof(DaysOfWeek), new[] { typeof(byte) }, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);
        return (Func<byte, DaysOfWeek>)method.CreateDelegate(typeof(Func<byte, DaysOfWeek>));
    }

    private static readonly Func<byte, DaysOfWeek> _expressionFunc = GetNumberConverter<DaysOfWeek, byte>();
    private static readonly Func<byte, DaysOfWeek> _dynamicMethodFunc = GetNumberConverterDynamicMethod();

    private static Func<TUnderlying, TEnum> GetNumberConverter<TEnum, TUnderlying>()
    {
        var input = Expression.Parameter(typeof(TUnderlying), "input");

        var λ = Expression.Lambda<Func<TUnderlying, TEnum>>(
            Expression.Convert(input, typeof(TEnum)),
            input);
        return λ.Compile();

                /* //DynamicMethod is not present in .net Standard 2.0
        var method = new DynamicMethod("Convert", typeof(TEnum), new[] { typeof(TUnderlying) }, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ret);
        return (Func<TUnderlying, TEnum>)method.CreateDelegate(typeof(Func<TUnderlying, TEnum>));*/
    }

    [Benchmark(Baseline = true)]
    public DaysOfWeek NativeTest()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= (DaysOfWeek)AllEnumValues[i];
        return current;
    }

    [Benchmark]
    public DaysOfWeek PointerDedicated()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= ToEnumPointer(AllEnumValues[i]);
        return current;
    }

    [Benchmark]
    public DaysOfWeek PointerGeneric()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= ToEnumPointer<DaysOfWeek, byte>(AllEnumValues[i]);
        return current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe DaysOfWeek ToEnumPointer(byte value)
        => *(DaysOfWeek*)(&value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TEnum ToEnumPointer<TEnum, TUnderlying>(TUnderlying value)
        where TEnum : unmanaged, Enum
        where TUnderlying : unmanaged
        => *(TEnum*)(&value);



    private static TEnum ToEnumCast<TEnum, TUnderlying>(TUnderlying number)
        where TEnum : Enum
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>,
        IFormattable =>
        (TEnum)(object)number;

    [Benchmark]
    public DaysOfWeek CastTest()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= ToEnumCast<DaysOfWeek, byte>(AllEnumValues[i]);
        return current;
    }

    [Benchmark]
    public DaysOfWeek ExpressionTest()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= _expressionFunc(AllEnumValues[i]);
        return current;
    }

    [Benchmark]
    public DaysOfWeek DynamicMethod()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
        {
            current |= _dynamicMethodFunc(AllEnumValues[i]);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek UnsafeAsTest()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
        {
            byte value = AllEnumValues[i];

            current |= Unsafe.As<byte, DaysOfWeek>(ref value);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek UnsafeAsRefTest()
    {
        Span<DaysOfWeek> enums = MemoryMarshal.Cast<byte, DaysOfWeek>(AllEnumValues.AsSpan());

        DaysOfWeek current = default;
        for (int i = enums.Length - 1; i >= 0; i--)
            current |= enums[i];
        return current;
    }

    /*[Benchmark]
    public DaysOfWeek IlGenericTest()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= EnumConverter.Convert.ToEnumIl<DaysOfWeek, byte>(AllEnumValues[i]);

        return current;
    }

    [Benchmark]
    public DayOfWeek IlDedicatedTest()
    {
        DayOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                        current |= EnumConverter.Convert.ToEnumIlConcrete(AllEnumValues[i]);

        return current;
    }*/

    internal static TEnum ToEnum<TEnum, TUnderlying>(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);


    [Benchmark]
    public DaysOfWeek SelectedSolution()
    {
        DaysOfWeek current = default;
        for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            current |= ToEnum<DaysOfWeek, byte>(AllEnumValues[i]);
        return current;
    }

    /*[MethodImpl(MethodImplOptions.ForwardRef)]
    public static extern TEnum ToEnumIl<TEnum, TUnderlying>(TUnderlying number)
        where TEnum : Enum
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable;

    [Benchmark]
    public DaysOfWeek ExternTest()
    {
    }*/
}
