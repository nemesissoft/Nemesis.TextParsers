#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nemesis.TextParsers.Settings;

/// <summary>
/// Create instance of <see cref="CollectionSettingsBase"/> 
/// </summary>
/// <remarks>
/// For performance reasons, all delimiters and escaped characters are single chars.
/// This makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance
/// </remarks>
/// <param name="DefaultCapacity">Capacity used for creating initial collection/list/array. Use no value (null) to calculate capacity each time based on input</param>
public abstract record CollectionSettingsBase(
    char ListDelimiter, char NullElementMarker, char EscapingSequenceStart,
    char? Start, char? End, byte? DefaultCapacity) : ISettings
{
    public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";


    public int GetCapacity(in ReadOnlySpan<char> input) => DefaultCapacity ?? CountCharacters(input, ListDelimiter) + 1;

    private static int CountCharacters(in ReadOnlySpan<char> input, char character)
    {
        var count = 0;
        for (int i = input.Length - 1; i >= 0; i--)
            if (input[i] == character) count++;
        return count;
    }

    public bool IsValid([NotNullWhen(false)] out string? error)
    {
        if (ListDelimiter == NullElementMarker ||
            ListDelimiter == EscapingSequenceStart ||
            ListDelimiter == Start ||
            ListDelimiter == End ||

            NullElementMarker == EscapingSequenceStart ||
            NullElementMarker == Start ||
            NullElementMarker == End ||

            EscapingSequenceStart == Start ||
            EscapingSequenceStart == End
        )
        {
            error = $"""
                {GetType().Name} requires unique characters to be used for parsing/formatting purposes. 
                Start ('{Start}') and end ('{End}') properties can be equal to each other
                """;
            return false;
        }
        else
        {
            error = null;
            return true;
        }
    }
}

/// <inheritdoc/>
public sealed record CollectionSettings(
    char ListDelimiter = '|',
    char NullElementMarker = '∅',
    char EscapingSequenceStart = '\\',
    char? Start = null,
    char? End = null,
    byte? DefaultCapacity = null
) : CollectionSettingsBase(ListDelimiter, NullElementMarker, EscapingSequenceStart, Start, End, DefaultCapacity),
    ISettings<CollectionSettings>
{
    public CollectionSettings DeepClone() => this with { };

    public static CollectionSettings Default { get; } = new();
}

/// <inheritdoc/>
public sealed record ArraySettings(
    char ListDelimiter = '|',
    char NullElementMarker = '∅',
    char EscapingSequenceStart = '\\',
    char? Start = null,
    char? End = null,
    byte? DefaultCapacity = null
) : CollectionSettingsBase(ListDelimiter, NullElementMarker, EscapingSequenceStart, Start, End, DefaultCapacity),
    ISettings<ArraySettings>
{
    public ArraySettings DeepClone() => this with { };

    public static ArraySettings Default { get; } = new();
}

/// <summary>
/// Create instance of <see cref="DictionarySettings"/>
/// </summary>
/// <param name="DefaultCapacity">Capacity used for creating initial dictionary. Use no value (null) to calculate capacity each time based on input</param>
public sealed record DictionarySettings(
    char DictionaryPairsDelimiter = ';',
    char DictionaryKeyValueDelimiter = '=',
    char NullElementMarker = '∅',
    char EscapingSequenceStart = '\\',
    char? Start = null,
    char? End = null,
    DictionaryBehaviour Behaviour = DictionaryBehaviour.OverrideKeys,
    byte? DefaultCapacity = null
) : ISettings<DictionarySettings>
{
    public bool IsValid([NotNullWhen(false)] out string? error)
    {
        if (DictionaryPairsDelimiter == DictionaryKeyValueDelimiter ||
            DictionaryPairsDelimiter == NullElementMarker ||
            DictionaryPairsDelimiter == EscapingSequenceStart ||
            DictionaryPairsDelimiter == Start ||
            DictionaryPairsDelimiter == End ||

            DictionaryKeyValueDelimiter == NullElementMarker ||
            DictionaryKeyValueDelimiter == EscapingSequenceStart ||
            DictionaryKeyValueDelimiter == Start ||
            DictionaryKeyValueDelimiter == End ||

            NullElementMarker == EscapingSequenceStart ||
            NullElementMarker == Start ||
            NullElementMarker == End ||

            EscapingSequenceStart == Start ||
            EscapingSequenceStart == End
        )
        {
            error = $"""
                {nameof(DictionarySettings)} requires unique characters to be used for parsing/formatting purposes. 
                Start ('{Start}') and end ('{End}') properties can be equal to each other
                """;
            return false;
        }
        else if (
#if NET
            !Enum.IsDefined(Behaviour)

#else
            !Enum.IsDefined(typeof(DictionaryBehaviour), Behaviour)

#endif
            )
        {
            error = $"Value of '{Behaviour}' is illegal for {nameof(DictionaryBehaviour)}";
            return false;
        }
        else
        {
            error = null;
            return true;
        }
    }

    public DictionarySettings DeepClone() => this with { };

    public static DictionarySettings Default { get; } = new();

    public override string ToString() =>
        $"{Start}Key1{DictionaryKeyValueDelimiter}Value1{DictionaryPairsDelimiter}…{DictionaryPairsDelimiter}KeyN{DictionaryKeyValueDelimiter}ValueN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}', created by {Behaviour}";


    public int GetCapacity(in ReadOnlySpan<char> input)
        => DefaultCapacity ?? CountCharacters(input, DictionaryPairsDelimiter) + 1;

    private static int CountCharacters(in ReadOnlySpan<char> input, char character)
    {
        var count = 0;
        for (int i = input.Length - 1; i >= 0; i--)
            if (input[i] == character) count++;
        return count;
    }
}

public enum DictionaryBehaviour : byte { OverrideKeys, DoNotOverrideKeys, ThrowOnDuplicate }
