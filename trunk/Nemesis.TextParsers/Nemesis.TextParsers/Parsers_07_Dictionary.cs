﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
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

        private class InnerDictionaryTransformer<TKey, TValue, TDict> : ITransformer<TDict>, IParser<TDict>
            where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private readonly DictionaryKind _kind;
            public InnerDictionaryTransformer(DictionaryKind kind) => _kind = kind;


            TDict IParser<TDict>.ParseText(string input) => Parse(input.AsSpan());

            public TDict Parse(ReadOnlySpan<char> input) =>//input.IsEmpty ? default :
                (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

            public string Format(TDict dict) =>//dict == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

            public override string ToString() => $"Transform IDictionary<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>, {_kind}";
        }

        public bool CanHandle(Type type) => DictionaryKindHelper.IsTypeSupported(type);

        public sbyte Priority => 50;
    }
}
