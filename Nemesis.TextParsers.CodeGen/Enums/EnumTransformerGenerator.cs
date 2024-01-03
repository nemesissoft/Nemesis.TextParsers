#nullable enable
using Nemesis.TextParsers.CodeGen.Utils;

namespace Nemesis.TextParsers.CodeGen.Enums;

[Generator]
public sealed partial class EnumTransformerGenerator : IncrementalGenerator
{
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            $"{ATTRIBUTE_NAME}.g.cs", SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8)));

        var transformersToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ATTRIBUTE_FULL_NAME,
                predicate: static (node, _) => node is EnumDeclarationSyntax,
                transform: GetTypeToGenerate)
            .Where(static result => result.IsError || (result.IsSuccess && result.Value is not null))
            .WithTrackingName(INPUTS);

        context.RegisterSourceOutput(transformersToGenerate,
           static (spc, result) => Execute(result, spc));
    }

    private static void Execute(Result<TransformerMeta?, Diagnostic> result, SourceProductionContext context)
    {
        if (result.IsError && result.Error is { } error)
            context.ReportDiagnostic(error);
        else if (result.IsSuccess && result.Value is { } meta)
        {
            var source = Render(in meta);
            context.AddSource($"{meta.TransformerName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string Render(in TransformerMeta meta)
    {
        var sb = new StringBuilder(HEADER, 1024).AppendLine();

        var enumName = meta.EnumFullyQualifiedName;
        var numberType = meta.UnderlyingType;
        var memberNames = meta.MemberNames;

        sb.AppendLine($$"""
            using System;
            using Nemesis.TextParsers;
            {{(
              string.IsNullOrEmpty(meta.TransformerNamespace)
                ? ""
                : $"""
                namespace {meta.TransformerNamespace};

                """
            )}}
            {{CODE_GEN_ATTRIBUTES}}
            {{(meta.IsPublic ? "public" : "internal")}} sealed class {{meta.TransformerName}} : TransformerBase<{{enumName}}>
            {
            """);


        //Format
        sb.AppendLine($$"""
                public override string Format({{enumName}} element) => element switch
                {
            """);

        foreach (var member in memberNames)
            sb.AppendLine($$"""
                    {{enumName}}.{{member}} => nameof({{enumName}}.{{member}}),
            """);

        sb.AppendLine("""
                    _ => element.ToString("G"),
                };
            """).AppendLine();

        //ParseCore
        if (meta.IsFlagEnum)
        {
            sb.AppendLine($$"""
                protected override {{enumName}} ParseCore(in ReadOnlySpan<char> input)
                {
                    if (input.IsWhiteSpace()) return default;

                    var enumStream = input.Split(',').GetEnumerator();

                    if (!enumStream.MoveNext()) 
                        throw new FormatException($"At least one element is expected to parse '{{enumName}}' enum");
                    var currentValue = ParseElement(enumStream.Current);

                    while (enumStream.MoveNext())
                    {
                        var element = ParseElement(enumStream.Current);

                        currentValue = ({{numberType}})(currentValue | element);
                    }

                    return ({{enumName}})currentValue;
                }
            """).AppendLine();
        }
        else
        {
            sb.AppendLine($$"""
                protected override {{enumName}} ParseCore(in ReadOnlySpan<char> input) =>
                    input.IsWhiteSpace() ? default : ({{enumName}})ParseElement(input);
            """).AppendLine();
        }


        //ParseElement
        sb.AppendLine($$"""
                private static {{numberType}} ParseElement(ReadOnlySpan<char> input)
                {
                    if (input.IsEmpty || input.IsWhiteSpace()) return default;
                    input = input.Trim();
            """);

        if (meta.AllowParsingNumerics)
        {
            sb.AppendLine($$"""
                    if (IsNumeric(input) && {{numberType}}.TryParse(input
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
            """);
        }
        else
        {
            sb.AppendLine($$"""
                    return ParseName(input);
            """);
        }

        sb.AppendLine("""
                }
            """).AppendLine();

        //ParseName
        sb.AppendLine($$"""
                private static {{numberType}} ParseName(ReadOnlySpan<char> input)
                {    
            """);

        bool hasAnyMembers = memberNames.Count > 0;
        bool isFirstBranch = true;
        foreach (var member in memberNames)
        {
            var @else = isFirstBranch ? "" : "else ";
            if (isFirstBranch)
                isFirstBranch = false;

            sb.AppendLine($$"""
                    {{@else}}if (IsEqual(input, nameof({{enumName}}.{{member}})))
                        return ({{numberType}}){{enumName}}.{{member}};            
            """).AppendLine();
        }

        var numberParsingText = meta.AllowParsingNumerics ? $" or number within {numberType} range. " : ". ";
        var caseInsensitiveText = meta.CaseInsensitive ? "Ignore case option on." : "Case sensitive option on.";
        var exceptionMessage = $$"""
            Enum of type '{{enumName}}' cannot be parsed from '{input.ToString()}'.
            Valid values are: [{{string.Join(" or ", memberNames)}}]{{numberParsingText}}
            {{caseInsensitiveText}}
            """;
        sb.AppendLine($$""""
                    {{(hasAnyMembers ? "else " : "")}}throw new FormatException(@$"{{exceptionMessage.Replace("\"", "\"\"")}}");        
            """");


        var stringComparison = meta.CaseInsensitive ? nameof(StringComparison.OrdinalIgnoreCase) : nameof(StringComparison.Ordinal);
        if (hasAnyMembers)
            sb.AppendLine().AppendLine($$"""
                    static bool IsEqual(ReadOnlySpan<char> input, string label) =>
                        MemoryExtensions.Equals(input, label.AsSpan(), StringComparison.{{stringComparison}});
            """);

        sb.AppendLine("""
                }
            """);





        // end of class
        sb.Append("""
            }
            """);

        return sb.ToString();
    }
}
