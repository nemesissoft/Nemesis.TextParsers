using System;
using System.ComponentModel;

namespace Nemesis.TextParsers.Settings
{
    /// <summary>
    /// Collection parsing settings
    /// </summary>
    /// <remarks><![CDATA[
    /// For performance reasons, all delimiters and escaped characters are single chars. This makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance. 
    /// Capacity used for creating initial collection/list/array. Use no value (null) to calculate capacity each time based on input
    /// ]]> </remarks>
    public abstract record CollectionSettingsBase(char ListDelimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End, byte? DefaultCapacity) : ISettings
    {
        public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

        public void Validate()
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
                throw new ArgumentException($@"{nameof(CollectionSettingsBase)} requires unique characters to be used for parsing/formatting purposes. 
Start ('{Start}') and end ('{End}') can be equal to each other");
        }

        public int GetCapacity(in ReadOnlySpan<char> input)
            => DefaultCapacity ?? CountCharacters(input, ListDelimiter) + 1;

        internal static int CountCharacters(in ReadOnlySpan<char> input, char character)
        {
            var count = 0;
            for (int i = input.Length - 1; i >= 0; i--)
                if (input[i] == character) count++;
            return count;
        }
    }

    /// <inheritdoc cref="CollectionSettingsBase" />
    public sealed record CollectionSettings(char ListDelimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End, byte? DefaultCapacity)
                       : CollectionSettingsBase(ListDelimiter, NullElementMarker, EscapingSequenceStart, Start, End, DefaultCapacity)
    {
        public static CollectionSettings Default { get; } = new('|', '∅', '\\', null, null, null);

        // ReSharper disable once RedundantOverriddenMember
        public override string ToString() => base.ToString();
    }

    /// <inheritdoc cref="CollectionSettingsBase" />
    public sealed record ArraySettings(char ListDelimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End, byte? DefaultCapacity)
                   : CollectionSettingsBase(ListDelimiter, NullElementMarker, EscapingSequenceStart, Start, End, DefaultCapacity)
    {
        public static ArraySettings Default { get; } = new('|', '∅', '\\', null, null, null);

        // ReSharper disable once RedundantOverriddenMember
        public override string ToString() => base.ToString();
    }




    /// <summary>
    /// Dictionary parsing settings
    /// </summary>
    /// <remarks><![CDATA[
    /// For performance reasons, all delimiters and escaped characters are single chars. This makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance. 
    /// Capacity used for creating initial collection/list/array. Use no value (null) to calculate capacity each time based on input
    /// ]]> </remarks>
    public sealed record DictionarySettings(char DictionaryPairsDelimiter, char DictionaryKeyValueDelimiter, char NullElementMarker, char EscapingSequenceStart, char? Start,
            char? End, DictionaryBehaviour Behaviour, byte? DefaultCapacity) : ISettings
    {
        public void Validate()
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
                throw new ArgumentException(
                    $@"{nameof(DictionarySettings)} requires unique characters to be used for parsing/formatting purposes. 
Start ('{Start}') and end ('{End}') can be equal to each other");


            if (!Enum.IsDefined(typeof(DictionaryBehaviour), Behaviour))
                throw new InvalidEnumArgumentException(nameof(Behaviour), (int)Behaviour, typeof(DictionaryBehaviour));
        }

        public static DictionarySettings Default { get; } = new(';', '=', '∅', '\\', null, null, DictionaryBehaviour.OverrideKeys, null);


        public override string ToString() =>
            $"{Start}Key1{DictionaryKeyValueDelimiter}Value1{DictionaryPairsDelimiter}…{DictionaryPairsDelimiter}KeyN{DictionaryKeyValueDelimiter}ValueN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}', created by {Behaviour}";


        public int GetCapacity(in ReadOnlySpan<char> input)
            => DefaultCapacity ?? CollectionSettingsBase.CountCharacters(input, DictionaryPairsDelimiter) + 1;

        
    }
    
    public enum DictionaryBehaviour : byte
    {
        OverrideKeys,
        DoNotOverrideKeys,
        ThrowOnDuplicate
    }
}
