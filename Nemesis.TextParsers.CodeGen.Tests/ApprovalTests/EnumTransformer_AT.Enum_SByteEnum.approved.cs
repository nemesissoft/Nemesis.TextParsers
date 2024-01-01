//HEAD

using System;
using Nemesis.TextParsers;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class SByteEnumTransformer : TransformerBase<SByteEnum>
{
    public override string Format(SByteEnum element) => element switch
    {
        SByteEnum.Sb1 => nameof(SByteEnum.Sb1),
        SByteEnum.Sb2 => nameof(SByteEnum.Sb2),
        SByteEnum.Sb3 => nameof(SByteEnum.Sb3),
        SByteEnum.Sb4 => nameof(SByteEnum.Sb4),
        _ => element.ToString("G"),
    };

    protected override SByteEnum ParseCore(in ReadOnlySpan<char> input) =>
        input.IsWhiteSpace() ? default : (SByteEnum)ParseElement(input);

    private static sbyte ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        return ParseName(input);
    }

    private static sbyte ParseName(ReadOnlySpan<char> input)
    {    
        if (IsEqual(input, nameof(SByteEnum.Sb1)))
            return (sbyte)SByteEnum.Sb1;            

        else if (IsEqual(input, nameof(SByteEnum.Sb2)))
            return (sbyte)SByteEnum.Sb2;            

        else if (IsEqual(input, nameof(SByteEnum.Sb3)))
            return (sbyte)SByteEnum.Sb3;            

        else if (IsEqual(input, nameof(SByteEnum.Sb4)))
            return (sbyte)SByteEnum.Sb4;            

        else throw new FormatException(@$"Enum of type 'SByteEnum' cannot be parsed from '{input.ToString()}'.
Valid values are: [Sb1 or Sb2 or Sb3 or Sb4]. 
Ignore case option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}