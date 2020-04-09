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
            text.IsEmpty ? Array.Empty<TTo>() : ParseStream<TTo>(text).ToArray();


        [PureMethod]
        public IReadOnlyCollection<TTo> ParseCollection<TTo>(string text, CollectionKind kind = CollectionKind.List) =>
            text == null ? null : ParseCollection<TTo>(text.AsSpan(), kind);

        [PureMethod]
        public IReadOnlyCollection<TTo> ParseCollection<TTo>(ReadOnlySpan<char> text, CollectionKind kind = CollectionKind.List) =>
            ParseStream<TTo>(text).ToCollection(kind);


        public LeanCollection<T> ParseLeanCollection<T>(string text) =>
            text == null ? new LeanCollection<T>() : ParseLeanCollection<T>(text.AsSpan());

        public LeanCollection<T> ParseLeanCollection<T>(ReadOnlySpan<char> text) =>
            ParseStream<T>(text).ToLeanCollection();

        [PureMethod]
        public ParsedSequence<TTo> ParseStream<TTo>(in ReadOnlySpan<char> text)
        {
            var tokens = text.Tokenize(ListDelimiter, EscapingSequenceStart, true);
            var parsed = tokens.Parse<TTo>(EscapingSequenceStart, NullElementMarker, ListDelimiter);

            return parsed;
        }
        
        #endregion
    }
}
