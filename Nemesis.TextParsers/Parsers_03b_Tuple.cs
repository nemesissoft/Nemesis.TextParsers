using System;
using System.Collections.Generic;
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

            Type keyType = pairType.GenericTypeArguments[0],
               valueType = pairType.GenericTypeArguments[1];

            var transType = typeof(InnerPairTransformer<,>).MakeGenericType(keyType, valueType);

            return (ITransformer<TPair>)Activator.CreateInstance(transType);
        }

        private abstract class TupleTransformer<TTuple> : ITransformer<TTuple>, IParser<TTuple>
            where TTuple : struct, ITuple
        {
            private const char TUPLE_DELIMITER = ',';
            private const char NULL_ELEMENT_MARKER = '∅';
            private const char ESCAPING_SEQUENCE_START = '\\';

            protected abstract byte Arity { get; }

            public TTuple ParseText(string input) => Parse(input.AsSpan());
            public TTuple Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;
                var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);
                var enumerator = kvpTokens.GetEnumerator();
                if (!enumerator.MoveNext())
                    throw new ArgumentException($@"'{TUPLE_DELIMITER}' separated list of arity {Arity} is needed to parse");

                TTuple result = ParseTuple(ref enumerator);

                if (enumerator.MoveNext())
                {
                    var remaining = enumerator.Current.ToString();
                    throw new ArgumentException($@"'{TUPLE_DELIMITER}' separated list of arity {Arity} cannot have more than {Arity} elements: '{remaining}'");
                }

                return result;
            }

            protected abstract TTuple ParseTuple(ref TokenSequence<char>.TokenSequenceEnumerator enumerator);

            public string Format(TTuple tuple)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

                FormatTuple(tuple, ref accumulator, TUPLE_DELIMITER);

                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            protected abstract void FormatTuple(TTuple tuple, ref ValueSequenceBuilder<char> accumulator, char delimiter);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected static TElement ParseElement<TElement>(in ReadOnlySpan<char> input, ISpanParser<TElement> parser)
            {
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
            protected static void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
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

            public sealed override string ToString() => $"Transform {typeof(TTuple).GetFriendlyName()}";
        }

        private sealed class Tuple2Transformer<TTuple, T1, T2> : TupleTransformer<TTuple> 
            where TTuple : struct, ITuple
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            
            protected override byte Arity => 2;

            public Tuple2Transformer()
            {
                _transformer1 = TextTransformer.Default.GetTransformer<T1>();
                _transformer2 = TextTransformer.Default.GetTransformer<T2>();
            }

            protected override TTuple ParseTuple(ref TokenSequence<char>.TokenSequenceEnumerator enumerator)
            {
                var v1 = ParseElement(enumerator.Current, _transformer1);

                if (!enumerator.MoveNext())
                    throw new ArgumentException($"'{v1}' has no next value");
                var v2 = ParseElement(enumerator.Current, _transformer2);
                ValueTuple.Create()
                return new TTuple();
            }

            protected override void FormatTuple(TTuple tuple, ref ValueSequenceBuilder<char> accumulator, char delimiter)
            {
                var t = (ValueTuple<T1, T2>) tuple;
                FormatElement(_transformer1, (T1)tuple[0], ref accumulator);
                accumulator.Append(delimiter);
                FormatElement(_transformer2, (T2)tuple[1], ref accumulator);
            }
        }



        private const byte MAX_ARITY = 5;

        public bool CanHandle(Type type) =>
            type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
            typeof(ITuple).IsAssignableFrom(type) && type.GenericTypeArguments?.Length <= MAX_ARITY;

        public sbyte Priority => 12;
    }
}
