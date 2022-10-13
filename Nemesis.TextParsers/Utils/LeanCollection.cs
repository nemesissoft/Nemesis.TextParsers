using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace Nemesis.TextParsers.Utils
{
    //TODO rework
    [PublicAPI]
    public static class LeanCollectionFactory
    {
        public static LeanCollection<T> FromOne<T>(T item1) => new(item1);

        public static LeanCollection<T> FromTwo<T>(T item1, T item2) => new(item1, item2);

        public static LeanCollection<T> FromThree<T>(T item1, T item2, T item3) => new(item1, item2, item3);

        /// <summary>
        /// Creates LeanCollection instance from array buffer. Caller needs to ensure that array has appropriate size (>3) - otherwise performance will be deteriorated 
        /// </summary>
        /// <param name="items">Buffer to create instance from</param>
        /// <returns>Array-based LeanCollection instance</returns>
        public static LeanCollection<T> FromArrayChecked<T>(T[] items) => new(items);

        /// <summary>
        /// Creates LeanCollection instance from array buffer. Takes care of checking array sizes and initializes instance in appropriate state  
        /// </summary>
        /// <param name="items">Buffer to create instance from</param>
        /// <param name="cloneBuffer">if left to default (<c>false</c>), LeanCollection takes ownership of this array buffer, otherwise it's copied</param>
        /// <returns>Valid LeanCollection instance</returns>
        public static LeanCollection<T> FromArray<T>(T[] items, bool cloneBuffer = false) =>
            items?.Length switch
            {
                null => new(),
                0 => new(),
                1 => new(items[0]),
                2 => new(items[0], items[1]),
                3 => new(items[0], items[1], items[2]),
                _ => new(cloneBuffer ? items.ToArray() : items)
            };
    }

    /// <summary>
    /// <![CDATA[Groups List<T> like operations for convenience]]>
    /// </summary>
    public interface IListOperations<T>
    {
        /// <summary>
        /// Convert <see cref="LeanCollection{T}"/> to <see cref="List{T}"/>. This obviously allocates. For tests only
        /// </summary>
        IReadOnlyList<T> ToList();

        LeanCollection<T> Sort(IComparer<T> comparer = null);
    }

    /// <summary>
    /// Stores up to 3 elements or an array in memory-efficient way. Implements <see cref="IEnumerable{T}"/> for convenience (LINQ, tests...) but ref-struct based <see cref="GetEnumerator()"/> methods should be preferred
    /// </summary>
    public readonly struct LeanCollection<T> : IEquatable<LeanCollection<T>>, IEnumerable<T>, IListOperations<T>
    {
        #region Fields
        enum CollectionSize : sbyte { More = -1, Zero = 0, One = 1, Two = 2, Three = 3 }

        private readonly CollectionSize _size;
        public int Size => _size == CollectionSize.More ? _items.Length : (int)_size;

        private readonly T _item1;
        private readonly T _item2;
        private readonly T _item3;
        private readonly T[] _items;

        #endregion

        #region Constructors
        internal LeanCollection(T item1)
        {
            _item1 = item1;
            _item2 = default;
            _item3 = default;

            _items = default;
            _size = CollectionSize.One;
        }

        internal LeanCollection(T item1, T item2)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = default;

            _items = default;
            _size = CollectionSize.Two;
        }

        internal LeanCollection(T item1, T item2, T item3)
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

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new LeanCollectionEnumeratorNonRef(this);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        public LeanCollectionEnumerator GetEnumerator() => new(this);

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

        public struct LeanCollectionEnumeratorNonRef : IEnumerator<T>
        {
            private readonly LeanCollection<T> _leanCollection;
            private int _index;

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public LeanCollectionEnumeratorNonRef(in LeanCollection<T> leanCollection)
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

            public void Reset()
            {
                Current = default;
                _index = _leanCollection._size == CollectionSize.More ? 0 : -1;
            }

            public void Dispose() { }
        }

        #endregion

        #region Conversions

        public static implicit operator LeanCollection<T>(T one) => new(one);
        public static implicit operator LeanCollection<T>((T, T) pair) => new(pair.Item1, pair.Item2);
        public static implicit operator LeanCollection<T>((T, T, T) triple) => new(triple.Item1, triple.Item2, triple.Item3);


        private bool IsMore(int expectedSize) => _size == CollectionSize.More && _items?.Length >= expectedSize;

        public static explicit operator T(LeanCollection<T> one) => one.IsMore(1) ? one._items[0] : one._item1;

        public static explicit operator (T, T)(LeanCollection<T> pair) => pair.IsMore(2) ? (pair._items[0], pair._items[1]) : (pair._item1, pair._item2);

        public static explicit operator (T, T, T)(LeanCollection<T> triple) => triple.IsMore(3) ? (triple._items[0], triple._items[1], triple._items[2]) : (triple._item1, triple._item2, triple._item3);

        public static explicit operator T[](LeanCollection<T> more) => more.IsMore(0)
                ? more._items.ToArray()
                : more._size switch
                {
                    CollectionSize.Zero => Array.Empty<T>(),
                    CollectionSize.One => new[] { more._item1 },
                    CollectionSize.Two => new[] { more._item1, more._item2 },
                    CollectionSize.Three => new[] { more._item1, more._item2, more._item3 },
                    //this is already covered above: CollectionSize.More => more._items.ToArray(),
                    _ => throw new ArgumentOutOfRangeException($"Internal state of {nameof(LeanCollection<T>)} was compromised")
                };

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

        public override bool Equals(object obj) => obj is LeanCollection<T> other && Equals(other);

        public override int GetHashCode()
        {
            const int PRIME = 397;
            return _size switch
            {
                CollectionSize.Zero => 0,
                CollectionSize.One => _item1?.GetHashCode() ?? 0,
                CollectionSize.Two => unchecked(((_item1?.GetHashCode() ?? 0) * PRIME) ^ (_item2?.GetHashCode() ?? 0)),
                CollectionSize.Three => unchecked(
                    ((((_item1?.GetHashCode() ?? 0) * PRIME) ^ (_item2?.GetHashCode() ?? 0)) * PRIME) ^ (_item3?.GetHashCode() ?? 0)
                    ),
                CollectionSize.More => GetHashCode(_items),
                _ => throw new ArgumentOutOfRangeException($"Internal state of {nameof(LeanCollection<T>)} was compromised")
            };
        }

        private static int GetHashCode(IEnumerable<T> enumerable)
            => enumerable is null ? 0 :
                unchecked(enumerable.Aggregate(0, (current, element) => (current * 397) ^ (element?.GetHashCode() ?? 0)));

        public static bool operator ==(LeanCollection<T> left, LeanCollection<T> right) => left.Equals(right);

        public static bool operator !=(LeanCollection<T> left, LeanCollection<T> right) => !left.Equals(right);
        #endregion

        /// <summary>Text representation. For debugging purposes only</summary>
        public override string ToString() => string.Join(" | ", ((IListOperations<T>)this).ToList());


        #region IListOperations
        IReadOnlyList<T> IListOperations<T>.ToList()
        {
            if (_size == CollectionSize.More) return _items;

            var list = new List<T>(Size);
            foreach (T element in this)
                list.Add(element);
            return list;
        }

        LeanCollection<T> IListOperations<T>.Sort(IComparer<T> comparer)
        {
            var size = _size;
            switch (size)
            {
                case CollectionSize.Zero:
                case CollectionSize.One:
                    return this;

                case CollectionSize.Two:
                    comparer ??= Comparer<T>.Default;
                    return comparer.Compare(_item1, _item2) <= 0 ? this : new(_item2, _item1);

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
                        return new(a, b, c);
                    }
                default:
                    comparer ??= Comparer<T>.Default;
                    Array.Sort(_items, comparer);
                    return new(_items);
            }
        } 
        #endregion
    }
}
