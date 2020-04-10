using System;
using System.Runtime.CompilerServices;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers
{
    public readonly ref struct ParsingSequence
    {
        #region Fields and properties
        private readonly TokenSequence<char> _tokenSource;
        private readonly char _escapingSequenceStart;
        private readonly char _nullElementMarker;
        private readonly char _sequenceDelimiter;
        #endregion

        public ParsingSequence(in TokenSequence<char> tokenSource, char escapingSequenceStart, 
            char nullElementMarker, char sequenceDelimiter)
        {
            _tokenSource = tokenSource;
            _escapingSequenceStart = escapingSequenceStart;
            _nullElementMarker = nullElementMarker;
            _sequenceDelimiter = sequenceDelimiter;
        }
        
        public ParsingSequenceEnumerator GetEnumerator() => new ParsingSequenceEnumerator(_tokenSource, _escapingSequenceStart, _nullElementMarker, _sequenceDelimiter);

        public ref struct ParsingSequenceEnumerator
        {
            #region Fields
            private TokenSequence<char>.TokenSequenceEnumerator _tokenSequenceEnumerator;
            private readonly char _escapingSequenceStart;
            private readonly char _nullElementMarker;
            private readonly char _sequenceDelimiter;
            #endregion

            public ParsingSequenceEnumerator(in TokenSequence<char> tokenSource, char escapingSequenceStart, char nullElementMarker, char sequenceDelimiter)
            {
                _tokenSequenceEnumerator = tokenSource.GetEnumerator();
                _escapingSequenceStart = escapingSequenceStart;
                _nullElementMarker = nullElementMarker;
                _sequenceDelimiter = sequenceDelimiter;

                Current = default;
            }
            

            public bool MoveNext()
            {
                bool canMove = _tokenSequenceEnumerator.MoveNext();
                Current = canMove ? ParseElement(_tokenSequenceEnumerator.Current) : default;
                return canMove;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ParserInput ParseElement(in ReadOnlySpan<char> input)
            {
                if (input.Length == 1 && input[0].Equals(_nullElementMarker))
                    return ParserInput.FromDefault();

                int idx = input.IndexOf(_escapingSequenceStart);
                if (idx >= 0)
                {
                    var bufferLength = Math.Min(Math.Max(input.Length * 13 / 10, 16), 256);
                    Span<char> initialBuffer = stackalloc char[bufferLength];
                    using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

                    bool escaped = false;
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < input.Length; i++)
                    {
                        char current = input[i];
                        if (escaped)
                        {
                            if (current == _escapingSequenceStart || 
                                current == _nullElementMarker ||
                                current == _sequenceDelimiter)
                                accumulator.Append(current);
                            else
                                throw new ArgumentException($@"Illegal escape sequence found in input: '{current}'.
Only ['{_escapingSequenceStart}','{_nullElementMarker}','{_sequenceDelimiter}'] are supported as escaping sequence characters.", nameof(input));

                            escaped = false;
                        }
                        else
                        {
                            bool isEscape = current.Equals(_escapingSequenceStart);
                            if (isEscape)
                                escaped = true;
                            else
                                accumulator.Append(current);
                        }
                    }

                    if (escaped)
                        throw new ArgumentException("Unfinished escaping sequence detected at the end of input", nameof(input));

                    var toParse = accumulator.AsSpan();
                    return ParserInput.FromText(toParse.ToArray());
                }
                return ParserInput.FromText(input);
            }

            public ParserInput Current { get; private set; }
        }
    }

    public readonly ref struct ParserInput
    {
        public bool IsDefault { get; }
        public ReadOnlySpan<char> Text { get; }

        private ParserInput(bool isDefault, ReadOnlySpan<char> text)
        {
            IsDefault = isDefault;
            Text = text;
        }

        public void Deconstruct(out bool isDefault, out ReadOnlySpan<char> text)
        {
            isDefault = IsDefault;
            text = Text;
        }

        public static ParserInput FromDefault() => new ParserInput(true, default);

        public static ParserInput FromText(ReadOnlySpan<char> text) => new ParserInput(false, text);

        public T ParseWith<T>(ITransformer<T> transformer)
            => IsDefault ? default : transformer.Parse(Text);

        public override string ToString() => IsDefault ? "<DEFAULT>" : Text.ToString();
    }
}
