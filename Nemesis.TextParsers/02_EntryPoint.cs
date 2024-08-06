using System.Collections.Concurrent;
using JetBrains.Annotations;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers;

public interface ITransformerStore
{
    ITransformer<TElement> GetTransformer<TElement>();
    ITransformer GetTransformer(Type type);

    bool IsSupportedForTransformation(Type type);
    string DescribeHandlerMatch(Type type);

    SettingsStore SettingsStore { get; }
}

public static class TextTransformer
{
    public static readonly IReadOnlyCollection<ISettings> DefaultSettings = [
        CollectionSettings.Default,
        ArraySettings.Default,
        DictionarySettings.Default,
        EnumSettings.Default,
        FactoryMethodSettings.Default,
        ValueTupleSettings.Default,
        KeyValuePairSettings.Default,
        DeconstructableSettings.Default
    ];

    public static readonly IReadOnlyCollection<Type> DefaultHandlers = [
        typeof(SimpleTransformerHandler),
        typeof(KeyValuePairTransformerHandler),
        typeof(ValueTupleTransformerHandler),
        typeof(TransformableHandler),
        typeof(ConventionTransformerHandler),
        typeof(TextFactoryTransformerHandler),
        typeof(EnumTransformerHandler),
        typeof(NullableTransformerHandler),
        typeof(DictionaryTransformerHandler),
        typeof(CustomDictionaryTransformerHandler),
        typeof(ArrayTransformerHandler),
        typeof(CollectionTransformerHandler),
        typeof(LeanCollectionTransformerHandler),
        typeof(CustomCollectionTransformerHandler),
        typeof(DeconstructionTransformerHandler),
        typeof(TypeConverterTransformerHandler)
    ];

    //TODO remove that to test DI
    public static ITransformerStore Default { get; } =
        StandardTransformerStore.Create(DefaultHandlers, new(DefaultSettings));

    public static ITransformerStore GetDefaultStoreWith(SettingsStore settingsStore) =>
        StandardTransformerStore.Create(DefaultHandlers, settingsStore);
}

public sealed class StandardTransformerStore : ITransformerStore
{
    private readonly IEnumerable<ITransformerHandler> _transformerHandlers;
    public SettingsStore SettingsStore { get; }


    private readonly ConcurrentDictionary<Type, ITransformer> _transformerCache = new();

    public StandardTransformerStore([NotNull] IEnumerable<ITransformerHandler> transformerHandlers,
        [NotNull] SettingsStore settingsStore)
    {
        _transformerHandlers = transformerHandlers ?? throw new ArgumentNullException(nameof(transformerHandlers));
        SettingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public static ITransformerStore Create(IEnumerable<Type> transformerHandlerTypes, SettingsStore settingsStore)
    {
        static bool IsUnique<TElement>(IEnumerable<TElement> list)
        {
            var diffChecker = new HashSet<TElement>();
            return list.All(diffChecker.Add);
        }

        var handlers = new List<ITransformerHandler>(16);
        var store = new StandardTransformerStore(handlers, settingsStore);

        handlers.AddRange(
            transformerHandlerTypes.Select(t => CreateTransformerHandler(t, store, settingsStore))
        );

        if (!IsUnique(handlers.Select(d => d.Priority)))
            throw new InvalidOperationException($"All priorities registered via {nameof(StandardTransformerStore)} have to be unique");

        handlers.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

        return store;
    }

    private static ITransformerHandler CreateTransformerHandler(Type handlerType, ITransformerStore transformerStore, SettingsStore settingsStore)
    {
        var ctors = handlerType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ctors.Length != 1)
            throw new NotSupportedException($"Only single constructor is supported for transformer handler: {handlerType.GetFriendlyName()}");

        var ctor = ctors[0];
        var @params = ctor.GetParameters();

        if (@params.Length == 0)
            return (ITransformerHandler)Activator.CreateInstance(handlerType, true);
        else
        {
            object GetArgument(Type argType)
            {
                if (argType == typeof(ITransformerStore))
                    return transformerStore;
                else if (typeof(ISettings).IsAssignableFrom(argType))
                    return settingsStore.GetSettingsFor(argType);
                else
                    throw new NotSupportedException(
                        $"Only supported arguments for auto-injection to {handlerType.GetFriendlyName()} are {nameof(ITransformerStore)} or {nameof(ISettings)} implementations");
            }

            var arguments = @params.Select(p => p.ParameterType)
                .Select(GetArgument)
                .ToArray();

            return (ITransformerHandler)Activator.CreateInstance(handlerType, arguments);
        }
    }

    #region GetTransformer

    public ITransformer<TElement> GetTransformer<TElement>() =>
        (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), _ => GetTransformerCore<TElement>());

    public ITransformer GetTransformer(Type elementType) =>
        _transformerCache.GetOrAdd(elementType, type =>
            (ITransformer)_getTransformerMethodGeneric.MakeGenericMethod(type).Invoke(this, null)
        );

    static StandardTransformerStore() =>
        _getTransformerMethodGeneric = Method.OfExpression<Func<StandardTransformerStore, ITransformer<int>>>(
            test => test.GetTransformerCore<int>()
        ).GetGenericMethodDefinition();


    private static readonly MethodInfo _getTransformerMethodGeneric;

    private ITransformer<TElement> GetTransformerCore<TElement>()
    {
        var type = typeof(TElement);

        if (type.IsGenericTypeDefinition)
            throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

        foreach (var handler in _transformerHandlers)
            if (handler.CanHandle(type))
                return handler.CreateTransformer<TElement>();

        throw new NotSupportedException($"Type '{type.GetFriendlyName()}' is not supported for text transformations. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string");
    }
    #endregion


    #region IsSupported

    private readonly ConcurrentDictionary<Type, bool> _isSupportedCache = new();

    public bool IsSupportedForTransformation(Type type) =>
        type != null &&
        _isSupportedCache.GetOrAdd(type, IsSupportedForTransformationCore);

    private bool IsSupportedForTransformationCore(Type type) =>
        !type.IsGenericTypeDefinition &&
        _transformerHandlers.Any(handler => handler.CanHandle(type));

    #endregion

    #region Diagnostics

    public string DescribeHandlerMatch(Type type)
    {
        var sb = new StringBuilder();

        foreach (var handler in _transformerHandlers)
            if (handler.CanHandle(type))
            {
                sb.AppendLine($"Handled by {handler.GetType().Name}: {handler.DescribeHandlerMatch()}");
                break;
            }
            else
            {
                sb.AppendLine($"MISS -- {handler.DescribeHandlerMatch()}");
            }

        return sb.ToString();
    }

    #endregion
}
