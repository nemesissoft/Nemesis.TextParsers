using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers
{
    public readonly ref struct ParsingPairSequence
    {
        #region Fields and properties
        private readonly TokenSequence<char> _tokenSource;
        private readonly char _escapingSequenceStart;
        private readonly char _nullElementMarker;
        private readonly char _dictionaryPairsDelimiter;
        private readonly char _dictionaryKeyValueDelimiter;

        #endregion

        #region Ctors/factories

        public ParsingPairSequence(scoped in TokenSequence<char> tokenSource, char escapingSequenceStart, char nullElementMarker, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter)
        {
            _tokenSource = tokenSource;
            _escapingSequenceStart = escapingSequenceStart;
            _nullElementMarker = nullElementMarker;
            _dictionaryPairsDelimiter = dictionaryPairsDelimiter;
            _dictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
        }
        #endregion

        public ParsingPairSequenceEnumerator GetEnumerator() => new(_tokenSource,
            _escapingSequenceStart, _nullElementMarker, _dictionaryPairsDelimiter, _dictionaryKeyValueDelimiter);

        public ref struct ParsingPairSequenceEnumerator
        {
            #region Fields and properties
            private TokenSequence<char>.TokenSequenceEnumerator _tokenSequenceEnumerator;
            private readonly char _escapingSequenceStart;
            private readonly char _nullElementMarker;
            private readonly char _dictionaryPairsDelimiter;
            private readonly char _dictionaryKeyValueDelimiter;
            #endregion

            public ParsingPairSequenceEnumerator(in TokenSequence<char> tokenSource, char escapingSequenceStart, char nullElementMarker, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter) : this()
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
            private PairParserInput ParsePair(scoped in ReadOnlySpan<char> input)
            {
                var delimiter = _dictionaryKeyValueDelimiter;
                var unescapedKvp = input.UnescapeCharacter(_escapingSequenceStart, _dictionaryPairsDelimiter);

                var kvpTokens = unescapedKvp.Tokenize(_dictionaryKeyValueDelimiter, _escapingSequenceStart, true);

                var kvpEnumerator = kvpTokens.GetEnumerator();

                if (!kvpEnumerator.MoveNext())
                    throw new ArgumentException($@"Key{delimiter}Value part was not found");
                var key = ProcessElement(kvpEnumerator.Current, _escapingSequenceStart, _dictionaryKeyValueDelimiter, _nullElementMarker);

                if (!kvpEnumerator.MoveNext())
                    throw new ArgumentException($"'{key.ToString()}' has no matching value");
                var value = ProcessElement(kvpEnumerator.Current, _escapingSequenceStart, _dictionaryKeyValueDelimiter, _nullElementMarker);

                if (kvpEnumerator.MoveNext())
                {
                    var remaining = kvpEnumerator.Current.ToString();
                    throw new ArgumentException($@"{key.ToString()}{delimiter}{value.ToString()} pair cannot have more than 2 elements: '{remaining}'");
                }
                //if (key == null) throw new ArgumentException("Key equal to NULL is not supported");

                return PairParserInput.FromKvp(key, value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ParserInput ProcessElement(scoped in ReadOnlySpan<char> input,
                char escapingSequenceStart, char dictionaryKeyValueDelimiter, char nullElementMarker)
            {
                var unescapedInput = input.UnescapeCharacter(escapingSequenceStart, dictionaryKeyValueDelimiter);

                if (unescapedInput.Length == 1 && unescapedInput[0].Equals(nullElementMarker))
                    return ParserInput.FromDefault();
                else
                {
                    unescapedInput = unescapedInput
                        .UnescapeCharacter(escapingSequenceStart, nullElementMarker, escapingSequenceStart);

                    return ParserInput.FromText(unescapedInput);
                }
            }

            public PairParserInput Current { get; private set; }
        }
    }


    public readonly ref struct PairParserInput
    {
        public ParserInput Key { get; }
        public ParserInput Value { get; }

        public PairParserInput(ParserInput key, ParserInput value)
        {
            Key = key;
            Value = value;
        }

        public void Deconstruct(out ParserInput key, out ParserInput value)
        {
            key = Key;
            value = Value;
        }

        public static PairParserInput FromKvp(scoped ParserInput key, scoped ParserInput value) => new(key, value);

        public override string ToString() => $"{Key.ToString()}={Value.ToString()}";
    }
}
