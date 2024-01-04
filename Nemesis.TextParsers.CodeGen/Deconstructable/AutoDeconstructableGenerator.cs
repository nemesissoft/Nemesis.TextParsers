using Nemesis.TextParsers.CodeGen.Utils;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable;

[Generator]
public partial class AutoDeconstructableGenerator : ISourceGenerator
{
    internal const string DECONSTRUCT = "Deconstruct";
    internal const string ATTRIBUTE_NAME = @"AutoDeconstructableAttribute";
    private const string ATTRIBUTE_SOURCE = @"using System;
namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    sealed class " + ATTRIBUTE_NAME + @" : Attribute { }
}
";
    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new DeconstructableSyntaxReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        context.CheckDebugger(nameof(AutoDeconstructableGenerator));

        /*if (!context.Compilation.ReferencedAssemblyNames.Any(ai => ai.Name.Equals("Nemesis.TextParsers", StringComparison.OrdinalIgnoreCase)))
            context.ReportDiagnostic(); */

        context.AddSource("AutoDeconstructableAttribute", SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));

        if (context.SyntaxReceiver is not DeconstructableSyntaxReceiver receiver || context.Compilation is not CSharpCompilation cSharpCompilation) return;

        var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
        var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8), options));

        var autoAttributeSymbol = compilation.GetTypeByMetadataName($"Auto.{ATTRIBUTE_NAME}");
        if (autoAttributeSymbol is null)
        {
            ReportDiagnostics(context, NoAutoAttributeRule, null);
            return;
        }

        /*var allTypes = compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>().Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute"))
            .Where(t => t != null).ToList();*/

        var deconstructableSettingsAttributeSymbol = compilation.GetTypeByMetadataName(DeconstructableSettingsAttributeName);
        if (deconstructableSettingsAttributeSymbol is null)
        {
            ReportDiagnostics(context, NoSettingsAttributeRule, null);
            return;
        }

        foreach (var type in receiver.CandidateTypes)
        {
            var model = compilation.GetSemanticModel(type.SyntaxTree);

            if (ShouldProcessType(type, autoAttributeSymbol, deconstructableSettingsAttributeSymbol, model, context, out var deconstructableSettingsAttributeData, out var typeSymbol)
                && typeSymbol != null)
            {
                if (!typeSymbol.ContainingSymbol.Equals(typeSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                {
                    ReportDiagnostics(context, NamespaceAndTypeNamesEqualRule, typeSymbol);
                    continue;
                }

                var namespaces = new HashSet<string> { "System", "Nemesis.TextParsers.Parsers", "Nemesis.TextParsers.Utils", "Nemesis.TextParsers" };
                if (type.SyntaxTree.GetRoot() is CompilationUnitSyntax compilationUnit)
                {
                    var sourceNamespacesWithoutUsing = compilationUnit.Usings.Select(u => u
                            .WithUsingKeyword(SyntaxFactory.MissingToken(SyntaxKind.UsingKeyword))
                            .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken))
                            .ToString())
                        .ToList();

                    foreach (var ns in sourceNamespacesWithoutUsing)
                        namespaces.Add(ns);
                }

                if (TryGetMembers(typeSymbol, context, namespaces, out var members) && members != null)
                {
                    var settings = GeneratedDeconstructableSettings.FromDeconstructableSettingsAttribute(deconstructableSettingsAttributeData);

                    string typeModifiers = GetTypeModifiers(type, typeSymbol);

                    string classSource = RenderRecord(typeSymbol, typeModifiers, members, settings, namespaces);
                    context.AddSource($"{typeSymbol.Name}_AutoDeconstructable.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }
    }

    private static string GetTypeModifiers(TypeDeclarationSyntax type, INamedTypeSymbol typeSymbol) =>
        type.Modifiers + " " + type switch
        {
            ClassDeclarationSyntax => "class",
            StructDeclarationSyntax => "struct",
            RecordDeclarationSyntax => typeSymbol.IsReferenceType ? "record" : "record struct",
            _ => throw new NotSupportedException("Only class, struct or record types are allowed")
        };

    private static bool ShouldProcessType(TypeDeclarationSyntax type, ISymbol autoAttributeSymbol, ISymbol deconstructableSettingsAttributeSymbol,
        SemanticModel? model, in GeneratorExecutionContext context, out AttributeData? deconstructableSettingsAttributeData, out INamedTypeSymbol? typeSymbol)
    {
        static AttributeData? GetAttribute(ISymbol typeSymbol, ISymbol attributeSymbol) =>
            typeSymbol.GetAttributes().FirstOrDefault(ad =>
                ad?.AttributeClass is { } @class && @class.Equals(attributeSymbol, SymbolEqualityComparer.Default));


        deconstructableSettingsAttributeData = null;
        typeSymbol = null;

        if (model.GetDeclaredSymbol(type) is { } ts && GetAttribute(ts, autoAttributeSymbol) != null)
        {
            if (type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                if (GetAttribute(ts, deconstructableSettingsAttributeSymbol) is { } settingsAttributeData)
                {
                    if (settingsAttributeData.ConstructorArguments.Length == 6 &&
                        settingsAttributeData.ConstructorArguments.All(a => a.Kind == TypedConstantKind.Primitive && (a.Value is char or bool)))
                    {
                        deconstructableSettingsAttributeData = settingsAttributeData;
                        typeSymbol = ts;
                        return true;
                    }
                    else
                        ReportDiagnostics(context, InvalidSettingsAttributeRule, ts);
                }
                else
                {
                    typeSymbol = ts;
                    return true;
                }
            }
            else
                ReportDiagnostics(context, NonPartialTypeRule, ts);
        }
        return false;
    }

    private static bool TryGetMembers(INamedTypeSymbol typeSymbol, in GeneratorExecutionContext context, ISet<string> namespaces, out IReadOnlyList<(string Name, string Type)>? members)
    {
        members = default;

        var ctors = typeSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility != Accessibility.Private)
            .ToList();
        if (ctors.Count == 0)
        {
            ReportDiagnostics(context, NoConstructor, typeSymbol);
            return false;
        }

        var deconstructs = typeSymbol.GetMembers().Where(s => s.Kind == SymbolKind.Method).OfType<IMethodSymbol>()
            .Where(m => string.Equals(m.Name, DECONSTRUCT, StringComparison.Ordinal) &&
                        m.Parameters is var @params &&
                        @params.All(p => p.RefKind == RefKind.Out)
            )
            .Select(m => (method: m, @params: m.Parameters))
            .OrderByDescending(p => p.@params.Length).ToList();
        if (deconstructs.Count == 0)
        {
            ReportDiagnostics(context, NoDeconstruct, typeSymbol);
            return false;
        }

        foreach (var (_, @params) in deconstructs)
        {
            var ctor = ctors.FirstOrDefault(c => IsCompatible(c.Parameters, @params));
            if (ctor == null) continue;

            static string GetTypeMinimalName(ITypeSymbol ts)
            {
                return ts.ContainingType is { } containingType
                    ? $"{containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.{ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"
                    : ts.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            }

            members = ctor.Parameters.Select(p => (p.Name, GetTypeMinimalName(p.Type))).ToList();

            if (members.Count > 0)
            {
                foreach (var parameter in ctor.Parameters)
                    ExtractNamespaces(parameter.Type, namespaces);
                return true;
            }
        }

        if (members != null && members.Count == 0)
        {
            ReportDiagnostics(context, NoContractMembersRule, typeSymbol);
            return false;
        }

        ReportDiagnostics(context, NoMatchingCtorAndDeconstructRule, typeSymbol);
        return false;
    }

    private static bool IsCompatible(IReadOnlyList<IParameterSymbol> left, IReadOnlyList<IParameterSymbol> right)
    {
        bool AreEqualByParamTypes()
        {
            for (var i = 0; i < left.Count; i++)
            {
                ITypeSymbol? leftType = left[i]?.Type, rightType = right[i]?.Type;

                if (leftType == null || rightType == null) return false;

                if (!leftType.Equals(rightType, SymbolEqualityComparer.Default))
                    return false;
            }
            return true;
        }

        return left.Count == right.Count && AreEqualByParamTypes();
    }
}
