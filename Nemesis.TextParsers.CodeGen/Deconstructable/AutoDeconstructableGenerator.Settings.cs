using System.Diagnostics;

#nullable enable

namespace Nemesis.TextParsers.CodeGen.Deconstructable
{
    public partial class AutoDeconstructableGenerator
    {
        internal static readonly string DeconstructableSettingsAttributeName = "Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute";

        private static DiagnosticDescriptor GetDiagnosticDescriptor(byte id, string message,
            DiagnosticSeverity diagnosticSeverity = DiagnosticSeverity.Error) =>
            new($"AutoDeconstructable{id:00}", "Couldn't generate automatic deconstructable pattern",
                "{0}: " + message, "AutoGenerator", diagnosticSeverity, true);

        internal static readonly DiagnosticDescriptor NonPartialTypeRule = GetDiagnosticDescriptor(1, $"Type decorated with {ATTRIBUTE_NAME} must be also declared partial");
        internal static readonly DiagnosticDescriptor InvalidSettingsAttributeRule = GetDiagnosticDescriptor(2, $"Attribute {DeconstructableSettingsAttributeName} must be constructed with 5 characters and bool type, or with default values");
        internal static readonly DiagnosticDescriptor NoMatchingCtorAndDeconstructRule = GetDiagnosticDescriptor(3, "No matching constructor and Deconstruct pair found");
        internal static readonly DiagnosticDescriptor NoAutoAttributeRule = GetDiagnosticDescriptor(4, $"Internal error: Auto.{ATTRIBUTE_NAME} is not defined");
        internal static readonly DiagnosticDescriptor NoSettingsAttributeRule = GetDiagnosticDescriptor(5, $"{DeconstructableSettingsAttributeName} is not recognized. Please reference Nemesis.TextParsers into your project");
        internal static readonly DiagnosticDescriptor NamespaceAndTypeNamesEqualRule = GetDiagnosticDescriptor(6, "Type name cannot be equal to containing namespace: '{1}'");
        internal static readonly DiagnosticDescriptor NoConstructor = GetDiagnosticDescriptor(7, "No constructor to support serialization. Only Record get constructors automatically. Private constructor is not enough - it cannot be called");
        internal static readonly DiagnosticDescriptor NoDeconstruct = GetDiagnosticDescriptor(8, "No Deconstruct to support serialization. Only Record get Deconstruct automatically. All Deconstruct parameters must have 'out' passing type");


        internal static readonly DiagnosticDescriptor NoContractMembersRule = GetDiagnosticDescriptor(50, "No members for serialization", DiagnosticSeverity.Warning);


        private static void ReportDiagnostics(GeneratorExecutionContext context, DiagnosticDescriptor rule, ISymbol? symbol) =>
            context.ReportDiagnostic(Diagnostic.Create(rule, symbol?.Locations[0] ?? Location.None, symbol?.Name, symbol?.ContainingNamespace?.ToString()));


        private static void ExtractNamespaces(ITypeSymbol typeSymbol, ICollection<string> namespaces)
        {
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType) //namedType.TypeParameters for unbound generics
            {
                namespaces.Add(namedType.ContainingNamespace.ToDisplayString());

                foreach (var arg in namedType.TypeArguments)
                    ExtractNamespaces(arg, namespaces);
            }
            else if (typeSymbol is IArrayTypeSymbol arraySymbol)
            {
                namespaces.Add("System");

                ITypeSymbol elementSymbol = arraySymbol.ElementType;
                while (elementSymbol is IArrayTypeSymbol innerArray)
                    elementSymbol = innerArray.ElementType;

                ExtractNamespaces(elementSymbol, namespaces);
            }
            /*else if (typeSymbol.TypeKind == TypeKind.Error || typeSymbol.TypeKind == TypeKind.Dynamic)
            {
                //add appropriate reference to your compilation 
            }*/
            else
                namespaces.Add(typeSymbol.ContainingNamespace.ToDisplayString());
        }


        private sealed class DeconstructableSyntaxReceiver : ISyntaxReceiver
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly List<TypeDeclarationSyntax> _candidateTypes = [];
            public IEnumerable<TypeDeclarationSyntax> CandidateTypes => _candidateTypes;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0 &&
                   (tds is RecordDeclarationSyntax or StructDeclarationSyntax or ClassDeclarationSyntax))
                    _candidateTypes.Add(tds);
            }
        }

        private readonly record struct GeneratedDeconstructableSettings
            (char Delimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End, bool UseDeconstructableEmpty)
        {
            public override string ToString() =>
                $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

            public static GeneratedDeconstructableSettings? FromDeconstructableSettingsAttribute(AttributeData? deconstructableSettingsAttributeData)
            {
                if (!(deconstructableSettingsAttributeData?.ConstructorArguments is { } args))
                    return null;

                char GetChar(int i) => args.Length > i ? (char)args[i].Value! : throw new ArgumentOutOfRangeException(nameof(args), $"Cannot obtain argument parameter no {i}");
                bool GetBool(int i) => args.Length > i ? (bool)args[i].Value! : throw new ArgumentOutOfRangeException(nameof(args), $"Cannot obtain argument parameter no {i}");

                return new(GetChar(0), GetChar(1), GetChar(2), GetChar(3), GetChar(4), GetBool(5));

            }
        }
    }
}

#if !NET
namespace System.Runtime.CompilerServices
{
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
