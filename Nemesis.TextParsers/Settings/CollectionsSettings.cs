namespace Nemesis.TextParsers.Settings;

public abstract class CollectionSettingsBase : ISettings
{
    //for performance reasons, all delimiters and escaped characters are single chars.
    //this makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance 
    public char ListDelimiter { get; private set; }
    public char NullElementMarker { get; private set; }
    public char EscapingSequenceStart { get; private set; }
    public char? Start { get; private set; }
    public char? End { get; private set; }
    /// <summary>
    /// Capacity used for creating initial collection/list/array. Use no value (null) to calculate capacity each time based on input  
    /// </summary>
    public byte? DefaultCapacity { get; private set; }
#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif    
    protected CollectionSettingsBase(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, byte? defaultCapacity)
    {
        if (listDelimiter == nullElementMarker ||
            listDelimiter == escapingSequenceStart ||
            listDelimiter == start ||
            listDelimiter == end ||

            nullElementMarker == escapingSequenceStart ||
            nullElementMarker == start ||
            nullElementMarker == end ||

            escapingSequenceStart == start ||
            escapingSequenceStart == end

        )
            throw new ArgumentException($@"{nameof(CollectionSettingsBase)} requires unique characters to be used for parsing/formatting purposes. 
Start ('{start}') and end ('{end}') can be equal to each other");


        ListDelimiter = listDelimiter;
        NullElementMarker = nullElementMarker;
        EscapingSequenceStart = escapingSequenceStart;
        Start = start;
        End = end;
        DefaultCapacity = defaultCapacity;
    }

    public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";


    public int GetCapacity(in ReadOnlySpan<char> input) => DefaultCapacity ?? CountCharacters(input, ListDelimiter) + 1;

    private static int CountCharacters(in ReadOnlySpan<char> input, char character)
    {
        var count = 0;
        for (int i = input.Length - 1; i >= 0; i--)
            if (input[i] == character) count++;
        return count;
    }
}

public sealed class CollectionSettings : CollectionSettingsBase
{
    public static CollectionSettings Default { get; } = new('|', '∅', '\\', null, null, null);

#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public CollectionSettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart,
        char? start, char? end, byte? defaultCapacity)
        : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, defaultCapacity) { }
}

public sealed class ArraySettings : CollectionSettingsBase
{
    public static ArraySettings Default { get; } = new('|', '∅', '\\', null, null, null);

#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public ArraySettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart,
        char? start, char? end, byte? defaultCapacity)
        : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, defaultCapacity) { }
}




public sealed class DictionarySettings : ISettings
{
    public char DictionaryPairsDelimiter { get; private set; }
    public char DictionaryKeyValueDelimiter { get; private set; }
    public char NullElementMarker { get; private set; }
    public char EscapingSequenceStart { get; private set; }
    public char? Start { get; private set; }
    public char? End { get; private set; }
    public DictionaryBehaviour Behaviour { get; private set; }
    /// <summary>
    /// Capacity used for creating initial dictionary. Use no value (null) to calculate capacity each time based on input  
    /// </summary>
    public byte? DefaultCapacity { get; private set; }

#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public DictionarySettings(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, DictionaryBehaviour behaviour, byte? defaultCapacity)
    {
        if (dictionaryPairsDelimiter == dictionaryKeyValueDelimiter ||
            dictionaryPairsDelimiter == nullElementMarker ||
            dictionaryPairsDelimiter == escapingSequenceStart ||
            dictionaryPairsDelimiter == start ||
            dictionaryPairsDelimiter == end ||

            dictionaryKeyValueDelimiter == nullElementMarker ||
            dictionaryKeyValueDelimiter == escapingSequenceStart ||
            dictionaryKeyValueDelimiter == start ||
            dictionaryKeyValueDelimiter == end ||

            nullElementMarker == escapingSequenceStart ||
            nullElementMarker == start ||
            nullElementMarker == end ||

            escapingSequenceStart == start ||
            escapingSequenceStart == end
        )
            throw new ArgumentException(
                $@"{nameof(DictionarySettings)} requires unique characters to be used for parsing/formatting purposes. 
Start ('{start}') and end ('{end}') can be equal to each other");



        DictionaryPairsDelimiter = dictionaryPairsDelimiter;
        DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
        NullElementMarker = nullElementMarker;
        EscapingSequenceStart = escapingSequenceStart;
        Start = start;
        End = end;
        Behaviour = behaviour;
        DefaultCapacity = defaultCapacity;
    }

    public static DictionarySettings Default { get; } = new(';', '=', '∅', '\\', null, null, DictionaryBehaviour.OverrideKeys, null);


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

public enum DictionaryBehaviour : byte
{
    OverrideKeys,
    DoNotOverrideKeys,
    ThrowOnDuplicate
}
