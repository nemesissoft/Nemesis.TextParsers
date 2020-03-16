using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers.Utils
{
    public class TupleHelper
    {
        private readonly char TUPLE_DELIMITER = ',';
        private readonly char NULL_ELEMENT_MARKER = '∅';
        private readonly char ESCAPING_SEQUENCE_START = '\\';
        private readonly char TUPLE_START = '(';
        private readonly char TUPLE_END = ')';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartFormat(ref ValueSequenceBuilder<char> accumulator) =>
            accumulator.Append(TUPLE_START);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFormat(ref ValueSequenceBuilder<char> accumulator) =>
            accumulator.Append(TUPLE_END);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) =>
            accumulator.Append(TUPLE_DELIMITER);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity)
        {
            input = UnParenthesize(input);

            var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);
            var enumerator = kvpTokens.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' was not found");

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

            bool tupleStartsWithParenthesis = start < span.Length && span[start] == TUPLE_START;

            if (!tupleStartsWithParenthesis) throw GetStateException();

            int end = span.Length - 1;
            for (; end > start; end--)
                if (!char.IsWhiteSpace(span[end]))
                    break;

            bool tupleEndsWithParenthesis = end > 0 && span[end] == TUPLE_END;

            if (!tupleEndsWithParenthesis) throw GetStateException();

            return span.Slice(start + 1, end - start - 1);

            static Exception GetStateException() => new ArgumentException(
                     "Tuple representation has to start and end with parentheses optionally lead in the beginning or trailed in the end by whitespace");
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
                throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' cannot have more than {arity} elements: '{remaining}'");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TElement ParseElement<TElement>(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, ISpanParser<TElement> parser)
        {
            ReadOnlySpan<char> input = enumerator.Current;
            var unescapedInput = input.UnescapeCharacter(ESCAPING_SEQUENCE_START, TUPLE_DELIMITER);

            if (unescapedInput.Length == 1 && unescapedInput[0].Equals(NULL_ELEMENT_MARKER))
                return default;
            else
            {
                unescapedInput = unescapedInput.UnescapeCharacter
                        (ESCAPING_SEQUENCE_START, NULL_ELEMENT_MARKER, ESCAPING_SEQUENCE_START);

                return parser.Parse(unescapedInput);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
        {
            string elementText = formatter.Format(element);
            if (elementText == null)
                accumulator.Append(NULL_ELEMENT_MARKER);
            else
            {
                foreach (char c in elementText)
                {
                    if (c == ESCAPING_SEQUENCE_START || c == NULL_ELEMENT_MARKER || c == TUPLE_DELIMITER)
                        accumulator.Append(ESCAPING_SEQUENCE_START);
                    accumulator.Append(c);
                }
            }
        }
    }
}
