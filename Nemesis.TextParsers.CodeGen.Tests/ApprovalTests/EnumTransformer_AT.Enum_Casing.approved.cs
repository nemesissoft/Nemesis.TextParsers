//HEAD

using System;
using Nemesis.TextParsers;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class CasingTransformer : TransformerBase<Casing>
{
    public override string Format(Casing element) => element switch
    {
        Casing.A => nameof(Casing.A),
        Casing.a => nameof(Casing.a),
        Casing.B => nameof(Casing.B),
        Casing.b => nameof(Casing.b),
        Casing.C => nameof(Casing.C),
        Casing.c => nameof(Casing.c),
        Casing.Good => nameof(Casing.Good),
        _ => element.ToString("G"),
    };

    protected override Casing ParseCore(in ReadOnlySpan<char> input) =>
        input.IsWhiteSpace() ? default : (Casing)ParseElement(input);

    private static int ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        if (IsNumeric(input) && int.TryParse(input
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

    private static int ParseName(ReadOnlySpan<char> input)
    {    
        if (IsEqual(input, nameof(Casing.A)))
            return (int)Casing.A;            

        else if (IsEqual(input, nameof(Casing.a)))
            return (int)Casing.a;            

        else if (IsEqual(input, nameof(Casing.B)))
            return (int)Casing.B;            

        else if (IsEqual(input, nameof(Casing.b)))
            return (int)Casing.b;            

        else if (IsEqual(input, nameof(Casing.C)))
            return (int)Casing.C;            

        else if (IsEqual(input, nameof(Casing.c)))
            return (int)Casing.c;            

        else if (IsEqual(input, nameof(Casing.Good)))
            return (int)Casing.Good;            

        else throw new FormatException(@$"Enum of type 'Casing' cannot be parsed from '{input.ToString()}'.
Valid values are: [A or a or B or b or C or c or Good] or number within int range. 
Case sensitive option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
    }
}