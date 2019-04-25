using System;
using PureMethod = System.Diagnostics.Contracts.PureAttribute;

namespace Nemesis.TextParsers
{
    public static class SpanSplitExtensions
    {
        #region Enumerators
        public readonly ref struct Enumerable1<T> where T : IEquatable<T>
        {
            public Enumerable1(ReadOnlySpan<T> sequence, T separator, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator = separator;
                _emptySequenceYieldsEmpty = emptySequenceYieldsEmpty;
            }

            private readonly ReadOnlySpan<T> _sequence;
            private readonly T _separator;
            private readonly bool _emptySequenceYieldsEmpty;

            public Enumerator1<T> GetEnumerator() => new Enumerator1<T>(_sequence, _separator, _emptySequenceYieldsEmpty);
        }

        public readonly ref struct Enumerable2<T> where T : IEquatable<T>
        {
            public Enumerable2(ReadOnlySpan<T> sequence, T separator1, T separator2, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator1 = separator1;
                _separator2 = separator2;
                _emptySequenceYieldsEmpty = emptySequenceYieldsEmpty;
            }

            private readonly ReadOnlySpan<T> _sequence;
            private readonly T _separator1;
            private readonly T _separator2;
            private readonly bool _emptySequenceYieldsEmpty;

            public Enumerator2<T> GetEnumerator() => new Enumerator2<T>(_sequence, _separator1, _separator2, _emptySequenceYieldsEmpty);
        }

        public readonly ref struct Enumerable3<T> where T : IEquatable<T>
        {
            public Enumerable3(ReadOnlySpan<T> sequence, T separator1, T separator2, T separator3, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator1 = separator1;
                _separator2 = separator2;
                _separator3 = separator3;
                _emptySequenceYieldsEmpty = emptySequenceYieldsEmpty;
            }

            private readonly ReadOnlySpan<T> _sequence; 
            private readonly T _separator1;
            private readonly T _separator2;
            private readonly T _separator3;
            private readonly bool _emptySequenceYieldsEmpty;

            public Enumerator3<T> GetEnumerator() =>new Enumerator3<T>(_sequence, _separator1, _separator2, _separator3, _emptySequenceYieldsEmpty);
        }

        public readonly ref struct EnumerableN<T> where T : IEquatable<T>
        {
            public EnumerableN(ReadOnlySpan<T> sequence, ReadOnlySpan<T> separators, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separators = separators;
                _emptySequenceYieldsEmpty = emptySequenceYieldsEmpty;
            }

            private readonly ReadOnlySpan<T> _sequence;
            private readonly ReadOnlySpan<T> _separators;
            private readonly bool _emptySequenceYieldsEmpty;

            public EnumeratorN<T> GetEnumerator() => new EnumeratorN<T>(_sequence, _separators, _emptySequenceYieldsEmpty);
        }



        public ref struct Enumerator1<T> where T : IEquatable<T>
        {
            public Enumerator1(ReadOnlySpan<T> sequence, T separator, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator = separator;
                Current = default;

                _trailingEmptyItem = !emptySequenceYieldsEmpty && _sequence.IsEmpty;
            }

            private ReadOnlySpan<T> _sequence;
            private readonly T _separator;
            private bool _trailingEmptyItem;
            
            public bool MoveNext()
            {
                if (_trailingEmptyItem)
                {
                    _trailingEmptyItem = false;
                    Current = default;
                    return true;
                }

                if (_sequence.IsEmpty)
                {
                    _sequence = Current = default;
                    return false;
                }

                int idx = _sequence.IndexOf(_separator);
                if (idx < 0)
                {
                    Current = _sequence;
                    _sequence = default;
                }
                else
                {
                    Current = _sequence.Slice(0, idx);
                    _sequence = _sequence.Slice(idx + 1);
                    if (_sequence.IsEmpty)
                        _trailingEmptyItem = true;
                }

                return true;
            }

            public ReadOnlySpan<T> Current { get; private set; }
        }

