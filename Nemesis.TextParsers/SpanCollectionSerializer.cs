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
            ParseStream<TTo>(text, out ushort capacity).ToCollection(kind, capacity);


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
