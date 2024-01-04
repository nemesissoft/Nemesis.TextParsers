using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;
using Nemesis.TextParsers.CodeGen.Enums;
using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests;

[TestFixture, Explicit]
[UseReporter(typeof(VisualStudioReporter), typeof(ClipboardReporter))]
internal class EnumTransformer_AT
{
    [Test] public void Enum_Month() => RunCase("Month");

    [Test] public void Enum_Months() => RunCase("Months");

    [Test] public void Enum_DaysOfWeek() => RunCase("DaysOfWeek");

    [Test] public void Enum_EmptyEnum() => RunCase("EmptyEnum");

    [Test] public void Enum_SByteEnum() => RunCase("SByteEnum");

    [Test] public void Enum_Int64Enum() => RunCase("Int64Enum");

    [Test] public void Enum_Casing() => RunCase("Casing");



    private static void RunCase(string name)
    {
        var (_, source, _, _) = EnumTransformerGeneratorTests.EnumCodeGenCases.SingleOrDefault(t => t.name == name);
        Assert.That(source, Is.Not.Null);
        Assert.That(source, Is.Not.Empty);


        var compilation = CreateValidCompilation(source);

        var sources = new EnumTransformerGenerator().RunIncrementalGeneratorAndGetGeneratedSources(compilation);

        var actual = ScrubGeneratorComments(sources.Single());

        actual = NormalizeNewLines(actual);

        Approvals.Verify(WriterFactory.CreateTextWriter(actual, "cs"));
    }
}
