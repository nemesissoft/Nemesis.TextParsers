extern alias original;
using Microsoft.CodeAnalysis;

using Nemesis.CodeAnalysis;
using Nemesis.TextParsers.CodeGen.Deconstructable;

using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests;

[TestFixture]
public class AutoDeconstructable_Diagnostics
{
    [TestCase("""
        namespace Tests;
        [Auto.AutoDeconstructable]
        public partial record RecordPoint2d(double X, double Y);
        """)]
    public void DiagnosticsRemoval_LackOfAutoAttribute(string source)
    {
        var compilation = CreateValidCompilation(source);
        var initialDiagnostics = CompilationUtils.GetCompilationIssues(compilation).ToList();

        Assert.That(initialDiagnostics, Has.Count.EqualTo(1));
        Assert.That(initialDiagnostics, Has.All.Contain("The type or namespace name 'Auto' could not be found"));


        CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void DiagnosticsRemoval_LackOfAutoAttribute_EndToEnd() => Assert.Multiple(() =>
    {
        foreach (var (_, source, _) in EndToEndCases.GetAutoDeconstructableCases())
        {
            DiagnosticsRemoval_LackOfAutoAttribute(source);
        }
    });



    private static readonly IEnumerable<TCD> _negativeDiagnostics = new (string source, string rule, string expectedMessagePart)[]
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
        .Select((t, i) => new TCD($@"using Auto; using Nemesis.TextParsers.Settings; namespace Nemesis.TextParsers.CodeGen.Tests {{ {t.source} }}", t.rule, t.expectedMessagePart)
            .SetName($"Negative{i + 1:00}_{t.rule}"));

    [TestCaseSource(nameof(_negativeDiagnostics))]
    public void Diagnostics_CheckNegativeCases(string source, string ruleName, string expectedMessagePart)
    {
        var rule = (DiagnosticDescriptor)typeof(AutoDeconstructableGenerator)
            .GetField(ruleName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) ?? throw new NotSupportedException($"Rule '{ruleName}' does not exist");

        var compilation = CreateValidCompilation(source);

        CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());

        var diagnosticsList = diagnostics.ToList();
        Assert.That(diagnosticsList, Has.Count.EqualTo(1));

        var diagnostic = diagnosticsList.Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(rule.Id));
            Assert.That(diagnostic.ToString(), Does.Contain(expectedMessagePart));
        });
    }

    [Test]
    public void Diagnostics_NoSettingsAttributeReference()
    {
        //Important - no NTP reference - test is about removal of NTP library and expected code gen (lack of) ability to generate code
        var compilation = CreateTestCompilation(@"[AutoDeconstructable] partial class DoesNotMatter { }");

        CompilationUtils.RunGenerators(compilation, out var diagnostics, new AutoDeconstructableGenerator());
        var diagnosticsList = diagnostics.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(diagnosticsList, Has.Count.EqualTo(1));

            var diagnostic = diagnosticsList.Single();
            Assert.That(diagnostic.Descriptor.Id, Is.EqualTo(AutoDeconstructableGenerator.NoSettingsAttributeRule.Id));
            Assert.That(diagnostic.ToString(), Does.Contain(@"Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute is not recognized. Please reference Nemesis.TextParsers into your project"));
        });
    }
}