        public ref struct Enumerator2<T> where T : IEquatable<T>
        {
            public Enumerator2(ReadOnlySpan<T> sequence, T separator1, T separator2, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator1 = separator1;
                _separator2 = separator2;
                Current = default;

                _trailingEmptyItem = !emptySequenceYieldsEmpty && _sequence.IsEmpty;
            }

            private ReadOnlySpan<T> _sequence;
            private readonly T _separator1;
            private readonly T _separator2;
            private bool _trailingEmptyItem;

            public bool MoveNext()
            {
                if (_trailingEmptyItem)
                {
                    _trailingEmptyItem = false;
                    Current = default;
                    return true;
                }

                if (_sequence.IsEmpty)
                {
                    _sequence = Current = default;
                    return false;
                }

                int idx = _sequence.IndexOfAny(_separator1, _separator2);
                if (idx < 0)
                {
                    Current = _sequence;
                    _sequence = default;
                }
                else
                {
                    Current = _sequence.Slice(0, idx);
                    _sequence = _sequence.Slice(idx + 1);
                    if (_sequence.IsEmpty)
                        _trailingEmptyItem = true;
                }

                return true;
            }

            public ReadOnlySpan<T> Current { get; private set; }
        }

        public ref struct Enumerator3<T> where T : IEquatable<T>
        {
            public Enumerator3(ReadOnlySpan<T> sequence, T separator1, T separator2, T separator3, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator1 = separator1;
                _separator2 = separator2;
                _separator3 = separator3;
                Current = default;

                _trailingEmptyItem = !emptySequenceYieldsEmpty && _sequence.IsEmpty;
            }

            private ReadOnlySpan<T> _sequence;
            private readonly T _separator1;
            private readonly T _separator2;
            private readonly T _separator3;
            private bool _trailingEmptyItem;

            public bool MoveNext()
            {
                if (_trailingEmptyItem)
                {
                    _trailingEmptyItem = false;
                    Current = default;
                    return true;
                }

                if (_sequence.IsEmpty)
                {
                    _sequence = Current = default;
                    return false;
                }

                int idx = _sequence.IndexOfAny(_separator1, _separator2, _separator3);
                if (idx < 0)
                {
                    Current = _sequence;
                    _sequence = default;
                }
                else
                {
                    Current = _sequence.Slice(0, idx);
                    _sequence = _sequence.Slice(idx + 1);
                    if (_sequence.IsEmpty)
                        _trailingEmptyItem = true;
                }

                return true;
            }

            public ReadOnlySpan<T> Current { get; private set; }
        }

        public ref struct EnumeratorN<T> where T : IEquatable<T>
        {
            public EnumeratorN(ReadOnlySpan<T> sequence, ReadOnlySpan<T> separators, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separators = separators;
                Current = default;

                _trailingEmptyItem = !emptySequenceYieldsEmpty && _sequence.IsEmpty;
            }

            private ReadOnlySpan<T> _sequence;
            private readonly ReadOnlySpan<T> _separators;
            private bool _trailingEmptyItem;
            
            public bool MoveNext()
            {
                if (_trailingEmptyItem)
                {
                    _trailingEmptyItem = false;
                    Current = default;
                    return true;
                }

                if (_sequence.IsEmpty)
                {
                    _sequence = Current = default;
                    return false;
                }

                int idx = _sequence.IndexOfAny(_separators);
                if (idx < 0)
                {
                    Current = _sequence;
                    _sequence = default;
                }
                else
                {
                    Current = _sequence.Slice(0, idx);
                    _sequence = _sequence.Slice(idx + 1);
                    if (_sequence.IsEmpty)
                        _trailingEmptyItem = true;
                }

                return true;
            }

            public ReadOnlySpan<T> Current { get; private set; }
        }
        #endregion

        #region Extensions
        [PureMethod]
        public static Enumerable1<T> Split<T>(this ReadOnlySpan<T> span, T separator, bool emptySequenceYieldsEmpty=false)
            where T : IEquatable<T> => new Enumerable1<T>(span, separator, emptySequenceYieldsEmpty);

        [PureMethod]
        public static Enumerable2<T> Split<T>(this ReadOnlySpan<T> span, T separator1, T separator2, bool emptySequenceYieldsEmpty = false)
            where T : IEquatable<T> => new Enumerable2<T>(span, separator1, separator2, emptySequenceYieldsEmpty);

        [PureMethod]
        public static Enumerable3<T> Split<T>(this ReadOnlySpan<T> span, T separator1, T separator2, T separator3, bool emptySequenceYieldsEmpty = false)
            where T : IEquatable<T> => new Enumerable3<T>(span, separator1, separator2, separator3, emptySequenceYieldsEmpty);

        [PureMethod]
        public static EnumerableN<T> Split<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> values, bool emptySequenceYieldsEmpty = false)
            where T : IEquatable<T> => new EnumerableN<T>(span, values, emptySequenceYieldsEmpty);
        #endregion
    }
}
