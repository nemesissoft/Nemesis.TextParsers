namespace Nemesis.TextParsers.Tests;

[TestFixture]
public class SpanSplitExtensionsTests
{
    [TestCase(",,", ',')]
    [TestCase("", 'a')]
    [TestCase(";;", ';')]
    [TestCase("a;;b", ';')]
    [TestCase("a", 'a')]
    [TestCase("aaaa", 'a')]
    [TestCase("abababab", 'a')]
    [TestCase("babababa", 'a')]
    [TestCase("aaaaab", 'a')]
    [TestCase("baaaaa", 'a')]
    [TestCase("zzzzzazzzzazzzzazzz", 'a')]
    [TestCase("123,456,", ',')]
    public void Split_EmptySequence_Test(string str, char separator)
    {
        var actual = new List<string>();
        foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator, true))
            actual.Add(part.ToString());

        string[] expected = str == "" ? new string[0] : str.Split(separator);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(",,", ',')]
    [TestCase("", 'a')]
    [TestCase("a", 'a')]
    [TestCase("aaaa", 'a')]
    [TestCase("abababab", 'a')]
    [TestCase("babababa", 'a')]
    [TestCase("aaaaab", 'a')]
    [TestCase("baaaaa", 'a')]
    [TestCase("zzzzzazzzzazzzzazzz", 'a')]
    [TestCase("123,456,", ',')]
    public void SplitTest(string str, char separator)
    {
        var actual = new List<string>();
        foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator))
            actual.Add(part.ToString());

        string[] expected = str.Split(separator);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(",,;,,", ',', ';')]
    [TestCase("", 'a', ';')]
    [TestCase("a", 'a', ';')]
    [TestCase("aaaa", 'a', ';')]
    [TestCase("aa;aa", 'a', ';')]
    [TestCase("abababab", 'a', ';')]
    [TestCase("aba;ba;bab", 'a', ';')]
    [TestCase("babababa", 'a', ';')]
    [TestCase("baba;baba", 'a', ';')]
    [TestCase("aaa;aab", 'a', ';')]
    [TestCase("baa;aaa", 'a', ';')]
    [TestCase("zzz;zzazzzzazzzzazzz", 'a', ';')]
    [TestCase("zzz;zzazzzzaz;zzzazzz", 'a', ';')]
    [TestCase("123,;456,", ',', ';')]
    public void Split2Test(string str, char separator1, char separator2)
    {
        var actual = new List<string>();
        foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator1, separator2))
            actual.Add(part.ToString());

        string[] expected = str.Split(separator1, separator2);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(",|,;,,", ',', ';', '|')]
    [TestCase("", 'a', ';', '|')]
    [TestCase("a", 'a', ';', '|')]
    [TestCase("aaaa", 'a', ';', '|')]
    [TestCase("aa;aa", 'a', ';', '|')]
    [TestCase("ab|ab||ab|ab", 'a', ';', '|')]
    [TestCase("aba;b|a;bab", 'a', ';', '|')]
    [TestCase("bab|ababa", 'a', ';', '|')]
    [TestCase("babababa", 'a', ';', '|')]
    [TestCase("baba;ba|ba", 'a', ';', '|')]
    [TestCase("aaa;aa|b", 'a', ';', '|')]
    [TestCase("baa;aaa", 'a', ';', '|')]
    [TestCase("zzz;zzazzzz;456|azzzzazzz|", 'a', ';', '|')]
    [TestCase("|zzz|;zzazzzzaz;|zzzazzz|", 'a', ';', '|')]
    [TestCase("123,;|456,", ',', ';', '|')]
    [TestCase(",;|,;|,;|", ',', ';', '|')]
    public void Split3Test(string str, char separator1, char separator2, char separator3)
    {
        var actual = new List<string>();
        foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separator1, separator2, separator3))
            actual.Add(part.ToString());

        string[] expected = str.Split(separator1, separator2, separator3);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(01, ",|/,;/,,", ',', ';', '|', '/')]
    [TestCase(02, "", 'a', ';', '|', '/')]
    [TestCase(03, "a", 'a', ';', '|', '/')]
    [TestCase(04, "aaaa", 'a', ';', '|', '/')]
    [TestCase(05, "aa;aa", 'a', ';', '|', '/')]
    [TestCase(06, "ab|a/b||a/b|ab", 'a', ';', '|', '/')]
    [TestCase(07, "ab/a;b|a;bab", 'a', ';', '|', '/')]
    [TestCase(08, "bab|ab/aba", 'a', ';', '|', '/')]
    [TestCase(09, "ba//b/ababa", 'a', ';', '|', '/')]
    [TestCase(10, "ba//ba;ba|ba/", 'a', ';', '|', '/')]
    [TestCase(11, "/aaa;aa|b/", 'a', ';', '|', '/')]
    [TestCase(12, "/baa;aaa/", 'a', ';', '|', '/')]
    [TestCase(13, "z/zz;zz/azzz/z;456|azz/zzazzz/|/", 'a', ';', '|', '/')]
    [TestCase(14, "|/zzz|;zzazzzzaz;|zzz/azzz/|", 'a', ';', '|', '/')]
    [TestCase(15, "1/23,;|45/6,/", ',', ';', '|', '/')]
    [TestCase(16, "/,;|/,;|/,;|/", ',', ';', '|', '/')]
    public void SplitNTest(int _, string str, char separator1, char separator2, char separator3, char separator4)
    {
        var actual = new List<string>();
        ReadOnlySpan<char> separators = stackalloc char[] { separator1, separator2, separator3, separator4 };
        foreach (ReadOnlySpan<char> part in str.AsSpan().Split(separators))
            actual.Add(part.ToString());

        string[] expected = str.Split(separator1, separator2, separator3, separator4);
        Assert.That(actual, Is.EqualTo(expected));
    }
}
