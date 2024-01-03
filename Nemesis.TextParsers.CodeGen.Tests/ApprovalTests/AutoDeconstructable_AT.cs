using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;
using Nemesis.TextParsers.CodeGen.Deconstructable;
using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests;

[TestFixture, Explicit]
[UseReporter(typeof(VisualStudioReporter), typeof(ClipboardReporter))]
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

        var generatedTrees = GetGeneratedTreesOnly(compilation, new AutoDeconstructableGenerator(), AutoDeconstructableGenerator.ATTRIBUTE_NAME);

        var actual = ScrubGeneratorComments(generatedTrees.Single());

        actual = IgnoreNewLinesComparer.NormalizeNewLines(actual);

        Approvals.Verify(WriterFactory.CreateTextWriter(actual, "cs"));
    }
}
