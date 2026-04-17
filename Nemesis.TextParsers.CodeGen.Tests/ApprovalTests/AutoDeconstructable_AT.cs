using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;
using Nemesis.TextParsers.CodeGen.Deconstructable;
using Nemesis.TextParsers.CodeGen.Enums;
using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests;

[TestFixture, Explicit]
[UseReporter(typeof(P4MergeReporter), typeof(PowerShellClipboardReporter))]
internal class AutoDeconstructable_AT
{
    [Test] public void ApprovalTestsRecord() => RunCase("Record");

    [Test] public void ApprovalTestsStruct() => RunCase("ReadOnlyStruct");

    [Test] public void ApprovalTestsLarge() => RunCase("Large");

    [Test] public void ApprovalTestsComplexTypes() => RunCase("ComplexType");

    [Test] public void ApprovalTestsSimpleWrapperStruct() => RunCase("SimpleWrapperStruct");


    private static void RunCase(string index)
    {
        var (_, source, _) = AutoDeconstructableTests.GetAutoDeconstructableCases().SingleOrDefault(t => t.name == index);
        Assert.That(source, Is.Not.Null);
        Assert.That(source, Is.Not.Empty);

        var compilation = CreateValidCompilation(source);

        var sources = new AutoDeconstructableGenerator().RunIncrementalGeneratorAndGetGeneratedSources(compilation);

        var actual = ScrubGeneratorComments(sources.Single());

        actual = NormalizeNewLines(actual);

        Approvals.Verify(WriterFactory.CreateTextWriter(actual, "cs"));
    }
}
