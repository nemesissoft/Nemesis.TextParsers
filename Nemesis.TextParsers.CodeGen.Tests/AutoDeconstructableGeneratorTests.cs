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

        private static IEnumerable<TestCaseData> _endToEndCases = EndToEndCases.AutoDeconstructableCases()
           .Select((t, i) => new TestCaseData($@"using Nemesis.TextParsers.Settings; namespace Nemesis.TextParsers.CodeGen.Tests {{ {t.source} }}", t.expectedCode)
               .SetName($"E2E_{i + 1:00}"));

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
                ((CompilationUnitSyntax)tree.GetRoot())
                .ToFullString()).ToList();
        }
    }
}


