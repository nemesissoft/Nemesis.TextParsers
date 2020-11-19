using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    [Generator]
    public partial class AutoDeconstructableGenerator : ISourceGenerator
    {
        internal const string DECONSTRUCT = "Deconstruct";
        internal const string ATTRIBUTE_NAME = @"AutoDeconstructableAttribute";
        private const string ATTRIBUTE_SOURCE = @"using System;
using System;

namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    sealed class " + ATTRIBUTE_NAME + @" : Attribute
    {
        public char Delimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char Start { get; }
        public char End { get; }

        public " + ATTRIBUTE_NAME + @"(char delimiter, char nullElementMarker, char escapingSequenceStart, char start = '\0', char end = '\0')
        {
            Delimiter = delimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
        }
    }
}
";
        public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new DeconstructableSyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("AutoDeconstructableAttribute", SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));

            if (!(context.SyntaxReceiver is DeconstructableSyntaxReceiver receiver) || !(context.Compilation is CSharpCompilation cSharpCompilation)) return;

            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8), options));

            var attributeSymbol = compilation.GetTypeByMetadataName($"Auto.{ATTRIBUTE_NAME}");
            if (attributeSymbol is null)
            {
                ReportError(context, DiagnosticsId.NoAutoAttribute, null, $"Internal error: Auto.{ATTRIBUTE_NAME} is not defined");
                return;
            }

            foreach (var record in receiver.CandidateRecords)
            {
                //TODO make clever assumptions about record layout or use runtime symbols
            }

            foreach (var type in receiver.CandidateTypes)
            {
                var model = compilation.GetSemanticModel(type.SyntaxTree);

                if (ShouldProcess(type, attributeSymbol, model, context, out var autoAttributeData, out var typeSymbol) && autoAttributeData != null && typeSymbol != null)
                    if (TryGetDefaultDeconstruct(type, model, out var deconstruct, out var ctor) && deconstruct != null && ctor != null)
                    {
                        //TODO get namespaces for some exotic member types 
                        var members = ctor.ParameterList.Parameters.Select(p => (p.Identifier.Text, model.GetTypeInfo(p.Type!).Type!.ToDisplayString())).ToList();
                        if (members.Count == 0)
                            ReportWarning(context, DiagnosticsId.NoContractMembers, typeSymbol, $"{typeSymbol.Name} does not possess members for serialization");
                        
                        var aa = autoAttributeData.ConstructorArguments;
                        var settings = new DeconstructableSettings(
                            (char)aa[0].Value!,
                            (char)aa[1].Value!,
                            (char)aa[2].Value!,
                            aa.Length >= 4 ? (char)aa[3].Value! : '\0',
                            aa.Length >= 5 ? (char)aa[4].Value! : '\0'
                        );

                        //TODO GENERATE: append namespaces from source file
                        var namespaces = new HashSet<string> { "System", "Nemesis.TextParsers.Parsers", "Nemesis.TextParsers.Utils" };

                        //TODO do not hard-code modifiers 
                        string classSource = RenderRecord(typeSymbol, "readonly partial struct", members, settings, namespaces, context);
                        context.AddSource($"{typeSymbol.Name}_AutoDeconstructable.cs", SourceText.From(classSource, Encoding.UTF8));
                    }
                    else
                    {
                        ReportError(context, DiagnosticsId.NoMatchingCtorAndDeconstruct, typeSymbol, $"{typeSymbol.Name} does not possess matching constructor and Deconstruct pair");
                    }
            }
        }

        private static bool ShouldProcess(TypeDeclarationSyntax type, ISymbol attributeSymbol, SemanticModel? model, in GeneratorExecutionContext context, out AttributeData? autoAttributeData, out INamedTypeSymbol? typeSymbol)
        {
            static AttributeData? GetAutoAttribute(ISymbol typeSymbol, ISymbol attributeSymbol) =>
                typeSymbol.GetAttributes().FirstOrDefault(ad =>
                    ad != null && ad.AttributeClass is { } @class &&
                    @class.Equals(attributeSymbol, SymbolEqualityComparer.Default));

            autoAttributeData = null;
            typeSymbol = null;

            if (model.GetDeclaredSymbol(type) is { } ts && GetAutoAttribute(ts, attributeSymbol) is { } auto)
            {
                if (type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    if (auto.ConstructorArguments.All(a => a.Kind == TypedConstantKind.Primitive && a.Value is char))
                    {
                        autoAttributeData = auto;
                        typeSymbol = ts;
                        return true;
                    }
                    else
                        ReportError(context, DiagnosticsId.NonPrimitiveCharacters, ts, $"Attribute {ATTRIBUTE_NAME} must be constructed with primitive character instances");
                }
                else
                    ReportError(context, DiagnosticsId.NonPartialType, ts, $"Type decorated with {ATTRIBUTE_NAME} must be also declared partial");
            }
            return false;
        }

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
