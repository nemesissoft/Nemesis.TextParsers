using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    public interface ITextTransformer
    {
        ITransformer<TElement> GetTransformer<TElement>();

        //object Parse(ReadOnlySpan<char> input);
        //string Format(object element);
    }

    public sealed class TextTransformer : ITextTransformer
    {
        private readonly IReadOnlyList<ICanCreateTransformer> _canParseByDelegateContracts;
        private readonly ConcurrentDictionary<Type, object> _transformerCache;

        private TextTransformer([NotNull] IReadOnlyList<ICanCreateTransformer> canParseByDelegateContracts, ConcurrentDictionary<Type, object> transformerCache = null)
        {
            _canParseByDelegateContracts = canParseByDelegateContracts ?? throw new ArgumentNullException(nameof(canParseByDelegateContracts));
            _transformerCache = transformerCache ?? new ConcurrentDictionary<Type, object>();
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
            (ITransformer<TElement>)_transformerCache.GetOrAdd(typeof(TElement), type =>
            {
                if (type.IsGenericTypeDefinition)
                    throw new NotSupportedException($"Parsing GenericTypeDefinition is not supported: {type.FullName}");

                foreach (var canParseByDelegate in _canParseByDelegateContracts)
                    if (canParseByDelegate.CanHandle(type))
                        return canParseByDelegate.CreateTransformer<TElement>();

                throw new NotSupportedException($"Type '{type.FullName}' is not supported for string transformations");
            });

        /*public object Parse(ReadOnlySpan<char> input)
        {

        }

        public string Format(object element)
        {
            return element is null ? 
                null : 
                GetTransformerCore(element.GetType())
        }

        private object GetTransformerCore(Type objectType) =>
            _transformerCache.GetOrAdd(objectType, type =>
            {
                if (type.IsGenericTypeDefinition)
                    throw new NotSupportedException($"Parsing GenericTypeDefinition is not supported: {type.FullName}");

                MethodInfo createTransformerMethod = typeof(ICanCreateTransformer)
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Single(m => m.Name == nameof(ICanCreateTransformer.CreateTransformer))
                        .MakeGenericMethod(type);

                foreach (var canParseByDelegate in _canParseByDelegateContracts)
                    if (canParseByDelegate.CanHandle(type))
                        return createTransformerMethod.Invoke(canParseByDelegate, null);

                throw new NotSupportedException($"Type '{type.FullName}' is not supported for string transformations");
            });*/



        /*public object GetTransformer(Type type)
        {
            var getTransformerMethod = typeof(TextTransformer).GetMethods()
                .SingleOrDefault(m => m.IsGenericMethod && m.Name == nameof(GetTransformer)) ??
                    throw new MissingMethodException(nameof(TextTransformer), nameof(GetTransformer));

            getTransformerMethod = getTransformerMethod.MakeGenericMethod(type);
            return getTransformerMethod.Invoke(null, null);
        }*/
    }
}
