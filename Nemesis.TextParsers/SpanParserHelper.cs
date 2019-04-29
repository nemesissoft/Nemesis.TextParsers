using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PureMethod = System.Diagnostics.Contracts.PureAttribute;

namespace Nemesis.TextParsers
{
    public static class SpanParserHelper
    {
        [PureMethod]
        public static TokenSequence<T> Tokenize<T>(this in ReadOnlySpan<T> sequence, T separator, T escapingElement,
            bool emptySequenceYieldsEmpty)
            where T : IEquatable<T> =>
            new TokenSequence<T>(sequence, separator, escapingElement, emptySequenceYieldsEmpty);

        [PureMethod]
        public static ParsedSequence<TTo> Parse<TTo>(this in TokenSequence<char> tokenSource, char escapingElement,
            char nullElement,
            char allowedEscapeCharacter1 = default) =>
            new ParsedSequence<TTo>(tokenSource, escapingElement, nullElement, allowedEscapeCharacter1);

        //TODO ToArray - 2 versions from managed and unmanaged. ToArrayUnmanaged - copy to buffer bool TryCopyTo(buffer, out int count
        [PureMethod]
        public static TTo[] ToArray<TTo>(this in ParsedSequence<TTo> parsedSequence, ushort potentialLength = 8)
        {
            var initialBuffer = ArrayPool<TTo>.Shared.Rent(potentialLength);
            try
            {
                var accumulator = new ValueSequenceBuilder<TTo>(initialBuffer);

                foreach (TTo part in parsedSequence)
                    accumulator.Append(part);

                var array = accumulator.AsSpan().ToArray();
                accumulator.Dispose();
                return array;
            }
            finally
            {
                ArrayPool<TTo>.Shared.Return(initialBuffer);
            }
        }

        //TODO functional tests + max capacity test + benchmark with standard ToArray
        [PureMethod]
        public static TTo[] ToArrayUnmanaged<TTo>(this in ParsedSequence<TTo> parsedSequence, ushort potentialLength = 8)
            where TTo : unmanaged
        {
            var elementSize = Marshal.SizeOf<TTo>();
            TTo[] rentedBuffer = null;
            Span<TTo> initialBuffer = potentialLength * elementSize <= 512
                ? stackalloc TTo[potentialLength]
                : (rentedBuffer = ArrayPool<TTo>.Shared.Rent(potentialLength));
            try
            {
                var accumulator = new ValueSequenceBuilder<TTo>(initialBuffer);

                foreach (TTo part in parsedSequence)
                    accumulator.Append(part);

                var array = accumulator.AsSpan().ToArray();
                accumulator.Dispose();
                return array;
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<TTo>.Shared.Return(rentedBuffer);
            }
        }

        //TODO test concrete implementation as (sorted)set, read only collection, linked list etc
        [PureMethod]
        public static ICollection<TTo> ToCollection<TTo>(this in ParsedSequence<TTo> parsedSequence,
            CollectionKind kind = CollectionKind.List, ushort potentialLength = 8)
        {
            if (kind == CollectionKind.List || kind == CollectionKind.ReadOnlyCollection)
            {
                var result = new List<TTo>(potentialLength);

                foreach (TTo part in parsedSequence)
                    result.Add(part);

                return kind == CollectionKind.List ?
                    (ICollection<TTo>)result :
                    result.AsReadOnly();
            }
            else if (kind == CollectionKind.HashSet || kind == CollectionKind.SortedSet)
            {
                ISet<TTo> result = kind == CollectionKind.HashSet
                    ? (ISet<TTo>)new HashSet<TTo>(
#if NETSTANDARD2_0
                
#else
                potentialLength
#endif
                        ) 
                    : new SortedSet<TTo>();

                foreach (TTo part in parsedSequence)
                    result.Add(part);

                return result;
            }
            else if (kind == CollectionKind.LinkedList)
            {
                var result = new LinkedList<TTo>();

                foreach (TTo part in parsedSequence)
                    result.AddLast(part);

                return result;
            }
            else
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        [PureMethod]
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this in ParsedPairSequence<TKey, TValue> parsedPairs,
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys,
            ushort potentialLength = 8)
        {
            var result = new Dictionary<TKey, TValue>(potentialLength);

            foreach (var pair in parsedPairs)
            {
                var key = pair.Key;
                var value = pair.Value;
                switch (behaviour)
                {
                    case DictionaryBehaviour.OverrideKeys:
                        result[key] = value; break;
                    case DictionaryBehaviour.DoNotOverrideKeys:
                        if (!result.ContainsKey(key))
                            result.Add(key, value);
                        break;
                    case DictionaryBehaviour.ThrowOnDuplicate:
                        result.Add(key, value); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(behaviour), behaviour, null);
                }
            }

            switch (kind)
            {
                //TODO test empty SortedDictionary+ReadOnlyDictionary and empty collection i.e. hash set
                case DictionaryKind.SortedDictionary:
                    return new SortedDictionary<TKey, TValue>(result);
                case DictionaryKind.ReadOnlyDictionary:
                    return new ReadOnlyDictionary<TKey, TValue>(result);
                default:
                    return result;
            }
        }


        //TODO add UnescapeCharacter with 2 characters + optimize appending to accumulator 
        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart,
            char character)
        {
            int idx = input.IndexOfAny(escapingSequenceStart, character);
            if (idx < 0) return input;
            else
            {
                Span<char> initialBuffer = stackalloc char[Math.Min(input.Length, 256)];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                for (int i = 0; i < input.Length; i++)
                {
                    var current = input[i];
                    if (current == escapingSequenceStart)
                    {
                        i++;
                        if (i == input.Length)
                            accumulator.Append(current);
                        else
                        {
                            current = input[i];
                            if (current == character)
                                accumulator.Append(current);
                            else
                            {
                                accumulator.Append(escapingSequenceStart);
                                accumulator.Append(current);
                            }
                        }
                    }
                    else
                        accumulator.Append(current);
                }

                var array = accumulator.AsSpan().ToArray();
                accumulator.Dispose();
                return array;
            }
        }
    }

    public enum CollectionKind : byte
    {
        List,
        ReadOnlyCollection,
        HashSet,
        SortedSet,
        LinkedList,
    }

    public enum DictionaryKind : byte
    {
        Dictionary,
        SortedDictionary,
        ReadOnlyDictionary
    }

    public enum DictionaryBehaviour : byte
    {
        OverrideKeys,
        DoNotOverrideKeys,
        ThrowOnDuplicate
    }
}
