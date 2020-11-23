extern alias original;
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

            var newComp = RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
            Assert.That(diagnostics, Is.Empty);


            var generatedTrees = newComp.RemoveSyntaxTrees(compilation.SyntaxTrees).SyntaxTrees;

            var root = (CompilationUnitSyntax)generatedTrees.Last().GetRoot();

            var actual = ScrubGeneratorComments(root.ToFullString());

            Assert.That(actual, Is.EqualTo(@"HEAD
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

            (@"[AutoDeconstructable] partial class NoMembers { }", nameof(AutoDeconstructableGenerator.NoMatchingCtorAndDeconstructRule), "NoMembers: No matching constructor and Deconstruct pair found"),

            (@"[AutoDeconstructable]
              partial class Ctor1Deconstruct2
              {
                  public int A { get; } public int B { get; }
                  public Ctor1Deconstruct2(int a) => A = a;
                  public void Deconstruct(out int a, out int b) { a = A; b = B; }
              }", nameof(AutoDeconstructableGenerator.NoMatchingCtorAndDeconstructRule), "Ctor1Deconstruct2: No matching constructor and Deconstruct pair found"),
            
            //(@"[AutoDeconstructable] partial class NoProperties { public NoProperties() {} public void Deconstruct() { }}", AutoDeconstructableGenerator.NoContractMembersRule, "Cannot emulate test"),
            
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

            var diagnosticDescriptor = diagnosticsList.Single().Descriptor;

            Assert.That(diagnosticDescriptor.Id, Is.EqualTo($"AutoDeconstructable06"));
            Assert.That(diagnosticDescriptor.MessageFormat.ToString(), Does.Contain(@"Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute is not recognized. Please reference Nemesis.TextParsers into your project"));
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
            
       }
           .Select((t, i) => new TestCaseData($@"using Nemesis.TextParsers.Settings; namespace Tests {{ [Auto.AutoDeconstructable] {t.typeDefinition} }}", t.expectedCodePart)
               .SetName($"{(i + 1):00}"));

        [TestCaseSource(nameof(_settingsCases))]
        public void SettingsRetrieval_ShouldEmitProperValues(string source, string expectedCodePart)
        {
            //arrange
            var compilation = CreateCompilation(source);
            
            //act
            var newComp = RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
            var generatedTrees = newComp.RemoveSyntaxTrees(compilation.SyntaxTrees).SyntaxTrees;
            var root = (CompilationUnitSyntax)generatedTrees.Last().GetRoot();
            var actual = root.ToFullString();

            Assert.That(diagnostics, Is.Empty);

            Assert.That(actual, Does.Contain(expectedCodePart));

        }
        //TODO add test for default settings (no attribute - use Settings store) and attribute provided settings (both default and user provided)
        //+ for UseDeconstructableEmpty == true/false:  public override Child GetEmpty() => new Child(_transformer_age.GetEmpty(), _transformer_weight.GetEmpty());
        //implement record support and add examples with that 

        /*

            [DeconstructableSettings(';', '␀', '/', '\0', '\0')]
            internal class Kindergarten
            {
                public string Address { get; }
                public Child[] Children { get; }

                public Kindergarten(string address, Child[] children)
                {
                    Address = address;
                    Children = children;
                }

                public void Deconstruct(out string address, out Child[] children)
                {
                    address = Address;
                    children = Children;
                }
            }

            [DeconstructableSettings('_')]
            internal class UnderscoreSeparatedProperties
            {
                public string Data1 { get; }
                public string Data2 { get; }
                public string Data3 { get; }

                public UnderscoreSeparatedProperties(string data1, string data2, string data3)
                {
                    Data1 = data1;
                    Data2 = data2;
                    Data3 = data3;
                }

                public void Deconstruct(out string data1, out string data2, out string data3)
                {
                    data1 = Data1;
                    data2 = Data2;
                    data3 = Data3;
                }

                public override string ToString() => $"{nameof(Data1)}: {Data1}, {nameof(Data2)}: {Data2}, {nameof(Data3)}: {Data3}";
            }*/
    }
}


