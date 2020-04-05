using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;
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

        [PureMethod]
        public static TTo[] ToArray<TTo>(this in ParsedSequence<TTo> parsedSequence, ushort capacity = 8)
        {
            var initialBuffer = ArrayPool<TTo>.Shared.Rent(capacity);
            try
            {
                using var accumulator = new ValueSequenceBuilder<TTo>(initialBuffer);

                foreach (TTo part in parsedSequence)
                    accumulator.Append(part);

                return accumulator.AsSpan().ToArray();
            }
            finally
            {
                ArrayPool<TTo>.Shared.Return(initialBuffer);
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

                        foreach (var part in parsedSequence)
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
#if NETSTANDARD2_0 || NETFRAMEWORK
                
#else
                            capacity
#endif
                        )
                            : new SortedSet<TTo>();

                        foreach (var part in parsedSequence)
                            result.Add(part);

                        return (IReadOnlyCollection<TTo>)result;
                    }
                case CollectionKind.LinkedList:
                    {
                        var result = new LinkedList<TTo>();

                        foreach (var part in parsedSequence)
                            result.AddLast(part);

                        return result;
                    }
                case CollectionKind.Stack:
                    {
                        var result = new Stack<TTo>(capacity);

                        foreach (var part in parsedSequence)
                            result.Push(part);

                        return result;
                    }
                case CollectionKind.Queue:
                    {
                        var result = new Queue<TTo>(capacity);

                        foreach (var part in parsedSequence)
                            result.Enqueue(part);

                        return result;
                    }
                case CollectionKind.ObservableCollection:
                    {
                        var result = new ObservableCollection<TTo>();

                        foreach (var part in parsedSequence)
                            result.Add(part);

                        return result;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, $"{nameof(kind)} = '{nameof(CollectionKind)}.{nameof(CollectionKind.Unknown)}' is not supported");
            }
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
                using var accumulator = new ValueSequenceBuilder<T>(initialBuffer);
                 
                accumulator.Append(first);
                accumulator.Append(second);
                accumulator.Append(third);
                accumulator.Append(enumerator.Current); //fourth

                if (enumerator.MoveNext())
                    accumulator.Append(enumerator.Current);
                
                return LeanCollectionFactory.FromArrayChecked(accumulator.AsSpan().ToArray());
            }
            finally
            {
                ArrayPool<T>.Shared.Return(initialBuffer);
            }
        }

        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart, char character)
        {
            int length = input.Length;
            if (length < 2) return input;

            if (input.IndexOf(escapingSequenceStart) is { } escapeStart &&
                escapeStart >= 0 && escapeStart < length - 1 &&
                input.Slice(escapeStart).IndexOf(character) >= 0
            ) //is it worth looking for escape sequence ?
            {
                Span<char> initialBuffer = stackalloc char[Math.Min(length, 256)];
                using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                for (int i = 0; i < length; i++)
                {
                    var current = input[i];
                    if (current == escapingSequenceStart)
                    {
                        i++;
                        if (i == length)
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
                return accumulator.AsSpan().ToArray();
            }
            else return input;
        }

        [PureMethod]
        public static ReadOnlySpan<char> UnescapeCharacter(this in ReadOnlySpan<char> input, char escapingSequenceStart, char character1, char character2)
        {
            int length = input.Length;
            if (length < 2) return input;

            if (input.IndexOf(escapingSequenceStart) is { } escapeStart &&
                escapeStart >= 0 && escapeStart < length - 1 &&
                input.Slice(escapeStart).IndexOfAny(character1, character2) >= 0
            ) //is it worth looking for escape sequence ?
            {
                Span<char> initialBuffer = stackalloc char[Math.Min(length, 256)];
                using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


                for (int i = 0; i < length; i++)
                {
                    var current = input[i];
                    if (current == escapingSequenceStart)
                    {
                        i++;
                        if (i == length)
                            accumulator.Append(current);
                        else
                        {
                            current = input[i];
                            if (current == character1 || current == character2)
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
                
                return accumulator.AsSpan().ToArray();
            }
            else return input;
        }
    }
}
