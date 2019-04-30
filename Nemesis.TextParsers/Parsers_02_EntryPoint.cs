using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    public class TextTransformer
    {
        private readonly ConcurrentDictionary<Type, object> _transformerCache;
        private readonly IReadOnlyList<ICanCreateTransformer> _canParseByDelegateContracts;

        private TextTransformer(ConcurrentDictionary<Type, object> transformerCache, IReadOnlyList<ICanCreateTransformer> canParseByDelegateContracts)
        {
            _transformerCache = transformerCache;
            _canParseByDelegateContracts = canParseByDelegateContracts;
        }

        public static TextTransformer Default { get; } = GetDefaultTextTransformer();

        private static TextTransformer GetDefaultTextTransformer()
        {
            var transformerCache = new ConcurrentDictionary<Type, object>();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var byDelegateList = new List<ICanCreateTransformer>(8);

            foreach (var type in types)
            {
                if (typeof(ICanTransformType).IsAssignableFrom(type) &&
                    type.DerivesOrImplementsGeneric(typeof(ITransformer<>)))
                {
                    var instance = (ICanTransformType)Activator.CreateInstance(type);
                    transformerCache.TryAdd(instance.Type, instance);
                }
                else if (typeof(ICanCreateTransformer).IsAssignableFrom(type))
                {
                    byDelegateList.Add((ICanCreateTransformer)Activator.CreateInstance(type));
                }
            }

            byDelegateList.Sort((i1, i2) => i1.Priority.CompareTo(i2.Priority));

            var canParseByDelegateContracts = byDelegateList;

            return new TextTransformer(transformerCache, canParseByDelegateContracts);
        }


        /*public object GetTransformer(Type type)
        {
            var getTransformerMethod = typeof(TextTransformer).GetMethods()
                .SingleOrDefault(m => m.IsGenericMethod && m.Name == nameof(GetTransformer)) ??
                    throw new MissingMethodException(nameof(TextTransformer), nameof(GetTransformer));

            getTransformerMethod = getTransformerMethod.MakeGenericMethod(type);
            return getTransformerMethod.Invoke(null, null);
        }*/

        public ITransformer<TElement> GetTransformer<TElement>() =>
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), type =>
            {
                if (type.IsGenericTypeDefinition)
                    throw new NotSupportedException($"Parsing GenericTypeDefinition is not supported: {type.FullName}");

                foreach (var canParseByDelegate in _canParseByDelegateContracts)
                    if (canParseByDelegate.CanHandle(type))
                        return canParseByDelegate.CreateTransformer<TElement>();

                throw new NotSupportedException($"Type '{type.FullName}' is not supported for string transformations");
            });
    }
}
