using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    public interface ITransformerStore
    {
        ITransformer<TElement> GetTransformer<TElement>();
        ITransformer GetTransformer(Type type);

        bool IsSupportedForTransformation(Type type);
    }

    public static class TextTransformer
    {
        public static ITransformerStore Default { get; } = StandardTransformerStore.GetDefaultTextTransformer();
    }

    //TODO CreateTransformer() with context - relation to parent ITransformerStore 
    //TODO implement ReadOnlyStore:ITransformerStore (with with Dictionary cache) in Test project
    internal sealed class StandardTransformerStore : ITransformerStore
    {
        private readonly IReadOnlyList<ICanCreateTransformer> _transformerCreators;
        private readonly ConcurrentDictionary<Type, ITransformer> _transformerCache;

        private StandardTransformerStore([NotNull] IReadOnlyList<ICanCreateTransformer> transformerCreators,
            ConcurrentDictionary<Type, ITransformer> transformerCache = null)
        {
            _transformerCreators = transformerCreators ?? throw new ArgumentNullException(nameof(transformerCreators));
            _transformerCache = transformerCache ?? new ConcurrentDictionary<Type, ITransformer>();
        }

        internal static ITransformerStore GetDefaultTextTransformer()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var transformerCreators = new List<ICanCreateTransformer>(16);

            foreach (var type in types)
                if (typeof(ICanCreateTransformer).IsAssignableFrom(type))
                    transformerCreators.Add((ICanCreateTransformer)Activator.CreateInstance(type));

            if (!IsUnique(transformerCreators.Select(d => d.Priority)))
                throw new InvalidOperationException($"All priorities registered via {nameof(ICanCreateTransformer)} have to be unique");


            transformerCreators.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

            return new StandardTransformerStore(transformerCreators);
        }

        private static bool IsUnique<TElement>(IEnumerable<TElement> list)
        {
            var diffChecker = new HashSet<TElement>();
            return list.All(diffChecker.Add);
        }

        public ITransformer<TElement> GetTransformer<TElement>() =>
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), t => GetTransformerCore<TElement>());

        public ITransformer GetTransformer(Type elementType) =>
            _transformerCache.GetOrAdd(elementType, type =>
                (ITransformer)_getTransformerMethodGeneric.MakeGenericMethod(type).Invoke(this, null)
            );

        public bool IsSupportedForTransformation(Type type) =>
            !type.IsGenericTypeDefinition &&
            _transformerCreators.FirstOrDefault(c => c.CanHandle(type)) is { } creator &&
            !(creator is AnyTransformerCreator);


        static StandardTransformerStore() =>
            _getTransformerMethodGeneric = Method.OfExpression<Func<StandardTransformerStore, ITransformer<int>>>(
                test => test.GetTransformerCore<int>()
            ).GetGenericMethodDefinition();


        private static readonly MethodInfo _getTransformerMethodGeneric;

        private ITransformer<TElement> GetTransformerCore<TElement>()
        {
            Type type = typeof(TElement);

            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

            foreach (var creator in _transformerCreators)
                if (creator.CanHandle(type))
                    return creator.CreateTransformer<TElement>();

            throw new NotSupportedException($"Type '{type.GetFriendlyName()}' is not supported for string transformations. Provide appropriate chain of responsibility");
        }
    }
}
