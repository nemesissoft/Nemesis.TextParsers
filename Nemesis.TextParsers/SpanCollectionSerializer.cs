using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using PureMethod = System.Diagnostics.Contracts.PureAttribute;

namespace Nemesis.TextParsers
{
    public sealed class SpanCollectionSerializer
    {
        #region Fields and properties
        //for performance reasons, all delimiters and escaped characters are single chars
        //this makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance 
        public char ListDelimiter { get; }
        public char DictionaryPairsDelimiter { get; }
        public char DictionaryKeyValueDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        #endregion

        #region Factory / ctors
        public SpanCollectionSerializer(char listDelimiter, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart)
        {
            var specialCharacters = new[] { listDelimiter, dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker, escapingSequenceStart };
            if (specialCharacters.Length != specialCharacters.Distinct().Count())
                throw new ArgumentException($"All special characters have to be distinct. Actual special characters:{string.Join(", ", specialCharacters.Select(c => $"'{c}'"))}");

            ListDelimiter = listDelimiter;
            DictionaryPairsDelimiter = dictionaryPairsDelimiter;
            DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
        }

        public static readonly SpanCollectionSerializer DefaultInstance = new SpanCollectionSerializer('|', ';', '=', '∅', '\\');

        #endregion

        #region Collection
        [PureMethod]
        public TTo[] ParseArray<TTo>(string text, ushort potentialLength = 8) =>
            text == null ? null : ParseArray<TTo>(text.AsSpan(), potentialLength);

        [PureMethod]
        public TTo[] ParseArray<TTo>(ReadOnlySpan<char> text, ushort potentialLength = 8) =>
            text.IsEmpty ? new TTo[0] : ParseStream<TTo>(text).ToArray(potentialLength);


        [PureMethod]
        public ICollection<TTo> ParseCollection<TTo>(string text,
            CollectionKind kind = CollectionKind.List, ushort potentialLength = 8) =>
            text == null ? null : ParseCollection<TTo>(text.AsSpan(), kind, potentialLength);

        [PureMethod]
        public ICollection<TTo> ParseCollection<TTo>(ReadOnlySpan<char> text,
            CollectionKind kind = CollectionKind.List, ushort potentialLength = 8) =>
            ParseStream<TTo>(text).ToCollection(kind, potentialLength);

        [PureMethod]
        public ParsedSequence<TTo> ParseStream<TTo>(in ReadOnlySpan<char> text)
        {
            var tokens = text.Tokenize(ListDelimiter, EscapingSequenceStart, true);
            var parsed = tokens.Parse<TTo>(EscapingSequenceStart, NullElementMarker, ListDelimiter);

            return parsed;
        }

        public string FormatCollection<TElement>(in LeanCollection<TElement> coll)
        {
            if (coll.Size == 0) return "";

            IFormatter<TElement> formatter = TextTransformer.Default.GetTransformer<TElement>();

            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            var enumerator = coll.GetEnumerator();
            while (enumerator.MoveNext())
                FormatElement(formatter, enumerator.Current, ref accumulator);

            var text = accumulator.AsSpanTo(accumulator.Length > 0 ? accumulator.Length - 1 : 0).ToString();
            accumulator.Dispose();
            return text;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public string FormatCollection<TElement>(IEnumerable<TElement> coll)
        {
            if (coll == null) return null;
            if (!coll.Any()) return "";

            IFormatter<TElement> formatter = TextTransformer.Default.GetTransformer<TElement>();

            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            using (var enumerator = coll.GetEnumerator())
                while (enumerator.MoveNext())
                    FormatElement(formatter, enumerator.Current, ref accumulator);

            var text = accumulator.AsSpanTo(accumulator.Length > 0 ? accumulator.Length - 1 : 0).ToString();
            accumulator.Dispose();
            return text;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
        {
            string elementText = formatter.Format(element);
            if (elementText == null)
                accumulator.Append(NullElementMarker);
            else
            {
                foreach (char c in elementText)
                {
                    if (c == EscapingSequenceStart || c == NullElementMarker || c == ListDelimiter)
                        accumulator.Append(EscapingSequenceStart);

                    accumulator.Append(c);
                }
            }

            accumulator.Append(ListDelimiter);
        }

        #endregion

        #region Dictionary

        public IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string text,
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys,
            ushort potentialLength = 8)
            => text == null ? null :
                ParseDictionary<TKey, TValue>(text.AsSpan(), kind, behaviour, potentialLength);


        public IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(ReadOnlySpan<char> text,
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys,
            ushort potentialLength = 8)
        {
            var parsedPairs = ParsePairsStream<TKey, TValue>(text);
            return parsedPairs.ToDictionary(kind, behaviour, potentialLength);
        }

        public ParsedPairSequence<TKey, TValue> ParsePairsStream<TKey, TValue>(ReadOnlySpan<char> text)
        {
            var potentialKvp = text.Tokenize(DictionaryPairsDelimiter, EscapingSequenceStart, true);

            var parsedPairs = new ParsedPairSequence<TKey, TValue>(potentialKvp, EscapingSequenceStart,
                NullElementMarker, DictionaryPairsDelimiter, DictionaryKeyValueDelimiter);

            return parsedPairs;
        }

        public string FormatDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            if (dict == null) return null;
            if (dict.Count == 0) return "";

            IFormatter<TKey> keyFormatter = TextTransformer.Default.GetTransformer<TKey>();
            IFormatter<TValue> valueFormatter = TextTransformer.Default.GetTransformer<TValue>();

            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            foreach (var pair in dict)
            {
                var key = pair.Key;
                var value = pair.Value;

                string keyText = keyFormatter.Format(key);
                foreach (char c in keyText)
                {
                    if (c == EscapingSequenceStart || c == NullElementMarker ||
                        c == DictionaryPairsDelimiter || c == DictionaryKeyValueDelimiter
                       )
                        accumulator.Append(EscapingSequenceStart);
                    accumulator.Append(c);
                }
                accumulator.Append(DictionaryKeyValueDelimiter);

                if (value == null)
                    accumulator.Append(NullElementMarker);
                else
                {
                    string valueText = valueFormatter.Format(value);
                    foreach (char c in valueText)
                    {
                        if (c == EscapingSequenceStart || c == NullElementMarker ||
                            c == DictionaryPairsDelimiter || c == DictionaryKeyValueDelimiter
                        )
                            accumulator.Append(EscapingSequenceStart);
                        accumulator.Append(c);
                    }
                }

                accumulator.Append(DictionaryPairsDelimiter);
            }

            var text = accumulator.AsSpanTo(accumulator.Length - 1).ToString();
            accumulator.Dispose();
            return text;
        }

        #endregion

        public override string ToString() => $@"Collection: Item1{ListDelimiter}Item2
Dictionary: key1{DictionaryKeyValueDelimiter}value1{DictionaryPairsDelimiter}key2{DictionaryKeyValueDelimiter}value2";
    }
}
