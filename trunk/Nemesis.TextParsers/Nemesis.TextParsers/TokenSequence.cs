using System;
using System.Runtime.CompilerServices;

namespace Nemesis.TextParsers
{
    /// <summary>
    /// Aids user in tokenization or arbitrary sequence. Returns tokens with potentially unescaped separator element
    /// </summary>
    public ref struct TokenSequence<TElement> where TElement : IEquatable<TElement>
    {
        public TokenSequence(ReadOnlySpan<TElement> sequence, TElement separator, TElement escapingElement, bool emptySequenceYieldsEmpty)
        {
            _sequence = sequence;
            _separator = separator;
            _escapingElement = escapingElement;
            _emptySequenceYieldsEmpty = emptySequenceYieldsEmpty;
        }

        private readonly ReadOnlySpan<TElement> _sequence;
        private readonly TElement _separator;
        private readonly TElement _escapingElement;
        private readonly bool _emptySequenceYieldsEmpty;

        public TokenSequenceEnumerator GetEnumerator() => new TokenSequenceEnumerator(_sequence, _separator, _escapingElement, _emptySequenceYieldsEmpty);

        public ref struct TokenSequenceEnumerator
        {
            public TokenSequenceEnumerator(ReadOnlySpan<TElement> sequence, TElement separator, TElement escapingElement, bool emptySequenceYieldsEmpty)
            {
                _sequence = sequence;
                _separator = separator;
                _escapingElement = escapingElement;
                Current = default;

                _trailingEmptyItem = !emptySequenceYieldsEmpty && _sequence.IsEmpty;
            }

            private ReadOnlySpan<TElement> _sequence;
            private readonly TElement _separator;
            private readonly TElement _escapingElement;
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

                int idx = FindUnescapedSeparator();
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int FindUnescapedSeparator()
            {
                int idx = _sequence.IndexOf(_separator);
                if (idx < 0) return -1;
                else
                {
                    bool escaped = false;

                    for (int i = 0; i < _sequence.Length; i++)
                    {
                        TElement current = _sequence[i];

                        bool isSeparator = current.Equals(_separator);
                        bool isEscape = current.Equals(_escapingElement);

                        if (!escaped && isSeparator)
                            return i;

                        if (!isEscape && escaped)
                            escaped = false;

                        if (isEscape)
                            escaped = !escaped;
                    }
                    return -1;
                }
            }
            public ReadOnlySpan<TElement> Current { get; private set; }
        }
    }
}
