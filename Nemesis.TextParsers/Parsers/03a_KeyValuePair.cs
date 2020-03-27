using System;
using System.Collections.Generic;
using System.Reflection;
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
            new KeyValuePairTransformer<TKey, TValue>(_transformerStore);


        private const string TYPE_NAME = "Key=Value pair";
        private static readonly TupleHelper _helper = new TupleHelper('=', '∅', '\\', null, null);

        private sealed class KeyValuePairTransformer<TKey, TValue> : TransformerBase<KeyValuePair<TKey, TValue>>
        {
            private readonly ITransformer<TKey> _keyTransformer;
            private readonly ITransformer<TValue> _valueTransformer;

            public KeyValuePairTransformer(ITransformerStore transformerStore)
            {
               _keyTransformer = transformerStore.GetTransformer<TKey>();
                _valueTransformer = transformerStore.GetTransformer<TValue>();
            }

            protected override KeyValuePair<TKey, TValue> ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

                var key = _helper.ParseElement(ref enumerator, _keyTransformer);

                _helper.ParseNext(ref enumerator, 2);
                var value = _helper.ParseElement(ref enumerator, _valueTransformer);


                _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

                return new KeyValuePair<TKey, TValue>(key, value);
            }

            public override string Format(KeyValuePair<TKey, TValue> element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                _helper.FormatElement(_keyTransformer, element.Key, ref accumulator);
                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_valueTransformer, element.Value, ref accumulator);


                _helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform KeyValuePair<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>";


            public override KeyValuePair<TKey, TValue> GetEmpty() =>
                new KeyValuePair<TKey, TValue>(
                    _keyTransformer.GetEmpty(),
                    _valueTransformer.GetEmpty() 
                );
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
