using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture]
public class IndexAndRangeTransformerTests
{
    private static TCD[] IndexParseData() =>
    [
        new("", Index.Start),
        new("  ", Index.Start),
        new(" ", Index.Start),
        new("1234", new Index(1234)),
        new("^5678", new Index(5678, true))
    ];

    [TestCaseSource(nameof(IndexParseData))]
    public void ParseIndex(string text, Index expected) =>
        Assert.That(Sut.DefaultStore.GetTransformer<Index>().Parse(text), Is.EqualTo(expected));


    private static TCD[] RangeParseData() =>
    [
        new("", Range.All),
        new("..", Range.All),
        new(" .. ", Range.All),
        new("5..", new Range(5, Index.End)),
        new("5.. ", new Range(5, Index.End)),
        new("..4", new Range(Index.Start, 4)),
        new("^6..^4", new Range(new Index(6, true), new Index(4, true))),
    ];

    [TestCaseSource(nameof(RangeParseData))]
    public void RangeParse(string text, Range expected) =>
        Assert.That(Sut.DefaultStore.GetTransformer<Range>().Parse(text), Is.EqualTo(expected));
}