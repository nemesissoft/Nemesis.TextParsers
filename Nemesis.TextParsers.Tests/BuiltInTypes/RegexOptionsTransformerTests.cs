using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture]
public class RegexOptionsTransformerTests
{
    [Test]
    public void ParseTests()
    {
        var sut = RegexOptionsTransformer.Instance;

        for (int i = 0; i < 1024; i++)
        {
            var option = (RegexOptions)i;
            TestHelper.RoundTrip(option, sut);
        }
    }
}
