using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EnumsNET;
using Nemesis.TextParsers;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class EnumParserBench
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

        public static readonly string[] AllEnums =
            //Enumerable.Range(0, 130).Select(i => i.ToString()).ToArray();
            Enumerable.Range(0, 130).Select(i => ((DaysOfWeek)i).ToString("G").Replace(" ", "")).ToArray();

        private static readonly ITransformer<DaysOfWeek> _parser = TextTransformer.Default.GetTransformer<DaysOfWeek>();

        static EnumParserBench() => _parser.Parse("10".AsSpan());

        [Benchmark]
        public static DaysOfWeek EnumsDotNetGenericIgnoreCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = Enums.Parse<DaysOfWeek>(text.AsSpan(), true);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetGenericObserveCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                // ReSharper disable once RedundantArgumentDefaultValue
                current = Enums.Parse<DaysOfWeek>(text.AsSpan(), false);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetUnsafe()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = Enums.ParseUnsafe<DaysOfWeek>(text.AsSpan());
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetNonGenericIgnoreCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), text.AsSpan(), true);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetNonGenericObserveCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), text.AsSpan(), false);
            }
            return current;
        }


        [Benchmark(Baseline = true)]
        public static DaysOfWeek EnumTransformer()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = _parser.Parse(text.AsSpan());
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek DedicatedCode()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = ParseDaysOfWeek(text.AsSpan());
            }
            return current;
        }

        private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> text)
        {
            if (text.IsEmpty || text.IsWhiteSpace()) return default;

            var enumStream = text.Split(',').GetEnumerator();

            if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {typeof(DaysOfWeek).Name} enum");
            byte currentValue = ParseDaysOfWeekElement(enumStream.Current);

            while (enumStream.MoveNext())
            {
                var element = ParseDaysOfWeekElement(enumStream.Current);

                currentValue |= element;
            }

            return (DaysOfWeek)currentValue;

            static byte ParseDaysOfWeekElement(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty || input.IsWhiteSpace()) return default;
                input = input.Trim();

                return IsNumeric(input) && byte.TryParse(
#if NET48
                input.ToString()
#else
                input
#endif
                , out byte number) ? number : ParseDaysOfWeekByLabelOr(input);
            }
            static bool IsNumeric(ReadOnlySpan<char> input)
            {
                char firstChar;
                return input.Length > 0 && (char.IsDigit(firstChar = input[0]) || firstChar == '-' || firstChar == '+');
            }

            static byte ParseDaysOfWeekByLabelOr(ReadOnlySpan<char> input)
            {
                if (input.Length == 4 && (input[3] == 'E' || input[3] == 'e') && (input[2] == 'N' || input[2] == 'n') &&
                    (input[1] == 'O' || input[1] == 'o') && (input[0] == 'N' || input[0] == 'n')
                )
                    return 0;
                else if (
                    input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') && (input[4] == 'A' || input[4] == 'a') &&
                    (input[3] == 'D' || input[3] == 'd') && (input[2] == 'N' || input[2] == 'n') &&
                    (input[1] == 'O' || input[1] == 'o') && (input[0] == 'M' || input[0] == 'm')
                )
                    return 1;
                else if (
                    input.Length == 7 && (input[6] == 'Y' || input[6] == 'y') && (input[5] == 'A' || input[5] == 'a') &&
                    (input[4] == 'D' || input[4] == 'd') && (input[3] == 'S' || input[3] == 's') &&
                    (input[2] == 'E' || input[2] == 'e') && (input[1] == 'U' || input[1] == 'u') &&
                    (input[0] == 'T' || input[0] == 't')
                )
                    return 2;
                else if (
                    input.Length == 9 && (input[8] == 'Y' || input[8] == 'y') &&
                    (input[7] == 'A' || input[7] == 'a') && (input[6] == 'D' || input[6] == 'd') &&
                    (input[5] == 'S' || input[5] == 's') && (input[4] == 'E' || input[4] == 'e') &&
                    (input[3] == 'N' || input[3] == 'n') && (input[2] == 'D' || input[2] == 'd') &&
                    (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
                )
                    return 4;
                else if (
                    input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                    (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                    (input[4] == 'S' || input[4] == 's') && (input[3] == 'R' || input[3] == 'r') &&
                    (input[2] == 'U' || input[2] == 'u') && (input[1] == 'H' || input[1] == 'h') &&
                    (input[0] == 'T' || input[0] == 't')
                )
                    return 8;
                else if (
                    input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                    (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                    (input[2] == 'I' || input[2] == 'i') && (input[1] == 'R' || input[1] == 'r') &&
                    (input[0] == 'F' || input[0] == 'f')
                )
                    return 16;
                else if (
                    input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                    (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                    (input[4] == 'R' || input[4] == 'r') && (input[3] == 'U' || input[3] == 'u') &&
                    (input[2] == 'T' || input[2] == 't') && (input[1] == 'A' || input[1] == 'a') &&
                    (input[0] == 'S' || input[0] == 's')
                )
                    return 32;
                else if (
                    input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                    (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                    (input[2] == 'N' || input[2] == 'n') && (input[1] == 'U' || input[1] == 'u') &&
                    (input[0] == 'S' || input[0] == 's')
                )
                    return 64;
                else if (
                    input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                    (input[6] == 'Y' || input[6] == 'y') &&
                    (input[5] == 'A' || input[5] == 'a') &&
                    (input[4] == 'D' || input[4] == 'd') &&
                    (input[3] == 'K' || input[3] == 'k') &&
                    (input[2] == 'E' || input[2] == 'e') &&
                    (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
                )
                    return 31;
                else if (
                    input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                    (input[6] == 'D' || input[6] == 'd') &&
                    (input[5] == 'N' || input[5] == 'n') &&
                    (input[4] == 'E' || input[4] == 'e') &&
                    (input[3] == 'K' || input[3] == 'k') &&
                    (input[2] == 'E' || input[2] == 'e') &&
                    (input[1] == 'E' || input[1] == 'e') &&
                    (input[0] == 'W' || input[0] == 'w')
                )
                    return 96;
                else if (
                    input.Length == 3 && (input[2] == 'L' || input[2] == 'l') &&
                    (input[1] == 'L' || input[1] == 'l') &&
                    (input[0] == 'A' || input[0] == 'a')
                )
                    return 127;
                else
                    throw new FormatException(
                        "Enum of type 'DaysOfWeek' cannot be parsed.Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");
            }
        }


        [Benchmark]
        public static DaysOfWeek NativeEnumIgnoreCaseGeneric()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = Enum.Parse<DaysOfWeek>(text, true);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek NativeEnumIgnoreCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);
            }
            return current;
        }

#if !NET48
        [Benchmark]
        public static DaysOfWeek NativeEnumObserveCase()
        {
            DaysOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = Enum.Parse<DaysOfWeek>(text, false);
            }
            return current;
        }
#endif
    }

    [MemoryDiagnoser]
    public class EnumParserBenchEdn
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

        private static readonly (int Start, int Length)[] _sliceData;
        private static readonly string _toParse;

        private static readonly ITransformer<DaysOfWeek> _parser = TextTransformer.Default.GetTransformer<DaysOfWeek>();

        static EnumParserBenchEdn()
        {
            _parser.Parse("10".AsSpan());

            const int MAX_ENUMS = 130;
            string[] allEnums =
                Enumerable.Range(0, MAX_ENUMS).Select(i => ((DaysOfWeek)i).ToString("G").Replace(" ", "")).ToArray();

            int maxLen = allEnums.Max(name => name.Length);
            string toParse = string.Join("", allEnums.Select(name =>
                    "/" + name.PadRight(maxLen)
                )
            );

            (int Start, int Length)[] sliceData = Enumerable.Range(0, allEnums.Length).Select(i => (Start: i * (maxLen + 1) + 1, Length: maxLen)).ToArray();

            //var t1 = toParse.AsSpan(sliceData[0].Start, sliceData[0].Length).ToString();

            //sanity check
            for (int i = 0; i < MAX_ENUMS; i++)
            {
                var text = toParse.AsSpan(sliceData[i].Start, sliceData[i].Length);
                var parsedEnum = Enum.Parse<DaysOfWeek>(text.ToString());

                if ((byte)parsedEnum != (byte)i)
                    throw new InvalidOperationException("Failed at " + i);
            }

            _sliceData = sliceData;
            _toParse = toParse;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetGenericIgnoreCase()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = Enums.Parse<DaysOfWeek>(span, true);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetGenericObserveCase()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = Enums.Parse<DaysOfWeek>(span, false);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetUnsafe()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = Enums.ParseUnsafe<DaysOfWeek>(span);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetNonGenericIgnoreCase()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), span, true);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek EnumsDotNetNonGenericObserveCase()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), span, false);
            }
            return current;
        }


        [Benchmark(Baseline = true)]
        public static DaysOfWeek EnumTransformer()
        {
            DaysOfWeek current = default;
            var toParse = _toParse.AsSpan();
            var sliceData = _sliceData;

            for (int i = sliceData.Length - 1; i >= 0; i--)
            {
                var (start, length) = sliceData[i];
                var span = toParse.Slice(start, length);
                current = _parser.Parse(span);
            }
            return current;
        }
    }

    /* .NET Core 3.1.22 (CoreCLR 4.700.21.56803, CoreFX 4.700.21.57101), X64 RyuJIT
|          Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |---------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
| EnumTransformer | 2.859 us | 0.0462 us | 0.0409 us |  1.00 |    0.00 |     - |     - |     - |         - |
|       Generated | 3.161 us | 0.0523 us | 0.0489 us |  1.11 |    0.02 |     - |     - |     - |         - |

    .NET Core 6.0.1 (CoreCLR 6.0.121.56705, CoreFX 6.0.121.56705), X64 RyuJIT
|          Method |     Mean |     Error |    StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |---------:|----------:|----------:|------:|------:|------:|------:|----------:|
| EnumTransformer | 2.648 us | 0.0419 us | 0.0392 us |  1.00 |     - |     - |     - |         - |
|       Generated | 2.226 us | 0.0144 us | 0.0128 us |  0.84 |     - |     - |     - |         - |*/
    [MemoryDiagnoser]
    public class EnumParserBenchNonFlagGenerated
    {
        public enum DayOfWeek : byte
        {
            None,
            Monday,
            Tuesday,
            Wednesday,
            Thursday,
            Friday,
            Saturday,
            Sunday,
        }

        public static readonly string[] AllEnums =
            Enumerable.Range(0, 50).Select(i => ((DayOfWeek)i).ToString("G").Replace(" ", "")).ToArray();

        private static readonly ITransformer<DayOfWeek> _parser = TextTransformer.Default.GetTransformer<DayOfWeek>();


        [Benchmark(Baseline = true)]
        public static DayOfWeek EnumTransformer()
        {
            DayOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = _parser.Parse(text.AsSpan());
            }
            return current;
        }

        [Benchmark]
        public static DayOfWeek Generated()
        {
            DayOfWeek current = default;
            for (int i = AllEnums.Length - 1; i >= 0; i--)
            {
                var text = AllEnums[i];
                current = ParseIgnoreCase(text);
            }
            return current;
        }

        private static DayOfWeek ParseIgnoreCase(string text) =>
            text switch
            {
                { } s when s.Equals(nameof(DayOfWeek.None), StringComparison.OrdinalIgnoreCase) => DayOfWeek.None,
                { } s when s.Equals(nameof(DayOfWeek.Monday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Monday,
                { } s when s.Equals(nameof(DayOfWeek.Tuesday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Tuesday,
                { } s when s.Equals(nameof(DayOfWeek.Wednesday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Wednesday,
                { } s when s.Equals(nameof(DayOfWeek.Thursday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Thursday,
                { } s when s.Equals(nameof(DayOfWeek.Friday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Friday,
                { } s when s.Equals(nameof(DayOfWeek.Saturday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Saturday,
                { } s when s.Equals(nameof(DayOfWeek.Sunday), StringComparison.OrdinalIgnoreCase) => DayOfWeek.Sunday,
                _ => Enum.Parse<DayOfWeek>(text, true)
            };
    }

    [MemoryDiagnoser]
    public class ToEnumBench
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

            // ReSharper disable once CommentTypo
            /* //DynamicMethod is not present in .net Standard 2.0
            var method = new DynamicMethod("Convert", typeof(TEnum), new[] { typeof(TUnderlying) }, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            return (Func<TUnderlying, TEnum>)method.CreateDelegate(typeof(Func<TUnderlying, TEnum>));*/
        }

        [Benchmark(Baseline = true)]
        public static DaysOfWeek NativeTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= (DaysOfWeek)AllEnumValues[i];
            return current;
        }

        [Benchmark]
        public static DaysOfWeek PointerDedicated()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= ToEnumPointer(AllEnumValues[i]);
            return current;
        }

        [Benchmark]
        public static DaysOfWeek PointerGeneric()
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
        public static DaysOfWeek CastTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= ToEnumCast<DaysOfWeek, byte>(AllEnumValues[i]);
            return current;
        }

        [Benchmark]
        public static DaysOfWeek ExpressionTest()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
                current |= _expressionFunc(AllEnumValues[i]);
            return current;
        }

        [Benchmark]
        public static DaysOfWeek DynamicMethod()
        {
            DaysOfWeek current = default;
            for (int i = AllEnumValues.Length - 1; i >= 0; i--)
            {
                current |= _dynamicMethodFunc(AllEnumValues[i]);
            }
            return current;
        }

        [Benchmark]
        public static DaysOfWeek UnsafeAsTest()
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
        public static DaysOfWeek UnsafeAsRefTest()
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
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                current |= EnumConverter.Convert.ToEnumIlConcrete(AllEnumValues[i]);

            return current;
        }*/

        internal static TEnum ToEnum<TEnum, TUnderlying>(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);


        [Benchmark]
        public static DaysOfWeek SelectedSolution()
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

    /* NET Core 3.1
|           Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|   HasFlag_Native | 0.2376 ns | 0.0026 ns | 0.0023 ns |  0.98 |    0.03 |     - |     - |     - |         - |
| HasFlags_Dynamic | 5.4291 ns | 0.1069 ns | 0.2183 ns | 22.68 |    1.01 |     - |     - |     - |         - |
| HasFlags_Bitwise | 0.2407 ns | 0.0043 ns | 0.0059 ns |  1.00 |    0.00 |     - |     - |     - |         - | */
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net47)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
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
        public static bool HasFlag_Native()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= VALUE.HasFlag(FileAccess.Read);

            return result;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
        public static bool HasFlags_Dynamic()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= HasFlags(VALUE, FileAccess.Read);

            return result;
        }

        [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE, Baseline = true)]
        public static bool HasFlags_Bitwise()
        {
            const FileAccess VALUE = FileAccess.ReadWrite;
            var result = true;

            for (var i = 0; i < OPERATIONS_PER_INVOKE; i++)
                result &= ((VALUE & FileAccess.Read) != 0);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return result;
        }


    }
}
