using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers.Utils
{
    //TODO change to readonly struct ?. Add tests
    public class TupleHelper
    {
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
                throw new ArgumentException($"{nameof(TupleHelper)} requires unique characters to be used for parsing/formatting purposes. {tupleStart} and {tupleEnd} can be equal to each other");

            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
        }

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
        public TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity)
        {
            input = UnParenthesize(input);

            var kvpTokens = input.Tokenize(_tupleDelimiter, _escapingSequenceStart, true);
            var enumerator = kvpTokens.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new ArgumentException($@"Tuple of arity={arity} separated by '{_tupleDelimiter}' was not found");

            return enumerator;
        }

        //TODO move that to common helpers + parse Complex class using common logic 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span)
        {
            if (_tupleStart is null && _tupleEnd is null)
                return span;

            int minLength = (_tupleStart.HasValue ? 1 : 0) + (_tupleEnd.HasValue ? 1 : 0);
            if (span.Length < minLength) throw GetStateException(span.ToString());

            int start = 0;

            if (_tupleStart.HasValue)
            {
                for (; start < span.Length; start++)
                    if (!char.IsWhiteSpace(span[start]))
                        break;

                bool startsWithChar = start < span.Length && span[start] == _tupleStart.Value;
                if (!startsWithChar) throw GetStateException(span.ToString());

                ++start;
            }


            int end = span.Length - 1;

            if (_tupleEnd.HasValue)
            {
                for (; end > start; end--)
                    if (!char.IsWhiteSpace(span[end]))
                        break;

                bool endsWithChar = end > 0 && span[end] == _tupleEnd.Value;
                if (!endsWithChar) throw GetStateException(span.ToString());

                --end;
            }

            return span.Slice(start, end - start + 1);

            Exception GetStateException(string text) => new ArgumentException(
                     $@"Tuple representation has to start with '{(_tupleStart is { } c1 ? c1.ToString() : "<nothing>")}' and end with '{(_tupleEnd is { } c2 ? c2.ToString() : "<nothing>")}' optionally lead in the beginning or trailed in the end by whitespace.
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


        public override string ToString() =>
            $"{_tupleStart}Item1{_tupleDelimiter}Item2{_tupleDelimiter}…{_tupleDelimiter}ItemN{_tupleEnd} escaped by '{_escapingSequenceStart}', null marked by '{_nullElementMarker}'";
    }
}
