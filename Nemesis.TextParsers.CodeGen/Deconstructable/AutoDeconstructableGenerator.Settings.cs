using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using LocalSettingsAttribute = Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    public partial class AutoDeconstructableGenerator
    {
        internal enum DiagnosticsId : byte
        {
            NonPartialType = 1,
            InvalidSettingsAttribute = 2,
            NoMatchingCtorAndDeconstruct = 3,
            NoContractMembers = 4,
            NoAutoAttribute = 5,
            NoSettingsAttribute = 6,
            NamespaceAndTypeNamesEqual = 7,
        }

        private static void ReportError(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string message) =>
            ReportDiagnostics(context, id, symbol, message, DiagnosticSeverity.Error);

        private static void ReportWarning(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string message) =>
            ReportDiagnostics(context, id, symbol, message, DiagnosticSeverity.Warning);

        private static void ReportDiagnostics(GeneratorExecutionContext context, DiagnosticsId id, ISymbol? symbol, string message, DiagnosticSeverity diagnosticSeverity) =>
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor($"AutoDeconstructable{(byte)id:00}",
                    $"Couldn't generate automatic deconstructable pattern for '{symbol?.Name}'",
                    messageFormat: "{0}: "+ message,
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
                //TODO add to only 1 list 
                if (syntaxNode is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0)
                    switch (tds)
                    {
                        case RecordDeclarationSyntax rds:
                            _candidateRecords.Add(rds);
                            break;

                        case StructDeclarationSyntax sds:
                            _candidateTypes.Add(sds);
                            break;
                        case ClassDeclarationSyntax cds:
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
            public bool UseDeconstructableEmpty { get; }

            private GeneratedDeconstructableSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, bool useDeconstructableEmpty)
            {
                Delimiter = delimiter;
                NullElementMarker = nullElementMarker;
                EscapingSequenceStart = escapingSequenceStart;
                Start = start;
                End = end;
                UseDeconstructableEmpty = useDeconstructableEmpty;
            }

            /*public void Deconstruct(out char delimiter, out char nullElementMarker, out char escapingSequenceStart, out char? start, out char? end, out bool useDeconstructableEmpty)
            {
                delimiter = Delimiter;
                nullElementMarker = NullElementMarker;
                escapingSequenceStart = EscapingSequenceStart;
                start = Start;
                end = End;
                useDeconstructableEmpty = UseDeconstructableEmpty;
            }*/

            public override string ToString() =>
                $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";


            //TODO get rid of that method - get all parameters from symbol analysis 
            public static GeneratedDeconstructableSettings? FromDeconstructableSettingsAttribute(AttributeData? deconstructableSettingsAttributeData)
            {
                if (deconstructableSettingsAttributeData?.ConstructorArguments is { } args)
                {
                    char GetCharValue(int i, char @default) => args.Length > i ? (char)args[i].Value! : @default;
                    bool GetBoolValue(int i, bool @default) => args.Length > i ? (bool)args[i].Value! : @default;

                    return new GeneratedDeconstructableSettings(
                        GetCharValue(0, LocalSettingsAttribute.DEFAULT_DELIMITER),
                        GetCharValue(1, LocalSettingsAttribute.DEFAULT_NULL_ELEMENT_MARKER),
                        GetCharValue(2, LocalSettingsAttribute.DEFAULT_ESCAPING_SEQUENCE_START),
                        GetCharValue(3, LocalSettingsAttribute.DEFAULT_START),
                        GetCharValue(4, LocalSettingsAttribute.DEFAULT_END),
                        GetBoolValue(5, LocalSettingsAttribute.DEFAULT_USE_DECONSTRUCTABLE_EMPTY)
                    );
                }
                else
                    return null;
            }
        }
    }
}
