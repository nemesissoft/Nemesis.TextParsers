using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers.Utils
{
    public readonly struct TupleHelper : IEquatable<TupleHelper>
    {
        #region Init
        private readonly char _tupleDelimiter;
        private readonly char _nullElementMarker;
        private readonly char _escapingSequenceStart;
        private readonly char? _tupleStart;
        private readonly char? _tupleEnd;

        public TupleHelper(char tupleDelimiter = ',', char nullElementMarker = '∅',
            char escapingSequenceStart = '\\', char? tupleStart = '(', char? tupleEnd = ')')
        {
            if (tupleDelimiter == nullElementMarker ||
                tupleDelimiter == escapingSequenceStart ||
                tupleDelimiter == tupleStart ||
                tupleDelimiter == tupleEnd ||

                nullElementMarker == escapingSequenceStart ||
                nullElementMarker == tupleStart ||
                nullElementMarker == tupleEnd ||

                escapingSequenceStart == tupleStart ||
                escapingSequenceStart == tupleEnd
            )
                throw new ArgumentException($@"{nameof(TupleHelper)} requires unique characters to be used for parsing/formatting purposes. Start ('{tupleStart}') and end ('{tupleEnd}') can be equal to each other
Passed parameters: 
{nameof(tupleDelimiter)} = '{tupleDelimiter}'
{nameof(nullElementMarker)} = '{nullElementMarker}'
{nameof(escapingSequenceStart)} = '{escapingSequenceStart }'
{nameof(tupleStart)} = '{tupleStart}'
{nameof(tupleEnd)} = '{tupleEnd}'");

            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
        }
        #endregion


        #region Formatting
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartFormat(ref ValueSequenceBuilder<char> accumulator)
        {
            if (_tupleStart is { } c)
                accumulator.Append(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFormat(ref ValueSequenceBuilder<char> accumulator)
        {
            if (_tupleEnd is { } c)
                accumulator.Append(c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) => accumulator.Append(_tupleDelimiter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
        {
            string elementText = formatter.Format(element);
            if (elementText == null)
                accumulator.Append(_nullElementMarker);
            else
            {
                foreach (char c in elementText)
                {
                    if (c == _escapingSequenceStart || c == _nullElementMarker || c == _tupleDelimiter)
                        accumulator.Append(_escapingSequenceStart);
                    accumulator.Append(c);
                }
            }
        }
        #endregion


        #region Parsing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity, string typeName = null)
        {
            input = UnParenthesize(input);

            var tokens = input.Tokenize(_tupleDelimiter, _escapingSequenceStart, false);
            var enumerator = tokens.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new ArgumentException($@"{typeName ?? "Tuple"} of arity={arity} separated by '{_tupleDelimiter}' was not found");

            return enumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span, string typeName = null)
        {
            if (_tupleStart is null && _tupleEnd is null)
                return span;

            int minLength = (_tupleStart.HasValue ? 1 : 0) + (_tupleEnd.HasValue ? 1 : 0);
            if (span.Length < minLength) throw GetStateException(span.ToString(), _tupleStart, _tupleEnd, typeName);

            int start = 0;

            if (_tupleStart.HasValue)
            {
                for (; start < span.Length; start++)
                    if (!char.IsWhiteSpace(span[start]))
                        break;

                bool startsWithChar = start < span.Length && span[start] == _tupleStart.Value;
                if (!startsWithChar) throw GetStateException(span.ToString(), _tupleStart, _tupleEnd, typeName);

                ++start;
            }


            int end = span.Length - 1;

            if (_tupleEnd.HasValue)
            {
                for (; end > start; end--)
                    if (!char.IsWhiteSpace(span[end]))
                        break;

                bool endsWithChar = end > 0 && span[end] == _tupleEnd.Value;
                if (!endsWithChar) throw GetStateException(span.ToString(), _tupleStart, _tupleEnd, typeName);

                --end;
            }

            return span.Slice(start, end - start + 1);

            static Exception GetStateException(string text, char? start, char? end, string typeName) => new ArgumentException(
                     $@"{typeName ?? "Tuple" } representation has to start with '{(start is { } c1 ? c1.ToString() : "<nothing>")}' and end with '{(end is { } c2 ? c2.ToString() : "<nothing>")}' optionally lead in the beginning or trailed in the end by whitespace.
These requirements were not met in:
'{text ?? "<NULL>"}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParseNext(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte index)
        {
            static string ToOrdinal(byte number)
            {
                int rem = number % 100;
                if (rem >= 11 && rem <= 13) return $"{number}th";

                return (number % 10) switch
                {
                    1 => $"{number}st",
                    2 => $"{number}nd",
                    3 => $"{number}rd",
                    _ => $"{number}th",
                };
            }

            var current = enumerator.Current;
            if (!enumerator.MoveNext())
                throw new ArgumentException($"{ToOrdinal(index)} element was not found after '{current.ToString()}'");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParseEnd(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte arity, string typeName = null)
        {
            if (enumerator.MoveNext())
            {
                var remaining = enumerator.Current.ToString();
                throw new ArgumentException($@"{typeName ?? "Tuple"} of arity={arity} separated by '{_tupleDelimiter}' cannot have more than {arity} elements: '{remaining}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TElement ParseElement<TElement>(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, ISpanParser<TElement> parser)
        {
            ReadOnlySpan<char> input = enumerator.Current;
            var unescapedInput = input.UnescapeCharacter(_escapingSequenceStart, _tupleDelimiter);

            if (unescapedInput.Length == 1 && unescapedInput[0].Equals(_nullElementMarker))
                return default;
            else
            {
                unescapedInput = unescapedInput.UnescapeCharacter
                        (_escapingSequenceStart, _nullElementMarker, _escapingSequenceStart);

                return parser.Parse(unescapedInput);
            }
        }

        #endregion

        
        #region Object helpers
        public override string ToString() =>
            $"{_tupleStart}Item1{_tupleDelimiter}Item2{_tupleDelimiter}…{_tupleDelimiter}ItemN{_tupleEnd} escaped by '{_escapingSequenceStart}', null marked by '{_nullElementMarker}'";

        public bool Equals(TupleHelper other) =>
            _tupleDelimiter == other._tupleDelimiter &&
            _nullElementMarker == other._nullElementMarker &&
            _escapingSequenceStart == other._escapingSequenceStart &&
            _tupleStart == other._tupleStart &&
            _tupleEnd == other._tupleEnd;

        public override bool Equals(object obj) => obj is TupleHelper other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _tupleDelimiter.GetHashCode();
                hashCode = (hashCode * 397) ^ _nullElementMarker.GetHashCode();
                hashCode = (hashCode * 397) ^ _escapingSequenceStart.GetHashCode();
                hashCode = (hashCode * 397) ^ _tupleStart.GetHashCode();
                hashCode = (hashCode * 397) ^ _tupleEnd.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(TupleHelper left, TupleHelper right) => left.Equals(right);

        public static bool operator !=(TupleHelper left, TupleHelper right) => !left.Equals(right);
        #endregion
    }
}
