﻿extern alias original;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Nemesis.TextParsers.CodeGen.Deconstructable;

using NUnit.Framework;

using static Nemesis.TextParsers.CodeGen.Tests.Utils;


namespace Nemesis.TextParsers.CodeGen.Tests
{
    [TestFixture]
    public class AutoDeconstructableGeneratorTests
    {
        //TODO test for missing start/end characters in DeconstructableSettings
        //TODO add tests with static using, + using Mnemonic = System.Double
        //TODO add tests with various modifiers + class/struct/record
        //TODO add test with single property and check Deconstruct/Ctor retrieval 


        //TODO rework this as approval tests
        [Test]
        public void SimpleGeneratorTest()
        {
            var source = @"
namespace Nemesis.TextParsers.CodeGen.Tests
{
    public record RecordPoint2d(double X, double Y) { }

    [Auto.AutoDeconstructable]
    [Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute(',', '∅', '\\', '[', ']')]
    public partial record RecordPoint3d(double X, double Y, double Z): RecordPoint2d(X, Y) 
    {
        public double Magnitude { get; init; } //will NOT be subject to deconstruction
    }

    [Auto.AutoDeconstructable]
    [Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute(';', '∅', '\\', '(', ')')]
    public readonly partial struct Point3d
    {
        public double X { get; } public double Y { get; } public double Z { get; }

        public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }

        public void Deconstruct(out double x, out System.Double y, out double z) { x = X; y = Y; z = Z; }
    }   
}";
            var compilation = CreateCompilation(source);

            var generatedTrees = GetGeneratedTreesOnly(compilation, 2);
            
            var actualRecord = ScrubGeneratorComments(generatedTrees[0]);
            var actualStruct = ScrubGeneratorComments(generatedTrees[1]);

            Assert.That(actualRecord, Is.EqualTo(@"HEAD
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
}
"));

            Assert.That(actualStruct, Is.EqualTo(@"HEAD
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
}
").Using(IgnoreNewLinesComparer.EqualityComparer));

        }

        [Test]
        public void DiagnosticsRemoval_LackOfAutoAttribute()
        {
            var source = @"namespace Tests
{
    [Auto.AutoDeconstructable]
    public partial record RecordPoint2d(double X, double Y) { }  
}";
            var compilation = CreateCompilation(source);
            var initialDiagnostics = GetCompilationIssues(compilation);

            Assert.That(initialDiagnostics, Has.Count.EqualTo(1));
            Assert.That(initialDiagnostics, Has.All.Contain("The type or namespace name 'Auto' could not be found"));


            RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
            Assert.That(diagnostics, Is.Empty);
        }

        private static readonly IEnumerable<TestCaseData> _negativeDiagnostics = new (string source, string rule, string expectedMessagePart)[]
        {
            (@"[AutoDeconstructable] class NonPartial { }", nameof(AutoDeconstructableGenerator.NonPartialTypeRule), "Type decorated with AutoDeconstructableAttribute must be also declared partial"),

            (@"[AutoDeconstructable]
              [DeconstructableSettings('1','2','3','4','5','6')]
              partial class NonPrimitive { }", nameof(AutoDeconstructableGenerator.InvalidSettingsAttributeRule), "DeconstructableSettingsAttribute must be constructed with 5 characters and bool type, or with default values"),

            (@"[AutoDeconstructable] partial class NoMatching { public NoMatching(int i){} public void Deconstruct(out float f){} }", nameof(AutoDeconstructableGenerator.NoMatchingCtorAndDeconstructRule), "NoMatching: No matching constructor and Deconstruct pair found"),

            (@"[AutoDeconstructable]
              partial class Ctor1Deconstruct2
              {
                  public int A { get; } public int B { get; }
                  public Ctor1Deconstruct2(int a) => A = a;
                  public void Deconstruct(out int a, out int b) { a = A; b = B; }
              }", nameof(AutoDeconstructableGenerator.NoMatchingCtorAndDeconstructRule), "Ctor1Deconstruct2: No matching constructor and Deconstruct pair found"),

            (@"[AutoDeconstructable] partial class NoProperties { public NoProperties() {} public void Deconstruct() { }}", nameof(AutoDeconstructableGenerator.NoContractMembersRule), "warning AutoDeconstructable50: NoProperties: No members for serialization"),

            (@"[AutoDeconstructable] partial class NoCtor { private NoCtor() {} public void Deconstruct() { }}", nameof(AutoDeconstructableGenerator.NoConstructor), "NoCtor: No constructor to support serialization. Only Record get constructors automatically. Private constructor is not enough - it cannot be called"),

            (@"[AutoDeconstructable] partial class NoDeconstruct { public NoDeconstruct() {} }", nameof(AutoDeconstructableGenerator.NoDeconstruct), "NoDeconstruct: No Deconstruct to support serialization. Only Record get Deconstruct automatically. All Deconstruct parameters must have 'out' passing type"),

            (@"namespace Test {
    partial class ContainingType { [Auto.AutoDeconstructable] partial class Test{} } 
}", nameof(AutoDeconstructableGenerator.NamespaceAndTypeNamesEqualRule), "Test: Type name cannot be equal to containing namespace: 'Nemesis.TextParsers.CodeGen.Tests.Test'"),
        }
            .Select((t, i) => new TestCaseData($@"using Auto; using Nemesis.TextParsers.Settings; namespace Nemesis.TextParsers.CodeGen.Tests {{ {t.source} }}", t.rule, t.expectedMessagePart)
                .SetName($"{(i + 1):00}_{t.rule}"));

        [TestCaseSource(nameof(_negativeDiagnostics))]
        public void Diagnostics_CheckNegativeCases(in string source, in string ruleName, in string expectedMessagePart)
        {
            var rule = (DiagnosticDescriptor)typeof(AutoDeconstructableGenerator)
                .GetField(ruleName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) ?? throw new NotSupportedException($"Rule '{ruleName}' does not exist");

            var compilation = CreateCompilation(source);

            RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());

            var diagnosticsList = diagnostics.ToList();
            Assert.That(diagnosticsList, Has.Count.EqualTo(1));

            var diagnostic = diagnosticsList.Single();

            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(rule.Id));
            Assert.That(diagnostic.ToString(), Does.Contain(expectedMessagePart));
        }

        [Test]
        public void Diagnostics_RemoveSettingsAttribute()
        {
            var source = @"[AutoDeconstructable] partial class DoeNotMatter { }";

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? throw new InvalidOperationException("The location of the .NET assemblies cannot be retrieved");

            var compilation = CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
                new[]
                {
                    MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    //Important - no NTP reference
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
            var diagnosticsList = diagnostics.ToList();


            Assert.That(diagnosticsList, Has.Count.EqualTo(1));

            var diagnostic = diagnosticsList.Single();
            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(AutoDeconstructableGenerator.NoSettingsAttributeRule.Id));
            Assert.That(diagnostic.ToString(), Does.Contain(@"Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute is not recognized. Please reference Nemesis.TextParsers into your project"));
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


