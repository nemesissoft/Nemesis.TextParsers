using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers.Utils
{
    public class TupleHelper
    {
        private readonly char _tupleDelimiter;
        private readonly char _nullElementMarker;
        private readonly char _escapingSequenceStart;
        private readonly char _tupleStart;
        private readonly char _tupleEnd;

        public TupleHelper(char tupleDelimiter = ',', char nullElementMarker = '∅',
            char escapingSequenceStart = '\\', char tupleStart = '(', char tupleEnd = ')')
        {
#if DEBUG
            var unique = new HashSet<char>
                {tupleDelimiter, nullElementMarker, escapingSequenceStart, tupleStart, tupleEnd};
            Debug.Assert(unique.Count == 5, $"{nameof(TupleHelper)} requires unique characters to be used for parsing/formatting purposes");
#endif

            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartFormat(ref ValueSequenceBuilder<char> accumulator) => accumulator.Append(_tupleStart);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFormat(ref ValueSequenceBuilder<char> accumulator) => accumulator.Append(_tupleEnd);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) => accumulator.Append(_tupleDelimiter);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity)
        {
            input = UnParenthesize(input);

            var kvpTokens = input.Tokenize(_tupleDelimiter, _escapingSequenceStart, true);
            var enumerator = kvpTokens.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new ArgumentException($@"Tuple of arity={arity} separated by '{_tupleDelimiter}' was not found");

            return enumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span)
        {
            int length = span.Length;
            if (length < 2) throw GetStateException();

            int start = 0;
            for (; start < length; start++)
                if (!char.IsWhiteSpace(span[start]))
                    break;

            bool tupleStartsWithParenthesis = start < span.Length && span[start] == _tupleStart;

            if (!tupleStartsWithParenthesis) throw GetStateException();

            int end = span.Length - 1;
            for (; end > start; end--)
                if (!char.IsWhiteSpace(span[end]))
                    break;

            bool tupleEndsWithParenthesis = end > 0 && span[end] == _tupleEnd;

            if (!tupleEndsWithParenthesis) throw GetStateException();

            return span.Slice(start + 1, end - start - 1);

            Exception GetStateException() => new ArgumentException(
                     $"Tuple representation has to start with '{_tupleStart}' and end with '{_tupleEnd}' optionally lead in the beginning or trailed in the end by whitespace");
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

            if (!enumerator.MoveNext())
                throw new ArgumentException($"{ToOrdinal(index)} tuple element was not found");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParseEnd(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte arity)
        {
            if (enumerator.MoveNext())
            {
                var remaining = enumerator.Current.ToString();
                throw new ArgumentException($@"Tuple of arity={arity} separated by '{_tupleDelimiter}' cannot have more than {arity} elements: '{remaining}'");
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
    }
}
