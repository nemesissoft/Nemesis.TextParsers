using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
                _settings,
                kind
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

    public abstract class DictionaryTransformerBase<TKey, TValue, TDict> : TransformerBase<TDict>
        where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        protected readonly ITransformer<TKey> KeyTransformer;
        protected readonly ITransformer<TValue> ValueTransformer;

        protected char DictionaryPairsDelimiter { get; }
        protected char DictionaryKeyValueDelimiter { get; }
        protected char NullElementMarker { get; }
        protected char EscapingSequenceStart { get; }
        protected char? Start { get; }
        protected char? End { get; }
        protected DictionaryBehaviour Behaviour { get; }

        protected DictionaryTransformerBase(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings)
        {
            KeyTransformer = keyTransformer;
            ValueTransformer = valueTransformer;
            (
                DictionaryPairsDelimiter, DictionaryKeyValueDelimiter, NullElementMarker,
                EscapingSequenceStart, Start, End, Behaviour
            ) = settings;
        }

        //TODO: add ParsePairsStream here
        /*         * TODO
         * remove parsers from parsed pair
         * move methods from spanParserHelper, SpanCollSer ...
         */

        public sealed override string Format(TDict dict)
        {
            if (dict == null) return null;
            
            using var enumerator = dict.GetEnumerator();
            if (!enumerator.MoveNext())
                return "";

            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            try
            {
                if (Start.HasValue)
                    accumulator.Append(Start.Value);

                do
                {
                    var pair = enumerator.Current;
                    var key = pair.Key;
                    var value = pair.Value;

                    if (key == null) accumulator.Append(NullElementMarker);
                    else Append(ref accumulator, KeyTransformer.Format(key));
                    
                    accumulator.Append(DictionaryKeyValueDelimiter); //=

                    if (value == null) accumulator.Append(NullElementMarker);
                    else Append(ref accumulator, ValueTransformer.Format(value));

                    accumulator.Append(DictionaryPairsDelimiter); //;
                } while (enumerator.MoveNext());

                if (End.HasValue)
                    accumulator.Append(End.Value);

                return accumulator.AsSpanTo(accumulator.Length > 0 ? accumulator.Length - 1 : 0).ToString();
            }
            finally { accumulator.Dispose(); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Append(ref ValueSequenceBuilder<char> accumulator, string text)
        {
            foreach (char c in text)
            {
                if (c == EscapingSequenceStart || c == NullElementMarker ||
                    c == DictionaryPairsDelimiter || c == DictionaryKeyValueDelimiter
                )
                    accumulator.Append(EscapingSequenceStart);
                accumulator.Append(c);
            }
        }
    }

    public sealed class DictionaryTransformer<TKey, TValue, TDict> : DictionaryTransformerBase<TKey, TValue, TDict>
        where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly DictionaryKind _kind;

        public DictionaryTransformer(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings, DictionaryKind kind) 
            : base(keyTransformer, valueTransformer, settings) =>
            _kind = kind;


        protected override TDict ParseCore(in ReadOnlySpan<char> input) =>
            (TDict)SpanCollectionSerializer.DefaultInstance.ParseDictionary<TKey, TValue>(input, _kind);

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
