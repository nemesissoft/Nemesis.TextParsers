using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using LocalSettingsAttribute = Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    [Generator]
    public partial class AutoDeconstructableGenerator : ISourceGenerator
    {
        internal static readonly string DeconstructableSettingsAttributeName = typeof(LocalSettingsAttribute).FullName;
        internal const string DECONSTRUCT = "Deconstruct";
        internal const string ATTRIBUTE_NAME = @"AutoDeconstructableAttribute";
        private const string ATTRIBUTE_SOURCE = @"using System;
using System;

namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    sealed class " + ATTRIBUTE_NAME + @" : Attribute { }
}
";
        public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new DeconstructableSyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("AutoDeconstructableAttribute", SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));

            if (!(context.SyntaxReceiver is DeconstructableSyntaxReceiver receiver) || !(context.Compilation is CSharpCompilation cSharpCompilation)) return;

            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8), options));

            var autoAttributeSymbol = compilation.GetTypeByMetadataName($"Auto.{ATTRIBUTE_NAME}");
            if (autoAttributeSymbol is null)
            {
                ReportError(context, DiagnosticsId.NoAutoAttribute, null, $"Internal error: Auto.{ATTRIBUTE_NAME} is not defined");
                return;
            }

            var deconstructableSettingsAttributeSymbol = compilation.GetTypeByMetadataName(DeconstructableSettingsAttributeName);
            if (deconstructableSettingsAttributeSymbol is null)
            {
                ReportError(context, DiagnosticsId.NoSettingsAttribute, null, $"{DeconstructableSettingsAttributeName} is not recognized. Please reference Nemesis.TextParsers into your project");
                return;
            }

            foreach (var record in receiver.CandidateRecords)
            {
                //var model = compilation.GetSemanticModel(record.SyntaxTree);
                //var recordSymbol = model.GetDeclaredSymbol(record);
                //TODO get meta using: recordSymbol.GetMembers() and recordSymbol.InstanceConstructors
            }

            foreach (var type in receiver.CandidateTypes)
            {
                var model = compilation.GetSemanticModel(type.SyntaxTree);

                //TODO check why deconstructableSettingsAttributeData is null ????
                if (ShouldProcessType(type, autoAttributeSymbol, deconstructableSettingsAttributeSymbol, model, context, out var deconstructableSettingsAttributeData, out var typeSymbol)
                    && typeSymbol != null)
                {
                    if (TryGetDefaultDeconstruct(type, model, out var deconstruct, out var ctor) && deconstruct != null && ctor != null)
                    {
                        //TODO get namespaces for some exotic member types 
                        var members = ctor.ParameterList.Parameters.Select(p => (p.Identifier.Text, model.GetTypeInfo(p.Type!).Type!.ToDisplayString())).ToList();
                        if (members.Count == 0)
                            ReportWarning(context, DiagnosticsId.NoContractMembers, typeSymbol, $"{typeSymbol.Name} does not possess members for serialization");


                        var settings = deconstructableSettingsAttributeData?.ConstructorArguments is { } args ?
                            new GeneratedDeconstructableSettings(
                                args.Length > 0 ? (char)args[0].Value! : LocalSettingsAttribute.DEFAULT_DELIMITER,
                                args.Length > 1 ? (char)args[1].Value! : LocalSettingsAttribute.DEFAULT_NULL_ELEMENT_MARKER,
                                args.Length > 2 ? (char)args[2].Value! : LocalSettingsAttribute.DEFAULT_ESCAPING_SEQUENCE_START,
                                args.Length > 3 ? (char)args[3].Value! : LocalSettingsAttribute.DEFAULT_START,
                                args.Length > 4 ? (char)args[4].Value! : LocalSettingsAttribute.DEFAULT_END
                            )
                            : (GeneratedDeconstructableSettings?)null;


                        //TODO GENERATE: append namespaces from source file
                        var namespaces = new HashSet<string> { "System", "Nemesis.TextParsers.Parsers", "Nemesis.TextParsers.Utils" };

                        string typeModifiers = type.Modifiers + " " + type switch
                        {
                            ClassDeclarationSyntax _ => "class",
                            StructDeclarationSyntax _ => "struct",
                            RecordDeclarationSyntax _ => "record",
                            _ => throw new NotSupportedException("Only class, struct or record types are allowed")
                        };

                        string classSource = RenderRecord(typeSymbol, typeModifiers, members, settings, namespaces, context);
                        context.AddSource($"{typeSymbol.Name}_AutoDeconstructable.cs", SourceText.From(classSource, Encoding.UTF8));
                    }
                    else
                    {
                        ReportError(context, DiagnosticsId.NoMatchingCtorAndDeconstruct, typeSymbol, $"{typeSymbol.Name} does not possess matching constructor and Deconstruct pair");
                    }
                }
            }
        }

        private static bool ShouldProcessType(TypeDeclarationSyntax type, ISymbol autoAttributeSymbol, ISymbol deconstructableSettingsAttributeSymbol,
            SemanticModel? model, in GeneratorExecutionContext context, out AttributeData? deconstructableSettingsAttributeData, out INamedTypeSymbol? typeSymbol)
        {
            static AttributeData? GetAttribute(ISymbol typeSymbol, ISymbol attributeSymbol) =>
                typeSymbol.GetAttributes().FirstOrDefault(ad =>
                    ad != null && ad.AttributeClass is { } @class &&
                    @class.Equals(attributeSymbol, SymbolEqualityComparer.Default));

            deconstructableSettingsAttributeData = null;
            typeSymbol = null;

            if (model.GetDeclaredSymbol(type) is { } ts && GetAttribute(ts, autoAttributeSymbol) != null)
            {
                if (type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    if (GetAttribute(ts, deconstructableSettingsAttributeSymbol) is { } settingsAttributeData)
                    {
                        if (settingsAttributeData.ConstructorArguments.All(a => a.Kind == TypedConstantKind.Primitive && a.Value is char))
                        {
                            deconstructableSettingsAttributeData = settingsAttributeData;
                            typeSymbol = ts;
                            return true;
                        }
                        else
                            ReportError(context, DiagnosticsId.NonPrimitiveCharacters, ts, $"Attribute {DeconstructableSettingsAttributeName} must be constructed with primitive character instances");
                    }
                    else
                    {
                        typeSymbol = ts;
                        return true;
                    }
                }
                else
                    ReportError(context, DiagnosticsId.NonPartialType, ts, $"Type decorated with {ATTRIBUTE_NAME} must be also declared partial");
            }
            return false;
        }

        //TODO - take all from ISymbol, not TypeDeclarationSyntax 
        private static bool TryGetDefaultDeconstruct(TypeDeclarationSyntax type, SemanticModel semanticModel, out MethodDeclarationSyntax? deconstruct, out ConstructorDeclarationSyntax? ctor)
        {
            deconstruct = default;
            ctor = default;

            var ctors = type.ChildNodes().OfType<ConstructorDeclarationSyntax>()
                .Where(c => c.ParameterList.Parameters.Count > 0)
                .ToList();
            if (ctors.Count == 0) return false;

            var deconstructs = type
                .ChildNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => string.Equals(m.Identifier.Text, DECONSTRUCT, StringComparison.Ordinal) &&
                           m.ParameterList.Parameters is var @params &&
                           @params.Count > 0 &&
                           @params.All(p => p.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OutKeyword)))

                           )
                .Select(m => (method: m, @params: m.ParameterList.Parameters))
                .OrderByDescending(p => p.@params.Count);

            foreach (var (method, @params) in deconstructs)
            {
                var compatibleCtor = ctors.FirstOrDefault(c => IsCompatible(c.ParameterList.Parameters, @params, semanticModel));
                if (compatibleCtor == null) continue;

                deconstruct = method;
                ctor = compatibleCtor;

                return true;
            }

            return false;
        }

        private static bool IsCompatible(IReadOnlyList<ParameterSyntax> left, IReadOnlyList<ParameterSyntax> right, SemanticModel model)
        {
            bool AreEqualByParamTypes()
            {
                for (var i = 0; i < left.Count; i++)
                {
                    TypeSyntax? leftType = left[i].Type, rightType = right[i].Type;

                    if (leftType == null || rightType == null) return false;

                    var leftTypeSymbol = model.GetTypeInfo(leftType);
                    var rightTypeSymbol = model.GetTypeInfo(rightType);

                    if (leftTypeSymbol.Type == null || rightTypeSymbol.Type == null) return false;

                    if (!leftTypeSymbol.Equals(rightTypeSymbol))
                        return false;
                }
                return true;
            }

            return left.Count == right.Count && AreEqualByParamTypes();
        }
    }
}
