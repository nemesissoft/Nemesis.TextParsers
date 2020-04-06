using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers
{
/*TODO
 LeanColl - format
 other - format 

        tests 
Add CollectionHelper like TupleHelper 
        Format - add []
 Parse - remove []
 
        */

    [UsedImplicitly]
    public sealed class ArrayTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly CollectionSettings _settings;
        public ArrayTransformerCreator(ITransformerStore transformerStore, CollectionSettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }


        public ITransformer<TArray> CreateTransformer<TArray>()
        {
            if (!TryGetElements(typeof(TArray), out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {typeof(TArray).GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = typeof(ArrayTransformer<>).MakeGenericType(elementType);

            return (ITransformer<TArray>)Activator.CreateInstance(transType);
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

    public sealed class ArrayTransformer<TElement> : TransformerBase<TElement[]>
    {
        protected override TElement[] ParseCore(in ReadOnlySpan<char> input) =>
            SpanCollectionSerializer.DefaultInstance.ParseArray<TElement>(input);

        public override string Format(TElement[] array) =>
            SpanCollectionSerializer.DefaultInstance.FormatCollection(array);

        public override TElement[] GetEmpty() => Array.Empty<TElement>();
    }
}
