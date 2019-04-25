using System;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class ArrayTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TArray> CreateTransformer<TArray>()
        {
            var elementType = typeof(TArray).GetElementType();

            var transType = typeof(InnerArrayTransformer<>).MakeGenericType(elementType);

            return (ITransformer<TArray>)Activator.CreateInstance(transType);
        }
        
        private class InnerArrayTransformer<TElement> : ITransformer<TElement[]>
        {
            public TElement[] Parse(ReadOnlySpan<char> input) =>
                    //input.IsEmpty ? default :
                    SpanCollectionSerializer.DefaultInstance.ParseArray<TElement>(input);

            public string Format(TElement[] array) =>
                    //array == null ? null :
                    SpanCollectionSerializer.DefaultInstance.FormatCollection(array);

            public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()}[]";
        }

        public bool CanHandle(Type type) => type.IsArray;

        public sbyte Priority => 60;
    }
}
