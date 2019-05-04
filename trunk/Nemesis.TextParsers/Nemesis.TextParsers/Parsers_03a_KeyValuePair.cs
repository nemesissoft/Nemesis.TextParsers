using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class KeyValuePairTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TPair> CreateTransformer<TPair>()
        {
            var pairType = typeof(TPair);

            Type keyType = pairType.GenericTypeArguments[0],
               valueType = pairType.GenericTypeArguments[1];

            var transType = typeof(InnerPairTransformer<,>).MakeGenericType(keyType, valueType);

            return (ITransformer<TPair>)Activator.CreateInstance(transType);
        }

        private class InnerPairTransformer<TKey, TValue> : ITransformer<KeyValuePair<TKey, TValue>>, IParser<KeyValuePair<TKey, TValue>>
        {
            private const char TUPLE_DELIMITER = '=';
            private const char NULL_ELEMENT_MARKER = '∅';
            private const char ESCAPING_SEQUENCE_START = '\\';

            private readonly ITransformer<TKey> _keyTransformer;
            private readonly ITransformer<TValue> _valueTransformer;

            public InnerPairTransformer()
            {
                _keyTransformer = TextTransformer.Default.GetTransformer<TKey>();
                _valueTransformer = TextTransformer.Default.GetTransformer<TValue>();
            }

            KeyValuePair<TKey, TValue> IParser<KeyValuePair<TKey, TValue>>.ParseText(string input) => Parse(input.AsSpan());

            public KeyValuePair<TKey, TValue> Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);

                var enumerator = kvpTokens.GetEnumerator();

                if (!enumerator.MoveNext()) throw GetArgumentException(0);
                var key = ParseElement(enumerator.Current, _keyTransformer);

                if (!enumerator.MoveNext()) throw GetArgumentException(1);
                var value = ParseElement(enumerator.Current, _valueTransformer);

                if (enumerator.MoveNext()) throw GetArgumentException(3);

                return new KeyValuePair<TKey, TValue>(key, value);

                Exception GetArgumentException(byte count, char delimiter = TUPLE_DELIMITER) => new ArgumentException($@"Key to value pair expects '{delimiter}' delimited collection to be of length 2, but was {count}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static TElement ParseElement<TElement>(in ReadOnlySpan<char> input, ISpanParser<TElement> parser)
            {
                var unescapedInput = input.UnescapeCharacter(ESCAPING_SEQUENCE_START, TUPLE_DELIMITER);

                if (unescapedInput.Length == 1 && unescapedInput[0].Equals(NULL_ELEMENT_MARKER))
                    return default;
                else
                {
                    unescapedInput = unescapedInput.UnescapeCharacter(ESCAPING_SEQUENCE_START, NULL_ELEMENT_MARKER);
                    unescapedInput = unescapedInput.UnescapeCharacter(ESCAPING_SEQUENCE_START, ESCAPING_SEQUENCE_START);
                    return parser.Parse(unescapedInput);
                }
            }


            public string Format(KeyValuePair<TKey, TValue> element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

                FormatElement(_keyTransformer, element.Key, ref accumulator);
                accumulator.Append(TUPLE_DELIMITER);
                FormatElement(_valueTransformer, element.Value, ref accumulator);

                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
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

            public override string ToString() => $"Transform KeyValuePair<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>";
        }

        public bool CanHandle(Type type) => type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        public sbyte Priority => 11;
    }
}
