﻿extern alias original;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Nemesis.TextParsers.CodeGen.Tests
{
    internal static class Utils
    {
        public static (Compilation compilation, SyntaxTree sourceTree, SemanticModel semanticModel) CreateTestCompilation(
            string source, Assembly[] additionalAssemblies = null, [CallerMemberName] string memberName = null)
        {
            var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

            IEnumerable<Assembly> assemblies = new[] { typeof(object).Assembly };
            if (additionalAssemblies != null)
                assemblies = assemblies.Concat(additionalAssemblies);

            var references = assemblies.Select(ass => MetadataReference.CreateFromFile(ass.Location));

            var compilation = CSharpCompilation.Create($"{memberName}_Compilation", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var sourceTree = compilation.SyntaxTrees.Single();
            return (compilation, sourceTree, compilation.GetSemanticModel(sourceTree));
        }

        public static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? throw new InvalidOperationException("The location of the .NET assemblies cannot be retrieved");

            return CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                new[]
                {
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BigInteger).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(original::Nemesis.TextParsers.ITransformer).GetTypeInfo()
                        .Assembly.Location),
                },
                new CSharpCompilationOptions(outputKind));
        }

        public static IReadOnlyCollection<string> GetCompilationIssues(Compilation compilation)
        {
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            return result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Select(d => d.ToString()).ToList();
        }

        public static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);

        public static Compilation RunGenerators(Compilation c, out IReadOnlyList<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out var compilation, out var diagnosticsArray);
            diagnostics = diagnosticsArray;
            return compilation;
        }

        private static readonly Regex _headerPattern = new Regex(@"/\*\s*<auto-generated>   .+?   </auto-generated>\s*\*/", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        private static readonly Regex _generatorPattern = new Regex(@""".*Generator""\s*,\s*""([0-9.]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        public static string ScrubGeneratorComments(string text)
        {
            text = _generatorPattern.Replace(text, "string.Empty, string.Empty");
            text = _headerPattern.Replace(text, "//HEAD");

            return text;
        }
    }


    internal class IgnoreNewLinesComparer : IComparer<string>, IEqualityComparer<string>
    {
        public static readonly IComparer<string> Comparer = new IgnoreNewLinesComparer();

        public static readonly IEqualityComparer<string> EqualityComparer = new IgnoreNewLinesComparer();

        public int Compare(string x, string y) => string.CompareOrdinal(NormalizeNewLines(x), NormalizeNewLines(y));

        public bool Equals(string x, string y) => NormalizeNewLines(x) == NormalizeNewLines(y);

        public int GetHashCode(string s) => NormalizeNewLines(s)?.GetHashCode() ?? 0;

        public static string NormalizeNewLines(string s) => s?
            .Replace(Environment.NewLine, "")
            .Replace("\n", "")
            .Replace("\r", "");
    }
}
