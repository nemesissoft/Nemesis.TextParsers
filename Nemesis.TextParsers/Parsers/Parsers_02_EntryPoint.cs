using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    public interface ITransformerStore
    {
        ITransformer<TElement> GetTransformer<TElement>();
        ITransformer GetTransformer(Type elementType);
    }

    public static class TextTransformer
    {
        public static ITransformerStore Default { get; } = StandardTransformerStore.GetDefaultTextTransformer();
    }

    //TODO CreateTransformer() with context - relation to parent ITransformerStore 
    //TODO implement ReadOnlyStore:ITransformerStore (with with Dictionary cache) in Test project
    internal sealed class StandardTransformerStore : ITransformerStore
    {
        private readonly IReadOnlyList<ICanCreateTransformer> _canParseByDelegateContracts;
        private readonly ConcurrentDictionary<Type, ITransformer> _transformerCache;

        private StandardTransformerStore([NotNull] IReadOnlyList<ICanCreateTransformer> canParseByDelegateContracts, ConcurrentDictionary<Type, ITransformer> transformerCache = null)
        {
            _canParseByDelegateContracts = canParseByDelegateContracts ?? throw new ArgumentNullException(nameof(canParseByDelegateContracts));
            _transformerCache = transformerCache ?? new ConcurrentDictionary<Type, ITransformer>();
        }

        internal static ITransformerStore GetDefaultTextTransformer()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var byDelegateList = new List<ICanCreateTransformer>(16);

            foreach (var type in types)
                if (typeof(ICanCreateTransformer).IsAssignableFrom(type))
                    byDelegateList.Add((ICanCreateTransformer)Activator.CreateInstance(type));

            byDelegateList.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

            var canParseByDelegateContracts = byDelegateList;

            return new StandardTransformerStore(canParseByDelegateContracts);
        }

        public ITransformer<TElement> GetTransformer<TElement>() =>
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), t=>GetTransformerCore<TElement>());

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
            Type type = typeof(TElement);

            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

            foreach (var canParseByDelegate in _canParseByDelegateContracts)
                if (canParseByDelegate.CanHandle(type))
                    return canParseByDelegate.CreateTransformer<TElement>();

            throw new NotSupportedException($"Type '{type.GetFriendlyName()}' is not supported for string transformations. Provide appropriate chain of responsibility");
        }
    }
}
