using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;

namespace Benchmarks.EnumBenchmarks;

/*
| Method                     | Job      | Mean       | Ratio    | Gen0   | BranchInstructions/Op | Timer/Op | BranchMispredictions/Op | LLCMisses/Op | Allocated |
|--------------------------- |--------- |-----------:|---------:|-------:|----------------------:|---------:|------------------------:|-------------:|----------:|-
| Native_IgnoreCase_Generic  | .NET 6.0 |   993.0 ns |     +19% |      - |                 2,467 |       10 |                       2 |            1 |         - |
| Native_ObserveCase_Generic | .NET 6.0 |   967.5 ns |     +13% |      - |                 2,475 |       10 |                       4 |            1 |         - |
| Native_IgnoreCase          | .NET 6.0 | 1,989.4 ns |    +131% | 0.0572 |                 4,203 |       20 |                      25 |           10 |     360 B |
| Native_ObserveCase         | .NET 6.0 | 1,945.7 ns |    +126% | 0.0572 |                 4,201 |       20 |                      25 |            9 |     360 B |
| ExpressionTrees            | .NET 6.0 |   858.6 ns | baseline |      - |                 1,678 |        9 |                       1 |            1 |         - |
| ConceptWithSettings        | .NET 6.0 |   862.2 ns |      +1% |      - |                 1,944 |        9 |                       4 |            1 |         - |
| ConceptWithoutSettings     | .NET 6.0 |   607.5 ns |     -29% |      - |                 1,419 |        6 |                       1 |            1 |         - |
| FromCodeGen                | .NET 6.0 |   601.5 ns |     -30% |      - |                 1,418 |        6 |                       1 |            0 |         - |
|                            |          |            |          |        |                       |          |                         |              |           |
| Native_IgnoreCase_Generic  | .NET 8.0 |   386.8 ns |     -21% |      - |                 1,116 |        4 |                       1 |            0 |         - |
| Native_ObserveCase_Generic | .NET 8.0 |   392.9 ns |     -20% |      - |                 1,125 |        4 |                       1 |            0 |         - |
| Native_IgnoreCase          | .NET 8.0 | 1,292.5 ns |    +163% | 0.0572 |                 2,971 |       13 |                       6 |           11 |     360 B |
| Native_ObserveCase         | .NET 8.0 | 1,409.1 ns |    +187% | 0.0572 |                 2,982 |       14 |                      15 |           11 |     360 B |
| ExpressionTrees            | .NET 8.0 |   491.1 ns | baseline |      - |                 1,203 |        5 |                       1 |            1 |         - |
| ConceptWithSettings        | .NET 8.0 |   456.6 ns |      -7% |      - |                 1,251 |        5 |                       1 |            0 |         - |
| ConceptWithoutSettings     | .NET 8.0 |   177.7 ns |     -64% |      - |                   566 |        2 |                       0 |            0 |         - |
| FromCodeGen                | .NET 8.0 |   177.5 ns |     -64% |      - |                   566 |        2 |                       0 |            0 |         - |
*/
//This benchamrk was create to determine whether it makes sens to create code gen for enum types 
[HardwareCounters(HardwareCounter.LlcMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions, HardwareCounter.Timer)]
public class EnumParserBench_CodeGen
{
    public static readonly string[] AllEnums =
        Enumerable.Range(0, 15).Select(i => ((Month)i).ToString("G").Replace(" ", "")).ToArray();

    private static readonly ITransformer<Month> _expressionTreesParser = TextTransformer.Default.GetTransformer<Month>();
    private static readonly TransformerWithSettings _parserWithSettings = new(new(true, true));
    private static readonly ConceptTransformer _conceptParser = new();
    private static readonly CodeGenTransformer _codeGenParser = new();

    public enum Month : byte
    {
        None = 0,
        January = 1, February = 2, March = 3, April = 4,
        May = 5, June = 6, July = 7, August = 8,
        September = 9, October = 10, November = 11, December = 12
    }


