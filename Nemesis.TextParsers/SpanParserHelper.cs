using System;
using Nemesis.TextParsers.Utils;
using PureMethod = System.Diagnostics.Contracts.PureAttribute;

namespace Nemesis.TextParsers
{
    public static class SpanParserHelper
    {
        [PureMethod]
        public static TokenSequence<T> Tokenize<T>(this in ReadOnlySpan<T> sequence, T separator, T escapingElement,
            bool emptySequenceYieldsEmpty)
            where T : IEquatable<T> =>
            new(sequence, separator, escapingElement, emptySequenceYieldsEmpty);

        [PureMethod]
        public static ParsingSequence PreParse(this in TokenSequence<char> tokenSource, char escapingElement,
            char nullElement, char sequenceDelimiter) =>
            new(tokenSource, escapingElement, nullElement, sequenceDelimiter);

        

        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart, char character)
        {
            int length = input.Length;
            if (length < 2) return input;

            if (input.IndexOf(escapingSequenceStart) is { } escapeStart &&
                escapeStart >= 0 && escapeStart < length - 1 &&
                input.Slice(escapeStart).IndexOf(character) >= 0
            ) //is it worth looking for escape sequence ?
            {
                var bufferLength = Math.Min(Math.Max(length, 16), 256);
                Span<char> initialBuffer = stackalloc char[bufferLength];
                using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                for (int i = 0; i < length; i++)
                {
                    var current = input[i];
                    if (current == escapingSequenceStart)
                    {
                        i++;
                        if (i == length)
                            accumulator.Append(current);
                        else
                        {
                            current = input[i];
                            if (current == character)
                                accumulator.Append(current);
                            else
                            {
                                accumulator.Append(escapingSequenceStart);
                                accumulator.Append(current);
                            }
                        }
                    }
                    else
                        accumulator.Append(current);
                }
                return accumulator.AsSpan().ToArray();
            }
            else return input;
        }

        /* TODO add tests that cover these cases i.e. escaping sequence in the end
         private ParserInput ParseElement(in ReadOnlySpan<char> input)*/

        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart, char character1, char character2)
        {
            int length = input.Length;
            if (length < 2) return input;

            if (input.IndexOf(escapingSequenceStart) is { } escapeStart &&
                escapeStart >= 0 && escapeStart < length - 1 &&
                input.Slice(escapeStart).IndexOfAny(character1, character2) >= 0
            ) //is it worth looking for escape sequence ?
            {
                var bufferLength = Math.Min(Math.Max(length, 16), 256);
                Span<char> initialBuffer = stackalloc char[bufferLength];
                using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                for (int i = 0; i < length; i++)
                {
                    var current = input[i];
                    if (current == escapingSequenceStart)
                    {
                        i++;
                        if (i == length)
                            accumulator.Append(current);
                        else
                        {
                            current = input[i];
                            if (current == character1 || current == character2)
                                accumulator.Append(current);
                            else
                            {
                                accumulator.Append(escapingSequenceStart);
                                accumulator.Append(current);
                            }
                        }
                    }
                    else
                        accumulator.Append(current);
                }
                
                return accumulator.AsSpan().ToArray();
            }
            else return input;
        }
        
        
        [PureMethod]
        public static ReadOnlySpan<char> UnParenthesize(this in ReadOnlySpan<char> span, char? startChar, char? endChar, string typeName = null)
        {
            if (startChar is null && endChar is null)
                return span;

            int minLength = (startChar.HasValue ? 1 : 0) + (endChar.HasValue ? 1 : 0);
            if (span.Length < minLength) throw GetStateException(span.ToString(), startChar, endChar, typeName);

            int start = 0;

            if (startChar.HasValue)
            {
                for (; start < span.Length; start++)
                    if (!char.IsWhiteSpace(span[start]))
                        break;

                bool startsWithChar = start < span.Length && span[start] == startChar.Value;
                if (!startsWithChar) throw GetStateException(span.ToString(), startChar, endChar, typeName);

                ++start;
            }


            int end = span.Length - 1;

            if (endChar.HasValue)
            {
                for (; end > start; end--)
                    if (!char.IsWhiteSpace(span[end]))
                        break;

                bool endsWithChar = end > 0 && span[end] == endChar.Value;
                if (!endsWithChar) throw GetStateException(span.ToString(), startChar, endChar, typeName);

                --end;
            }

            return span.Slice(start, end - start + 1);

            static Exception GetStateException(string text, char? start, char? end, string typeName) => new ArgumentException(
                $@"{typeName ?? "Object" } representation has to start with '{(start is { } c1 ? c1.ToString() : "<nothing>")}' and end with '{(end is { } c2 ? c2.ToString() : "<nothing>")}' optionally lead in the beginning or trailed in the end by whitespace.
These requirements were not met in:
'{text ?? "<NULL>"}'");
        }
    }
}
