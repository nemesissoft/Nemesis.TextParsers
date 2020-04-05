using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DictionaryTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly DictionarySettings _settings;
        public DictionaryTransformerCreator(ITransformerStore transformerStore, DictionarySettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }


        public ITransformer<TDictionary> CreateTransformer<TDictionary>()
        {
            var dictType = typeof(TDictionary);
            if (!TryGetDictMeta(dictType, out var kind, out var keyType, out var valueType))
                throw new NotSupportedException($"Type {dictType.GetFriendlyName()} is not supported by {GetType().Name}");

            var createMethod = Method.OfExpression<
                Func<DictionaryTransformerCreator, DictionaryKind, ITransformer<Dictionary<int, int>>>
            >((@this, k) => @this.CreateDictionaryTransformer<int, int, Dictionary<int, int>>(k)
            ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(keyType, valueType, dictType);

            return (ITransformer<TDictionary>)createMethod.Invoke(this, new object[] { kind });
        }

        private ITransformer<TDict> CreateDictionaryTransformer<TKey, TValue, TDict>(DictionaryKind kind)
            where TDict : IEnumerable<KeyValuePair<TKey, TValue>> =>
            new DictionaryTransformer<TKey, TValue, TDict>(
                _transformerStore.GetTransformer<TKey>(),
                _transformerStore.GetTransformer<TValue>(),
                kind,
                _settings
            );

        public bool CanHandle(Type type) =>
            TryGetDictMeta(type, out _, out var keyType, out var valueType) &&
            _transformerStore.IsSupportedForTransformation(keyType) &&
            _transformerStore.IsSupportedForTransformation(valueType);

        private static bool TryGetDictMeta(Type type, out DictionaryKind kind, out Type keyType, out Type valueType)
        {
            if (DictionaryKindHelper.IsTypeSupported(type))
            {
                (kind, keyType, valueType) = DictionaryKindHelper.GetDictionaryMeta(type);
                return true;
            }
            else
            {
                (kind, keyType, valueType) = (default, default, default);
                return false;
            }
        }

        public sbyte Priority => 50;
    }

    public sealed class DictionaryTransformer<TKey, TValue, TDict> : TransformerBase<TDict>
        where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly ITransformer<TKey> _keyParser;
        private readonly ITransformer<TValue> _valueParser;
        private readonly DictionaryKind _kind;
        private readonly DictionarySettings _settings;

        public DictionaryTransformer(ITransformer<TKey> keyParser, ITransformer<TValue> valueParser, DictionaryKind kind, DictionarySettings settings)
        {
            _keyParser = keyParser;
            _valueParser = valueParser;
            _kind = kind;
            _settings = settings;
        }


        protected override TDict ParseCore(in ReadOnlySpan<char> input) =>
            (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

        public override string Format(TDict dict) =>
            SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

        /*
         * TODO:fix Format tests - remove dependency 
         * move Format method, remember about settings
         * 
         * remove parsers from parsed pair
         * move methods from spanParserHelper, SpanCollSer ...
         */

        public override TDict GetEmpty() =>
            (TDict)(_kind switch
            {
                DictionaryKind.SortedDictionary => new SortedDictionary<TKey, TValue>(),
                DictionaryKind.SortedList => new SortedList<TKey, TValue>(0),
                DictionaryKind.ReadOnlyDictionary => new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0)),
                DictionaryKind.Unknown => throw new NotSupportedException($"Dictionary kind {_kind} is not supported for empty element query"),
                //DictionaryKind.Dictionary => 
                _ => (IDictionary<TKey, TValue>)new Dictionary<TKey, TValue>(0),
            });

        public override string ToString() => $"Transform {typeof(TDict).GetFriendlyName()} AS {_kind}<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>";
    }
}
