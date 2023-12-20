//HEAD

using System;
using Nemesis.TextParsers;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class DaysOfWeekTransformer : TransformerBase<DaysOfWeek>
{
    public override string Format(DaysOfWeek element) => element switch
    {
        DaysOfWeek.None => nameof(DaysOfWeek.None),
        DaysOfWeek.Monday => nameof(DaysOfWeek.Monday),
        DaysOfWeek.Tuesday => nameof(DaysOfWeek.Tuesday),
        DaysOfWeek.Wednesday => nameof(DaysOfWeek.Wednesday),
        DaysOfWeek.Thursday => nameof(DaysOfWeek.Thursday),
        DaysOfWeek.Friday => nameof(DaysOfWeek.Friday),
        DaysOfWeek.Saturday => nameof(DaysOfWeek.Saturday),
        DaysOfWeek.Sunday => nameof(DaysOfWeek.Sunday),
        DaysOfWeek.Weekdays => nameof(DaysOfWeek.Weekdays),
        DaysOfWeek.Weekends => nameof(DaysOfWeek.Weekends),
        DaysOfWeek.All => nameof(DaysOfWeek.All),
        _ => element.ToString("G"),
    };

    protected override DaysOfWeek ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsWhiteSpace()) return default;

        var enumStream = input.Split(',').GetEnumerator();

        if (!enumStream.MoveNext()) 
            throw new FormatException($"At least one element is expected to parse 'DaysOfWeek' enum");
        var currentValue = ParseElement(enumStream.Current);

        while (enumStream.MoveNext())
        {
            var element = ParseElement(enumStream.Current);

            currentValue = (byte)(currentValue | element);
        }

        return (DaysOfWeek)currentValue;
    }

    private static byte ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        if (IsNumeric(input) && byte.TryParse(input
#if NETFRAMEWORK
    .ToString() //legacy frameworks do not support parsing from ReadOnlySpan<char>
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
        if (IsEqual(input, nameof(DaysOfWeek.None)))
            return (byte)DaysOfWeek.None;            

        else if (IsEqual(input, nameof(DaysOfWeek.Monday)))
            return (byte)DaysOfWeek.Monday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Tuesday)))
            return (byte)DaysOfWeek.Tuesday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Wednesday)))
            return (byte)DaysOfWeek.Wednesday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Thursday)))
            return (byte)DaysOfWeek.Thursday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Friday)))
            return (byte)DaysOfWeek.Friday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Saturday)))
            return (byte)DaysOfWeek.Saturday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Sunday)))
            return (byte)DaysOfWeek.Sunday;            

        else if (IsEqual(input, nameof(DaysOfWeek.Weekdays)))
            return (byte)DaysOfWeek.Weekdays;            

        else if (IsEqual(input, nameof(DaysOfWeek.Weekends)))
            return (byte)DaysOfWeek.Weekends;            

        else if (IsEqual(input, nameof(DaysOfWeek.All)))
            return (byte)DaysOfWeek.All;            

        else throw new FormatException(@$"Enum of type 'DaysOfWeek' cannot be parsed from '{input.ToString()}'.
Valid values are: [None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All] or number within byte range. 
Ignore case option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}