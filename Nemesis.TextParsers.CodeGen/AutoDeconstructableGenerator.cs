using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Nemesis.TextParsers.CodeGen
{
    [Generator]
    public class AutoDeconstructableGenerator : ISourceGenerator
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
        public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("AutoDeconstructableAttribute", SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8));

            if (context.SyntaxReceiver is not SyntaxReceiver receiver || context.Compilation is not CSharpCompilation cSharpCompilation) return;

            var options = cSharpCompilation.SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ATTRIBUTE_SOURCE, Encoding.UTF8), options));

            var attributeSymbol = compilation.GetTypeByMetadataName($"Auto.{ATTRIBUTE_NAME}");
            if (attributeSymbol is null)
            {
                //TODO error: report diagnostic 
                return;
            }

            var processTypes = new List<(INamedTypeSymbol TypeSymbol, IEnumerable<(string Name, string Type)> Members, DeconstructableSettings Settings)>();
            foreach (var record in receiver.CandidateRecords)
            {
                //TODO make clever assumptions about record layout or use runtime symbols
            }

            foreach (var type in receiver.CandidateTypes)
            {
                var model = compilation.GetSemanticModel(type.SyntaxTree);

                //TODO inspect why autoAttributeData has null arguments 
                if (ShouldProcess(type, attributeSymbol, model, context, out var autoAttributeData, out var typeSymbol) && autoAttributeData != null && typeSymbol != null)
                    if (TryGetDefaultDeconstruct(type, model, out var deconstruct, out var ctor) && deconstruct != null && ctor != null)
                    {
                        //TODO get namespaces for some exotic member types 
                        var members = ctor.ParameterList.Parameters.Select(p => (p.Identifier.Text, model.GetTypeInfo(p.Type!).Type!.ToDisplayString())).ToList();

                        var aa = autoAttributeData.ConstructorArguments;
                        var settings = new DeconstructableSettings(
                            (char)aa[0].Value!,
                            (char)aa[1].Value!,
                            (char)aa[2].Value!,
                            aa.Length >= 4 ? (char)aa[3].Value! : '\0',
                            aa.Length >= 5 ? (char)aa[4].Value! : '\0'
                        );
                        processTypes.Add((typeSymbol, members, settings));
                    }
                    else
                    {
                        //TODO report error diagnostic - no compatible ctor/Deconstruct pair
                    }
            }
            
            foreach (var pt in processTypes)
            {
                string classSource = ProcessRecord(pt.TypeSymbol, pt.Members, pt.Settings, context);
                context.AddSource($"{pt.TypeSymbol.Name}_AutoDeconstructable.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessRecord(INamedTypeSymbol typeSymbol, IEnumerable<(string Name, string Type)> members, DeconstructableSettings settings, in GeneratorExecutionContext context)
        {
            throw new NotImplementedException();
        }

        private string ProcessRecord(INamedTypeSymbol classSymbol, IEnumerable<string> properties, INamedTypeSymbol attributeSymbol, INamedTypeSymbol recordHelperSymbol, in GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default)) return ""; //TODO: issue a diagnostic that it must be top level

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var source = new StringBuilder($@"
using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
//TODO GENERATE: append namespaces from source file

namespace {namespaceName}
{{
    partial record {classSymbol.Name}
    {{

         protected virtual bool PrintMembers(System.Text.StringBuilder builder) 
         {{         
");

            foreach (var property in properties)
                ProcessProperty(source, property, recordHelperSymbol);

            source.Append(@"
               return true; 
         }
    }
}");
            return source.ToString();
        }

        private void ProcessProperty(StringBuilder source, string property, INamedTypeSymbol recordHelperSymbol)
        {
            source.AppendLine($"               builder.Append(\"{property}\");");
            source.AppendLine($"               builder.Append(\" = \");");
            source.AppendLine($"               builder.Append({recordHelperSymbol.ToDisplayString()}.FormatValue({property}));");
            source.AppendLine($"               builder.Append(\", \");");
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
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor("AutoDeconstructable002",
                                $"Attribute {ATTRIBUTE_NAME} must be constructed with primitive character instances",
                                messageFormat: "Couldn't generate automatic deconstructable pattern for '{0}'.",
                                category: "AutoGenerator",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true), ts.Locations[0], ts.Name));
                    //TODO commonalize ReportDiagnostic logic
                }
                else
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("AutoDeconstructable001",
                            $"Type decorated with {ATTRIBUTE_NAME} must be also declared partial",
                            messageFormat: "Couldn't generate automatic deconstructable pattern for '{0}'.",
                            category: "AutoGenerator",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true), ts.Locations[0], ts.Name));
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
            //TODO report warning diagnostic when parameter names are different
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
        
        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly List<RecordDeclarationSyntax> _candidateRecords = new List<RecordDeclarationSyntax>();
            public IEnumerable<RecordDeclarationSyntax> CandidateRecords => _candidateRecords;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly List<TypeDeclarationSyntax> _candidateTypes = new List<TypeDeclarationSyntax>();
            public IEnumerable<TypeDeclarationSyntax> CandidateTypes => _candidateTypes;


            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                static bool HasConstructorDeConstructPair(SyntaxNode node) =>
                    node.ChildNodes().OfType<ConstructorDeclarationSyntax>()
                        .Any(ctor => ctor.ParameterList.Parameters.Count > 0) &&
                    node.ChildNodes().OfType<MethodDeclarationSyntax>()
                        .Any(m => m.Identifier.Text == DECONSTRUCT && m.ParameterList.Parameters.Count > 0);


                if (syntaxNode is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0)
                    switch (tds)
                    {
                        case RecordDeclarationSyntax rds: _candidateRecords.Add(rds); break;

                        case StructDeclarationSyntax sds when HasConstructorDeConstructPair(sds): _candidateTypes.Add(sds); break;
                        case ClassDeclarationSyntax cds when HasConstructorDeConstructPair(cds): _candidateTypes.Add(cds); break;
                    }
            }
        }

        readonly struct DeconstructableSettings
        {
            public char Delimiter { get; }
            public char NullElementMarker { get; }
            public char EscapingSequenceStart { get; }
            public char? Start { get; }
            public char? End { get; }

            public DeconstructableSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
            {
                Delimiter = delimiter;
                NullElementMarker = nullElementMarker;
                EscapingSequenceStart = escapingSequenceStart;
                Start = start;
                End = end;
            }
        }
    }


    //TODO add enum parser generator from string 
}
