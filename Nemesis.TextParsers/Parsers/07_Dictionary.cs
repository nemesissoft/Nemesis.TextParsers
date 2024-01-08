﻿using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

//TODO support ILookup with null as key 
[UsedImplicitly]
public sealed class DictionaryTransformerHandler : ITransformerHandler
{
    private readonly ITransformerStore _transformerStore;
    private readonly DictionarySettings _settings;
    public DictionaryTransformerHandler(ITransformerStore transformerStore, DictionarySettings settings)
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
            Func<DictionaryTransformerHandler, DictionaryKind, ITransformer<Dictionary<int, int>>>
        >((@this, k) => @this.CreateDictionaryTransformer<int, int, Dictionary<int, int>>(k)
        ).GetGenericMethodDefinition();

        createMethod = createMethod.MakeGenericMethod(keyType, valueType, dictType);

        return (ITransformer<TDictionary>)createMethod.Invoke(this, [kind]);
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

    public override string ToString() =>
        $"Create transformer for Dictionary-like structures with settings:{_settings}";

    string ITransformerHandler.DescribeHandlerMatch() => "Dictionary-like structure with key/value supported for transformation";
}

public abstract class DictionaryTransformerBase<TKey, TValue, TDict> : TransformerBase<TDict>
    where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly ITransformer<TKey> _keyTransformer;
    private readonly ITransformer<TValue> _valueTransformer;

    protected readonly DictionarySettings Settings;

    protected DictionaryTransformerBase(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings)
    {
        _keyTransformer = keyTransformer;
        _valueTransformer = valueTransformer;
        Settings = settings;
    }


    protected ParsingPairSequence ParsePairsStream(scoped in ReadOnlySpan<char> text)
    {
        var toParse = text;

        if (Settings.Start.HasValue || Settings.End.HasValue)
            toParse = toParse.UnParenthesize(Settings.Start, Settings.End, "Dictionary");

        var potentialKvp = toParse.Tokenize(Settings.DictionaryPairsDelimiter,
            Settings.EscapingSequenceStart, true);

        var parsedPairs = new ParsingPairSequence(potentialKvp,
            Settings.EscapingSequenceStart,
            Settings.NullElementMarker,
            Settings.DictionaryPairsDelimiter,
            Settings.DictionaryKeyValueDelimiter);

        return parsedPairs;
    }

    protected void PopulateDictionary(in ParsingPairSequence parsingPairs, IDictionary<TKey, TValue> result)
    {
        foreach (var (keyInput, valInput) in parsingPairs)
        {
            var key = keyInput.ParseWith(_keyTransformer);
            var val = valInput.ParseWith(_valueTransformer);

            if (key is null) throw new ArgumentException("Key equal to NULL is not supported");

            switch (Settings.Behaviour)
            {
                case DictionaryBehaviour.OverrideKeys:
                    result[key] = val; break;
                case DictionaryBehaviour.DoNotOverrideKeys:
                    if (!result.ContainsKey(key))
                        result.Add(key, val);
                    break;
                case DictionaryBehaviour.ThrowOnDuplicate:
                    if (!result.ContainsKey(key))
                        result.Add(key, val);
                    else
                        throw new ArgumentException($"The key '{key}' has already been added");
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(Settings)}.{nameof(Settings.Behaviour)} has invalid value of {Settings.Behaviour}", (Exception)null);
            }
        }
    }

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
            if (Settings.Start.HasValue)
                accumulator.Append(Settings.Start.Value);

            do
            {
                var pair = enumerator.Current;
                var key = pair.Key;
                var value = pair.Value;

                if (key == null) accumulator.Append(Settings.NullElementMarker);
                else Append(ref accumulator, _keyTransformer.Format(key));

                accumulator.Append(Settings.DictionaryKeyValueDelimiter); //=

                if (value == null) accumulator.Append(Settings.NullElementMarker);
                else Append(ref accumulator, _valueTransformer.Format(value));

                accumulator.Append(Settings.DictionaryPairsDelimiter); //;
            } while (enumerator.MoveNext());

            accumulator.Shrink();

            if (Settings.End.HasValue)
                accumulator.Append(Settings.End.Value);

            return accumulator.ToString();
        }
        finally { accumulator.Dispose(); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Append(ref ValueSequenceBuilder<char> accumulator, string text)
    {
        foreach (char c in text)
        {
            if (c == Settings.EscapingSequenceStart ||
                c == Settings.NullElementMarker ||
                c == Settings.DictionaryPairsDelimiter ||
                c == Settings.DictionaryKeyValueDelimiter
            )
                accumulator.Append(Settings.EscapingSequenceStart);
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


    protected override TDict ParseCore(in ReadOnlySpan<char> input)
    {
        var parsedPairs = ParsePairsStream(input);

        IDictionary<TKey, TValue> result = _kind switch
        {
            DictionaryKind.Unknown => throw new NotSupportedException($"{nameof(_kind)} = '{nameof(DictionaryKind)}.{nameof(DictionaryKind.Unknown)}' is not supported"),
            DictionaryKind.SortedDictionary => new SortedDictionary<TKey, TValue>(),
            DictionaryKind.SortedList => new SortedList<TKey, TValue>(Settings.GetCapacity(input)),
            _ => new Dictionary<TKey, TValue>(Settings.GetCapacity(input))
        };

        PopulateDictionary(parsedPairs, result);

        return (TDict)(_kind == DictionaryKind.ReadOnlyDictionary
            ? new ReadOnlyDictionary<TKey, TValue>(result)
            : result);
    }

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
