using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers
{
    public ref struct ParsedSequence<TTo>
    {
        private static readonly ISpanParser<TTo> _parser;

        static ParsedSequence() => _parser = TextTransformer.Default.GetTransformer<TTo>();
        /*var parserType = typeof(ISpanParser<>).MakeGenericType(typeof(TTo)); var type = parserType.Assembly.GetTypes().First(t => parserType.IsAssignableFrom(t));_parser = (ISpanParser<TTo>)Activator.CreateInstance(type);*/

        public ParsedSequence(TokenSequence<char> tokenSource, char escapingElement, char nullElement, char allowedEscapeCharacter1 = default)
        {
            _tokenSource = tokenSource;
            _escapingElement = escapingElement;
            _nullElement = nullElement;
            _allowedEscapeCharacter1 = allowedEscapeCharacter1;
        }

        private readonly TokenSequence<char> _tokenSource;
        private readonly char _escapingElement;
        private readonly char _nullElement;
        private readonly char _allowedEscapeCharacter1;

        public ParsedSequenceEnumerator GetEnumerator() => new ParsedSequenceEnumerator(_tokenSource, _escapingElement, _nullElement, _allowedEscapeCharacter1);

        public ref struct ParsedSequenceEnumerator
        {
            public ParsedSequenceEnumerator(TokenSequence<char> tokenSource, char escapingElement, char nullElement, char allowedEscapeCharacter1)
            {
                _tokenSequenceEnumerator = tokenSource.GetEnumerator();
                _escapingElement = escapingElement;
                _nullElement = nullElement;
                _allowedEscapeCharacter1 = allowedEscapeCharacter1;

                Current = default;
            }

            private TokenSequence<char>.TokenSequenceEnumerator _tokenSequenceEnumerator;
            private readonly char _escapingElement;
            private readonly char _nullElement;
            private readonly char _allowedEscapeCharacter1;

            public bool MoveNext()
            {
                bool canMove = _tokenSequenceEnumerator.MoveNext();
                Current = canMove ? ParseElement(_tokenSequenceEnumerator.Current) : default;
                return canMove;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private TTo ParseElement(in ReadOnlySpan<char> input)
            {
                if (input.Length == 1 && input[0].Equals(_nullElement))
                    return default;

                int idx = input.IndexOf(_escapingElement);
                if (idx >= 0)
                {
                    var bufferLength = Math.Min(Math.Max(input.Length * 13 / 10, 16), 256);
                    Span<char> initialBuffer = stackalloc char[bufferLength];
                    var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

                    bool escaped = false;
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < input.Length; i++)
                    {
                        char current = input[i];
                        if (escaped)
                        {
                            if (current == _escapingElement || current == _nullElement ||
                                current == _allowedEscapeCharacter1)
                                accumulator.Append(current);
                            else
                                throw new ArgumentException($@"Illegal escape sequence found in input: '{current}'.
Only ['{_escapingElement}','{_nullElement}','{_allowedEscapeCharacter1}'] are supported as escaping sequence characters.", nameof(input));

                            escaped = false;
                        }
                        else
                        {
                            bool isEscape = current.Equals(_escapingElement);
                            if (isEscape)
                                escaped = true;
                            else
                                accumulator.Append(current);
                        }
                    }

                    if (escaped)
                        throw new ArgumentException("Unfinished escaping sequence detected at the end of input", nameof(input));

                    var toParse = accumulator.AsSpan();
                    var result = Parse(in toParse);
                    accumulator.Dispose();
                    return result;
                }
                return Parse(input);
            }

            private static TTo Parse(in ReadOnlySpan<char> input) => _parser.Parse(input);

            public TTo Current { get; private set; }
        }
    }
}