    [Benchmark]
    public Month Native_IgnoreCase_Generic()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enum.Parse<Month>(AllEnums[i], true);
        }
        return current;
    }

    [Benchmark]
    public Month Native_ObserveCase_Generic()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = Enum.Parse<Month>(AllEnums[i], false);
        }
        return current;
    }

    [Benchmark]
    public Month Native_IgnoreCase()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = (Month)Enum.Parse(typeof(Month), AllEnums[i], true);
        }
        return current;
    }

    [Benchmark]
    public Month Native_ObserveCase()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = (Month)Enum.Parse(typeof(Month), AllEnums[i], false);
        }
        return current;
    }

    [Benchmark(Baseline = true)]
    public Month ExpressionTrees()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = _expressionTreesParser.Parse(AllEnums[i].AsSpan());
        }
        return current;
    }


    [Benchmark]
    public Month ConceptWithSettings()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = _parserWithSettings.Parse(AllEnums[i].AsSpan());
        }
        return current;
    }

    [Benchmark]
    public Month ConceptWithoutSettings()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = _conceptParser.Parse(AllEnums[i].AsSpan());
        }
        return current;
    }

    [Benchmark]
    public Month FromCodeGen()
    {
        Month current = default;
        for (int i = AllEnums.Length - 1; i >= 0; i--)
        {
            current = _codeGenParser.Parse(AllEnums[i].AsSpan());
        }
        return current;
    }

    sealed class TransformerWithSettings : TransformerBase<Month>
    {
        private readonly EnumSettings _enumSettings;
        private readonly StringComparison _stringComparison;
        private readonly ByteTransformer _numberTransformer = (ByteTransformer)ByteTransformer.Instance;

        private static readonly bool IsFlagEnum = typeof(Month).IsDefined(typeof(FlagsAttribute), false);

        public TransformerWithSettings(EnumSettings enumSettings)
        {
            _enumSettings = enumSettings;
            _stringComparison = enumSettings.CaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }

        public override string ToString() => $"Transform {nameof(Month)} based on {typeof(byte).Name} ({_enumSettings})";

        public override string Format(Month element) => element switch
        {
            Month.None => nameof(Month.None),
            Month.January => nameof(Month.January),
            Month.February => nameof(Month.February),
            Month.March => nameof(Month.March),
            Month.April => nameof(Month.April),
            Month.May => nameof(Month.May),
            Month.June => nameof(Month.June),
            Month.July => nameof(Month.July),
            Month.August => nameof(Month.August),
            Month.September => nameof(Month.September),
            Month.October => nameof(Month.October),
            Month.November => nameof(Month.November),
            Month.December => nameof(Month.December),
            _ => element.ToString("G"),
        };

        protected override Month ParseCore(in ReadOnlySpan<char> input)
        {
            if (input.IsWhiteSpace()) return default;

            //this branch was deleted as code gen will have access to FlagsAttribute
            /*if (IsFlagEnum)
            {
                var enumStream = input.Split(',').GetEnumerator();

                if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {nameof(Month)} enum");
                var currentValue = ParseElement(enumStream.Current);

                while (enumStream.MoveNext())
                {
                    var element = ParseElement(enumStream.Current);

                    currentValue = (byte)(currentValue | element);
                }

                return (Month)currentValue;
            }
            else*/
            return (Month)ParseElement(input);
        }

        private byte ParseElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();

            if (_enumSettings.AllowParsingNumerics && IsNumeric(input) &&
                _numberTransformer.TryParse(in input, out var number))
            {
                return number;
            }
            else
            {
                return Parse(input);
            }

            static bool IsNumeric(ReadOnlySpan<char> input) =>
                input.Length > 0 && input[0] is var first &&
                (char.IsDigit(first) || first is '-' or '+');
        }

        private byte Parse(ReadOnlySpan<char> input)
        {
            var cmp = _stringComparison;
            if (IsEqual(input, nameof(Month.None), cmp))
                return (byte)Month.None;

            else if (IsEqual(input, nameof(Month.January), cmp))
                return (byte)Month.January;

            else if (IsEqual(input, nameof(Month.February), cmp))
                return (byte)Month.February;

            else if (IsEqual(input, nameof(Month.March), cmp))
                return (byte)Month.March;

            else if (IsEqual(input, nameof(Month.April), cmp))
                return (byte)Month.April;

            else if (IsEqual(input, nameof(Month.May), cmp))
                return (byte)Month.May;

            else if (IsEqual(input, nameof(Month.June), cmp))
                return (byte)Month.June;

            else if (IsEqual(input, nameof(Month.July), cmp))
                return (byte)Month.July;

            else if (IsEqual(input, nameof(Month.August), cmp))
                return (byte)Month.August;

            else if (IsEqual(input, nameof(Month.September), cmp))
                return (byte)Month.September;

            else if (IsEqual(input, nameof(Month.October), cmp))
                return (byte)Month.October;

            else if (IsEqual(input, nameof(Month.November), cmp))
                return (byte)Month.November;

            else if (IsEqual(input, nameof(Month.December), cmp))
                return (byte)Month.December;

            else throw new FormatException(
              $"Enum of type '{nameof(Month)}' cannot be parsed. " +
              $"Valid values are: None or January or February or March or April or May or June or July or August or September or October or November or December" +
              (_enumSettings.AllowParsingNumerics ? $" or number within {typeof(byte).Name} range. " : ". ") +
              (_enumSettings.CaseInsensitive ? "Ignore case option on." : "Case sensitive option on.")
            );

            static bool IsEqual(ReadOnlySpan<char> input, string label, StringComparison comparison) =>
                MemoryExtensions.Equals(input, label.AsSpan(), comparison);
        }
    }

    sealed class ConceptTransformer : TransformerBase<Month>
    {
        public override string Format(Month element) => element switch
        {
            Month.None => nameof(Month.None),
            Month.January => nameof(Month.January),
            Month.February => nameof(Month.February),
            Month.March => nameof(Month.March),
            Month.April => nameof(Month.April),
            Month.May => nameof(Month.May),
            Month.June => nameof(Month.June),
            Month.July => nameof(Month.July),
            Month.August => nameof(Month.August),
            Month.September => nameof(Month.September),
            Month.October => nameof(Month.October),
            Month.November => nameof(Month.November),
            Month.December => nameof(Month.December),
            _ => element.ToString("G"),
        };

        protected override Month ParseCore(in ReadOnlySpan<char> input) =>
            input.IsWhiteSpace() ? default : (Month)ParseElement(input);

        private static byte ParseElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();

            if (IsNumeric(input) && byte.TryParse(input, out var number))
                return number;
            else
                return Parse(input);


            static bool IsNumeric(ReadOnlySpan<char> input) =>
                input.Length > 0 && input[0] is var first &&
                (char.IsDigit(first) || first is '-' or '+');
        }

        private static byte Parse(ReadOnlySpan<char> input)
        {
            if (IsEqual(input, nameof(Month.None)))
                return (byte)Month.None;

            else if (IsEqual(input, nameof(Month.January)))
                return (byte)Month.January;

            else if (IsEqual(input, nameof(Month.February)))
                return (byte)Month.February;

            else if (IsEqual(input, nameof(Month.March)))
                return (byte)Month.March;

            else if (IsEqual(input, nameof(Month.April)))
                return (byte)Month.April;

            else if (IsEqual(input, nameof(Month.May)))
                return (byte)Month.May;

            else if (IsEqual(input, nameof(Month.June)))
                return (byte)Month.June;

            else if (IsEqual(input, nameof(Month.July)))
                return (byte)Month.July;

            else if (IsEqual(input, nameof(Month.August)))
                return (byte)Month.August;

            else if (IsEqual(input, nameof(Month.September)))
                return (byte)Month.September;

            else if (IsEqual(input, nameof(Month.October)))
                return (byte)Month.October;

            else if (IsEqual(input, nameof(Month.November)))
                return (byte)Month.November;

            else if (IsEqual(input, nameof(Month.December)))
                return (byte)Month.December;

            else throw new FormatException("""
              Enum of type 'Month' cannot be parsed.
              Valid values are: None or January or February or March or April or May or June or July or August or September or October or November or December or number within byte range.
              Ignore case option on.
              """);

            static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class CodeGenTransformer : TransformerBase<Month>
    {
        public override string Format(Month element) => element switch
        {
            Month.None => nameof(Month.None),
            Month.January => nameof(Month.January),
            Month.February => nameof(Month.February),
            Month.March => nameof(Month.March),
            Month.April => nameof(Month.April),
            Month.May => nameof(Month.May),
            Month.June => nameof(Month.June),
            Month.July => nameof(Month.July),
            Month.August => nameof(Month.August),
            Month.September => nameof(Month.September),
            Month.October => nameof(Month.October),
            Month.November => nameof(Month.November),
            Month.December => nameof(Month.December),
            _ => element.ToString("G"),
        };

        protected override Month ParseCore(in ReadOnlySpan<char> input) =>
            input.IsWhiteSpace() ? default : (Month)ParseElement(input);

        private static byte ParseElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();
            if (IsNumeric(input) && byte.TryParse(input, out var number))
                return number;
            else
                return ParseName(input);


            static bool IsNumeric(ReadOnlySpan<char> input) =>
                input.Length > 0 && input[0] is var first &&
                (char.IsDigit(first) || first is '-' or '+');
        }

        private static byte ParseName(ReadOnlySpan<char> input)
        {
            if (IsEqual(input, nameof(Month.None)))
                return (byte)Month.None;

            else if (IsEqual(input, nameof(Month.January)))
                return (byte)Month.January;

            else if (IsEqual(input, nameof(Month.February)))
                return (byte)Month.February;

            else if (IsEqual(input, nameof(Month.March)))
                return (byte)Month.March;

            else if (IsEqual(input, nameof(Month.April)))
                return (byte)Month.April;

            else if (IsEqual(input, nameof(Month.May)))
                return (byte)Month.May;

            else if (IsEqual(input, nameof(Month.June)))
                return (byte)Month.June;

            else if (IsEqual(input, nameof(Month.July)))
                return (byte)Month.July;

            else if (IsEqual(input, nameof(Month.August)))
                return (byte)Month.August;

            else if (IsEqual(input, nameof(Month.September)))
                return (byte)Month.September;

            else if (IsEqual(input, nameof(Month.October)))
                return (byte)Month.October;

            else if (IsEqual(input, nameof(Month.November)))
                return (byte)Month.November;

            else if (IsEqual(input, nameof(Month.December)))
                return (byte)Month.December;

            else throw new FormatException(@"Enum of type 'Month' cannot be parsed.
Valid values are: [None or January or February or March or April or May or June or July or August or September or October or November or December] or number within byte range. 
Ignore case option on.");

            static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
