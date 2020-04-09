using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class CustomDictionaryTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly DictionarySettings _settings;

        public CustomDictionaryTransformerCreator(ITransformerStore transformerStore, DictionarySettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }


        public ITransformer<TDictionary> CreateTransformer<TDictionary>()
        {
            var dictType = typeof(TDictionary);
            var supportsDeserializationLogic = typeof(IDeserializationCallback).IsAssignableFrom(dictType);

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (IsCustomDictionary(dictType, out var meta1))
            {
                var createMethod =
                    (GetType().GetMethod(nameof(CreateCustomDictionaryTransformer), FLAGS)
                        ?? throw new MissingMethodException(GetType().Name, nameof(CreateCustomDictionaryTransformer))
                    ).GetGenericMethodDefinition();

                createMethod = createMethod.MakeGenericMethod(meta1.keyType, meta1.valueType, dictType);

                return (ITransformer<TDictionary>)createMethod.Invoke(this, new object[] { supportsDeserializationLogic });
            }
            else if (IsReadOnlyDictionary(dictType, out var meta2))
            {
                var createMethod =
                    (GetType().GetMethod(nameof(CreateReadOnlyDictionaryTransformer), FLAGS)
                     ?? throw new MissingMethodException(GetType().Name, nameof(CreateReadOnlyDictionaryTransformer))
                    ).GetGenericMethodDefinition();

                createMethod = createMethod.MakeGenericMethod(meta2.keyType, meta2.valueType, dictType);

                return (ITransformer<TDictionary>)createMethod.Invoke(this,
                    new object[] { supportsDeserializationLogic, meta2.ctor });
            }
            else
                throw new NotSupportedException(
                    "Only concrete types based on IDictionary or IReadOnlyDictionary are supported");
        }

        private ITransformer<TDict> CreateCustomDictionaryTransformer<TKey, TValue, TDict>(bool supportsDeserializationLogic)
            where TDict : IDictionary<TKey, TValue>, new() =>
            new CustomDictionaryTransformer<TKey, TValue, TDict>(
                _transformerStore.GetTransformer<TKey>(),
                _transformerStore.GetTransformer<TValue>(),
                _settings,
                supportsDeserializationLogic
            );

        private ITransformer<TDict> CreateReadOnlyDictionaryTransformer<TKey, TValue, TDict>
            (bool supportsDeserializationLogic, ConstructorInfo ctor)
            where TDict : IReadOnlyDictionary<TKey, TValue>
        {
            var dictConversion = ReadOnlyDictionaryTransformer<TKey, TValue, TDict>.GetDictConverter(ctor);

            return new ReadOnlyDictionaryTransformer<TKey, TValue, TDict>(
                _transformerStore.GetTransformer<TKey>(),
                _transformerStore.GetTransformer<TValue>(),
                _settings,
                supportsDeserializationLogic,
                dictConversion
            );
        }

        private static bool IsCustomDictionary(Type dictType, out (Type keyType, Type valueType) meta)
        {
            Type iDict = typeof(IDictionary<,>);
            bool isCustomDict =
                !dictType.IsAbstract && !dictType.IsInterface &&
                dictType.DerivesOrImplementsGeneric(iDict) &&
                dictType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;

            if (isCustomDict)
            {
                var genericInterfaceType =
                    dictType.IsGenericType && dictType.GetGenericTypeDefinition() == iDict
                        ? dictType
                        : TypeMeta.GetGenericRealization(dictType, iDict)
                          ?? throw new InvalidOperationException($"Type has to be or implement {iDict.GetFriendlyName()}");
                meta = (genericInterfaceType.GenericTypeArguments[0], genericInterfaceType.GenericTypeArguments[1]);
                return true;
            }
            else
            {
                meta = default;
                return false;
            }
        }

        private static bool IsReadOnlyDictionary(Type dictType, out (Type keyType, Type valueType, ConstructorInfo ctor) meta)
        {
            meta = default;

            Type iReadOnlyDict = typeof(IReadOnlyDictionary<,>);
            bool isReadOnlyDict =
                !dictType.IsAbstract && !dictType.IsInterface &&
                dictType.DerivesOrImplementsGeneric(iReadOnlyDict);

            if (isReadOnlyDict)
            {
                var genericInterfaceType =
                    dictType.IsGenericType && dictType.GetGenericTypeDefinition() == iReadOnlyDict
                        ? dictType
                        : TypeMeta.GetGenericRealization(dictType, iReadOnlyDict)
                          ?? throw new InvalidOperationException($"Type has to be or implement {iReadOnlyDict.GetFriendlyName()}");
                Type keyType = genericInterfaceType.GenericTypeArguments[0],
                   valueType = genericInterfaceType.GenericTypeArguments[1];

                var iDictType = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);

                var ctor = dictType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(
                    c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.DerivesOrImplementsGeneric(iDictType)
                );
                if (ctor != null)
                {
                    meta = (keyType, valueType, ctor);
                    return true;
                }
            }
            return false;
        }

        public bool CanHandle(Type type) =>
            IsCustomDictionary(type, out var meta1) &&
            _transformerStore.IsSupportedForTransformation(meta1.keyType) &&
            _transformerStore.IsSupportedForTransformation(meta1.valueType)
            ||
            IsReadOnlyDictionary(type, out var meta2) &&
            _transformerStore.IsSupportedForTransformation(meta2.keyType) &&
            _transformerStore.IsSupportedForTransformation(meta2.valueType)
        ;

        public sbyte Priority => 51;
    }


    public abstract class CustomDictionaryTransformerBase<TKey, TValue, TDict> : DictionaryTransformerBase<TKey, TValue, TDict>
        where TDict : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly bool _supportsDeserializationLogic;

        protected CustomDictionaryTransformerBase(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings, bool supportsDeserializationLogic)
            : base(keyTransformer, valueTransformer, settings)
            => _supportsDeserializationLogic = supportsDeserializationLogic;


        protected override TDict ParseCore(in ReadOnlySpan<char> input)
        {
            var stream = ParsePairsStream(input);
            TDict result = GetDictionary(stream);

            if (_supportsDeserializationLogic && result is IDeserializationCallback callback)
                callback.OnDeserialization(this);

            return result;
        }

        protected abstract TDict GetDictionary(in ParsedPairSequence stream);

        public sealed override string ToString() => $"Transform custom {typeof(TDict).GetFriendlyName()} with ({typeof(TKey).GetFriendlyName()}, {typeof(TValue).GetFriendlyName()}) elements";
    }

    public sealed class CustomDictionaryTransformer<TKey, TValue, TDict> : CustomDictionaryTransformerBase<TKey, TValue, TDict>
        where TDict : IDictionary<TKey, TValue>, new()
    {
        public CustomDictionaryTransformer(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings, bool supportsDeserializationLogic)
            : base(keyTransformer, valueTransformer, settings, supportsDeserializationLogic) { }

        protected override TDict GetDictionary(in ParsedPairSequence stream)
        {
            var result = new TDict();
            PopulateDictionary(stream, result);
            return result;
        }

        public override TDict GetEmpty() => new TDict();
    }

    public sealed class ReadOnlyDictionaryTransformer<TKey, TValue, TDict> : CustomDictionaryTransformerBase<TKey, TValue, TDict>
        where TDict : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly Func<IDictionary<TKey, TValue>, TDict> _dictConversion;

        public ReadOnlyDictionaryTransformer(ITransformer<TKey> keyTransformer, ITransformer<TValue> valueTransformer, DictionarySettings settings, bool supportsDeserializationLogic, Func<IDictionary<TKey, TValue>, TDict> dictConversion)
            : base(keyTransformer, valueTransformer, settings, supportsDeserializationLogic)
            => _dictConversion = dictConversion;

        internal static Func<IDictionary<TKey, TValue>, TDict> GetDictConverter(ConstructorInfo ctorInfo)
        {
            Type keyType = typeof(TKey), valueType = typeof(TValue),
                iDictType = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);

            var param = Expression.Parameter(iDictType, "dict");
            var ctor = Expression.New(ctorInfo, param);

            var λ = Expression.Lambda<Func<IDictionary<TKey, TValue>, TDict>>(ctor, param);
            return λ.Compile();
        }

        protected override TDict GetDictionary(in ParsedPairSequence stream)
        {
            var innerDict = new Dictionary<TKey, TValue>();

            PopulateDictionary(stream, innerDict);

            var result = _dictConversion(innerDict);
            return result;
        }

        public override TDict GetEmpty() => _dictConversion(new Dictionary<TKey, TValue>());
    }
}
