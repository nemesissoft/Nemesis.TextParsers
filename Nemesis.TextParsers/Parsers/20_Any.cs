using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class AnyTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>() =>
            throw new NotSupportedException(
                $"{typeof(TElement).GetFriendlyName()} is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string");

        public bool CanHandle(Type type) => true;

        public sbyte Priority => sbyte.MaxValue;
    }
}
