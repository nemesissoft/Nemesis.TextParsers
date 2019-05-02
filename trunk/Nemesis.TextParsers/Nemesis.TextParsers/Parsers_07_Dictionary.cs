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

            var genericInterfaceType = dictType.IsGenericType && (dictType.GetGenericTypeDefinition() == typeof(IDictionary<,>) || dictType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                ? dictType
                : TypeMeta.GetConcreteInterfaceOfType(dictType, typeof(IDictionary<,>))
                    ?? throw new InvalidOperationException("Type has to be or implement IDictionary<,>");

            Type keyType = genericInterfaceType.GenericTypeArguments[0],
               valueType = genericInterfaceType.GenericTypeArguments[1];

            var kind = GetDictionaryKind(dictType);

            var transType = typeof(InnerDictionaryTransformer<,,>).MakeGenericType(keyType, valueType, dictType);

            return (ITransformer<TDictionary>)Activator.CreateInstance(transType, kind);
        }

        private static DictionaryKind GetDictionaryKind(Type dictType)
        {
            if (dictType.DerivesOrImplementsGeneric(typeof(SortedDictionary<,>)))
                return DictionaryKind.SortedDictionary;
            else if (dictType.DerivesOrImplementsGeneric(typeof(SortedList<,>)))
                return DictionaryKind.SortedList;
            else if (dictType.DerivesOrImplementsGeneric(typeof(ReadOnlyDictionary<,>))
                     ||
                     dictType.IsGenericType && dictType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                return DictionaryKind.ReadOnlyDictionary;
            else
                return DictionaryKind.Dictionary;
        }

        private class InnerDictionaryTransformer<TKey, TValue, TDict> : ITransformer<TDict>, IParser<TDict>
            where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            private readonly DictionaryKind _kind;

            public InnerDictionaryTransformer(DictionaryKind kind) => _kind = kind;


            TDict IParser<TDict>.ParseText(string input) => Parse(input.AsSpan());

            public TDict Parse(ReadOnlySpan<char> input) =>
                //input.IsEmpty ? default :
                (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

            public string Format(TDict dict) =>
                //dict == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatDictionary(dict);

            public override string ToString() => $"Transform IDictionary<{typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}>, {_kind}";
        }

        public bool CanHandle(Type type) =>
            type.DerivesOrImplementsGeneric(typeof(IDictionary<,>))
                ||
            type.DerivesOrImplementsGeneric(typeof(IReadOnlyDictionary<,>))
        ;

        public sbyte Priority => 50;
    }
}
