using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DictionaryTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDictionary> CreateTransformer<TDictionary>()
        {
            var dictType = typeof(TDictionary);

            var (kind, keyType, valueType) = DictionaryKindHelper.GetDictionaryMeta(dictType);

            var transType = typeof(InnerDictionaryTransformer<,,>).MakeGenericType(keyType, valueType, dictType);

            return (ITransformer<TDictionary>)Activator.CreateInstance(transType, kind);
        }

        private sealed class InnerDictionaryTransformer<TKey, TValue, TDict> : TransformerBase<TDict>
            where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private readonly DictionaryKind _kind;
            public InnerDictionaryTransformer(DictionaryKind kind) => _kind = kind;

            
            public override TDict Parse(in ReadOnlySpan<char> input) =>//input.IsEmpty ? default :
                (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

            public override string Format(TDict dict) =>//dict == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

            public override string ToString() => $"Transform {typeof(TDict).GetFriendlyName()} AS {_kind}<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>";
        }

        public bool CanHandle(Type type) => DictionaryKindHelper.IsTypeSupported(type);

        public sbyte Priority => 50;
    }
}
