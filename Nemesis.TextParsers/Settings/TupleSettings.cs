using System;
using Nemesis.TextParsers.Utils;
using DectorSett = Nemesis.TextParsers.Settings.DeconstructableSettings;

namespace Nemesis.TextParsers.Settings
{
    public abstract class TupleSettings : ISettings
    {
        public char Delimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }

        protected TupleSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        {
            Delimiter = delimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
        }

        public override string ToString() =>
            $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

        public TupleHelper ToTupleHelper() =>
            new TupleHelper(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End);
    }

    public sealed class ValueTupleSettings : TupleSettings
    {
        public ValueTupleSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
            : base(delimiter, nullElementMarker, escapingSequenceStart, start, end) { }

        public static ValueTupleSettings Default { get; } = new ValueTupleSettings(',', '∅', '\\', '(', ')');
    }

    public sealed class KeyValuePairSettings : TupleSettings
    {
        public KeyValuePairSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
            : base(delimiter, nullElementMarker, escapingSequenceStart, start, end) { }

        public static KeyValuePairSettings Default { get; } = new KeyValuePairSettings('=', '∅', '\\', null, null);

        public override string ToString() =>
            $"{Start}Key{Delimiter}Value{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
    }

    public sealed class DeconstructableSettings : TupleSettings
    {
        public const char DEFAULT_DELIMITER = ';';
        public const char DEFAULT_NULL_ELEMENT_MARKER = '∅';
        public const char DEFAULT_ESCAPING_SEQUENCE_START = '\\';
        public const char DEFAULT_START = '(';
        public const char DEFAULT_END = ')';
        public const bool DEFAULT_USE_DECONSTRUCTABLE_EMPTY = true;

        public bool UseDeconstructableEmpty { get; }
        public DeconstructableSettings(char delimiter = DEFAULT_DELIMITER,
                char nullElementMarker = DEFAULT_NULL_ELEMENT_MARKER, 
                char escapingSequenceStart = DEFAULT_ESCAPING_SEQUENCE_START,
                char? start = DEFAULT_START, char? end = DEFAULT_END,
                bool useDeconstructableEmpty = DEFAULT_USE_DECONSTRUCTABLE_EMPTY)
            : base(delimiter, nullElementMarker, escapingSequenceStart, start, end)
            => UseDeconstructableEmpty = useDeconstructableEmpty;

        public static DectorSett Default { get; } = new DectorSett();

        public override string ToString() =>
            $@"{base.ToString()}. {(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator.";
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class DeconstructableSettingsAttribute : Attribute
    {
        public char Delimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char Start { get; }
        public char End { get; }
        public bool UseDeconstructableEmpty { get; }

        /// <summary>Initialize DeconstructableSettingsAttribute </summary>
        /// <param name="delimiter">Properties text delimiter</param>
        /// <param name="nullElementMarker">Null element marker</param>
        /// <param name="escapingSequenceStart">Escaping sequence start</param>
        /// <param name="start">Starting character. Use default(char)=='\0' to omit this character</param>
        /// <param name="end">End character. Use default(char)=='\0' to omit this character</param>
        /// <param name="useDeconstructableEmpty">When <c>true</c>, a default "empty" instance will be crated from empty string i.e. new Class(empty(parameter1), ..., empty(parameterN)) </param>
        public DeconstructableSettingsAttribute(char delimiter = DectorSett.DEFAULT_DELIMITER,
            char nullElementMarker = DectorSett.DEFAULT_NULL_ELEMENT_MARKER, char escapingSequenceStart = DectorSett.DEFAULT_ESCAPING_SEQUENCE_START,
            char start = DectorSett.DEFAULT_START, char end = DectorSett.DEFAULT_END,
            bool useDeconstructableEmpty = DectorSett.DEFAULT_USE_DECONSTRUCTABLE_EMPTY)
        {
            Delimiter = delimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
            UseDeconstructableEmpty = useDeconstructableEmpty;
        }

        public DectorSett ToSettings() => new DectorSett(Delimiter, NullElementMarker, EscapingSequenceStart,
            Start == '\0' ? (char?)null : Start,
            End == '\0' ? (char?)null : End,
            UseDeconstructableEmpty);
    }
}
