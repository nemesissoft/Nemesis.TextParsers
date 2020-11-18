using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nemesis.TextParsers.CodeGen
{
    [Generator]
    public class AutoDeconstructableGenerator : ISourceGenerator
    {
        internal const string DECONSTRUCT = "Deconstruct";
        internal const string ATTRIBUTE_NAME = @"AutoDeconstructableAttribute";
        private const string ATTRIBUTE_TEXT = @"using System;
using System;

namespace Auto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    sealed class " + ATTRIBUTE_NAME + @" : Attribute
    {
        public char Delimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }

        public " + ATTRIBUTE_NAME + @"(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
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
            var attributeSource = SourceText.From(ATTRIBUTE_TEXT, Encoding.UTF8);
            context.AddSource("AutoDeconstructableAttribute", attributeSource);

            if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;

            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(attributeSource, options));

            var attributeSymbol = compilation.GetTypeByMetadataName($"Auto.{ATTRIBUTE_NAME}");

            foreach (var record in receiver.CandidateRecords)
            {
                var ss = TryGetDefaultDeconstruct(record, out var deconstruct, out var ctor);
            }

            // loop over the candidate fields, and keep the ones that are actually annotated
                /*var recordSymbols = new List<(INamedTypeSymbol TypeSymbol, IEnumerable<string> Properties)>();
                foreach (var record in receiver.CandidateRecords)
                {
                    var model = compilation.GetSemanticModel(record.SyntaxTree);
                    var recordSymbol = model.GetDeclaredSymbol(record);

                    if (recordSymbol.GetAttributes().Any(ad =>
                        ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                        recordSymbols.Add(
                            (recordSymbol,
                                record.ChildNodes().OfType<PropertyDeclarationSyntax>().Select(p => p.Identifier.Text)
                                .Concat(
                                record.ParameterList.ChildNodes().OfType<ParameterSyntax>().Select(p => p.Identifier.Text)
                                ).ToList()
                            ));
                }*/

                // group the fields by class, and generate the source


                /*foreach (var rs in recordSymbols)
                {
                    string classSource = ProcessRecord(rs.TypeSymbol, rs.Properties, attributeSymbol, recordHelperSymbol, context);
                    context.AddSource($"{rs.TypeSymbol.Name}_betterToString.cs", SourceText.From(classSource, Encoding.UTF8));
                }*/
        }
        
        private static bool TryGetDefaultDeconstruct(SyntaxNode type, out MethodDeclarationSyntax deconstruct, out ConstructorDeclarationSyntax ctor)
        {
            deconstruct = default;
            ctor = default;

            var ctors = type.ChildNodes().OfType<ConstructorDeclarationSyntax>()
                .Where(c=>c.ParameterList.Parameters.Count > 0)
                .Select(c => (ctor: c, @params: c.ParameterList.Parameters)).ToList();
            if (ctors.Count == 0) return false;

            var deconstructs = type
                .ChildNodes().OfType<MethodDeclarationSyntax>()
                .Where(m=> string.Equals(m.Identifier.Text, DECONSTRUCT, StringComparison.Ordinal) &&
                           m.ParameterList.Parameters is var @params &&
                           @params.Count >0 &&
                           @params.All(p=>p.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OutKeyword)))
                           
                           )
                .Select(m => (method: m, @params: m.ParameterList.Parameters))
                .OrderByDescending(p => p.@params.Count);

            foreach (var (method, @params) in deconstructs)
            {
                /*var ctorPair = ctors.FirstOrDefault(p => IsCompatible(p.@params, @params));
                if (ctorPair.ctor is { } c)
                {
                    deconstruct = method;
                    ctor = c;

                    return true;
                }*/
            }

            return false;
        }
        private static Type FlattenRef(Type type) => type.IsByRef ? type.GetElementType() : type;
        private static bool IsCompatible(IReadOnlyList<ParameterInfo> left, IReadOnlyList<ParameterInfo> right)
        {
            bool AreEqualByParamTypes()
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (var i = 0; i < left.Count; i++)
                    if (FlattenRef(left[i].ParameterType)
                        !=
                        FlattenRef(right[i].ParameterType)
                    )
                        return false;
                return true;
            }

            return left != null && right != null && left.Count == right.Count && AreEqualByParamTypes();
        }

        private string ProcessRecord(INamedTypeSymbol classSymbol, IEnumerable<string> properties, INamedTypeSymbol attributeSymbol, INamedTypeSymbol recordHelperSymbol, in GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default)) return null; //TODO: issue a diagnostic that it must be top level

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var source = new StringBuilder($@"
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

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            private readonly List<RecordDeclarationSyntax> _candidateRecords = new List<RecordDeclarationSyntax>();
            public IEnumerable<RecordDeclarationSyntax> CandidateRecords => _candidateRecords;

            private readonly List<TypeDeclarationSyntax> _candidateTypes = new List<TypeDeclarationSyntax>();
            public IEnumerable<TypeDeclarationSyntax> CandidateTypes => _candidateTypes;


            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                static bool HasConstructorDeConstructPair(SyntaxNode node) =>
                    node.ChildNodes().OfType<ConstructorDeclarationSyntax>()
                        .Any(ctor => ctor.ParameterList.Parameters.Count > 0) &&
                    node.ChildNodes().OfType<MethodDeclarationSyntax>()
                        .Any(m => m.Identifier.Text == DECONSTRUCT && m.ParameterList.Parameters.Count > 0);

                
                if (syntaxNode is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0 &&
                    tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    switch (tds)
                    {
                        case RecordDeclarationSyntax rds: _candidateRecords.Add(rds); break;

                        case StructDeclarationSyntax sds when HasConstructorDeConstructPair(sds): _candidateTypes.Add(sds); break;
                        case ClassDeclarationSyntax cds when HasConstructorDeConstructPair(cds): _candidateTypes.Add(cds); break;
                    }
            }
        }
    }


    //TODO add enum parser generator from string 
}
