using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

public abstract class EnumerableTransformerBase<TElement, TCollection> : TransformerBase<TCollection>
    where TCollection : IEnumerable<TElement>
{
    protected readonly ITransformer<TElement> ElementTransformer;
    protected readonly CollectionSettingsBase Settings;
    protected EnumerableTransformerBase(ITransformer<TElement> elementTransformer, CollectionSettingsBase settings)
    {
        ElementTransformer = elementTransformer;
        Settings = settings;
    }

    protected ParsingSequence ParseStream(in ReadOnlySpan<char> text)
    {
        var toParse = text;
        if (Settings.Start.HasValue || Settings.End.HasValue)
            toParse = toParse.UnParenthesize(Settings.Start, Settings.End, "Collection");

        var tokens = toParse.Tokenize(Settings.ListDelimiter, Settings.EscapingSequenceStart, false);
        var parsed = tokens.PreParse(Settings.EscapingSequenceStart, Settings.NullElementMarker, Settings.ListDelimiter);

        return parsed;
    }

    public sealed override string Format(TCollection coll)
    {
        if (coll == null) return null;

        using var enumerator = coll.GetEnumerator();
        if (!enumerator.MoveNext())
            return "";

        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            if (Settings.Start.HasValue)
                accumulator.Append(Settings.Start.Value);

            do
            {
                var element = enumerator.Current;

                string elementText = ElementTransformer.Format(element);
                if (elementText == null)
                    accumulator.Append(Settings.NullElementMarker);
                else
                {
                    foreach (char c in elementText)
                    {
                        if (c == Settings.EscapingSequenceStart || c == Settings.NullElementMarker ||
                            c == Settings.ListDelimiter)
                            accumulator.Append(Settings.EscapingSequenceStart);

                        accumulator.Append(c);
                    }
                }
                accumulator.Append(Settings.ListDelimiter);
            } while (enumerator.MoveNext());

            accumulator.Shrink();

            if (Settings.End.HasValue)
                accumulator.Append(Settings.End.Value);

            return accumulator.ToString();
        }
        finally { accumulator.Dispose(); }
    }
}
