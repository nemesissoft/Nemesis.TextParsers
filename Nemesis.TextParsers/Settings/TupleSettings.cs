using System;

using Nemesis.TextParsers.Utils;

using Dsa = Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;

namespace Nemesis.TextParsers.Settings
{
    public abstract record TupleSettingsBase(char Delimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End) : ISettings
    {
        public override string ToString() =>
            $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

        public void Validate()
        {
            if (Delimiter == NullElementMarker ||
                Delimiter == EscapingSequenceStart ||
                Delimiter == Start ||
                Delimiter == End ||

                NullElementMarker == EscapingSequenceStart ||
                NullElementMarker == Start ||
                NullElementMarker == End ||

                EscapingSequenceStart == Start ||
                EscapingSequenceStart == End

            )
                throw new ArgumentException($@"{nameof(TupleSettingsBase)} requires unique characters to be used for parsing/formatting purposes. 
Start ('{Start}') and end ('{End}') can be equal to each other");
        }

        public TupleHelper ToTupleHelper() => new(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End);
    }

    public sealed record ValueTupleSettings(char Delimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End)
        : TupleSettingsBase(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End)
    {
        public static ValueTupleSettings Default { get; } = new(',', '∅', '\\', '(', ')');

        // ReSharper disable once RedundantOverriddenMember
        public override string ToString() => base.ToString();
    }

    public sealed record KeyValuePairSettings(char Delimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End)
        : TupleSettingsBase(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End)
    {
        public static KeyValuePairSettings Default { get; } = new('=', '∅', '\\', null, null);

        public override string ToString() =>
            $"{Start}Key{Delimiter}Value{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
    }

    public sealed record DeconstructableSettings(char Delimiter, char NullElementMarker, char EscapingSequenceStart, char? Start, char? End, bool UseDeconstructableEmpty)
        : TupleSettingsBase(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End)
    {
        public static DeconstructableSettings Default { get; } = new(Dsa.DEFAULT_DELIMITER,
            Dsa.DEFAULT_NULL_ELEMENT_MARKER,
            Dsa.DEFAULT_ESCAPING_SEQUENCE_START, Dsa.DEFAULT_START, Dsa.DEFAULT_END,
            Dsa.DEFAULT_USE_DECONSTRUCTABLE_EMPTY);

        public override string ToString() =>
            $@"{base.ToString()}. {(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator.";
    }

    public static class DeconstructableSettingsAttributeExtensions
    {
        public static DeconstructableSettings ToSettings(this Dsa attr) => new(attr.Delimiter, attr.NullElementMarker, attr.EscapingSequenceStart,
            attr.Start == '\0' ? (char?)null : attr.Start,
            attr.End == '\0' ? (char?)null : attr.End,
            attr.UseDeconstructableEmpty);
    }
}
