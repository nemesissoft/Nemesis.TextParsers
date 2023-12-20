//HEAD

using System;
using Nemesis.TextParsers;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class EmptyEnumTransformer : TransformerBase<EmptyEnum>
{
    public override string Format(EmptyEnum element) => element switch
    {
        _ => element.ToString("G"),
    };

    protected override EmptyEnum ParseCore(in ReadOnlySpan<char> input) =>
        input.IsWhiteSpace() ? default : (EmptyEnum)ParseElement(input);

    private static ulong ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        if (IsNumeric(input) && ulong.TryParse(input
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

    private static ulong ParseName(ReadOnlySpan<char> input)
    {    
        throw new FormatException(@$"Enum of type 'EmptyEnum' cannot be parsed from '{input.ToString()}'.
Valid values are: [] or number within ulong range. 
Case sensitive option on.");        
    }
}