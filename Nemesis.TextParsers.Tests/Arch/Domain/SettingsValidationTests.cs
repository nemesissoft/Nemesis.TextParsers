using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests.Arch.Domain;

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
    public void CollectionSettings_ValidationFails_ForInvalidState(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
    {
        var isValid = new CollectionSettings(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, 0)
            .IsValid(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("CollectionSettings requires unique characters to be used for parsing/formatting purposes"));
        });
    }

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
    public void DictionarySettings_ValidationFails_ForInvalidState(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
    {
        var isValid = new DictionarySettings(dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker, escapingSequenceStart, start, end, DictionaryBehaviour.OverrideKeys, 0)
            .IsValid(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("DictionarySettings requires unique characters to be used for parsing/formatting purposes"));
        });
    }

    [TestCase((DictionaryBehaviour)3)]
    [TestCase((DictionaryBehaviour)4)]
    [TestCase((DictionaryBehaviour)255)]
    public void DictionarySettings_ValidationFails_ForInvalidState_Behaviour(DictionaryBehaviour behaviour)
    {
        var isValid = new DictionarySettings('A', 'B', 'C', 'D', 'E', 'F', behaviour, 0)
            .IsValid(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("is illegal for DictionaryBehaviour"));
        });
    }

    [TestCase('A', 'A', 'C', 'D', 'E')]
    [TestCase('A', 'B', 'A', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'A', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'A')]

    [TestCase('A', 'B', 'B', 'D', 'E')]
    [TestCase('A', 'B', 'C', 'B', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'B')]

    [TestCase('A', 'B', 'C', 'C', 'E')]
    [TestCase('A', 'B', 'C', 'D', 'C')]
    public void TupleSettings_ValidationFails_ForInvalidState(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
    {
        var isValid = new ValueTupleSettings(delimiter, nullElementMarker, escapingSequenceStart, start, end)
            .IsValid(out var error);
        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(error, Does.Contain("ValueTupleSettings requires unique characters to be used for parsing/formatting purposes"));
        });
    }
}
