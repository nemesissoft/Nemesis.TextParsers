using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Utils;
using PureMethod = System.Diagnostics.Contracts.PureAttribute;

namespace Nemesis.TextParsers
{
    [PublicAPI]
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
        public TTo[] ParseArray<TTo>(string text) =>
            text == null ? null : ParseArray<TTo>(text.AsSpan());

        [PureMethod]
        public TTo[] ParseArray<TTo>(ReadOnlySpan<char> text) =>
            text.IsEmpty ? Array.Empty<TTo>() : ParseStream<TTo>(text, out var capacity).ToArray(capacity);


        [PureMethod]
        public IReadOnlyCollection<TTo> ParseCollection<TTo>(string text, CollectionKind kind = CollectionKind.List) =>
            text == null ? null : ParseCollection<TTo>(text.AsSpan(), kind);

        [PureMethod]
        public IReadOnlyCollection<TTo> ParseCollection<TTo>(ReadOnlySpan<char> text, CollectionKind kind = CollectionKind.List) =>
            ParseStream<TTo>(text, out var capacity).ToCollection(kind, capacity);


        public LeanCollection<T> ParseLeanCollection<T>(string text) =>
            text == null ? new LeanCollection<T>() : ParseLeanCollection<T>(text.AsSpan());

        public LeanCollection<T> ParseLeanCollection<T>(ReadOnlySpan<char> text) =>
            ParseStream<T>(text, out _).ToLeanCollection();

        [PureMethod]
        public ParsedSequence<TTo> ParseStream<TTo>(in ReadOnlySpan<char> text, out ushort capacity)
        {
            capacity = (ushort)(CountCharacters(text, ListDelimiter) + 1);

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
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys)
            => text == null ? null :
                ParseDictionary<TKey, TValue>(text.AsSpan(), kind, behaviour);


        public IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(ReadOnlySpan<char> text,
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys)
        {
            var parsedPairs = ParsePairsStream<TKey, TValue>(text, out ushort capacity);
            return parsedPairs.ToDictionary(kind, behaviour, capacity);
        }

        public ParsedPairSequence<TKey, TValue> ParsePairsStream<TKey, TValue>(ReadOnlySpan<char> text, out ushort capacity)
        {
            capacity = (ushort)(CountCharacters(text, DictionaryPairsDelimiter) + 1);

            var potentialKvp = text.Tokenize(DictionaryPairsDelimiter, EscapingSequenceStart, true);

            var parsedPairs = new ParsedPairSequence<TKey, TValue>(potentialKvp, EscapingSequenceStart,
                NullElementMarker, DictionaryPairsDelimiter, DictionaryKeyValueDelimiter);

            return parsedPairs;
        }

        public string FormatDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> dict)
        {
            switch (dict)
            {
                case null: return null;
                case IReadOnlyCollection<KeyValuePair<TKey, TValue>> roColl when roColl.Count == 0:
                case ICollection<KeyValuePair<TKey, TValue>> coll when coll.Count == 0:
                    return "";
            }
            
            IFormatter<TKey> keyFormatter = TextTransformer.Default.GetTransformer<TKey>();
            IFormatter<TValue> valueFormatter = TextTransformer.Default.GetTransformer<TValue>();

            Span<char> initialBuffer = stackalloc char[32];
            using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

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
            return accumulator.AsSpanTo(accumulator.Length > 0 ? accumulator.Length - 1 : 0).ToString();
        }

        #endregion

        #region Helpers

        private static ushort CountCharacters(in ReadOnlySpan<char> input, char character)
        {
            ushort count = 0;
            for (int i = input.Length - 1; i >= 0; i--)
                if (input[i] == character)
                    count++;

            return count;
        }

        #endregion

        public override string ToString() => $@"Collection: Item1{ListDelimiter}Item2
Dictionary: key1{DictionaryKeyValueDelimiter}value1{DictionaryPairsDelimiter}key2{DictionaryKeyValueDelimiter}value2
Null marker: {NullElementMarker}";
    }
}
