using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class KeyValuePairTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        public KeyValuePairTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;


        public ITransformer<TPair> CreateTransformer<TPair>()
        {
            if (!TryGetElements(typeof(TPair), out var keyType, out var valueType))
                throw new NotSupportedException($"Type {typeof(TPair).GetFriendlyName()} is not supported by {GetType().Name}");

            var m = _createTransformerCoreMethod.MakeGenericMethod(keyType, valueType);

            return (ITransformer<TPair>)m.Invoke(this, null);
        }

        private static readonly MethodInfo _createTransformerCoreMethod = Method.OfExpression<
            Func<KeyValuePairTransformerCreator, ITransformer<KeyValuePair<int, int>>>
        >(creator => creator.CreateTransformerCore<int, int>()).GetGenericMethodDefinition();

        private ITransformer<KeyValuePair<TKey, TValue>> CreateTransformerCore<TKey, TValue>() =>
            new InnerPairTransformer<TKey, TValue>(_transformerStore.GetTransformer<TKey>(), _transformerStore.GetTransformer<TValue>());


        private sealed class InnerPairTransformer<TKey, TValue> : TransformerBase<KeyValuePair<TKey, TValue>>
        {
            private const char TUPLE_DELIMITER = '=';
            private const char NULL_ELEMENT_MARKER = '∅';
            private const char ESCAPING_SEQUENCE_START = '\\';

            private readonly ITransformer<TKey> _keyTransformer;
            private readonly ITransformer<TValue> _valueTransformer;

            public InnerPairTransformer(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer)
            {
                _keyTransformer = keyTransformer;
                _valueTransformer = valueTransformer;
            }

            protected override KeyValuePair<TKey, TValue> ParseCore(in ReadOnlySpan<char> input)
            {
                var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);

                var enumerator = kvpTokens.GetEnumerator();

                if (!enumerator.MoveNext())
                    throw new ArgumentException($@"Key{TUPLE_DELIMITER}Value part was not found");
                var key = ParseElement(enumerator.Current, _keyTransformer);

                if (!enumerator.MoveNext())
                    throw new ArgumentException($"'{key}' has no matching value");
                var value = ParseElement(enumerator.Current, _valueTransformer);

                if (enumerator.MoveNext())
                {
                    var remaining = enumerator.Current.ToString();
                    throw new ArgumentException($@"{key}{TUPLE_DELIMITER}{value} pair cannot have more than 2 elements: '{remaining}'");
                }

                return new KeyValuePair<TKey, TValue>(key, value);

                //Exception GetArgumentException(byte count, char delimiter = TUPLE_DELIMITER) => new ArgumentException($@"Key to value pair expects '{delimiter}' delimited collection to be of length 2, but was {count}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static TElement ParseElement<TElement>(in ReadOnlySpan<char> input, ISpanParser<TElement> parser)
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


            public override string Format(KeyValuePair<TKey, TValue> element)
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

        public bool CanHandle(Type type) =>
            TryGetElements(type, out var keyType, out var valueType) &&
            _transformerStore.IsSupportedForTransformation(keyType) &&
            _transformerStore.IsSupportedForTransformation(valueType);

        private static bool TryGetElements(Type type, out Type keyType, out Type valueType)
        {
            if (type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
                TypeMeta.TryGetGenericRealization(type, typeof(KeyValuePair<,>), out var kvp))
            {
                keyType = kvp.GenericTypeArguments[0];
                valueType = kvp.GenericTypeArguments[1];
                return true;
            }
            else
            {
                keyType = valueType = null;
                return false;
            }
        }

        public sbyte Priority => 11;
    }
}
