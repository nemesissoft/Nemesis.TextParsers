﻿#nullable enable
extern alias original;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nemesis.CodeAnalysis;
using Nemesis.TextParsers.CodeGen.Deconstructable;

namespace Nemesis.TextParsers.CodeGen.Tests;

internal static class CodeGenUtils
{
    public static Compilation CreateValidCompilation(string source, [CallerMemberName] string? memberName = null) =>
        CreateTestCompilation(source, [typeof(original::Nemesis.TextParsers.ITransformer).GetTypeInfo().Assembly], memberName);

    public static Compilation CreateTestCompilation(string source, Assembly[]? additionalAssemblies = null, [CallerMemberName] string? memberName = null)
    {
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? throw new InvalidOperationException("The location of the .NET assemblies cannot be retrieved");

        static SyntaxTree Parse(string source) =>
            CSharpSyntaxTree
            .ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        SyntaxTree[] trees =
        [
            Parse(source)
#if !NET
            ,
            Parse("""
                namespace System.Runtime.CompilerServices
                {
                    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                    internal static class IsExternalInit { }
                }

                """)
#endif
        ];

        var references = new List<PortableExecutableReference>(8);
        void AddRef(string path) =>
            references.Add(MetadataReference.CreateFromFile(path));

        foreach (var t in new[] { typeof(Binder), typeof(BigInteger) })
            AddRef(t.GetTypeInfo().Assembly.Location);

        if (additionalAssemblies is not null)
            foreach (var ass in additionalAssemblies)
                AddRef(ass.Location);
#if NET
        AddRef(Path.Combine(assemblyPath, "System.Runtime.dll"));
#else
        var standardAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "netstandard");

        AddRef(standardAssembly?.Location
               ?? throw new NotSupportedException("netstandard is needed for legacy framework tests")
        );

        AddRef(typeof(System.ComponentModel.EditorBrowsableAttribute).GetTypeInfo().Assembly.Location);
#endif

        return CSharpCompilation.Create($"{memberName}_Compilation", trees,
            references, new(OutputKind.DynamicallyLinkedLibrary));
    }


    private static readonly Regex _headerPattern = new(@"/\*\s*<auto-generated>   .+?   </auto-generated>\s*\*/", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
    private static readonly Regex _generatorPattern = new(@""".*Generator""\s*,\s*""([0-9.]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    public static string ScrubGeneratorComments(string text)
    {
        text = _generatorPattern.Replace(text, "string.Empty, string.Empty");
        text = _headerPattern.Replace(text, "//HEAD");
        return text;
    }

    public static IReadOnlyList<string> GetGeneratedTreesOnly(Compilation compilation, int requiredCardinality = 1)
    {
        var newComp = CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
        Assert.That(diagnostics, Is.Empty);

        SyntaxTree? attributeTree = null;
        foreach (var tree in newComp.SyntaxTrees)
        {
            var attributeDeclaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cds => string.Equals(cds.Identifier.ValueText, AutoDeconstructableGenerator.ATTRIBUTE_NAME, StringComparison.Ordinal));
            if (attributeDeclaration != null)
            {
                attributeTree = tree;
                break;
            }
        }
        Assert.That(attributeTree, Is.Not.Null, "Auto attribute not found among generated trees");

        var toRemove = compilation.SyntaxTrees.Append(attributeTree!);

        var generatedTrees = newComp.RemoveSyntaxTrees(toRemove).SyntaxTrees.ToList();
        Assert.That(generatedTrees, Has.Count.EqualTo(requiredCardinality));

        return generatedTrees.Select(tree =>
            ((CompilationUnitSyntax)tree.GetRoot())
            .ToFullString()).ToList();
    }
}


internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
{
    public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();

    public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

    public int Compare(string? x, string? y) => string.CompareOrdinal(NormalizeNewLines(x), NormalizeNewLines(y));

    public bool Equals(string? x, string? y) => NormalizeNewLines(x) == NormalizeNewLines(y);

    public int GetHashCode(string s) => NormalizeNewLines(s)?.GetHashCode() ?? 0;

    public static string? NormalizeNewLines(string? s) => s?
        .Replace(Environment.NewLine, "")
        .Replace("\n", "")
        .Replace("\r", "");
}
