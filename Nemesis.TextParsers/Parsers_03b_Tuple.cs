using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class TupleTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TTuple> CreateTransformer<TTuple>()
        {
            var tupleType = typeof(TTuple);

            var types = tupleType.GenericTypeArguments;

            Type transType;
            switch (types?.Length)
            {
                case 2:
                    transType = typeof(Tuple2Transformer<,>);
                    break;
                case 3:
                    transType = typeof(Tuple3Transformer<,,>);
                    break;
                case 4:
                    transType = typeof(Tuple4Transformer<,,,>);
                    break;
                case 5:
                    transType = typeof(Tuple5Transformer<,,,,>);
                    break;
                default:
                    throw new InvalidOperationException($"Only ValueTuple with arity 2-{MAX_ARITY} are supported");
            }

            transType = transType.MakeGenericType(types);

            return (ITransformer<TTuple>)Activator.CreateInstance(transType);
        }

        private class Tuple2Transformer<T1, T2> : ITransformer<(T1, T2)>, ITextParser<(T1, T2)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;

            private byte Arity => 2;

            public Tuple2Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
            }

            (T1, T2) ITextParser<(T1, T2)>.ParseText(string input) => Parse(input.AsSpan());

            public (T1, T2) Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, Arity);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);


                Helper.ParseEnd(ref enumerator, Arity);

                return (t1, t2);
            }

            public string Format((T1, T2) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()})";
        }

        private class Tuple3Transformer<T1, T2, T3> : ITransformer<(T1, T2, T3)>, ITextParser<(T1, T2, T3)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;

            private byte Arity => 3;

            public Tuple3Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
            }

            (T1, T2, T3) ITextParser<(T1, T2, T3)>.ParseText(string input) => Parse(input.AsSpan());

            public (T1, T2, T3) Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, Arity);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseEnd(ref enumerator, Arity);

                return (t1, t2, t3);
            }

            public string Format((T1, T2, T3) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer3, element.Item3, ref accumulator);
                

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()})";
        }

        private class Tuple4Transformer<T1, T2, T3, T4> : ITransformer<(T1, T2, T3, T4)>, ITextParser<(T1, T2, T3, T4)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;

            private byte Arity => 4;

            public Tuple4Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
            }

            (T1, T2, T3, T4) ITextParser<(T1, T2, T3, T4)>.ParseText(string input) => Parse(input.AsSpan());

            public (T1, T2, T3, T4) Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, Arity);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseEnd(ref enumerator, Arity);

                return (t1, t2, t3, t4);
            }

            public string Format((T1, T2, T3, T4) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer3, element.Item3, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer4, element.Item4, ref accumulator);
                

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()})";
        }

        private class Tuple5Transformer<T1, T2, T3, T4, T5> : ITransformer<(T1, T2, T3, T4, T5)>, ITextParser<(T1, T2, T3, T4, T5)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;

            private byte Arity => 5;

            public Tuple5Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
                _transformer5 = TextTransformer.Default.GetTransformer<T5>();
            }

            (T1, T2, T3, T4, T5) ITextParser<(T1, T2, T3, T4, T5)>.ParseText(string input) => Parse(input.AsSpan());

            public (T1, T2, T3, T4, T5) Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, Arity);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseEnd(ref enumerator, Arity);

                return (t1, t2, t3, t4, t5);
            }

            public string Format((T1, T2, T3, T4, T5) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer3, element.Item3, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer4, element.Item4, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer5, element.Item5, ref accumulator);


                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()})";
        }

        private static class Helper
        {
            private const char TUPLE_DELIMITER = ',';
            private const char NULL_ELEMENT_MARKER = '∅';
            private const char ESCAPING_SEQUENCE_START = '\\';
            private const char TUPLE_START = '(';
            private const char TUPLE_END = ')';

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void StartFormat(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_START);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EndFormat(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_END);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_DELIMITER);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity)
            {
                input = UnParenthesize(input);

                var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);
                var enumerator = kvpTokens.GetEnumerator();

                if (!enumerator.MoveNext())
                    throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' was not found");

                return enumerator;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span)
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

                Exception GetStateException() => new ArgumentException(
                         "Tuple representation has to start and end with parentheses optionally lead in the beginning or trailed in the end by whitespace");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ParseNext(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte index)
            {
                string ToOrdinal(byte number)
                {
                    int rem = number % 100;
                    if (rem >= 11 && rem <= 13) return $"{number}th";

                    switch (number % 10)
                    {
                        case 1:
                            return $"{number}st";
                        case 2:
                            return $"{number}nd";
                        case 3:
                            return $"{number}rd";
                        default:
                            return $"{number}th";
                    }
                }

                if (!enumerator.MoveNext())
                    throw new ArgumentException($"{ToOrdinal(index)} tuple element was not found");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ParseEnd(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte arity)
            {
                if (enumerator.MoveNext())
                {
                    var remaining = enumerator.Current.ToString();
                    throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' cannot have more than {arity} elements: '{remaining}'");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TElement ParseElement<TElement>(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, ISpanParser<TElement> parser)
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
            public static void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
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

        private const byte MAX_ARITY = 5;

        public bool CanHandle(Type type) =>
            type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
            //typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(type) //not supported in .NET Standard 2.0
            type.Namespace == "System" &&
            typeof(ValueType).IsAssignableFrom(type) &&
            type.GenericTypeArguments?.Length is int arity && arity <= MAX_ARITY && arity >= 2
            ;

        public sbyte Priority => 12;
    }
}
