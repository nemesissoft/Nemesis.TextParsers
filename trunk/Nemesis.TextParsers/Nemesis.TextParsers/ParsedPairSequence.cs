using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers
{
    public readonly ref struct ParsedPairSequence<TKey, TValue>
    {
        #region Fields and properties
        private readonly TokenSequence<char> _tokenSource;
        private readonly char _escapingSequenceStart;
        private readonly char _nullElementMarker;
        private readonly char _dictionaryPairsDelimiter;
        private readonly char _dictionaryKeyValueDelimiter;

        private static readonly ISpanParser<TKey> _keyParser;
        private static readonly ISpanParser<TValue> _valueParser;
        #endregion

        #region Ctors/factories
        static ParsedPairSequence()
        {
            _keyParser = TextTransformer.Default.GetTransformer<TKey>();
            _valueParser = TextTransformer.Default.GetTransformer<TValue>();
        }

        public ParsedPairSequence(in TokenSequence<char> tokenSource, char escapingSequenceStart, char nullElementMarker, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter)
        {
            _tokenSource = tokenSource;
            _escapingSequenceStart = escapingSequenceStart;
            _nullElementMarker = nullElementMarker;
            _dictionaryPairsDelimiter = dictionaryPairsDelimiter;
            _dictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
        }
        #endregion

        public ParsedPairSequenceEnumerator GetEnumerator() => new ParsedPairSequenceEnumerator(_tokenSource,
            _escapingSequenceStart, _nullElementMarker, _dictionaryPairsDelimiter, _dictionaryKeyValueDelimiter);

        public ref struct ParsedPairSequenceEnumerator
        {
            #region Fields and properties
            private TokenSequence<char>.TokenSequenceEnumerator _tokenSequenceEnumerator;
            private readonly char _escapingSequenceStart;
            private readonly char _nullElementMarker;
            private readonly char _dictionaryPairsDelimiter;
            private readonly char _dictionaryKeyValueDelimiter;
            #endregion

            public ParsedPairSequenceEnumerator(in TokenSequence<char> tokenSource, char escapingSequenceStart, char nullElementMarker, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter) : this()
            {
                _tokenSequenceEnumerator = tokenSource.GetEnumerator();

                _escapingSequenceStart = escapingSequenceStart;
                _nullElementMarker = nullElementMarker;
                _dictionaryPairsDelimiter = dictionaryPairsDelimiter;
                _dictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;

                Current = default;
            }

            public bool MoveNext()
            {
                bool canMove = _tokenSequenceEnumerator.MoveNext();
                Current = canMove ? ParsePair(_tokenSequenceEnumerator.Current) : default;
                return canMove;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private KeyValuePair<TKey, TValue> ParsePair(in ReadOnlySpan<char> input)
            {
                var delimiter = _dictionaryKeyValueDelimiter;
                var unescapedKvp = input.UnescapeCharacter(_escapingSequenceStart, _dictionaryPairsDelimiter);

                var kvpTokens = unescapedKvp.Tokenize(_dictionaryKeyValueDelimiter, _escapingSequenceStart, true);

                var enumerator = kvpTokens.GetEnumerator();

                if (!enumerator.MoveNext())
                    throw new ArgumentException($@"Key{delimiter}Value part was not found");
                var key = ParseElement(enumerator.Current, _keyParser, _escapingSequenceStart, _dictionaryKeyValueDelimiter, _nullElementMarker);

                if (!enumerator.MoveNext())
                    throw new ArgumentException($"'{key}' has no matching value");
                var value = ParseElement(enumerator.Current, _valueParser, _escapingSequenceStart, _dictionaryKeyValueDelimiter, _nullElementMarker);

                if (enumerator.MoveNext())
                {
                    var remaining = enumerator.Current.ToString();
                    throw new ArgumentException($@"{key}{delimiter}{value} pair cannot have more than 2 elements: '{remaining}'");
                }

                if (key == null) throw new ArgumentException("Key equal to NULL is not supported");

                return new KeyValuePair<TKey, TValue>(key, value);

                //Exception GetArgumentException(byte? count) => new ArgumentException($@"Key to value pair expects '{delimiter}' delimited collection to be of length 2 (with first part not being null), but was {count?.ToString() ?? "NULL"}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static TElement ParseElement<TElement>(in ReadOnlySpan<char> input, ISpanParser<TElement> parser,
                char escapingSequenceStart, char dictionaryKeyValueDelimiter, char nullElementMarker)
            {
                var unescapedInput = input.UnescapeCharacter(escapingSequenceStart, dictionaryKeyValueDelimiter);

                if (unescapedInput.Length == 1 && unescapedInput[0].Equals(nullElementMarker))
                    return default;
                else
                {
                    unescapedInput = unescapedInput
                        .UnescapeCharacter(escapingSequenceStart, nullElementMarker, escapingSequenceStart);
                    
                    return parser.Parse(unescapedInput);
                }
            }

            public KeyValuePair<TKey, TValue> Current { get; private set; }
        }
    }
}
