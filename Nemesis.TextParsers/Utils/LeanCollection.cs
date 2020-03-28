using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Nemesis.TextParsers.Utils
{
    [PublicAPI]
    public static class LeanCollectionFactory
    {
        public static LeanCollection<T> FromArrayChecked<T>(T[] items) => new LeanCollection<T>(items);

        public static LeanCollection<T> FromArray<T>(T[] items) =>
            items?.Length switch
            {
                null => new LeanCollection<T>(),
                0 => new LeanCollection<T>(),
                1 => new LeanCollection<T>(items[0]),
                2 => new LeanCollection<T>(items[0], items[1]),
                3 => new LeanCollection<T>(items[0], items[1], items[2]),
                _ => new LeanCollection<T>(items)
            };

        public static LeanCollection<T> ToLeanCollection<T>(T one) => new LeanCollection<T>(one);
        public static LeanCollection<T> ToLeanCollection<T>((T, T) pair) => new LeanCollection<T>(pair.Item1, pair.Item2);
        public static LeanCollection<T> ToLeanCollection<T>((T, T, T) triple) => new LeanCollection<T>(triple.Item1, triple.Item2, triple.Item3);
        public static LeanCollection<T> ToLeanCollection<T>(T[] span) => new LeanCollection<T>(span);

        public static T ToSingleValue<T>(LeanCollection<T> one) => one._item1;
        public static (T, T) ToPair<T>(LeanCollection<T> pair) => (pair._item1, pair._item2);
        public static (T, T, T) ToTriple<T>(LeanCollection<T> triple) => (triple._item1, triple._item2, triple._item3);
        public static T[] ToArray<T>(LeanCollection<T> more) => more._items;
    }

    /// <summary>
    /// Stores up to 3 elements or an array
    /// </summary>
    public readonly struct LeanCollection<T> : IEquatable<LeanCollection<T>>
    {
        #region Fields
        enum CollectionSize : sbyte { More = -1, [UsedImplicitly] Zero = 0, One = 1, Two = 2, Three = 3 }

        private readonly CollectionSize _size;
        public int Size => _size == CollectionSize.More ? _items.Length : (int)_size;

        // ReSharper disable InconsistentNaming
        internal readonly T _item1;
        internal readonly T _item2;
        internal readonly T _item3;
        internal readonly T[] _items;
        // ReSharper restore InconsistentNaming
        #endregion

        #region Constructors
        public LeanCollection(T item1)
        {
            _item1 = item1;
            _item2 = default;
            _item3 = default;

            _items = default;
            _size = CollectionSize.One;
        }

        public LeanCollection(T item1, T item2)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = default;

            _items = default;
            _size = CollectionSize.Two;
        }

        public LeanCollection(T item1, T item2, T item3)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;

            _items = default;
            _size = CollectionSize.Three;
        }

        internal LeanCollection(T[] items)
        {
            _item1 = default;
            _item2 = default;
            _item3 = default;

            _size = CollectionSize.More;
            _items = items;
        }
        
        #endregion

        #region Enumerations
        public LeanCollectionEnumerator GetEnumerator() => new LeanCollectionEnumerator(this);

        public ref struct LeanCollectionEnumerator
        {
            private readonly LeanCollection<T> _leanCollection;
            private int _index;

            public T Current { get; private set; }

            public LeanCollectionEnumerator(in LeanCollection<T> leanCollection)
            {
                _leanCollection = leanCollection;
                Current = default;
                _index = leanCollection._size == CollectionSize.More ? 0 : -1;
            }

            public bool MoveNext()
            {
                var canMove = false;
                if (_index >= 0)
                {
                    var items = _leanCollection._items;
                    if (_index < items.Length)
                    {
                        Current = items[_index++];
                        canMove = true;
                    }
                    else
                        Current = default;
                }
                else
                {
                    switch (_index)
                    {
                        case -1 when _leanCollection._size >= CollectionSize.One:
                            Current = _leanCollection._item1;
                            canMove = true;
                            --_index;
                            break;
                        case -2 when _leanCollection._size >= CollectionSize.Two:
                            Current = _leanCollection._item2;
                            canMove = true;
                            --_index;
                            break;
                        case -3 when _leanCollection._size >= CollectionSize.Three:
                            Current = _leanCollection._item3;
                            canMove = true;
                            --_index;
                            break;
                        default:
                            Current = default;
                            break;
                    }
                }
                return canMove;
            }
        }

        /// <summary>
        /// Convert <see cref="LeanCollection{T}"/> to <see cref="List{T}"/>. This obviously allocates. For tests only
        /// </summary>
        public IReadOnlyList<T> ToList()
        {
            var list = new List<T>(Size);
            foreach (T element in this)
                list.Add(element);
            return list;
        }
        #endregion

        #region Conversions
