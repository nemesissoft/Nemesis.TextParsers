using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

public sealed class KeyValuePairTransformerHandler(ITransformerStore transformerStore, KeyValuePairSettings settings) : ITransformerHandler
{
    private readonly TupleHelper _helper = settings.ToTupleHelper();


    public ITransformer<TPair> CreateTransformer<TPair>()
    {
        if (!TryGetElements(typeof(TPair), out var keyType, out var valueType))
            throw new NotSupportedException(
                $"Type {typeof(TPair).GetFriendlyName()} is not supported by {GetType().Name}");

        var method = (
            GetType().GetMethod(nameof(CreateTransformerCore), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{nameof(KeyValuePairTransformerHandler)}.{nameof(CreateTransformerCore)} method not found.")
        ).MakeGenericMethod(keyType, valueType);

        return (ITransformer<TPair>) method.Invoke(this, null);
    }

    private KeyValuePairTransformer<TKey, TValue> CreateTransformerCore<TKey, TValue>() =>
        new(_helper, transformerStore.GetTransformer<TKey>(), transformerStore.GetTransformer<TValue>());


    private sealed class KeyValuePairTransformer<TKey, TValue>(TupleHelper helper, ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer)
        : TransformerBase<KeyValuePair<TKey, TValue>>
    {
        protected override KeyValuePair<TKey, TValue> ParseCore(in ReadOnlySpan<char> input)
        {
            const string TYPE_NAME = "Key-value pair";

            var enumerator = helper.ParseStart(input, 2, TYPE_NAME);

            var key = helper.ParseElement(ref enumerator, keyTransformer);
            var value = helper.ParseElement(ref enumerator, valueTransformer, 2, TYPE_NAME);

            helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public override string Format(KeyValuePair<TKey, TValue> element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                helper.StartFormat(ref accumulator);

                helper.FormatElement(keyTransformer, element.Key, ref accumulator);
                helper.AddDelimiter(ref accumulator);
                helper.FormatElement(valueTransformer, element.Value, ref accumulator);

                helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally
            {
                accumulator.Dispose();
            }
        }

        public override KeyValuePair<TKey, TValue> GetEmpty() => new(keyTransformer.GetEmpty(), valueTransformer.GetEmpty());
    }

    public bool CanHandle(Type type) =>
        TryGetElements(type, out var keyType, out var valueType) &&
        transformerStore.IsSupportedForTransformation(keyType) &&
        transformerStore.IsSupportedForTransformation(valueType);

    private static bool TryGetElements(Type type, out Type keyType, out Type valueType)
    {
        if (type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
            TypeMeta.TryGetGenericRealization(type, typeof(KeyValuePair<,>), out var kvp) &&
            kvp is not null)
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

    public override string ToString() =>
        $"Create transformer for KeyValuePair struct with settings:{_helper}";

    string ITransformerHandler.DescribeHandlerMatch() => "Key-value pair with properties supported for transformation";
}