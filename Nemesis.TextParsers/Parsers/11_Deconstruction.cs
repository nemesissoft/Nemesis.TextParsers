using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

/*TODO
 more than 8 params 
tests 
recursive tests
    */

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>()
        {
            var type = typeof(TDeconstructable);

            if (!TryGetDeconstruct(type, out var genesisPair))
                throw new NotSupportedException($"{nameof(DeconstructionTransformerCreator)} supports cases with at lease one {DECONSTRUCT} method with matching constructor");


            var transType = typeof(DeconstructionTransformer<>).MakeGenericType(type);

            return (ITransformer<TDeconstructable>)Activator.CreateInstance(transType);
        }

        private sealed class DeconstructionTransformer<TDeconstructable> : TransformerBase<TDeconstructable>
        {
            private readonly ITransformer[] _transformers;
            private readonly MethodInfo _deconstruct;
            private readonly ConstructorInfo _ctor;

            public DeconstructionTransformer(MethodInfo deconstruct, ConstructorInfo ctor)
            {
                _deconstruct = deconstruct;
                _ctor = ctor;
                _transformers = _deconstruct.GetParameters()
                    .Select(p => TextTransformer.Default.GetTransformer(p.ParameterType))
                    .ToArray();
            }


            /*public override (T1, T2, T3, T4) Parse(in ReadOnlySpan<char> input)
            {
                if (input.IsEmpty) return default;

                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4);
            }

            public override string Format((T1, T2, T3, T4) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer3, element.Item3, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer4, element.Item4, ref accumulator);


                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }*/
            
            public override TDeconstructable Parse(in ReadOnlySpan<char> input)
            {
                throw new NotImplementedException();
            }

            public override string Format(TDeconstructable element)
            {
                throw new NotImplementedException();
            }

            public override string ToString() => 
                $"Transform {typeof(TDeconstructable).GetFriendlyName()} by deconstruction into ({string.Join(", ", _deconstruct?.GetParameters().Select(p => p.ParameterType.GetFriendlyName()) ?? Enumerable.Empty<string>())})";
        }

        private static class Helper
        {
            private const char TUPLE_DELIMITER = ',';
            private const char NULL_ELEMENT_MARKER = '∅';
            private const char ESCAPING_SEQUENCE_START = '\\';
            private const char TUPLE_START = '(';
            private const char TUPLE_END = ')';

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void StartFormat(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_START);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EndFormat(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_END);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void AddDelimiter(ref ValueSequenceBuilder<char> accumulator) =>
                accumulator.Append(TUPLE_DELIMITER);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TokenSequence<char>.TokenSequenceEnumerator ParseStart(ReadOnlySpan<char> input, byte arity)
            {
                input = UnParenthesize(input);

                var kvpTokens = input.Tokenize(TUPLE_DELIMITER, ESCAPING_SEQUENCE_START, true);
                var enumerator = kvpTokens.GetEnumerator();

                if (!enumerator.MoveNext())
                    throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' was not found");

                return enumerator;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span)
            {
                int length = span.Length;
                if (length < 2) throw GetStateException();

                int start = 0;
                for (; start < length; start++)
                    if (!char.IsWhiteSpace(span[start]))
                        break;

                bool tupleStartsWithParenthesis = start < span.Length && span[start] == TUPLE_START;

                if (!tupleStartsWithParenthesis) throw GetStateException();

                int end = span.Length - 1;
                for (; end > start; end--)
                    if (!char.IsWhiteSpace(span[end]))
                        break;

                bool tupleEndsWithParenthesis = end > 0 && span[end] == TUPLE_END;

                if (!tupleEndsWithParenthesis) throw GetStateException();

                return span.Slice(start + 1, end - start - 1);

                static Exception GetStateException() => new ArgumentException(
                         "Tuple representation has to start and end with parentheses optionally lead in the beginning or trailed in the end by whitespace");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ParseNext(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte index)
            {
                static string ToOrdinal(byte number)
                {
                    int rem = number % 100;
                    if (rem >= 11 && rem <= 13) return $"{number}th";

                    return (number % 10) switch
                    {
                        1 => $"{number}st",
                        2 => $"{number}nd",
                        3 => $"{number}rd",
                        _ => $"{number}th",
                    };
                }

                if (!enumerator.MoveNext())
                    throw new ArgumentException($"{ToOrdinal(index)} tuple element was not found");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ParseEnd(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, byte arity)
            {
                if (enumerator.MoveNext())
                {
                    var remaining = enumerator.Current.ToString();
                    throw new ArgumentException($@"Tuple of arity={arity} separated by '{TUPLE_DELIMITER}' cannot have more than {arity} elements: '{remaining}'");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TElement ParseElement<TElement>(ref TokenSequence<char>.TokenSequenceEnumerator enumerator, ISpanParser<TElement> parser)
            {
                ReadOnlySpan<char> input = enumerator.Current;
                var unescapedInput = input.UnescapeCharacter(ESCAPING_SEQUENCE_START, TUPLE_DELIMITER);

                if (unescapedInput.Length == 1 && unescapedInput[0].Equals(NULL_ELEMENT_MARKER))
                    return default;
                else
                {
                    unescapedInput = unescapedInput.UnescapeCharacter
                            (ESCAPING_SEQUENCE_START, NULL_ELEMENT_MARKER, ESCAPING_SEQUENCE_START);

                    return parser.Parse(unescapedInput);
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void FormatElement<TElement>(IFormatter<TElement> formatter, TElement element, ref ValueSequenceBuilder<char> accumulator)
            {
                string elementText = formatter.Format(element);
                if (elementText == null)
                    accumulator.Append(NULL_ELEMENT_MARKER);
                else
                {
                    foreach (char c in elementText)
                    {
                        if (c == ESCAPING_SEQUENCE_START || c == NULL_ELEMENT_MARKER || c == TUPLE_DELIMITER)
                            accumulator.Append(ESCAPING_SEQUENCE_START);
                        accumulator.Append(c);
                    }
                }
            }
        }

        private const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private const string DECONSTRUCT = "Deconstruct";
        private static bool TryGetDeconstruct(Type type, out (MethodInfo deconstruct, ConstructorInfo ctor) result)
        {
            result = default;

            var ctors = type.GetConstructors(ALL_FLAGS).Select(c => (ctor: c, @params: c.GetParameters())).ToList();
            if (ctors.Count == 0) return false;

            var deconstructs = type
                .GetMethods(ALL_FLAGS)
                .Select(m => (method: m, @params: m.GetParameters()))
                .Where(pair => string.Equals(pair.method.Name, DECONSTRUCT, StringComparison.Ordinal) &&
                               pair.@params.Length > 0 &&
                               pair.@params.All(p => p.IsOut) //TODO that is not necessary ?
                                                              //TODO + check if param type is supported for transformation ?
                )
                .OrderByDescending(p => p.@params.Length);

            static bool IsCompatible(IReadOnlyList<ParameterInfo> left, IReadOnlyList<ParameterInfo> right)
            {
                bool AreEqualByParamTypes()
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    for (var i = 0; i < left.Count; i++)
                        if (left[i].ParameterType != right[i].ParameterType)
                            return false;
                    return true;
                }

                return left != null && right != null && left.Count == right.Count && AreEqualByParamTypes();
            }

            foreach (var (method, @params) in deconstructs)
            {
                var ctorPair = ctors.FirstOrDefault(p => IsCompatible(p.@params, @params));
                if (ctorPair.ctor is { } ctor)
                {
                    result = (method, ctor);
                    return true;
                }
            }

            return false;
        }

        public bool CanHandle(Type type) => TryGetDeconstruct(type, out _);

        public sbyte Priority => 126;
    }
}
