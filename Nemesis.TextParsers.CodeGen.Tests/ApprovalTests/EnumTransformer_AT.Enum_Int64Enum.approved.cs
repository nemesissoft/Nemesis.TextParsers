//HEAD

using System;
using Nemesis.TextParsers;
namespace ContainingNamespace;

[System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
[System.Runtime.CompilerServices.CompilerGenerated]
internal sealed class Int64EnumTransformer : TransformerBase<ContainingNamespace.Int64Enum>
{
    public override string Format(ContainingNamespace.Int64Enum element) => element switch
    {
        ContainingNamespace.Int64Enum.L1 => nameof(ContainingNamespace.Int64Enum.L1),
        ContainingNamespace.Int64Enum.L2 => nameof(ContainingNamespace.Int64Enum.L2),
        ContainingNamespace.Int64Enum.L3 => nameof(ContainingNamespace.Int64Enum.L3),
        ContainingNamespace.Int64Enum.L4 => nameof(ContainingNamespace.Int64Enum.L4),
        _ => element.ToString("G"),
    };

    protected override ContainingNamespace.Int64Enum ParseCore(in ReadOnlySpan<char> input) =>
        input.IsWhiteSpace() ? default : (ContainingNamespace.Int64Enum)ParseElement(input);

    private static long ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();
        return ParseName(input);
    }

    private static long ParseName(ReadOnlySpan<char> input)
    {    
        if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L1)))
            return (long)ContainingNamespace.Int64Enum.L1;            

        else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L2)))
            return (long)ContainingNamespace.Int64Enum.L2;            

        else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L3)))
            return (long)ContainingNamespace.Int64Enum.L3;            

        else if (IsEqual(input, nameof(ContainingNamespace.Int64Enum.L4)))
            return (long)ContainingNamespace.Int64Enum.L4;            

        else throw new FormatException(@$"Enum of type 'ContainingNamespace.Int64Enum' cannot be parsed from '{input.ToString()}'.
Valid values are: [L1 or L2 or L3 or L4]. 
Case sensitive option on.");        

        static bool IsEqual(ReadOnlySpan<char> input, string label) =>
            MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.Ordinal);
    }
}