using System;
using System.Buffers;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    /*TODO               
     Parse - remove []
        Format - add [] start + end - tests
        */

    [UsedImplicitly]
    public sealed class ArrayTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly ArraySettings _settings;
        public ArrayTransformerCreator(ITransformerStore transformerStore, ArraySettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }


        public ITransformer<TArray> CreateTransformer<TArray>()
        {
            if (!TryGetElements(typeof(TArray), out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {typeof(TArray).GetFriendlyName()} is not supported by {GetType().Name}");


            var createMethod = Method.OfExpression<
                Func<ArrayTransformerCreator, ITransformer<int[]>>
            >(@this => @this.CreateArrayTransformer<int>()
            ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(elementType);

            return (ITransformer<TArray>)createMethod.Invoke(this, null);
        }

        private ITransformer<TElement[]> CreateArrayTransformer<TElement>() =>
            new ArrayTransformer<TElement>(
                _transformerStore.GetTransformer<TElement>(),
                _settings
        );



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

    public sealed class ArrayTransformer<TElement> : EnumerableTransformerBase<TElement, TElement[]>
    {
        public ArrayTransformer(ITransformer<TElement> elementTransformer, ArraySettings settings)
            : base(elementTransformer, settings) { }

        protected override TElement[] ParseCore(in ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
                return Array.Empty<TElement>();

            var stream = ParseStream(input);

            int capacity = Settings.GetCapacity(input);

            var initialBuffer = ArrayPool<TElement>.Shared.Rent(capacity);
            var accumulator = new ValueSequenceBuilder<TElement>(initialBuffer);
            try
            {
                foreach (var part in stream)
                    accumulator.Append(part.ParseWith(ElementTransformer));

                return accumulator.AsSpan().ToArray();
            }
            finally
            {
                accumulator.Dispose();
                ArrayPool<TElement>.Shared.Return(initialBuffer);
            }
        }


        public override TElement[] GetEmpty() => Array.Empty<TElement>();
    }
}