#pragma warning disable CA2225 // Operator overloads have named alternates
        public static implicit operator LeanCollection<T>(T one) => new LeanCollection<T>(one);
        public static implicit operator LeanCollection<T>((T, T) pair) => new LeanCollection<T>(pair.Item1, pair.Item2);
        public static implicit operator LeanCollection<T>((T, T, T) triple) => new LeanCollection<T>(triple.Item1, triple.Item2, triple.Item3);
        public static implicit operator LeanCollection<T>(T[] span) => new LeanCollection<T>(span);

        public static implicit operator T(LeanCollection<T> one) => one._item1;
        public static implicit operator (T, T)(LeanCollection<T> pair) => (pair._item1, pair._item2);
        public static implicit operator (T, T, T)(LeanCollection<T> triple) => (triple._item1, triple._item2, triple._item3);
        public static implicit operator T[](LeanCollection<T> more) => more._items;
#pragma warning restore CA2225 // Operator overloads have named alternates

        #endregion

        #region Equality
        public bool Equals(LeanCollection<T> other)
        {
            if (_size != other._size) return false;
            var equalityComparer = EqualityComparer<T>.Default;

            var enumerator = GetEnumerator();
            var enumerator2 = other.GetEnumerator();
            {
                while (enumerator.MoveNext())
                    if (!enumerator2.MoveNext() || !equalityComparer.Equals(enumerator.Current, enumerator2.Current))
                        return false;

                if (enumerator2.MoveNext())
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => !(obj is null) && obj is LeanCollection<T> other && Equals(other);

        public override int GetHashCode() => unchecked((int)_size * 397); //size is enough for GetHashCode. After all LeanCollection is not supposed to be used as dictionary key 

        public static bool operator ==(LeanCollection<T> left, LeanCollection<T> right) => left.Equals(right);

        public static bool operator !=(LeanCollection<T> left, LeanCollection<T> right) => !left.Equals(right);
        #endregion

        public override string ToString() => SpanCollectionSerializer.DefaultInstance.FormatCollection(this);

        [Pure]
        public LeanCollection<T> Sort(IComparer<T> comparer = null)
        {
            var size = _size;
            switch (size)
            {
                case CollectionSize.Zero:
                case CollectionSize.One:
                    return this;

                case CollectionSize.Two:
                    comparer ??= Comparer<T>.Default;
                    return comparer.Compare(_item1, _item2) <= 0 ? this : new LeanCollection<T>(_item2, _item1);

                case CollectionSize.Three:
                    static void Swap(ref T t1, ref T t2)
                    {
                        var temp = t2;
                        t2 = t1;
                        t1 = temp;
                    }
                    comparer ??= Comparer<T>.Default;
                    if (comparer.Compare(_item1, _item2) <= 0 && comparer.Compare(_item2, _item3) <= 0)
                        return this;
                    else
                    {
                        T a = _item1, b = _item2, c = _item3;

                        if (comparer.Compare(a, c) > 0)
                            Swap(ref a, ref c);

                        if (comparer.Compare(a, b) > 0)
                            Swap(ref a, ref b);

                        if (comparer.Compare(b, c) > 0)
                            Swap(ref b, ref c);
                        return new LeanCollection<T>(a, b, c);
                    }
                default:
                    comparer ??= Comparer<T>.Default;
                    Array.Sort(_items, comparer);
                    return new LeanCollection<T>(_items);
            }
        }
    }
}
