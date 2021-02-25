﻿extern alias original;
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nemesis.CodeAnalysis;
using Nemesis.TextParsers.CodeGen.Deconstructable;

using NUnit.Framework;

using static Nemesis.TextParsers.CodeGen.Tests.Utils;


namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    public partial class AutoDeconstructableGeneratorTests
    {
        private static readonly IEnumerable<TestCaseData> _endToEndCases = EndToEndCases.AutoDeconstructableCases()
           .Select((t, i) => new TestCaseData($@"
using Nemesis.TextParsers.Settings; 
namespace Nemesis.TextParsers.CodeGen.Tests {{ {t.source} }}", t.expectedCode)
               .SetName($"E2E_{i + 1:00}_{t.name}"));
        
        [TestCaseSource(nameof(_endToEndCases))]
        public void EndToEndTests(string source, string expectedCode)
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
               .SetName($"Settings{i + 1:00}"));

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

        public static IReadOnlyList<string> GetGeneratedTreesOnly(Compilation compilation, int requiredCardinality = 1)
        {
            var newComp = CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
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
                ((CompilationUnitSyntax)tree.GetRoot())
                .ToFullString()).ToList();
        }

        [Test]
        public void Generate_When_StaticUsing_And_Mnemonics()
        {
            var compilation = CreateCompilation(@"using SD = System.Double;
using static Tests3.ContainerClass3;

namespace Tests1
{
    [Auto.AutoDeconstructable]
    public partial record Numbers(System.Single FloatNumber, SD DoubleNumber, Tests2.ContainerClass2.NestedClass2 NestedFullyQualified, NestedClass3 NestedUsingStatic) { }
}

namespace Tests2
{
    public static class ContainerClass2 { public class NestedClass2 { } }
}

namespace Tests3
{
    public static class ContainerClass3 { public class NestedClass3 { } }
}");

            var generatedTrees = GetGeneratedTreesOnly(compilation);

            var actual = ScrubGeneratorComments(generatedTrees.Single());

            Assert.That(actual, Is.EqualTo(@"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
using Tests2;
using Tests3;
using static Tests3.ContainerClass3;
using SD = System.Double;

namespace Tests1
{
    [Transformer(typeof(NumbersTransformer))]
    public partial record Numbers 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class NumbersTransformer : TransformerBase<Numbers>
    {
        private readonly ITransformer<float> _transformer_FloatNumber = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<double> _transformer_DoubleNumber = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<ContainerClass2.NestedClass2> _transformer_NestedFullyQualified = TextTransformer.Default.GetTransformer<ContainerClass2.NestedClass2>();
        private readonly ITransformer<ContainerClass3.NestedClass3> _transformer_NestedUsingStatic = TextTransformer.Default.GetTransformer<ContainerClass3.NestedClass3>();
        private const int ARITY = 4;


        private readonly TupleHelper _helper;

        public NumbersTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override Numbers ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_FloatNumber);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformer_DoubleNumber);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformer_NestedFullyQualified);

            _helper.ParseNext(ref enumerator, 4);
            var t4 = _helper.ParseElement(ref enumerator, _transformer_NestedUsingStatic);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Numbers(t1, t2, t3, t4);
        }

        public override string Format(Numbers element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (FloatNumber, DoubleNumber, NestedFullyQualified, NestedUsingStatic) = element;
                _helper.FormatElement(_transformer_FloatNumber, FloatNumber, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_DoubleNumber, DoubleNumber, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_NestedFullyQualified, NestedFullyQualified, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_NestedUsingStatic, NestedUsingStatic, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}").Using(IgnoreNewLinesComparer.EqualityComparer));
        }
    }
}


