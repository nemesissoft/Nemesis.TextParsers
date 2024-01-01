namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture]
public class EnumParsing_ConceptTests
{
    private static string ToTick(bool result) => result ? "✔" : "✖";
    private static string ToOperator(bool result) => result ? "==" : "!=";

    [Flags]
    enum DaysOfWeek : byte
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

    [Test]
    public void Concept_ParseViaExpressionTrees()
    {
        var failed = new List<string>();

        for (int i = 0; i < 135; i++)
        {
            var enumValue = (DaysOfWeek)i;
            var text = enumValue.ToString("G");

            var actual = ParseDaysOfWeek(text.AsSpan());
            var actualNative = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);

            bool pass = Equals(actual, enumValue) &&
                        Equals(actualNative, enumValue) &&
                        Equals(actual, actualNative);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}";

            if (!pass)
                failed.Add(message);
        }

        Assert.That(failed, Is.Empty, () =>
            $"Failed cases:{Environment.NewLine}{string.Join(Environment.NewLine, failed)}"
        );
    }

    private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;

        var enumStream = input.Split(',').GetEnumerator();

        if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {nameof(DaysOfWeek)} enum");
        byte currentValue = ParseDaysOfWeekElement(enumStream.Current);

        while (enumStream.MoveNext())
        {
            var element = ParseDaysOfWeekElement(enumStream.Current);

            currentValue |= element;
        }

        return (DaysOfWeek)currentValue;

    }

    private static byte ParseDaysOfWeekElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();

        return IsNumeric(input) && byte.TryParse(
#if NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , out byte number) ? number : ParseDaysOfWeekByLabelOr(input);

        static bool IsNumeric(ReadOnlySpan<char> input) =>
                input.Length > 0 && input[0] is var first &&
                (char.IsDigit(first) || first is '-' or '+');
    }

    /*private static byte ParseDaysOfWeekByLabelStd(ReadOnlySpan<char> text)
    {
        if (text.Length == 4 && char.ToUpper(text[0]) == 'N' && char.ToUpper(text[1]) == 'O' &&
            char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'E'
        )
            return 0;
        else if (text.Length == 6 && char.ToUpper(text[0]) == 'M' && char.ToUpper(text[1]) == 'O' &&
            char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' &&
            char.ToUpper(text[5]) == 'Y'
        )
            return 1;
        else if (text.Length == 7 && char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'U' &&
            char.ToUpper(text[2]) == 'E' && char.ToUpper(text[3]) == 'S' && char.ToUpper(text[4]) == 'D' &&
            char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y'
        )
            return 2;
        else if (text.Length == 9 && char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' &&
            char.ToUpper(text[2]) == 'D' && char.ToUpper(text[3]) == 'N' && char.ToUpper(text[4]) == 'E' &&
            char.ToUpper(text[5]) == 'S' && char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'A' &&
            char.ToUpper(text[8]) == 'Y'
        )
            return 4;
        else if (text.Length == 8 && char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'H' &&
            char.ToUpper(text[2]) == 'U' && char.ToUpper(text[3]) == 'R' &&
            char.ToUpper(text[4]) == 'S' && char.ToUpper(text[5]) == 'D' &&
            char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y'
        )
            return 8;
        else if (text.Length == 6 && char.ToUpper(text[0]) == 'F' && char.ToUpper(text[1]) == 'R' &&
            char.ToUpper(text[2]) == 'I' && char.ToUpper(text[3]) == 'D' &&
            char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y'
        )
            return 16;
        else if (text.Length == 8 && char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'A' &&
            char.ToUpper(text[2]) == 'T' && char.ToUpper(text[3]) == 'U' &&
            char.ToUpper(text[4]) == 'R' && char.ToUpper(text[5]) == 'D' &&
            char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y'
        )
            return 32;
        else if (text.Length == 6 && char.ToUpper(text[0]) == 'S' &&
            char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'N' &&
            char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' &&
            char.ToUpper(text[5]) == 'Y'
        )
            return 64;
        else if (text.Length == 8 && char.ToUpper(text[0]) == 'W' &&
            char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
            char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'D' &&
            char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y' &&
            char.ToUpper(text[7]) == 'S'
        )
            return 31;
        else if (text.Length == 8 && char.ToUpper(text[0]) == 'W' &&
            char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
            char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'E' &&
            char.ToUpper(text[5]) == 'N' && char.ToUpper(text[6]) == 'D' &&
            char.ToUpper(text[7]) == 'S'
        )
            return 96;
        else if (text.Length == 3 && char.ToUpper(text[0]) == 'A' &&
                 char.ToUpper(text[1]) == 'L' && char.ToUpper(text[2]) == 'L'
        )
            return 127;
        else
            throw new FormatException("Enum of type 'DaysOfWeek' cannot be parsed. Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");
        //return 0;
    }

    private static byte ParseDaysOfWeekByLabelSwitch(ReadOnlySpan<char> text)
    {
        switch (text.Length)
        {
            case 4:
                if (char.ToUpper(text[0]) == 'N' && char.ToUpper(text[1]) == 'O' && char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'E')
                    return 0;
                else
                    break;
            case 6:
                if (char.ToUpper(text[0]) == 'M' && char.ToUpper(text[1]) == 'O' && char.ToUpper(text[2]) == 'N' &&
                    char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                    return 1;
                else if (char.ToUpper(text[0]) == 'F' && char.ToUpper(text[1]) == 'R' && char.ToUpper(text[2]) == 'I' &&
                         char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                    return 16;
                else if (char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'N' &&
                         char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                    return 64;
                else
                    break;
            case 7:
                if (char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'E' &&
                    char.ToUpper(text[3]) == 'S' && char.ToUpper(text[4]) == 'D' && char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y')
                    return 2;
                else
                    break;
            case 8:
                if (char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'H' && char.ToUpper(text[2]) == 'U' &&
                    char.ToUpper(text[3]) == 'R' && char.ToUpper(text[4]) == 'S' && char.ToUpper(text[5]) == 'D' &&
                    char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y')
                    return 8;
                else if (char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'A' && char.ToUpper(text[2]) == 'T' &&
                         char.ToUpper(text[3]) == 'U' && char.ToUpper(text[4]) == 'R' && char.ToUpper(text[5]) == 'D' &&
                         char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y')
                    return 32;

                else if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                         char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'D' && char.ToUpper(text[5]) == 'A' &&
                         char.ToUpper(text[6]) == 'Y' && char.ToUpper(text[7]) == 'S')
                    return 31;
                else if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                         char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'E' && char.ToUpper(text[5]) == 'N' &&
                         char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'S')
                    return 96;
                else
                    break;
            case 9:
                if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'D' &&
                    char.ToUpper(text[3]) == 'N' && char.ToUpper(text[4]) == 'E' && char.ToUpper(text[5]) == 'S' &&
                    char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'A' && char.ToUpper(text[8]) == 'Y')
                    return 4;
                else
                    break;
            default:
                if (char.ToUpper(text[0]) == 'A' && char.ToUpper(text[1]) == 'L' && char.ToUpper(text[2]) == 'L')
                    return 127;
                else
                    break;
        }
        throw new FormatException("Enum of type 'DaysOfWeek' cannot be parsed. Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");

    }*/

    private static byte ParseDaysOfWeekByLabelOr(ReadOnlySpan<char> input)
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



    //for tests this does not need to be added: [Transformer(typeof(MonthCodeGenTransformer))]
    enum Month : byte
    {
        None = 0,
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    [Test]
    public void Concept_ParseViaGeneratedCode()
    {
        var failed = new List<string>();
        var sut = new MonthCodeGenTransformer();

        for (int i = 0; i < 15; i++)
        {
            var enumValue = (Month)i;
            var text = enumValue.ToString("G");
            text = i % 2 == 0 ? text : text.ToUpperInvariant();

            var actual = sut.Parse(text.AsSpan());
            var actualNative = (Month)Enum.Parse(typeof(Month), text, true);

            bool pass = Equals(actual, enumValue) &&
                        Equals(actualNative, enumValue) &&
                        Equals(actual, actualNative);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}";

            if (!pass)
                failed.Add(message);
        }

        Assert.That(failed, Is.Empty, () =>
            $"Failed cases:{Environment.NewLine}{string.Join(Environment.NewLine, failed)}"
        );
    }


    sealed class MonthCodeGenTransformer : TransformerBase<Month>
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

        private static byte ParseElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();
            if (IsNumeric(input) && byte.TryParse(input
#if NETFRAMEWORK
    .ToString()
#endif
                , out var number))
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