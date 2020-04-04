using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers
{
    public interface ITransformerStore
    {
        ITransformer<TElement> GetTransformer<TElement>();
        ITransformer GetTransformer(Type type);

        bool IsSupportedForTransformation(Type type);

        //TODO can that be done better ?
        SettingsStore SettingsStore { get; }
    }

    public static class TextTransformer
    {
        public static ITransformerStore Default { get; } =
            StandardTransformerStore.GetDefault(SettingsStoreBuilder.GetDefault().Build());
        
        public static ITransformerStore GetDefaultStoreWith(SettingsStore settingsStore) =>
            StandardTransformerStore.GetDefault(settingsStore);
    }

    internal sealed class StandardTransformerStore : ITransformerStore
    {
        private readonly IEnumerable<ICanCreateTransformer> _transformerCreators;
        public SettingsStore SettingsStore { get; }


        private readonly ConcurrentDictionary<Type, ITransformer> _transformerCache = new ConcurrentDictionary<Type, ITransformer>();

        public StandardTransformerStore([NotNull] IEnumerable<ICanCreateTransformer> transformerCreators,
            [NotNull] SettingsStore settingsStore)
        {
            _transformerCreators = transformerCreators ?? throw new ArgumentNullException(nameof(transformerCreators));
            SettingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        }

        public static ITransformerStore GetDefault(SettingsStore settingsStore)
        {
            static bool IsUnique<TElement>(IEnumerable<TElement> list)
            {
                var diffChecker = new HashSet<TElement>();
                return list.All(diffChecker.Add);
            }

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var transformerCreators = new List<ICanCreateTransformer>(16);
            var store = new StandardTransformerStore(transformerCreators, settingsStore);

            transformerCreators.AddRange(
                from type in types
                where typeof(ICanCreateTransformer).IsAssignableFrom(type)
                select CreateTransformerCreator(type, store, settingsStore)
            );

            if (!IsUnique(transformerCreators.Select(d => d.Priority)))
                throw new InvalidOperationException($"All priorities registered via {nameof(ICanCreateTransformer)} have to be unique");

            transformerCreators.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

            return store;
        }

        private static ICanCreateTransformer CreateTransformerCreator(Type type, ITransformerStore transformerStore, SettingsStore settingsStore)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (ctors.Length != 1)
                throw new NotSupportedException($"Only single constructor is supported for transformer creator: {type.GetFriendlyName()}");

            var ctor = ctors[0];
            var @params = ctor.GetParameters();

            if (@params.Length == 0)
                return (ICanCreateTransformer)Activator.CreateInstance(type, true);
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
                            $"Only supported arguments for auto-injection to {type.GetFriendlyName()} are {nameof(ITransformerStore)} or {nameof(ISettings)} implementations");
                }
                
                var arguments = @params.Select(p => p.ParameterType)
                    .Select(GetArgument)
                    .ToArray();

                return (ICanCreateTransformer)Activator.CreateInstance(type, arguments);
            }
        }

        #region GetTransformer

        public ITransformer<TElement> GetTransformer<TElement>() =>
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), t => GetTransformerCore<TElement>());

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

            foreach (var creator in _transformerCreators)
                if (creator.CanHandle(type))
                    return creator.CreateTransformer<TElement>();

            throw new NotSupportedException($"Type '{type.GetFriendlyName()}' is not supported for string transformations. Provide appropriate chain of responsibility");
        }
        #endregion


        #region IsSupported

        private readonly ConcurrentDictionary<Type, bool> _isSupportedCache = new ConcurrentDictionary<Type, bool>();

        public bool IsSupportedForTransformation(Type type) =>
            type != null &&
            _isSupportedCache.GetOrAdd(type, IsSupportedForTransformationCore);

        private bool IsSupportedForTransformationCore(Type type) =>
            !type.IsGenericTypeDefinition &&
            _transformerCreators.FirstOrDefault(c => c.CanHandle(type)) is { } creator &&
            !(creator is AnyTransformerCreator);

        #endregion
    }
}
