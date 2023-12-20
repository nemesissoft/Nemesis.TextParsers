//HEAD

using System;
using Nemesis.TextParsers;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
public sealed class MonthCodeGenTransformer : TransformerBase<Month>
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

        else throw new FormatException(@$"Enum of type 'Month' cannot be parsed from '{input.ToString()}'.
Valid values are: [None or January or February or March or April or May or June or July or August or September or October or November or December] or number within byte range. 
Ignore case option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}