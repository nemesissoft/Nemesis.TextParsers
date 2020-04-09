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

        protected CollectionSettingsBase(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        {
            ListDelimiter = listDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
        }

        public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
    }

    public class CollectionSettings : CollectionSettingsBase
    {
        public static CollectionSettings Default { get; } =
                    new CollectionSettings('|', '∅', '\\', null, null);

        public CollectionSettings(char listDelimiter, char nullElementMarker,
            char escapingSequenceStart, char? start, char? end)
            : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end) { }
    }

    public class ArraySettings : CollectionSettingsBase
    {
        public static ArraySettings Default { get; } =
            new ArraySettings('|', '∅', '\\', null, null);

        public ArraySettings(char listDelimiter, char nullElementMarker,
            char escapingSequenceStart, char? start, char? end)
            : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end) { }
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

        public DictionarySettings(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, DictionaryBehaviour behaviour)
        {
            DictionaryPairsDelimiter = dictionaryPairsDelimiter;
            DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            Behaviour = behaviour;
        }

        public void Deconstruct(out char dictionaryPairsDelimiter, out char dictionaryKeyValueDelimiter, out char nullElementMarker, out char escapingSequenceStart, out char? start, out char? end, out DictionaryBehaviour behaviour)
        {
            dictionaryPairsDelimiter = DictionaryPairsDelimiter;
            dictionaryKeyValueDelimiter = DictionaryKeyValueDelimiter;
            nullElementMarker = NullElementMarker;
            escapingSequenceStart = EscapingSequenceStart;
            start = Start;
            end = End;
            behaviour = Behaviour;
        }

        public static DictionarySettings Default { get; } =
            new DictionarySettings(';', '=', '∅', '\\', null, null, DictionaryBehaviour.OverrideKeys);


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
