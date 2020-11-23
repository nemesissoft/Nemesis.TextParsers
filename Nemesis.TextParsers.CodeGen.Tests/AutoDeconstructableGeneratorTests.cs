extern alias original;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nemesis.TextParsers.CodeGen.Deconstructable;

using NUnit.Framework;

using static Nemesis.TextParsers.CodeGen.Tests.Utils;


namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    public partial class AutoDeconstructableGeneratorTests
    {
        //TODO test for missing start/end characters in DeconstructableSettings
        //TODO add tests with static using, + using Mnemonic = System.Double
        //TODO add tests with various modifiers + class/struct/record
        //TODO add test with single property and check Deconstruct/Ctor retrieval 


        //TODO rework this as approval tests

        private static readonly IEnumerable<TestCaseData> _endToEndCases = new (string source, string expectedCode)[]
        {
            (@"public record RecordPoint2d(double X, double Y) { }

               [Auto.AutoDeconstructable]
               [DeconstructableSettings(',', '∅', '\\', '[', ']')]
               public partial record RecordPoint3d(double X, double Y, double Z): RecordPoint2d(X, Y) 
               {
                   public double Magnitude { get; init; } //will NOT be subject to deconstruction
               }", @"HEAD
using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(RecordPoint3dTransformer))]
    public partial record RecordPoint3d 
    {
#if DEBUG
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(META)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class RecordPoint3dTransformer : TransformerBase<RecordPoint3d>
    {
        private readonly ITransformer<double> _transformer_X = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(',', '∅', '\\', '[', ']');

        public override RecordPoint3d GetEmpty() => new RecordPoint3d(_transformer_X.GetEmpty(), _transformer_Y.GetEmpty(), _transformer_Z.GetEmpty());
        protected override RecordPoint3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_X);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformer_Y);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformer_Z);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new RecordPoint3d(t1, t2, t3);
        }

        public override string Format(RecordPoint3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (X, Y, Z) = element;
                _helper.FormatElement(_transformer_X, X, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Y, Y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Z, Z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),



            (@"[Auto.AutoDeconstructable]
               [DeconstructableSettings(';', '∅', '\\', '(', ')')]
               public readonly partial struct Point3d
               {
                   public double X { get; } public double Y { get; } public double Z { get; }
                   public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }

                   public void Deconstruct(out double x, out System.Double y, out double z) { x = X; y = Y; z = Z; }
               }", @"HEAD
using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(Point3dTransformer))]
    public readonly partial struct Point3d 
    {
#if DEBUG
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(META)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class Point3dTransformer : TransformerBase<Point3d>
    {
        private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');

        public override Point3d GetEmpty() => new Point3d(_transformer_x.GetEmpty(), _transformer_y.GetEmpty(), _transformer_z.GetEmpty());
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
}")
        }
           .Select((t, i) => new TestCaseData($@"using Nemesis.TextParsers.Settings; namespace Nemesis.TextParsers.CodeGen.Tests {{ {t.source} }}", t.expectedCode)
               .SetName($"{i + 1:00}"));
        
        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode)
        {
            var compilation = CreateCompilation(source);

            var generatedTrees = GetGeneratedTreesOnly(compilation);
            
            var actual = ScrubGeneratorComments(generatedTrees.Single());
            
            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }


        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests_ApprovalTests(string source, string expectedCode)
        {
            var compilation = CreateCompilation(source);

            var generatedTrees = GetGeneratedTreesOnly(compilation);

            var actual = ScrubGeneratorComments(generatedTrees.Single());

            Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
        }

        private static readonly IEnumerable<TestCaseData> _settingsCases = new (string typeDefinition, string expectedCodePart)[]
        {
            (@"[DeconstructableSettings(':', '␀', '/', '{', '}')]
            readonly partial struct Child
            {
                public byte Age { get; }
                public float Weight { get; }
                public Child(byte age, float weight) { Age = age; Weight = weight; }
                public void Deconstruct(out byte age, out float weight) { age = Age; weight = Weight; }                
            }", "private readonly TupleHelper _helper = new TupleHelper(':', '␀', '/', '{', '}');"),


            (@"[DeconstructableSettings('_', '␀', '*', '<', '>')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '<', '>');"),

            (@"[DeconstructableSettings('_', '␀', '*', '<')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '<', ')');"),

            (@"[DeconstructableSettings('_', '␀', '*')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '(', ')');"),

            (@"[DeconstructableSettings('_', '␀')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '\\', '(', ')');"),

            (@"[DeconstructableSettings('_')]
            partial record T(byte B) { }", @"new TupleHelper('_', '∅', '\\', '(', ')');"),

            (@"[DeconstructableSettings]
            partial record T(byte B) { }", @"new TupleHelper(';', '∅', '\\', '(', ')');"),

            (@"partial record T(byte B) { }", @"_helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();"),


            (@"[DeconstructableSettings(useDeconstructableEmpty: true)]
            partial record T(byte B) { }", @"public override T GetEmpty() => new T(_transformer_B.GetEmpty());"),

            (@"[DeconstructableSettings(useDeconstructableEmpty: false)]
            partial record T(byte B) { }", @"NOT CONTAIN:GetEmpty()"),
        }
           .Select((t, i) => new TestCaseData($@"using Nemesis.TextParsers.Settings; namespace Tests {{ [Auto.AutoDeconstructable] {t.typeDefinition} }}", t.expectedCodePart)
               .SetName($"{(i + 1):00}"));

        [TestCaseSource(nameof(_settingsCases))]
        public void SettingsRetrieval_ShouldEmitProperValues(string source, string expectedCodePart)
        {
            bool matchNotContain = expectedCodePart.StartsWith("NOT CONTAIN:");
            if (matchNotContain)
                expectedCodePart = expectedCodePart[12..];

            //arrange
            var compilation = CreateCompilation(source);

            //act
            var generatedTrees = GetGeneratedTreesOnly(compilation);
            var actual = generatedTrees.Single();


            //assert
            Assert.That(actual, matchNotContain ? Does.Not.Contain(expectedCodePart) : Does.Contain(expectedCodePart));
        }

        private static IReadOnlyList<string> GetGeneratedTreesOnly(Compilation compilation, int requiredCardinality = 1)
        {
            var newComp = RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
            Assert.That(diagnostics, Is.Empty);

            SyntaxTree attributeTree = null;
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

            var toRemove = compilation.SyntaxTrees.Append(attributeTree);

            var generatedTrees = newComp.RemoveSyntaxTrees(toRemove).SyntaxTrees.ToList();
            Assert.That(generatedTrees, Has.Count.EqualTo(requiredCardinality));
            
            return generatedTrees.Select(tree =>
                ((CompilationUnitSyntax) tree.GetRoot())
                .ToFullString()).ToList();
        }
    }
}


