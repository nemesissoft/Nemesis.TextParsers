using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests.Infrastructure;

[TestFixture]
public class SettingsValidationTests
{
    [TestCase('A', 'A', 'C', 'D', 'E')]
    [TestCase('A', 'B', 'A', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'A', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'A')]

    [TestCase('A', 'B', 'B', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'B', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'B')]

    [TestCase('A', 'B', 'C', 'C', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'C')]
    public void CollectionSettings_ThrowsExceptionForInvalidState(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end) =>
        Assert.That(
            () => new CollectionSettings(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, 0),
            Throws.ArgumentException.And.Message.Contains("CollectionSettings requires unique characters to be used for parsing/formatting purposes")
            );

    [TestCase('A', 'A', 'C', 'D', 'E', 'F')]
    [TestCase('A', 'B', 'A', 'D', 'E', 'F')]
    [TestCase('A', 'B', 'C', 'A', 'E', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'A', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'E', 'A')]

    [TestCase('A', 'B', 'B', 'D', 'E', 'F')]
    [TestCase('A', 'B', 'C', 'B', 'E', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'B', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'E', 'B')]

    [TestCase('A', 'B', 'C', 'C', 'E', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'C', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'E', 'C')]

    [TestCase('A', 'B', 'C', 'D', 'D', 'F')]
    [TestCase('A', 'B', 'C', 'D', 'E', 'D')]
    public void DictionarySettings_ThrowsExceptionForInvalidState(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end) =>
        Assert.That(
            () => new DictionarySettings(dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker, escapingSequenceStart, start, end, DictionaryBehaviour.OverrideKeys, 0),
            Throws.ArgumentException.And.Message.Contains("DictionarySettings requires unique characters to be used for parsing/formatting purposes")
            );

    [TestCase((DictionaryBehaviour)3)]
    [TestCase((DictionaryBehaviour)4)]
    [TestCase((DictionaryBehaviour)255)]
    public void DictionarySettings_ThrowsExceptionForInvalidState_Behaviour(DictionaryBehaviour behaviour) =>
        Assert.That(
            () => new DictionarySettings('A', 'B', 'C', 'D', 'E', 'F', behaviour, 0),
            Throws.ArgumentException.And.Message.Contains("is illegal for DictionaryBehaviour")
            );

    [TestCase('A', 'A', 'C', 'D', 'E')]
    [TestCase('A', 'B', 'A', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'A', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'A')]

    [TestCase('A', 'B', 'B', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'B', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'B')]

    [TestCase('A', 'B', 'C', 'C', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'C')]
    public void TupleSettings_ThrowsExceptionForInvalidState(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end) =>
        Assert.That(
            () => new ValueTupleSettings(delimiter, nullElementMarker, escapingSequenceStart, start, end),
            Throws.ArgumentException.And.Message.Contains("ValueTupleSettings requires unique characters to be used for parsing/formatting purposes")
            );
}
