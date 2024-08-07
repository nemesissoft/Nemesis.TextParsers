using JetBrains.Annotations;

using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class KeyValuePairTransformerHandler : ITransformerHandler
{
    private readonly TupleHelper _helper;
    private readonly ITransformerStore _transformerStore;

    public KeyValuePairTransformerHandler(ITransformerStore transformerStore, KeyValuePairSettings settings)
    {
        _transformerStore = transformerStore;
        _helper = settings.ToTupleHelper();
    }


    public ITransformer<TPair> CreateTransformer<TPair>()
    {
        if (!TryGetElements(typeof(TPair), out var keyType, out var valueType))
            throw new NotSupportedException(
                $"Type {typeof(TPair).GetFriendlyName()} is not supported by {GetType().Name}");

        var m = _createTransformerCoreMethod.MakeGenericMethod(keyType, valueType);

        return (ITransformer<TPair>)m.Invoke(this, null);
    }

    private static readonly MethodInfo _createTransformerCoreMethod = Method.OfExpression<
        Func<KeyValuePairTransformerHandler, ITransformer<KeyValuePair<int, int>>>
    >(handler => handler.CreateTransformerCore<int, int>()).GetGenericMethodDefinition();

    private ITransformer<KeyValuePair<TKey, TValue>> CreateTransformerCore<TKey, TValue>() =>
        new KeyValuePairTransformer<TKey, TValue>(_helper,
            _transformerStore.GetTransformer<TKey>(),
            _transformerStore.GetTransformer<TValue>()
        );


    private sealed class KeyValuePairTransformer<TKey, TValue> : TransformerBase<KeyValuePair<TKey, TValue>>
    {
        private const string TYPE_NAME = "Key-value pair";

        private readonly TupleHelper _helper;
        private readonly ITransformer<TKey> _keyTransformer;
        private readonly ITransformer<TValue> _valueTransformer;

        public KeyValuePairTransformer(TupleHelper helper, ITransformer<TKey> keyTransformer,
            ITransformer<TValue> valueTransformer)
        {
            _helper = helper;
            _keyTransformer = keyTransformer;
            _valueTransformer = valueTransformer;
        }

        protected override KeyValuePair<TKey, TValue> ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

            var key = _helper.ParseElement(ref enumerator, _keyTransformer);
            var value = _helper.ParseElement(ref enumerator, _valueTransformer, 2, TYPE_NAME);

            _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public override string Format(KeyValuePair<TKey, TValue> element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                _helper.StartFormat(ref accumulator);

                _helper.FormatElement(_keyTransformer, element.Key, ref accumulator);
                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_valueTransformer, element.Value, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally
            {
                accumulator.Dispose();
            }
        }

        public override KeyValuePair<TKey, TValue> GetEmpty() => new(_keyTransformer.GetEmpty(), _valueTransformer.GetEmpty());
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

    public override string ToString() =>
        $"Create transformer for KeyValuePair struct with settings:{_helper}";

    string ITransformerHandler.DescribeHandlerMatch() => "Key-value pair with properties supported for transformation";
}