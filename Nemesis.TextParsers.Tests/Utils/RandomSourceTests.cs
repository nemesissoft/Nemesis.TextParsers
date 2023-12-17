namespace Nemesis.TextParsers.Tests.Utils;

[TestFixture]
internal class RandomSourceTests
{
    private RandomSource _sut;

    [SetUp]
    public void BeforeEachTest() => _sut = new RandomSource(42);

    private static IEnumerable<T> GetValues<T>(Func<int, T> generator) =>
        Enumerable.Range(1, 5).Select(i => generator(i)).ToArray();

    [Test]
    public void Next_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.Next());

        Assert.That(actual, Is.EqualTo(new[] { 1434747710, 302596119, 269548474, 1122627734, 361709742 }));
    }

    [Test]
    public void Next_Max_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.Next(i * 100));

        Assert.That(actual, Is.EqualTo(new[] { 66, 28, 37, 209, 84 }));
    }

    [Test]
    public void Next_MinMax_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.Next(i * -100, i * 100));

        Assert.That(actual, Is.EqualTo(new[] { 33, -144, -225, 18, -332 }));
    }

    [Test]
    public void NextDouble_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextDouble());

        Assert.That(actual, Is.EqualTo(new[] { 0.6681064659115423, 0.14090729837348093, 0.12551828945312568, 0.5227642760252413, 0.16843422416990353 }));
    }

    [Test]
    public void NextString_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextString('A', 'z', 8));

        Assert.That(actual, Is.EqualTo(new[] { "gIH_JPk^", "KmNO^SWP", "_CpbXIFi", "p`CjIui^", "KCRbpA`o" }));
    }

    [Test]
    public void NextElement_List_ShouldGenerateValues()
    {
        var chars = Enumerable.Range(65, 24).Select(i => (char)i).ToArray();
        var actual = GetValues(i => _sut.NextElement(chars));

        Assert.That(actual, Is.EqualTo(new[] { 'Q', 'D', 'D', 'M', 'E' }));
    }

    [Test]
    public void NextElement_Span_ShouldGenerateValues()
    {
        var chars = Enumerable.Range(65, 24).Select(i => (char)i).ToArray();
        var actual = GetValues(i => _sut.NextElement((Span<char>)chars));

        Assert.That(actual, Is.EqualTo(new[] { 'Q', 'D', 'D', 'M', 'E' }));
    }

    [Test]
    public void NextFloatingNumber_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextFloatingNumber(1000, i % 2 == 0));

        Assert.That(actual, Is.EqualTo(new[] { 336.213, -748.963, 45.529, -474.815, 448.817 }));
    }

    private enum ZeroEnum : byte { }

    [Test]
    public void NextEnum_ZeroElements_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextEnum<ZeroEnum, byte>());

        Assert.That(actual, Is.EqualTo(new[] { (ZeroEnum)1, (ZeroEnum)0, (ZeroEnum)0, (ZeroEnum)1, (ZeroEnum)0 }));
    }

    [Test]
    public void NextEnum_Standard_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextEnum<DayOfWeek, int>());

        Assert.That(actual, Is.EqualTo(new[] { DayOfWeek.Thursday, DayOfWeek.Sunday, DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Monday }));
    }

    [Test]
    public void NextEnum_Flag_ShouldGenerateValues()
    {
        var actual = GetValues(i => _sut.NextEnum<DaysOfWeek, byte>());

        Assert.That(actual, Is.EqualTo(new[] { (DaysOfWeek)169, DaysOfWeek.Monday | DaysOfWeek.Tuesday | DaysOfWeek.Saturday, DaysOfWeek.Weekdays, (DaysOfWeek)132, DaysOfWeek.Tuesday | DaysOfWeek.Thursday | DaysOfWeek.Saturday }));
    }
}
