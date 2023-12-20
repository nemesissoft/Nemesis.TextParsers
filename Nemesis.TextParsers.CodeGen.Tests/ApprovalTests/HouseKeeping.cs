using ApprovalTests.Maintenance;

namespace Nemesis.TextParsers.CodeGen.Tests.ApprovalTests;

[TestFixture, Explicit]
internal class HouseKeeping
{
    [Test]
    public void Check() => ApprovalMaintenance.VerifyNoAbandonedFiles();
}
