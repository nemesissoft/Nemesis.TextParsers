using Nemesis.TextParsers.Utils;

using Dsa = Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;

namespace Nemesis.TextParsers.Settings;

public abstract class TupleSettings : ISettings
{
    public char Delimiter { get; private set; }
    public char NullElementMarker { get; private set; }
    public char EscapingSequenceStart { get; private set; }
    public char? Start { get; private set; }
    public char? End { get; private set; }

#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
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

    public TupleHelper ToTupleHelper() => new(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End);
}

public sealed class ValueTupleSettings : TupleSettings
{
#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public ValueTupleSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        : base(delimiter, nullElementMarker, escapingSequenceStart, start, end) { }

    public static ValueTupleSettings Default { get; } = new(',', '∅', '\\', '(', ')');
}

public sealed class KeyValuePairSettings : TupleSettings
{
#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public KeyValuePairSettings(char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        : base(delimiter, nullElementMarker, escapingSequenceStart, start, end) { }

    public static KeyValuePairSettings Default { get; } = new('=', '∅', '\\', null, null);

    public override string ToString() =>
        $"{Start}Key{Delimiter}Value{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
}

public sealed class DeconstructableSettings : TupleSettings
{
    public bool UseDeconstructableEmpty { get; private set; }

#if NET
    [System.Text.Json.Serialization.JsonConstructor]
#endif 
    public DeconstructableSettings(char delimiter = Dsa.DEFAULT_DELIMITER,
            char nullElementMarker = Dsa.DEFAULT_NULL_ELEMENT_MARKER,
            char escapingSequenceStart = Dsa.DEFAULT_ESCAPING_SEQUENCE_START,
            char? start = Dsa.DEFAULT_START, char? end = Dsa.DEFAULT_END,
            bool useDeconstructableEmpty = Dsa.DEFAULT_USE_DECONSTRUCTABLE_EMPTY)
        : base(delimiter, nullElementMarker, escapingSequenceStart, start, end)
        => UseDeconstructableEmpty = useDeconstructableEmpty;

    public static DeconstructableSettings Default { get; } = new();

    public override string ToString() =>
        $@"{base.ToString()}. {(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator.";
}

public static class DeconstructableSettingsAttributeExtensions
{
    public static DeconstructableSettings ToSettings(this Dsa attr) => new(attr.Delimiter, attr.NullElementMarker, attr.EscapingSequenceStart,
        attr.Start == '\0' ? null : attr.Start,
        attr.End == '\0' ? null : attr.End,
        attr.UseDeconstructableEmpty);
}
