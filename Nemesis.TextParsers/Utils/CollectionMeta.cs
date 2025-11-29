#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

using JetBrains.Annotations;

using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Utils;

/// <summary>
/// Aids in providing metadata for GUI applications 
/// </summary>
[PublicAPI]
public readonly struct CollectionMeta : IEquatable<CollectionMeta>
{
    public CollectionKind Kind { get; }
    public Type ElementType { get; }

    public bool IsValid => Kind != CollectionKind.Unknown && ElementType != null;

    internal CollectionMeta(CollectionKind kind, Type elementType)
    {
        if (!Enum.IsDefined(typeof(CollectionKind), kind))
            throw new InvalidEnumArgumentException(nameof(kind), (int)kind, typeof(CollectionKind));

        Kind = kind;
        ElementType = elementType;
    }

    public void Deconstruct(out CollectionKind kind, out Type elementType) { kind = Kind; elementType = ElementType; }

    public override string ToString() => $"{Kind}, {ElementType.GetFriendlyName() ?? "<NoType>"}";

    #region Equals

    public bool Equals(CollectionMeta other) => Kind == other.Kind && ElementType == other.ElementType;

    public override bool Equals(object? obj) => obj is CollectionMeta other && Equals(other);

    public override int GetHashCode() => unchecked(((int)Kind * 397) ^ ElementType.GetHashCode());

    public static bool operator ==(CollectionMeta left, CollectionMeta right) => left.Equals(right);

    public static bool operator !=(CollectionMeta left, CollectionMeta right) => !left.Equals(right);

    #endregion

    public object? CreateCollection(IEnumerable sourceElements)
    {
        if (!IsValid) return null;

        var createCollMethod = typeof(CollectionMeta).GetMethod(nameof(CreateCollectionGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ??
                               throw new MissingMethodException(nameof(CollectionMeta), nameof(CreateCollectionGeneric));
        createCollMethod = createCollMethod.MakeGenericMethod(ElementType);

        return createCollMethod.Invoke(this, [sourceElements]);
    }

    public IReadOnlyCollection<TDestElem?>? CreateCollectionGeneric<TDestElem>(IEnumerable sourceElements)
    {
        if (!IsValid) return null;
        int? count = (sourceElements as ICollection)?.Count;

        var input = new List<TDestElem?>(count ?? 8);
        foreach (var element in sourceElements)
            input.Add(ConvertElement<TDestElem>(element));

        return Kind switch
        {
            CollectionKind.Array => input.ToArray(),
            CollectionKind.List => input,
            CollectionKind.ReadOnlyCollection => input.AsReadOnly(),
            CollectionKind.HashSet => new HashSet<TDestElem?>(input),
            CollectionKind.SortedSet => new SortedSet<TDestElem?>(input),
            CollectionKind.LinkedList => new LinkedList<TDestElem?>(input),
            CollectionKind.Stack => new Stack<TDestElem?>(input),
            CollectionKind.Queue => new Queue<TDestElem?>(input),
            CollectionKind.ObservableCollection => new ObservableCollection<TDestElem?>(input),
            CollectionKind.ReadOnlyObservableCollection => new ReadOnlyObservableCollection<TDestElem?>(new ObservableCollection<TDestElem?>(input)),
            //case CollectionKind.Unknown:
            _ => throw new NotSupportedException($@"{nameof(Kind)} = '{Kind}' is not supported"),
        };
    }

    private static TDest? ConvertElement<TDest>(object? element)
    {
        try
        {
            return element switch
            {
                TDest dest => dest,
                null => default,
                IFormattable formattable when typeof(TDest) == typeof(string) =>
                (TDest)(object)formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => (TDest?)TypeMeta.GetDefault(typeof(TDest))
            };
        }
        catch (FormatException) { return default; }
    }
}

internal static class CollectionMetaHelper
{
    public static CollectionMeta GetCollectionMeta(Type collectionType)
    {
        if (collectionType != null)
        {
            if (collectionType.IsArray && collectionType.GetElementType() is { } arrayElementType)
                return new(CollectionKind.Array, arrayElementType);
            else if (IsTypeSupported(collectionType) &&
                     GetCollectionKind(collectionType) is { } kind &&
                     kind != CollectionKind.Unknown
                    )
            {
                Type elementType = GetElementType(collectionType);
                return new(kind, elementType);
            }
        }

        return default;
    }

    private static CollectionKind GetCollectionKind(Type collectionType)
    {
        if (collectionType.IsArray)
            return CollectionKind.Array;

        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() is { } definition)
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

            else if (definition == typeof(ObservableCollection<>))
                return CollectionKind.ObservableCollection;

            else if (definition == typeof(ReadOnlyObservableCollection<>))
                return CollectionKind.ReadOnlyObservableCollection;
        }

        return CollectionKind.Unknown;
    }

    private static Type GetElementType(Type collectionType)
    {
        var genericInterfaceType =
            collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? collectionType
                : TypeMeta.GetGenericRealization(collectionType, typeof(IEnumerable<>))
                  ?? throw new InvalidOperationException("Type has to be or implement IEnumerable<>");

        return genericInterfaceType.GenericTypeArguments[0];
    }

    private static readonly HashSet<Type> _supportedCollectionTypes =
    [
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

        typeof(ObservableCollection<>),
        typeof(ReadOnlyObservableCollection<>),
    ];

    public static bool IsTypeSupported(Type collectionType) =>
        collectionType is { IsGenericType: true } &&
        collectionType.GetGenericTypeDefinition() is { } definition &&
        _supportedCollectionTypes.Contains(definition);
}

public enum CollectionKind : byte
{
    Unknown,

    Array,

    List,
    ReadOnlyCollection,

    HashSet,
    SortedSet,

    LinkedList,
    Stack,
    Queue,

    ObservableCollection,
    ReadOnlyObservableCollection
}
