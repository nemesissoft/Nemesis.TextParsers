using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class ArrayTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        public ArrayTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;


        public ITransformer<TArray> CreateTransformer<TArray>()
        {
            if (!TryGetElements(typeof(TArray), out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {typeof(TArray).GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = typeof(InnerArrayTransformer<>).MakeGenericType(elementType);

            return (ITransformer<TArray>)Activator.CreateInstance(transType);
        }

        private sealed class InnerArrayTransformer<TElement> : TransformerBase<TElement[]>
        {
            protected override TElement[] ParseCore(in ReadOnlySpan<char> input) =>
                    SpanCollectionSerializer.DefaultInstance.ParseArray<TElement>(input);

            public override string Format(TElement[] array) =>
                SpanCollectionSerializer.DefaultInstance.FormatCollection(array);

            public override TElement[] GetEmpty() => Array.Empty<TElement>();

            public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()}[]";
        }

        public bool CanHandle(Type type) =>
            TryGetElements(type, out var elementType) &&
            _transformerStore.IsSupportedForTransformation(elementType)
        ;

        private static bool TryGetElements(Type type, out Type elementType)
        {
            if (type.IsArray &&
                type.GetArrayRank() == 1 /*do not support multi dimension arrays - jagged arrays should be preferred anyway */
            )
            {
                elementType = type.GetElementType();
                return true;
            }
            else
            {
                elementType = null;
                return false;
            }
        }

        public sbyte Priority => 60;
    }
}
