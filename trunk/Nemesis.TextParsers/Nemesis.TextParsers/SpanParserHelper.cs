using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Nemesis.Essentials.Runtime;
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
            char nullElement, char allowedEscapeCharacter1 = default) =>
            new ParsedSequence<TTo>(tokenSource, escapingElement, nullElement, allowedEscapeCharacter1);

        //TODO ToArray - 2 versions from managed and unmanaged. ToArrayUnmanaged - copy to buffer bool TryCopyTo(buffer, out int count
        [PureMethod]
        public static TTo[] ToArray<TTo>(this in ParsedSequence<TTo> parsedSequence, ushort capacity = 8)
        {
            var initialBuffer = ArrayPool<TTo>.Shared.Rent(capacity);
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
        public static TTo[] ToArrayUnmanaged<TTo>(this in ParsedSequence<TTo> parsedSequence, ushort capacity = 8)
            where TTo : unmanaged
        {
            var elementSize = Marshal.SizeOf<TTo>();
            TTo[] rentedBuffer = null;
            Span<TTo> initialBuffer = capacity * elementSize <= 512
                ? stackalloc TTo[capacity]
                : (rentedBuffer = ArrayPool<TTo>.Shared.Rent(capacity));
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


        [PureMethod]
        public static IReadOnlyCollection<TTo> ToCollection<TTo>(this in ParsedSequence<TTo> parsedSequence,
            CollectionKind kind = CollectionKind.List, ushort capacity = 8)
        {
            switch (kind)
            {
                case CollectionKind.List:
                case CollectionKind.ReadOnlyCollection:
                    {
                        var result = new List<TTo>(capacity);

                        foreach (TTo part in parsedSequence)
                            result.Add(part);

                        return kind == CollectionKind.List ?
                            (IReadOnlyCollection<TTo>)result :
                            result.AsReadOnly();
                    }
                case CollectionKind.HashSet:
                case CollectionKind.SortedSet:
                    {
                        ISet<TTo> result = kind == CollectionKind.HashSet
                            ? (ISet<TTo>)new HashSet<TTo>(
#if NETSTANDARD2_0
                
#else
                            capacity
#endif
                        )
                            : new SortedSet<TTo>();

                        foreach (TTo part in parsedSequence)
                            result.Add(part);

                        return (IReadOnlyCollection<TTo>)result;
                    }
                case CollectionKind.LinkedList:
                    {
                        var result = new LinkedList<TTo>();

                        foreach (TTo part in parsedSequence)
                            result.AddLast(part);

                        return result;
                    }
                case CollectionKind.Stack:
                    {
                        var result = new Stack<TTo>(capacity);

                        foreach (TTo part in parsedSequence)
                            result.Push(part);

                        return result;
                    }
                case CollectionKind.Queue:
                    {
                        var result = new Queue<TTo>(capacity);

                        foreach (TTo part in parsedSequence)
                            result.Enqueue(part);

                        return result;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, $"{nameof(kind)} = '{nameof(CollectionKind)}.{nameof(CollectionKind.Unknown)}' is not supported");
            }
        }

        [PureMethod]
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this in ParsedPairSequence<TKey, TValue> parsedPairs,
            DictionaryKind kind = DictionaryKind.Dictionary, DictionaryBehaviour behaviour = DictionaryBehaviour.OverrideKeys,
            ushort capacity = 8)
        {
            if (kind == DictionaryKind.Unknown)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, $"{nameof(kind)} = '{nameof(DictionaryKind)}.{nameof(DictionaryKind.Unknown)}' is not supported");

            IDictionary<TKey, TValue> result =
                kind == DictionaryKind.SortedDictionary ? new SortedDictionary<TKey, TValue>() :
                 (
                     kind == DictionaryKind.SortedList
                     ? new SortedList<TKey, TValue>(capacity)
                     : (IDictionary<TKey, TValue>)new Dictionary<TKey, TValue>(capacity)
                 );

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
                        if (!result.ContainsKey(key))
                            result.Add(key, value);
                        else
                            throw new ArgumentException($"The key '{key}' has already been added");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(behaviour), behaviour, null);
                }
            }

            return kind == DictionaryKind.ReadOnlyDictionary
                ? new ReadOnlyDictionary<TKey, TValue>(result)
                : result;
        }


        [PureMethod]
        public static LeanCollection<T> ToLeanCollection<T>(this in ParsedSequence<T> parsedSequence)
        {
            var enumerator = parsedSequence.GetEnumerator();

            if (!enumerator.MoveNext()) return new LeanCollection<T>();
            var first = enumerator.Current;

            if (!enumerator.MoveNext()) return new LeanCollection<T>(first);
            var second = enumerator.Current;

            if (!enumerator.MoveNext()) return new LeanCollection<T>(first, second);
            var third = enumerator.Current;

            if (!enumerator.MoveNext()) return new LeanCollection<T>(first, second, third);


            var initialBuffer = ArrayPool<T>.Shared.Rent(8);
            try
            {
                var accumulator = new ValueSequenceBuilder<T>(initialBuffer);
                accumulator.Append(first);
                accumulator.Append(second);
                accumulator.Append(third);
                accumulator.Append(enumerator.Current); //fourth

                if (enumerator.MoveNext())
                    accumulator.Append(enumerator.Current);

                var array = accumulator.AsSpan().ToArray();
                accumulator.Dispose();
                return LeanCollection<T>.FromArrayChecked(array);
            }
            finally
            {
                ArrayPool<T>.Shared.Return(initialBuffer);
            }
        }

        //TODO add UnescapeCharacter with 2 characters + optimize appending to accumulator 
        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart, char character)
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

        internal static TDest ConvertElement<TDest>(object element)
        {
            try
            {
                if (element is TDest dest)
                    return dest;
                else if (element is null)
                    return default;
                else if (typeof(TDest) == typeof(string) && element is IFormattable formattable)
                    return (TDest)(object)formattable.ToString(null, CultureInfo.InvariantCulture);
                else
                    return (TDest)TypeMeta.GetDefault(typeof(TDest));
            }
            catch (Exception) { return default; }
        }
    }

    public enum DictionaryBehaviour : byte
    {
        OverrideKeys,
        DoNotOverrideKeys,
        ThrowOnDuplicate
    }
}
