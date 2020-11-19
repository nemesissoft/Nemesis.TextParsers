using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nemesis.TextParsers.CodeGen.Deconstructable;
using NUnit.Framework;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    public class AutoDeconstructableGeneratorTests
    {
        //TODO test for missing start/end characters in DeconstructableSettings
        //TODO add tests with static using, + using Mnemonic = System.Double
        //TODO add tests with various modifiers + class/struct/record
        //TODO test all reported diagnostics

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
            Assert.That(diagnostics, Is.Empty);

            var generatedTrees = newComp.RemoveSyntaxTrees(comp.SyntaxTrees).SyntaxTrees;

            var root = (CompilationUnitSyntax) generatedTrees.Last().GetRoot();

            var actual = root.ToFullString();

            Assert.That(actual, Is.EqualTo(@"using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(Point3dTransformer))]
    readonly partial struct Point3d { }

    [System.CodeDom.Compiler.GeneratedCode(""AutoDeconstructableGenerator"", ""1.0"")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class Point3dTransformer : TransformerBase<Point3d>
    {
        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');
        private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;

        protected override Point3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_x);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformer_y);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformer_z);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Point3d(t1, t2, t3);
        }

        public override string Format(Point3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (x, y, z) = element;
                _helper.FormatElement(_transformer_x, x, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_y, y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_z, z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}
").Using(IgnoreNewLinesComparer.EqualityComparer));

        }

        private static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.ConsoleApplication)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(outputKind));

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);

        private static Compilation RunGenerators(Compilation c, out IReadOnlyList<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out var compilation, out var diagnosticsArray);
            diagnostics = diagnosticsArray;
            return compilation;
        }
    }
}
