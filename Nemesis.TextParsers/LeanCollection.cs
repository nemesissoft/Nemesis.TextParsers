using System;
using System.Collections.Generic;

namespace Nemesis.TextParsers
{
    /// <summary>
    /// Stores up to 3 elements or an array
    /// </summary>
    public readonly ref struct LeanCollection<T>
    {
        enum CollectionSize : byte { More = 0, One, Two, Three }

        private readonly CollectionSize _size;
        public int Size => _size == CollectionSize.More ? _items.Length : (int)_size;

        private readonly T _item1;
        private readonly T _item2;
        private readonly T _item3;
        private readonly ReadOnlySpan<T> _items;

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

        public LeanCollection(ReadOnlySpan<T> items)
        {
            _item1 = default;
            _item2 = default;
            _item3 = default;

            _items = items;
            _size = CollectionSize.More;
        }

        public LeanCollectionEnumerator GetEnumerator() => new LeanCollectionEnumerator(this);

        public ref struct LeanCollectionEnumerator
        {
            private readonly LeanCollection<T> _leanCollection;
            private int _index;

            public T Current { get; private set; }

            public LeanCollectionEnumerator(LeanCollection<T> leanCollection)
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

        public IReadOnlyList<T> ToList()
        {
            var list = new List<T>(Size);
            foreach (T element in this)
                list.Add(element);
            return list;
        }
    }
}
