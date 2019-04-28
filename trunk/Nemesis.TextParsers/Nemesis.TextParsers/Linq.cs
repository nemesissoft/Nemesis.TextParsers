using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    /* TODO ?
     public static class Linq
    {




        public readonly ref struct WhereSequence<T>
        {
            private readonly ParsedSequence<T> _sequence;
            private readonly Func<T, bool> _predicate;

            public WhereSequence(ParsedSequence<T> sequence, [NotNull] Func<T, bool> predicate)
            {
                _sequence = sequence;
                _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            }

            public WhereIterator GetEnumerator() => new WhereIterator(_sequence, _predicate);

            public ref struct WhereIterator
            {
                private ParsedSequence<T>.ParsedSequenceEnumerator _sequenceEnumerator;
                private readonly Func<T, bool> _predicate;
                public T Current { get; private set; }

                public WhereIterator(ParsedSequence<T> sequence, Func<T, bool> predicate)
                {
                    _sequenceEnumerator = sequence.GetEnumerator();
                    _predicate = predicate;
                    Current = default;
                }

                public bool MoveNext()
                {
                    while (_sequenceEnumerator.MoveNext())
                    {
                        T current = _sequenceEnumerator.Current;
                        if (_predicate(current))
                        {
                            Current = current;
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }*/
}
