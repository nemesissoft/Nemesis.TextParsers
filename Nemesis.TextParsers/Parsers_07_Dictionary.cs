using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            var genericInterfaceType = TypeMeta.GetConcreteInterfaceOfType(dictType, typeof(IDictionary<,>))
                ?? throw new InvalidOperationException("Type has to implement IDictionary<,>");

            Type keyType = genericInterfaceType.GenericTypeArguments[0],
               valueType = genericInterfaceType.GenericTypeArguments[1];

            var kind = GetDictionaryKind(dictType);

            var transType = typeof(InnerDictionaryTransformer<,,>).MakeGenericType(keyType, valueType, dictType);

            return (ITransformer<TDictionary>)Activator.CreateInstance(transType, kind);
        }

        private static DictionaryKind GetDictionaryKind(Type dictType) =>
            dictType.DerivesOrImplementsGeneric(typeof(SortedDictionary<,>))
                ? DictionaryKind.SortedDictionary
                : (dictType.DerivesOrImplementsGeneric(typeof(ReadOnlyDictionary<,>)) 
                   || 
                   dictType.IsGenericType && dictType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                    ? DictionaryKind.ReadOnlyDictionary
                    : DictionaryKind.Dictionary;

        private class InnerDictionaryTransformer<TKey, TValue, TDict> : ITransformer<TDict>
            where TDict : IDictionary<TKey, TValue>
        {
            private readonly DictionaryKind _kind;

            public InnerDictionaryTransformer(DictionaryKind kind) => _kind = kind;

            public TDict Parse(ReadOnlySpan<char> input) =>
                //input.IsEmpty ? default :
                (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

            public string Format(TDict dict) =>
                //dict == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

            public override string ToString() => $"Transform IDictionary<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>, {_kind}";
        }

        public bool CanHandle(Type type) => type.DerivesOrImplementsGeneric(typeof(IDictionary<,>));

        public sbyte Priority => 50;
    }
}
