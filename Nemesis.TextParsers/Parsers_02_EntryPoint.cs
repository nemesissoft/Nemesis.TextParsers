using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    public interface ITextTransformer
    {
        ITransformer<TElement> GetTransformer<TElement>();
        ITransformer GetTransformer(Type elementType);
    }

    //TODO implementation with Dictionary cache - settings file ?
    //TODO CreateTransformer() with context - relation to parent 
    public sealed class TextTransformer : ITextTransformer
    {
        private readonly IReadOnlyList<ICanCreateTransformer> _canParseByDelegateContracts;
        private readonly ConcurrentDictionary<Type, ITransformer> _transformerCache;
        
        private TextTransformer([NotNull] IReadOnlyList<ICanCreateTransformer> canParseByDelegateContracts, ConcurrentDictionary<Type, ITransformer> transformerCache = null)
        {
            _canParseByDelegateContracts = canParseByDelegateContracts ?? throw new ArgumentNullException(nameof(canParseByDelegateContracts));
            _transformerCache = transformerCache ?? new ConcurrentDictionary<Type, ITransformer>();
        }

        public static ITextTransformer Default { get; } = GetDefaultTextTransformer();

        private static ITextTransformer GetDefaultTextTransformer()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var byDelegateList = new List<ICanCreateTransformer>(16);

            foreach (var type in types)
                if (typeof(ICanCreateTransformer).IsAssignableFrom(type))
                    byDelegateList.Add((ICanCreateTransformer)Activator.CreateInstance(type));

            byDelegateList.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

            var canParseByDelegateContracts = byDelegateList;

            return new TextTransformer(canParseByDelegateContracts);
        }

        public ITransformer<TElement> GetTransformer<TElement>() =>
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), GetTransformerCore<TElement>);

        public ITransformer GetTransformer(Type elementType) =>
            _transformerCache.GetOrAdd(elementType, type => 
                (ITransformer) _getTransformerMethodGeneric.MakeGenericMethod(type).Invoke(this, new object[] {type})
            );

        private static readonly MethodInfo _getTransformerMethodGeneric =
            typeof(TextTransformer)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .SingleOrDefault(m => m.IsGenericMethod && m.Name == nameof(GetTransformerCore)) ??
            throw new MissingMethodException(nameof(TextTransformer), nameof(GetTransformerCore));

        private ITransformer<TElement> GetTransformerCore<TElement>(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

            foreach (var canParseByDelegate in _canParseByDelegateContracts)
                if (canParseByDelegate.CanHandle(type))
                    return canParseByDelegate.CreateTransformer<TElement>();

            throw new NotSupportedException($"Type '{type.FullName}' is not supported for string transformations");
        }
    }
}
