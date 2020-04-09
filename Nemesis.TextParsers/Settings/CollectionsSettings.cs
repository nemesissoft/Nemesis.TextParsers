namespace Nemesis.TextParsers.Settings
{
    public abstract class CollectionSettingsBase : ISettings
    {
        //for performance reasons, all delimiters and escaped characters are single chars.
        //this makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance 
        public char ListDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }
        public ushort DefaultCapacity { get; }

        protected CollectionSettingsBase(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, ushort defaultCapacity)
        {
            ListDelimiter = listDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            DefaultCapacity = defaultCapacity;
        }

        public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
    }

    public class CollectionSettings : CollectionSettingsBase
    {
        public static CollectionSettings Default { get; } =
            new CollectionSettings('|', '∅', '\\', null, null, 8);

        public CollectionSettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart,
            char? start, char? end, ushort defaultCapacity) 
            : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, defaultCapacity) { }
    }

    public class ArraySettings : CollectionSettingsBase
    {
        public static ArraySettings Default { get; } =
            new ArraySettings('|', '∅', '\\', null, null, 8);

        public ArraySettings(char listDelimiter, char nullElementMarker, 
            char escapingSequenceStart, char? start, char? end, ushort defaultCapacity) 
            : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, defaultCapacity) { }
    }




    public class DictionarySettings : ISettings
    {
        public char DictionaryPairsDelimiter { get; }
        public char DictionaryKeyValueDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }
        public DictionaryBehaviour Behaviour { get; }
        public ushort DefaultCapacity { get; }

        public DictionarySettings(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, DictionaryBehaviour behaviour, ushort defaultCapacity)
        {
            DictionaryPairsDelimiter = dictionaryPairsDelimiter;
            DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            Behaviour = behaviour;
            DefaultCapacity = defaultCapacity;
        }
        
        public static DictionarySettings Default { get; } =
            new DictionarySettings(';', '=', '∅', '\\', null, null, DictionaryBehaviour.OverrideKeys, 8);


        public override string ToString() =>
            $"{Start}Key1{DictionaryKeyValueDelimiter}Value1{DictionaryPairsDelimiter}…{DictionaryPairsDelimiter}KeyN{DictionaryKeyValueDelimiter}ValueN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}', created by {Behaviour}";
    }

    public enum DictionaryBehaviour : byte
    {
        OverrideKeys,
        DoNotOverrideKeys,
        ThrowOnDuplicate
    }
}
