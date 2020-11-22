using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    public partial class AutoDeconstructableGenerator
    {
        enum DiagnosticsId : byte
        {
            NonPartialType = 1,
            NonPrimitiveCharacters = 2,
            NoMatchingCtorAndDeconstruct = 3,
            NoContractMembers = 4,
            NoAutoAttribute = 5,
            NoSettingsAttribute = 6,
            NamespaceAndTypeNamesEqual = 7,
        }

        private static void ReportError(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string title) =>
            ReportDiagnostics(context, id, symbol, title, DiagnosticSeverity.Error);

        private static void ReportWarning(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string title) =>
            ReportDiagnostics(context, id, symbol, title, DiagnosticSeverity.Warning);

        private static void ReportDiagnostics(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string title, DiagnosticSeverity diagnosticSeverity) =>
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor($"AutoDeconstructable{(byte)id:00}",
                    title,
                    messageFormat: "Couldn't generate automatic deconstructable pattern for '{0}'.",
                    category: "AutoGenerator",
                    diagnosticSeverity,
                    isEnabledByDefault: true), symbol?.Locations[0], symbol?.Name));


        private sealed class DeconstructableSyntaxReceiver : ISyntaxReceiver
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
                        case RecordDeclarationSyntax rds:
                            _candidateRecords.Add(rds);
                            break;

                        case StructDeclarationSyntax sds when HasConstructorDeConstructPair(sds):
                            _candidateTypes.Add(sds);
                            break;
                        case ClassDeclarationSyntax cds when HasConstructorDeConstructPair(cds):
                            _candidateTypes.Add(cds);
                            break;
                    }
            }
        }

        private readonly struct GeneratedDeconstructableSettings
        {
            public char Delimiter { get; }
            public char NullElementMarker { get; }
            public char EscapingSequenceStart { get; }
            public char? Start { get; }
            public char? End { get; }

            public GeneratedDeconstructableSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
            {
                Delimiter = delimiter;
                NullElementMarker = nullElementMarker;
                EscapingSequenceStart = escapingSequenceStart;
                Start = start;
                End = end;
            }

            public void Deconstruct(out char delimiter, out char nullElementMarker, out char escapingSequenceStart, out char? start, out char? end)
            {
                delimiter = Delimiter;
                nullElementMarker = NullElementMarker;
                escapingSequenceStart = EscapingSequenceStart;
                start = Start;
                end = End;
            }

            public override string ToString() =>
                $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
        }
    }
}
