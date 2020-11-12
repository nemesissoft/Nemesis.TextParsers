﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class CollectionTransformerCreator : ICanCreateTransformer //standard .net framework collection handler 
    {
        private readonly ITransformerStore _transformerStore;
        private readonly CollectionSettings _settings;
        public CollectionTransformerCreator(ITransformerStore transformerStore, CollectionSettings settings)
        {
            _transformerStore = transformerStore;
            _settings = settings;
        }

        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            if (!TryGetElements(collectionType, out _, out var kind, out var elementType) || elementType == null)
                throw new NotSupportedException($"Type {collectionType.GetFriendlyName()} is not supported by {GetType().Name}");


            var createMethod = Method.OfExpression<
                Func<CollectionTransformerCreator, CollectionKind, ITransformer<List<int>>>
            >((@this, k) => @this.CreateCollectionTransformer<int, List<int>>(k)
            ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(elementType, collectionType);

            return (ITransformer<TCollection>)createMethod.Invoke(this, new object[] { kind });
        }

        private ITransformer<TCollection> CreateCollectionTransformer<TElement, TCollection>(CollectionKind kind)
            where TCollection : IEnumerable<TElement>
            => new CollectionTransformer<TElement, TCollection>(
                _transformerStore.GetTransformer<TElement>(),
                _settings, kind
            );

        public bool CanHandle(Type type) =>
            TryGetElements(type, out bool isArray, out var kind, out var elementType) &&
            !isArray &&
            kind != CollectionKind.Unknown &&
            _transformerStore.IsSupportedForTransformation(elementType);

        private static bool TryGetElements(Type type, out bool isArray, out CollectionKind kind, out Type elementType)
        {
            if (CollectionMetaHelper.IsTypeSupported(type))
            {
                (isArray, kind, elementType) = CollectionMetaHelper.GetCollectionMeta(type);
                return true;
            }
            else
            {
                (isArray, kind, elementType) = (default, default, default);
                return false;
            }
        }

        public sbyte Priority => 70;
        
        public override string ToString() =>
            $"Create transformer for Collection-like structures with settings:{_settings}";
    }

    public sealed class CollectionTransformer<TElement, TCollection> : EnumerableTransformerBase<TElement, TCollection>
        where TCollection : IEnumerable<TElement>
    {
        private readonly CollectionKind _kind;
        public CollectionTransformer(ITransformer<TElement> elementTransformer,
            CollectionSettings settings, CollectionKind kind) : base(elementTransformer, settings)
            => _kind = kind;


        protected override TCollection ParseCore(in ReadOnlySpan<char> input)
        {
            var stream = ParseStream(input);
            var capacity = Settings.GetCapacity(input);

            switch (_kind)
            {
                case CollectionKind.List:
                case CollectionKind.ReadOnlyCollection:
                    {
                        var result = new List<TElement>(capacity);

                        foreach (var part in stream)
                            result.Add(part.ParseWith(ElementTransformer));

                        return (TCollection)(
                                 _kind == CollectionKind.List
                                     ? (IReadOnlyCollection<TElement>)result
                                     : result.AsReadOnly()
                        );
                    }
                case CollectionKind.HashSet:
                case CollectionKind.SortedSet:
                    {
                        ISet<TElement> result = _kind == CollectionKind.HashSet
                            ? (ISet<TElement>)new HashSet<TElement>(
#if NETSTANDARD2_0 || NETFRAMEWORK

#else
                            capacity
#endif
                        )
                            : new SortedSet<TElement>();

                        foreach (var part in stream)
                            result.Add(part.ParseWith(ElementTransformer));

                        return (TCollection)(IReadOnlyCollection<TElement>)result;
                    }
                case CollectionKind.LinkedList:
                    {
                        var result = new LinkedList<TElement>();

                        foreach (var part in stream)
                            result.AddLast(part.ParseWith(ElementTransformer));

                        return (TCollection)(IReadOnlyCollection<TElement>)result;
                    }
                case CollectionKind.Stack:
                    {
                        var initialBuffer = ArrayPool<TElement>.Shared.Rent(capacity);
                        var accumulator = new ValueSequenceBuilder<TElement>(initialBuffer);
                        try
                        {
                            foreach (var part in stream)
                                accumulator.Append(part.ParseWith(ElementTransformer));

                            var elements = accumulator.AsSpan();


                            var result = new Stack<TElement>(elements.Length);
                            for (int i = elements.Length - 1; i >= 0; i--)
                                result.Push(elements[i]);

                            return (TCollection)(IReadOnlyCollection<TElement>)result;
                        }
                        finally
                        {
                            accumulator.Dispose();
                            ArrayPool<TElement>.Shared.Return(initialBuffer);
                        }
                    }
                case CollectionKind.Queue:
                    {
                        var result = new Queue<TElement>(capacity);

                        foreach (var part in stream)
                            result.Enqueue(part.ParseWith(ElementTransformer));

                        return (TCollection)(IReadOnlyCollection<TElement>)result;
                    }
                case CollectionKind.ObservableCollection:
                    {
                        var result = new ObservableCollection<TElement>();

                        foreach (var part in stream)
                            result.Add(part.ParseWith(ElementTransformer));

                        return (TCollection)(IReadOnlyCollection<TElement>)result;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(_kind), _kind, $"{nameof(_kind)} = '{nameof(CollectionKind)}.{nameof(CollectionKind.Unknown)}' is not supported");
            }
        }


        public override TCollection GetEmpty() =>
            (TCollection)(_kind switch
            {
                CollectionKind.ReadOnlyCollection => new ReadOnlyCollection<TElement>(new List<TElement>(0)),
                CollectionKind.HashSet => new HashSet<TElement>(),
                CollectionKind.SortedSet => new SortedSet<TElement>(),
                CollectionKind.LinkedList => new LinkedList<TElement>(),
                CollectionKind.Stack => new Stack<TElement>(0),
                CollectionKind.Queue => new Queue<TElement>(0),
                CollectionKind.ObservableCollection => new ObservableCollection<TElement>(),
                CollectionKind.Unknown => throw new NotSupportedException($"Collection kind {_kind} is not supported for empty element query"),
                //CollectionKind.List
                _ => (IEnumerable<TElement>)new List<TElement>(0),
            });


        public override string ToString() => $"Transform {typeof(TCollection).GetFriendlyName()} AS {_kind}<{typeof(TElement).GetFriendlyName()}>";
    }
}
