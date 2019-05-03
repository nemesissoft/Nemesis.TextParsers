using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    //TODO measure perf
    /// <summary>
    /// Stores up to 3 elements or an array
    /// </summary>
    public readonly struct LeanCollection<T> : IEquatable<LeanCollection<T>>
    {
        #region Fields
        enum CollectionSize : sbyte { More = -1, [UsedImplicitly] Zero = 0, One = 1, Two = 2, Three = 3 }

        private readonly CollectionSize _size;
        public int Size => _size == CollectionSize.More ? _items.Length : (int)_size;

        private readonly T _item1;
        private readonly T _item2;
        private readonly T _item3;
        private readonly T[] _items;
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

        private LeanCollection(T[] items)
        {
            _item1 = default;
            _item2 = default;
            _item3 = default;

            _size = CollectionSize.More;
            _items = items;
        }

        public static LeanCollection<T> FromArrayChecked(T[] items) => new LeanCollection<T>(items);

        public static LeanCollection<T> FromArray(T[] items)
        {
            int? length = items?.Length;
            switch (length)
            {
                case null:
                case 0:
                    return new LeanCollection<T>();
                case 1:
                    return new LeanCollection<T>(items[0]);
                case 2:
                    return new LeanCollection<T>(items[0], items[1]);
                case 3:
                    return new LeanCollection<T>(items[0], items[1], items[2]);
                default:
                    return new LeanCollection<T>(items);
            }
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
                    if (_index == -1 && _leanCollection._size >= CollectionSize.One)
                    {
                        Current = _leanCollection._item1;
                        canMove = true;
                        --_index;
                    }
                    else if (_index == -2 && _leanCollection._size >= CollectionSize.Two)
                    {
                        Current = _leanCollection._item2;
                        canMove = true;
                        --_index;
                    }
                    else if (_index == -3 && _leanCollection._size >= CollectionSize.Three)
                    {
                        Current = _leanCollection._item3;
                        canMove = true;
                        --_index;
                    }
                    else
                        Current = default;
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
        public static implicit operator LeanCollection<T>(T one) => new LeanCollection<T>(one);
        public static implicit operator LeanCollection<T>((T, T) pair) => new LeanCollection<T>(pair.Item1, pair.Item2);
        public static implicit operator LeanCollection<T>((T, T, T) triple) => new LeanCollection<T>(triple.Item1, triple.Item2, triple.Item3);

        public static implicit operator LeanCollection<T>(T[] span) => new LeanCollection<T>(span);


        public static implicit operator T(LeanCollection<T> one) => one._item1;
        public static implicit operator (T, T) (LeanCollection<T> pair) => (pair._item1, pair._item2);
        public static implicit operator (T, T, T) (LeanCollection<T> triple) => (triple._item1, triple._item2, triple._item3);

        public static implicit operator T[] (LeanCollection<T> more) => more._items;
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

        public LeanCollection<T> Sort(IComparer<T> comparer = null)
        {
            //TODO
            var size = _size;
            switch (size)
            {
                case CollectionSize.Zero:
                case CollectionSize.One:
                    return this;

                case CollectionSize.Two:
                    comparer = comparer ?? Comparer<T>.Default;
                    if(comparer.Compare(_item1, _item2) <=0)
                    return this;
                    else
                    {
                        
                    }

                case CollectionSize.Three:
                    return this;
                default:
                    Array.Sort(_items, comparer);
                    return new LeanCollection<T>(_items);
            }

            comparer = comparer ?? Comparer<T>.Default;
            /*if (size == CollectionSize.Zero || size == CollectionSize.One)
              
            else
            {
                
            }*/



        }
    }
}
