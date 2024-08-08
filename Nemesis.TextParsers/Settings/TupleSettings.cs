#nullable enable
using System.Diagnostics.CodeAnalysis;
using Nemesis.TextParsers.Utils;

using Dsa = Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;

namespace Nemesis.TextParsers.Settings;

public abstract record TupleSettings(
    char Delimiter,
    char NullElementMarker,
    char EscapingSequenceStart,
    char? Start,
    char? End) : ISettings
{
    public abstract ISettings DeepClone();

    public bool IsValid([NotNullWhen(false)] out string? error)
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
        {
            error = $"""
                {GetType().Name} requires unique characters to be used for parsing/formatting purposes. 
                Start ('{Start}') and end ('{End}') properties can be equal to each other.
                Passed parameters: 
                  {nameof(Delimiter)} = '{Delimiter}'
                  {nameof(NullElementMarker)} = '{NullElementMarker}'
                  {nameof(EscapingSequenceStart)} = '{EscapingSequenceStart}'
                  {nameof(Start)} = '{Start}'
                  {nameof(End)} = '{End}'
                """;
            return false;
        }
        else
        {
            error = null;
            return true;
        }

    }

    public override string ToString() =>
        $"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

    public TupleHelper ToTupleHelper() => new(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End);
}

public sealed record ValueTupleSettings(
    char Delimiter = ',',
    char NullElementMarker = '∅',
    char EscapingSequenceStart = '\\',
    char? Start = '(',
    char? End = ')'
) : TupleSettings(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End),
    ISettings<ValueTupleSettings>
{
    public override ISettings DeepClone() => this with { };

    public static ValueTupleSettings Default { get; } = new();
}

public sealed record KeyValuePairSettings(
    char Delimiter = '=',
    char NullElementMarker = '∅',
    char EscapingSequenceStart = '\\',
    char? Start = null,
    char? End = null
) : TupleSettings(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End),
    ISettings<KeyValuePairSettings>
{
    public override ISettings DeepClone() => this with { };

    public static KeyValuePairSettings Default { get; } = new();

    public override string ToString() =>
        $"{Start}Key{Delimiter}Value{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";
}

public sealed record DeconstructableSettings(
    char Delimiter = Dsa.DEFAULT_DELIMITER,
    char NullElementMarker = Dsa.DEFAULT_NULL_ELEMENT_MARKER,
    char EscapingSequenceStart = Dsa.DEFAULT_ESCAPING_SEQUENCE_START,
    char? Start = Dsa.DEFAULT_START,
    char? End = Dsa.DEFAULT_END,
    bool UseDeconstructableEmpty = Dsa.DEFAULT_USE_DECONSTRUCTABLE_EMPTY
) : TupleSettings(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End),
    ISettings<DeconstructableSettings>
{
    public override ISettings DeepClone() => this with { };

    public static DeconstructableSettings Default { get; } = new();

    public override string ToString() =>
        $@"{base.ToString()}. {(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator.";
}

public static class DeconstructableSettingsAttributeExtensions
{
    public static DeconstructableSettings ToSettings(this Dsa attr) => new(
        attr.Delimiter,
        attr.NullElementMarker,
        attr.EscapingSequenceStart,
        attr.Start == '\0' ? null : attr.Start,
        attr.End == '\0' ? null : attr.End,
        attr.UseDeconstructableEmpty);
}
