using System;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class LeanCollectionTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        private readonly CollectionSettings _settings;
        public LeanCollectionTransformerCreator(ITransformerStore transformerStore, CollectionSettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }

        public ITransformer<TLean> CreateTransformer<TLean>()
        {
            if (!TryGetElements(typeof(TLean), out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {typeof(TLean).GetFriendlyName()} is not supported by {GetType().Name}");

            var createMethod = Method.OfExpression<
                Func<LeanCollectionTransformerCreator, ITransformer<LeanCollection<int>>>
            >(@this => @this.CreateLeanCollectionTransformer<int>()
            ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(elementType);

            return (ITransformer<TLean>)createMethod.Invoke(this, null);
        }

        private ITransformer<LeanCollection<TElement>> CreateLeanCollectionTransformer<TElement>() =>
            new LeanCollectionTransformer<TElement>(_transformerStore.GetTransformer<TElement>(), _settings);

        public bool CanHandle(Type type) =>
            TryGetElements(type, out var elementType) &&
            _transformerStore.IsSupportedForTransformation(elementType);

        private static bool TryGetElements(Type type, out Type elementType)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition &&
                TypeMeta.TryGetGenericRealization(type, typeof(LeanCollection<>), out var lean))
            {
                elementType = lean.GenericTypeArguments[0];
                return true;
            }
            else
            {
                elementType = null;
                return false;
            }
        }

        public sbyte Priority => 71;
    }

    public sealed class LeanCollectionTransformer<TElement> : TransformerBase<LeanCollection<TElement>>
    {
        private readonly ITransformer<TElement> _elementTransformer;
        private readonly CollectionSettings _settings;
        public LeanCollectionTransformer(ITransformer<TElement> elementTransformer, CollectionSettings settings)
        {
            _elementTransformer = elementTransformer;
            _settings = settings;
        }

        protected override LeanCollection<TElement> ParseCore(in ReadOnlySpan<char> input)
        {
            /* if (_start.HasValue || _end.HasValue) text = text.UnParenthesize(_start, _end, "Dictionary");*/
            return SpanCollectionSerializer.DefaultInstance.ParseLeanCollection<TElement>(input);
        }

        public override string Format(LeanCollection<TElement> coll)
        {
            if (coll.Size == 0) return "";

            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            try
            {
                if (_settings.Start.HasValue)
                    accumulator.Append(_settings.Start.Value);

                var enumerator = coll.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string elementText = _elementTransformer.Format(enumerator.Current);
                    if (elementText == null)
                        accumulator.Append(_settings.NullElementMarker);
                    else
                    {
                        foreach (char c in elementText)
                        {
                            if (c == _settings.EscapingSequenceStart ||
                                c == _settings.NullElementMarker ||
                                c == _settings.ListDelimiter)
                                accumulator.Append(_settings.EscapingSequenceStart);
                            accumulator.Append(c);
                        }
                    }
                    accumulator.Append(_settings.ListDelimiter);
                }

                accumulator.Shrink();

                if (_settings.End.HasValue)
                    accumulator.Append(_settings.End.Value);

                return accumulator.ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}
