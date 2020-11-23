using System.Linq;

using ApprovalTests;
using ApprovalTests.Maintenance;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;

using NUnit.Framework;

using static Nemesis.TextParsers.CodeGen.Tests.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests
{
    [TestFixture, Explicit]
    [UseReporter(typeof(TortoiseDiffReporter), typeof(ClipboardReporter))]
    internal class AutoDeconstructableGeneratorApprovalTests
    {
        [Test] public void ApprovalTestsRecord() => RunCase(0);
        
        
        [Test] public void ApprovalTestsStruct() => RunCase(1);


        [Test]
        public void HouseKeeping()
        {
            ApprovalMaintenance.VerifyNoAbandonedFiles();
        }

        private static void RunCase(int index)
        {
            var compilation = CreateCompilation(EndToEndCases.AutoDeconstructableCases()[index].source);

            var generatedTrees = AutoDeconstructableGeneratorTests.GetGeneratedTreesOnly(compilation);

            var actual = ScrubGeneratorComments(generatedTrees.Single());

            actual = Normalize(actual);

            Approvals.Verify(WriterFactory.CreateTextWriter(actual, "cs"));
        }

        private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
