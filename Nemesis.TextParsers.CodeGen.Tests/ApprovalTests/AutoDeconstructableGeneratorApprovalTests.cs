using ApprovalTests;
using ApprovalTests.Maintenance;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;

using static Nemesis.TextParsers.CodeGen.Tests.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests;

[TestFixture, Explicit]
[UseReporter(typeof(TortoiseDiffReporter), typeof(ClipboardReporter))]
internal class AutoDeconstructableGeneratorApprovalTests
{
    [Test] public void ApprovalTestsRecord() => RunCase("Record");

    [Test] public void ApprovalTestsStruct() => RunCase("ReadOnlyStruct");

    [Test] public void ApprovalTestsLarge() => RunCase("Large");

    [Test] public void ApprovalTestsComplexTypes() => RunCase("ComplexType");

    [Test] public void ApprovalTestsSimpleWrapperStruct() => RunCase("SimpleWrapperStruct");


    [Test]
    public void HouseKeeping() => ApprovalMaintenance.VerifyNoAbandonedFiles();

    private static void RunCase(string index)
    {
        var (_, source, _) = EndToEndCases.AutoDeconstructableCases().SingleOrDefault(t => t.name == index);
        Assert.That(source, Is.Not.Null);
        Assert.That(source, Is.Not.Empty);

        var compilation = CreateCompilation(
            $@"using Nemesis.TextParsers.Settings; namespace Nemesis.TextParsers.CodeGen.Tests {{ {source} }}");

        var generatedTrees = GetGeneratedTreesOnly(compilation);

        var actual = ScrubGeneratorComments(generatedTrees.Single());

        actual = Normalize(actual);

        Approvals.Verify(WriterFactory.CreateTextWriter(actual, "cs"));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
}
