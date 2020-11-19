using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit;
using NUnit.Framework;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    public class AutoDeconstructableGeneratorTests
    {
        [Test]
        public void SimpleGeneratorTest()
        {
            var source = @"
namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Auto.AutoDeconstructable(',', '∅', '\\', '[', ']')]
    public partial record RecordPoint3d(double X, double Y, double Z) { }

    [Auto.AutoDeconstructable(';', '∅', '\\', '(', ')')]
    public readonly partial struct Point3d
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out System.Double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }
    }   
}";
            var comp = CreateCompilation(source);
            var newComp = RunGenerators(comp, out var diagnostics, new AutoDeconstructableGenerator());
            var generatedTrees = newComp.RemoveSyntaxTrees(comp.SyntaxTrees).SyntaxTrees;

            var root = generatedTrees.Last().GetRoot() as CompilationUnitSyntax;

            var actual = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First().ToString();

            Assert.That(actual, Is.EqualTo(@""));

        }

        private static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.ConsoleApplication)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(outputKind));

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);

        private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out var d, out diagnostics);
            return d;
        }
    }
}
