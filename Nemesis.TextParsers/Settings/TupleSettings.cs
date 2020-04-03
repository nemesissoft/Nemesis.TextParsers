using Nemesis.TextParsers.Utils;

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
    //TODO
    public sealed class KeyValuePairSettings : TupleSettings
    {
        public KeyValuePairSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
            : base(delimiter, nullElementMarker, escapingSequenceStart, start, end) { }

        public static KeyValuePairSettings Default { get; } = new KeyValuePairSettings('=', '∅', '\\', null, null);
    }
    //TODO
    public sealed class DeconstructableSettings : TupleSettings
    {
        public bool UseDeconstructableEmpty { get; }
        public DeconstructableSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, bool useDeconstructableEmpty)
            : base(delimiter, nullElementMarker, escapingSequenceStart, start, end)
            => UseDeconstructableEmpty = useDeconstructableEmpty;

        public static DeconstructableSettings Default { get; } =
            new DeconstructableSettings(';', '∅', '\\', '(', ')', true);

        public override string ToString() =>
            $@"{base.ToString()}. {(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator.";
    }
}
