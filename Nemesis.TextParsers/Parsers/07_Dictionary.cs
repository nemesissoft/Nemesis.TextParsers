using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DictionaryTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        public DictionaryTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;



        public ITransformer<TDictionary> CreateTransformer<TDictionary>()
        {
            var dictType = typeof(TDictionary);
            if (!TryGetDictMeta(dictType, out var kind, out var keyType, out var valueType))
                throw new NotSupportedException($"Type {dictType.GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = typeof(InnerDictionaryTransformer<,,>).MakeGenericType(keyType, valueType, dictType);

            return (ITransformer<TDictionary>)Activator.CreateInstance(transType, kind);
        }

        private sealed class InnerDictionaryTransformer<TKey, TValue, TDict> : TransformerBase<TDict>, ISupportEmpty<TDict>
            where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private readonly DictionaryKind _kind;
            public InnerDictionaryTransformer(DictionaryKind kind) => _kind = kind;


            public override TDict Parse(in ReadOnlySpan<char> input) =>//input.IsEmpty ? default :
                (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

            public override string Format(TDict dict) =>//dict == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

            public TDict GetEmpty() =>
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
}
