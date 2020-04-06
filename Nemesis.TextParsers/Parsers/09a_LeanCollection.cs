﻿using System;
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

            var transType = typeof(LeanCollectionTransformer<>).MakeGenericType(elementType);

            return (ITransformer<TLean>)Activator.CreateInstance(transType);
        }

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

    public class LeanCollectionTransformer<TElement> : TransformerBase<LeanCollection<TElement>>
    {
        protected override LeanCollection<TElement> ParseCore(in ReadOnlySpan<char> input) =>
            SpanCollectionSerializer.DefaultInstance.ParseLeanCollection<TElement>(input);

        public override string Format(LeanCollection<TElement> coll) =>
            SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);
    }
}
