using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class TupleTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TTuple> CreateTransformer<TTuple>()
        {
            var tupleType = typeof(TTuple);

            var types = tupleType.GenericTypeArguments;
            var transType = (types?.Length) switch
            {
                1 => typeof(Tuple1Transformer<>),
                2 => typeof(Tuple2Transformer<,>),
                3 => typeof(Tuple3Transformer<,,>),
                4 => typeof(Tuple4Transformer<,,,>),
                5 => typeof(Tuple5Transformer<,,,,>),
                6 => typeof(Tuple6Transformer<,,,,,>),
                7 => typeof(Tuple7Transformer<,,,,,,>),
                8 => typeof(TupleRestTransformer<,,,,,,,>),
                _ => throw new NotSupportedException($"Only ValueTuple with arity 1-{MAX_ARITY} are supported"),
            };
            transType = transType.MakeGenericType(types);

            return (ITransformer<TTuple>)Activator.CreateInstance(transType);
        }

        private sealed class Tuple1Transformer<T1> : TransformerBase<ValueTuple<T1>>
        {
            private readonly ITransformer<T1> _transformer1;

            private const byte ARITY = 1;

            public Tuple1Transformer() => _transformer1 = TextTransformer.Default.GetTransformer<T1>();

            public override ValueTuple<T1> Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseEnd(ref enumerator, ARITY);

                return new ValueTuple<T1>(t1);
            }

            public override string Format(ValueTuple<T1> element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()})";
        }

        private sealed class Tuple2Transformer<T1, T2> : TransformerBase<(T1, T2)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;

            private const byte ARITY = 2;

            public Tuple2Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
            }

            public override (T1, T2) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);


                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2);
            }

            public override string Format((T1, T2) element)
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

        private sealed class Tuple3Transformer<T1, T2, T3> : TransformerBase<(T1, T2, T3)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;

            private const byte ARITY = 3;

            public Tuple3Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
            }

            public override (T1, T2, T3) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3);
            }

            public override string Format((T1, T2, T3) element)
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

        private sealed class Tuple4Transformer<T1, T2, T3, T4> : TransformerBase<(T1, T2, T3, T4)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;

            private const byte ARITY = 4;

            public Tuple4Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
            }

            public override (T1, T2, T3, T4) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4);
            }

            public override string Format((T1, T2, T3, T4) element)
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

        private sealed class Tuple5Transformer<T1, T2, T3, T4, T5> : TransformerBase<(T1, T2, T3, T4, T5)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;

            private const byte ARITY = 5;

            public Tuple5Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
                _transformer5 = TextTransformer.Default.GetTransformer<T5>();
            }

            public override (T1, T2, T3, T4, T5) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5);
            }

            public override string Format((T1, T2, T3, T4, T5) element)
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

        private sealed class Tuple6Transformer<T1, T2, T3, T4, T5, T6> : TransformerBase<(T1, T2, T3, T4, T5, T6)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;

            private const byte ARITY = 6;

            public Tuple6Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
                _transformer5 = TextTransformer.Default.GetTransformer<T5>();
                _transformer6 = TextTransformer.Default.GetTransformer<T6>();
            }

            public override (T1, T2, T3, T4, T5, T6) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5, t6);
            }

            public override string Format((T1, T2, T3, T4, T5, T6) element)
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
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer6, element.Item6, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()})";
        }

        private sealed class Tuple7Transformer<T1, T2, T3, T4, T5, T6, T7> : TransformerBase<(T1, T2, T3, T4, T5, T6, T7)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;
            private readonly ITransformer<T7> _transformer7;

            private const byte ARITY = 7;

            public Tuple7Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
                _transformer5 = TextTransformer.Default.GetTransformer<T5>();
                _transformer6 = TextTransformer.Default.GetTransformer<T6>();
                _transformer7 = TextTransformer.Default.GetTransformer<T7>();
            }

            public override (T1, T2, T3, T4, T5, T6, T7) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseNext(ref enumerator, 7);
                var t7 = Helper.ParseElement(ref enumerator, _transformer7);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5, t6, t7);
            }

            public override string Format((T1, T2, T3, T4, T5, T6, T7) element)
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
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer6, element.Item6, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer7, element.Item7, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()},{typeof(T7).GetFriendlyName()})";
        }

        private sealed class TupleRestTransformer<T1, T2, T3, T4, T5, T6, T7, TRest> : TransformerBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>> where TRest : struct
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;
            private readonly ITransformer<T7> _transformer7;
            private readonly ITransformer<TRest> _transformerRest;

            private const byte ARITY = 8;

            public TupleRestTransformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
                _transformer3 = TextTransformer.Default.GetTransformer<T3>();
                _transformer4 = TextTransformer.Default.GetTransformer<T4>();
                _transformer5 = TextTransformer.Default.GetTransformer<T5>();
                _transformer6 = TextTransformer.Default.GetTransformer<T6>();
                _transformer7 = TextTransformer.Default.GetTransformer<T7>();
                _transformerRest = TextTransformer.Default.GetTransformer<TRest>();
            }

            public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseNext(ref enumerator, 7);
                var t7 = Helper.ParseElement(ref enumerator, _transformer7);

                Helper.ParseNext(ref enumerator, 8);
                var tRest = Helper.ParseElement(ref enumerator, _transformerRest);

                Helper.ParseEnd(ref enumerator, ARITY);

                return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(t1, t2, t3, t4, t5, t6, t7, tRest);
            }

            public override string Format(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> element)
            {
                var initialBuffer = ArrayPool<char>.Shared.Rent(32);
                try
                {
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
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer6, element.Item6, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer7, element.Item7, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformerRest, element.Rest, ref accumulator);

                    Helper.EndFormat(ref accumulator);
                    var text = accumulator.AsSpan().ToString();
                    accumulator.Dispose();
                    return text;
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(initialBuffer);
                }
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()},{typeof(T7).GetFriendlyName()},{typeof(TRest).GetFriendlyName()})";
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

                static Exception GetStateException() => new ArgumentException(
                         "Tuple representation has to start and end with parentheses optionally lead in the beginning or trailed in the end by whitespace");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ParseNext(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte index)
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

        private const byte MAX_ARITY = 8;

        public bool CanHandle(Type type) =>
            type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
#if NETSTANDARD2_0 || NETFRAMEWORK
            type.Namespace == "System" &&
            type.Name.StartsWith("ValueTuple`") &&
            typeof(ValueType).IsAssignableFrom(type) &&          
#else
            typeof(ITuple).IsAssignableFrom(type) &&
#endif
            type.GenericTypeArguments?.Length is { } arity && arity <= MAX_ARITY && arity >= 1
            ;

        public sbyte Priority => 12;
    }
}
