using EnumsNET;
using Nemesis.TextParsers;

namespace Benchmarks.EnumBenchmarks;

/*
| Method                     | Job      | Mean     | Ratio    | BranchInstructions/Op | BranchMispredictions/Op | Timer/Op | LLCMisses/Op | Gen0   | Allocated |
|--------------------------- |--------- |---------:|---------:|----------------------:|------------------------:|---------:|-------------:|-------:|----------:|-
| EDN_Generic_IgnoreCase     | .NET 6.0 | 35.33 us |     +78% |                77,505 |                     390 |      358 |           51 |      - |         - |
| EDN_Generic_ObserveCase    | .NET 6.0 | 24.59 us |     +24% |                43,701 |                     377 |      248 |           26 |      - |         - |
| EDN_Unsafe                 | .NET 6.0 | 24.46 us |     +23% |                42,855 |                     364 |      247 |           26 |      - |         - |
| EDN_NonGeneric_IgnoreCase  | .NET 6.0 | 35.26 us |     +77% |                77,953 |                     379 |      354 |           95 | 0.4883 |    3121 B |
| EDN_NonGeneric_ObserveCase | .NET 6.0 | 25.84 us |     +30% |                46,693 |                     377 |      261 |           89 | 0.4883 |    3120 B |
| EnumTransformer            | .NET 6.0 | 19.89 us | baseline |                39,891 |                     328 |      201 |           27 |      - |         - |
| DedicatedCode              | .NET 6.0 | 19.53 us |      -2% |                39,279 |                     371 |      196 |           16 |      - |         - |
| Native_Generic_IgnoreCase  | .NET 6.0 | 18.99 us |      -4% |                43,103 |                     382 |      195 |           22 |      - |         - |
| Native_Generic_ObserveCase | .NET 6.0 | 18.52 us |      -7% |                43,610 |                     416 |      186 |           19 |      - |         - |
| Native_IgnoreCase          | .NET 6.0 | 26.47 us |     +33% |                57,964 |                     472 |      267 |           97 | 0.4883 |    3121 B |
| Native_ObserveCase         | .NET 6.0 | 18.40 us |      -7% |                43,602 |                     404 |      188 |           23 |      - |         - |
|                            |          |          |          |                       |                         |          |              |        |           |
| EDN_Generic_IgnoreCase     | .NET 8.0 | 17.71 us |     +42% |                34,891 |                     184 |      179 |           16 |      - |         - |
| EDN_Generic_ObserveCase    | .NET 8.0 | 14.86 us |     +19% |                27,650 |                     166 |      149 |           11 |      - |         - |
| EDN_Unsafe                 | .NET 8.0 | 14.93 us |     +20% |                28,228 |                     194 |      150 |           16 |      - |         - |
| EDN_NonGeneric_IgnoreCase  | .NET 8.0 | 18.91 us |     +51% |                36,786 |                     252 |      192 |          110 | 0.4883 |    3120 B |
| EDN_NonGeneric_ObserveCase | .NET 8.0 | 16.12 us |     +29% |                30,715 |                     198 |      163 |          106 | 0.4883 |    3120 B |
| EnumTransformer            | .NET 8.0 | 12.51 us | baseline |                29,090 |                      55 |      126 |           14 |      - |         - |
| DedicatedCode              | .NET 8.0 | 10.41 us |     -17% |                26,648 |                      34 |      105 |            9 |      - |         - |
| Native_Generic_IgnoreCase  | .NET 8.0 | 10.16 us |     -19% |                26,134 |                     142 |      102 |           11 |      - |         - |
| Native_Generic_ObserveCase | .NET 8.0 | 12.50 us |      +0% |                26,329 |                     294 |      126 |           14 |      - |         - |
| Native_IgnoreCase          | .NET 8.0 | 19.99 us |     +60% |                42,202 |                     422 |      202 |          107 | 0.4883 |    3120 B |
| Native_ObserveCase         | .NET 8.0 | 11.23 us |     -10% |                26,441 |                     215 |      113 |            9 |      - |         - |
*/
[HardwareCounters(HardwareCounter.LlcMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions, HardwareCounter.Timer)]
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
        Enumerable.Range(0, 130).Select(i => ((DaysOfWeek)i).ToString("G").Replace(" ", "")).ToArray();

    private static readonly ITransformer<DaysOfWeek> _parser = TextTransformer.Default.GetTransformer<DaysOfWeek>();


    [Benchmark]
    public DaysOfWeek EDN_Generic_IgnoreCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enums.Parse<DaysOfWeek>(AllEnums[i].AsSpan(), true);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek EDN_Generic_ObserveCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enums.Parse<DaysOfWeek>(AllEnums[i].AsSpan(), false);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek EDN_Unsafe()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enums.ParseUnsafe<DaysOfWeek>(AllEnums[i].AsSpan());
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek EDN_NonGeneric_IgnoreCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), AllEnums[i].AsSpan(), true);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek EDN_NonGeneric_ObserveCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = (DaysOfWeek)Enums.Parse(typeof(DaysOfWeek), AllEnums[i].AsSpan(), false);
        }
        return current;
    }


    [Benchmark(Baseline = true)]
    public DaysOfWeek EnumTransformer()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = _parser.Parse(AllEnums[i].AsSpan());
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek DedicatedCode()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = ParseDaysOfWeek(AllEnums[i].AsSpan());
        }
        return current;
    }

    private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || text.IsWhiteSpace()) return default;

        var enumStream = SpanSplitExtensions.Split(text, ',').GetEnumerator();

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
    public DaysOfWeek Native_Generic_IgnoreCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enum.Parse<DaysOfWeek>(AllEnums[i], true);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek Native_Generic_ObserveCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enum.Parse<DaysOfWeek>(AllEnums[i], false);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek Native_IgnoreCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), AllEnums[i], true);
        }
        return current;
    }

    [Benchmark]
    public DaysOfWeek Native_ObserveCase()
    {
        DaysOfWeek current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enum.Parse<DaysOfWeek>(AllEnums[i], false);
        }
        return current;
    }
}