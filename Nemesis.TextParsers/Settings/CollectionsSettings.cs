using System;

namespace Nemesis.TextParsers.Settings
{
    /* TODO check if coll/dict special chars are distinct
      TODO check also start/end char 
        var specialCharacters = new[] { listDelimiter, dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker, escapingSequenceStart };
                if (specialCharacters.Length != specialCharacters.Distinct().Count())
                    throw new ArgumentException($"All special characters have to be distinct. Actual special characters:{string.Join(", ", specialCharacters.Select(c => $"'{c}'"))}");
                    
        */

    public abstract class CollectionSettingsBase : ISettings
    {
        //for performance reasons, all delimiters and escaped characters are single chars.
        //this makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance 
        public char ListDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }
        public byte? DefaultCapacity { get; }

        protected CollectionSettingsBase(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, byte? defaultCapacity)
        {
            ListDelimiter = listDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            DefaultCapacity = defaultCapacity;
        }

        public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

        
        public int GetCapacity(in ReadOnlySpan<char> input)
            => DefaultCapacity ?? CountCharacters(input, ListDelimiter);

        private static int CountCharacters(in ReadOnlySpan<char> input, char character)
        {
            var count = 0;
            for (int i = input.Length - 1; i >= 0; i--)
                if (input[i] == character) count++;
            return count;
        }
    }

    public class CollectionSettings : CollectionSettingsBase
    {
        public static CollectionSettings Default { get; } =
            new CollectionSettings('|', '∅', '\\', null, null, null);

        public CollectionSettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart,
            char? start, char? end, byte? defaultCapacity) 
            : base(listDelimiter, nullElementMarker, escapingSequenceStart, start, end, defaultCapacity) { }
    }

    public class ArraySettings : CollectionSettingsBase
    {
        public static ArraySettings Default { get; } =
            new ArraySettings('|', '∅', '\\', null, null, null);

        public ArraySettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart, 
            char? start, char? end, byte? defaultCapacity) 
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
        public byte? DefaultCapacity { get; }

        public DictionarySettings(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, DictionaryBehaviour behaviour, byte? defaultCapacity)
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
            new DictionarySettings(';', '=', '∅', '\\', null, null, DictionaryBehaviour.OverrideKeys, null);


        public override string ToString() =>
            $"{Start}Key1{DictionaryKeyValueDelimiter}Value1{DictionaryPairsDelimiter}…{DictionaryPairsDelimiter}KeyN{DictionaryKeyValueDelimiter}ValueN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}', created by {Behaviour}";


        public int GetCapacity(in ReadOnlySpan<char> input)
            => DefaultCapacity ?? CountCharacters(input, DictionaryPairsDelimiter);

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
}
