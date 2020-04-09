using System;
using System.Collections.Generic;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    public abstract class EnumerableTransformerBase<TElement, TCollection> : TransformerBase<TCollection>
        where TCollection : IEnumerable<TElement>
    {
        private readonly ITransformer<TElement> _elementTransformer;
        private readonly CollectionSettingsBase _settings;
        protected EnumerableTransformerBase(ITransformer<TElement> elementTransformer, CollectionSettingsBase settings)
        {
            _elementTransformer = elementTransformer;
            _settings = settings;
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
                if (_settings.Start.HasValue)
                    accumulator.Append(_settings.Start.Value);
                
                do
                {
                    var element = enumerator.Current;

                    string elementText = _elementTransformer.Format(element);
                    if (elementText == null)
                        accumulator.Append(_settings.NullElementMarker);
                    else
                    {
                        foreach (char c in elementText)
                        {
                            if (c == _settings.EscapingSequenceStart || c == _settings.NullElementMarker ||
                                c == _settings.ListDelimiter)
                                accumulator.Append(_settings.EscapingSequenceStart);

                            accumulator.Append(c);
                        }
                    }
                    accumulator.Append(_settings.ListDelimiter);
                } while (enumerator.MoveNext());

                accumulator.Shrink();

                if (_settings.End.HasValue)
                    accumulator.Append(_settings.End.Value);

                return accumulator.ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}
