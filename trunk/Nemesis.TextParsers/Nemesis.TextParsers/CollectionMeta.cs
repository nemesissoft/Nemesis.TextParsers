﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    public readonly struct CollectionMeta : IEquatable<CollectionMeta>
    {
        public bool IsArray { get; }
        public CollectionKind Kind { get; }
        public Type ElementType { get; }

        public bool IsValid => (IsArray || Kind != CollectionKind.Unknown) && ElementType != null;

        public CollectionMeta(bool isArray, CollectionKind kind, Type elementType)
        {
            if (!Enum.IsDefined(typeof(CollectionKind), kind))
                throw new InvalidEnumArgumentException(nameof(kind), (int)kind, typeof(CollectionKind));

            IsArray = isArray;
            Kind = kind;
            ElementType = elementType;
        }

        public void Deconstruct(out bool isArray, out CollectionKind kind, out Type elementType)
        {
            isArray = IsArray;
            kind = Kind;
            elementType = ElementType;
        }

        public override string ToString() => $"Array:{IsArray}, {Kind}, {ElementType?.FullName}";

        #region Equals
        public bool Equals(CollectionMeta other) =>
            IsArray == other.IsArray && Kind == other.Kind && ElementType == other.ElementType;

        public override bool Equals(object obj) =>
            !(obj is null) && obj is CollectionMeta other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsArray.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ (ElementType != null ? ElementType.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(CollectionMeta left, CollectionMeta right) => left.Equals(right);

        public static bool operator !=(CollectionMeta left, CollectionMeta right) => !left.Equals(right);
        #endregion

        public static CollectionMeta GetCollectionMeta(Type collectionType)
        {
            if (collectionType != null)
            {
                if (collectionType.IsArray && collectionType.GetElementType() is Type arrayElementType)
                    return new CollectionMeta(true, CollectionKind.Unknown, arrayElementType);
                else if (CollectionMetaHelper.IsTypeSupported(collectionType))
                {
                    var kind = CollectionMetaHelper.GetCollectionKind(collectionType);
                    Type elementType = CollectionMetaHelper.GetElementType(collectionType);
                    return new CollectionMeta(false, kind, elementType);
                }
            }

            return default;
        }

        public object CreateCollection(IList sourceElements)
        {
            if (!IsValid) return null;

            var createCollMethod = typeof(CollectionMeta).GetMethod(nameof(CreateCollectionGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ??
                                   throw new MissingMethodException(nameof(CollectionMeta), nameof(CreateCollectionGeneric));
            createCollMethod = createCollMethod.MakeGenericMethod(ElementType);

            return createCollMethod.Invoke(this, new object[] { sourceElements });
        }

        public IReadOnlyCollection<TDestElem> CreateCollectionGeneric<TDestElem>(IList sourceElements)
        {
            if (!IsValid) return null;
            int count = sourceElements.Count;

            if (IsArray)
            {
                var result = new TDestElem[count];

                for (int i = 0; i < count; i++)
                    result[i] = SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]);

                return result;
            }
            switch (Kind)
            {
                case CollectionKind.List:
                case CollectionKind.ReadOnlyCollection:
                    {
                        var result = new List<TDestElem>(count);

                        for (int i = 0; i < count; i++)
                            result.Add(SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]));

                        return Kind == CollectionKind.List ?
                            (IReadOnlyCollection<TDestElem>)result :
                            result.AsReadOnly();
                    }
                case CollectionKind.HashSet:
                case CollectionKind.SortedSet:
                    {
                        ISet<TDestElem> result = Kind == CollectionKind.HashSet
                            ? (ISet<TDestElem>)new HashSet<TDestElem>(
#if NETSTANDARD2_0
                
#else
                                count
#endif
                        )
                            : new SortedSet<TDestElem>();

                        for (int i = 0; i < count; i++)
                            result.Add(SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]));

                        return (IReadOnlyCollection<TDestElem>)result;
                    }
                case CollectionKind.LinkedList:
                    {
                        var result = new LinkedList<TDestElem>();

                        for (int i = 0; i < count; i++)
                            result.AddLast(SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]));

                        return result;
                    }
                case CollectionKind.Stack:
                    {
                        var result = new Stack<TDestElem>(count);

                        for (int i = 0; i < count; i++)
                            result.Push(SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]));

                        return result;
                    }
                case CollectionKind.Queue:
                    {
                        var result = new Queue<TDestElem>(count);

                        for (int i = 0; i < count; i++)
                            result.Enqueue(SpanParserHelper.ConvertElement<TDestElem>(sourceElements[i]));

                        return result;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
            }
        }
    }

    internal static class CollectionMetaHelper
    {
        public static CollectionKind GetCollectionKind(Type collectionType)
        {
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() is Type definition)
            {
                if (definition == typeof(IEnumerable<>) ||
                    definition == typeof(ICollection<>) ||
                    definition == typeof(IList<>) ||
                    definition == typeof(List<>))
                    return CollectionKind.List;

                else if (definition == typeof(IReadOnlyCollection<>) ||
                         definition == typeof(IReadOnlyList<>) ||
                         definition == typeof(ReadOnlyCollection<>))
                    return CollectionKind.ReadOnlyCollection;

                else if (definition == typeof(ISet<>) ||
                         definition == typeof(HashSet<>))
                    return CollectionKind.HashSet;
                else if (definition == typeof(SortedSet<>))
                    return CollectionKind.SortedSet;


                else if (definition == typeof(LinkedList<>))
                    return CollectionKind.LinkedList;
                else if (definition == typeof(Stack<>))
                    return CollectionKind.Stack;
                else if (definition == typeof(Queue<>))
                    return CollectionKind.Queue;
                else
                    return CollectionKind.Unknown;
            }
            else
                return CollectionKind.Unknown;
        }

        private static readonly HashSet<Type> _supportedCollectionTypes = new HashSet<Type>()
        {
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(List<>),

            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(ReadOnlyCollection<>),

            typeof(ISet<>),
            typeof(SortedSet<>),
            typeof(HashSet<>),

            typeof(LinkedList<>),
            typeof(Stack<>),
            typeof(Queue<>),
        };

        public static bool IsTypeSupported(Type collectionType) =>
            collectionType.IsGenericType &&
            collectionType.GetGenericTypeDefinition() is Type definition &&
            _supportedCollectionTypes.Contains(definition);

        public static Type GetElementType(Type collectionType)
        {
            var genericInterfaceType =
                collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? collectionType
                    : TypeMeta.GetConcreteInterfaceOfType(collectionType, typeof(IEnumerable<>))
                        ?? throw new InvalidOperationException("Type has to be or implement IEnumerable<>");

            return genericInterfaceType.GenericTypeArguments[0];
        }
    }

    public enum CollectionKind : byte
    {
        Unknown,

        List,
        ReadOnlyCollection,

        HashSet,
        SortedSet,

        LinkedList,
        Stack,
        Queue
    }
}
