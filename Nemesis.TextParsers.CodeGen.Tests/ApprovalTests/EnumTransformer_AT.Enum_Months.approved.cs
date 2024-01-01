//HEAD

using System;
using Nemesis.TextParsers;
namespace SpecialNamespace;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class MonthsTransformer : TransformerBase<Months>
{
    public override string Format(Months element) => element switch
    {
        Months.None => nameof(Months.None),
        Months.January => nameof(Months.January),
        Months.February => nameof(Months.February),
        Months.March => nameof(Months.March),
        Months.April => nameof(Months.April),
        Months.May => nameof(Months.May),
        Months.June => nameof(Months.June),
        Months.July => nameof(Months.July),
        Months.August => nameof(Months.August),
        Months.September => nameof(Months.September),
        Months.October => nameof(Months.October),
        Months.November => nameof(Months.November),
        Months.December => nameof(Months.December),
        Months.Summer => nameof(Months.Summer),
        Months.All => nameof(Months.All),
        _ => element.ToString("G"),
    };

    protected override Months ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsWhiteSpace()) return default;

        var enumStream = input.Split(',').GetEnumerator();

        if (!enumStream.MoveNext()) 
            throw new FormatException($"At least one element is expected to parse 'Months' enum");
        var currentValue = ParseElement(enumStream.Current);

        while (enumStream.MoveNext())
        {
            var element = ParseElement(enumStream.Current);

            currentValue = (short)(currentValue | element);
        }

        return (Months)currentValue;
    }

    private static short ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        return ParseName(input);
    }

    private static short ParseName(ReadOnlySpan<char> input)
    {    
        if (IsEqual(input, nameof(Months.None)))
            return (short)Months.None;            

        else if (IsEqual(input, nameof(Months.January)))
            return (short)Months.January;            

        else if (IsEqual(input, nameof(Months.February)))
            return (short)Months.February;            

        else if (IsEqual(input, nameof(Months.March)))
            return (short)Months.March;            

        else if (IsEqual(input, nameof(Months.April)))
            return (short)Months.April;            

        else if (IsEqual(input, nameof(Months.May)))
            return (short)Months.May;            

        else if (IsEqual(input, nameof(Months.June)))
            return (short)Months.June;            

        else if (IsEqual(input, nameof(Months.July)))
            return (short)Months.July;            

        else if (IsEqual(input, nameof(Months.August)))
            return (short)Months.August;            

        else if (IsEqual(input, nameof(Months.September)))
            return (short)Months.September;            

        else if (IsEqual(input, nameof(Months.October)))
            return (short)Months.October;            

        else if (IsEqual(input, nameof(Months.November)))
            return (short)Months.November;            

        else if (IsEqual(input, nameof(Months.December)))
            return (short)Months.December;            

        else if (IsEqual(input, nameof(Months.Summer)))
            return (short)Months.Summer;            

        else if (IsEqual(input, nameof(Months.All)))
            return (short)Months.All;            

        else throw new FormatException(@$"Enum of type 'Months' cannot be parsed from '{input.ToString()}'.
Valid values are: [None or January or February or March or April or May or June or July or August or September or October or November or December or Summer or All]. 
Case sensitive option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
    }
}