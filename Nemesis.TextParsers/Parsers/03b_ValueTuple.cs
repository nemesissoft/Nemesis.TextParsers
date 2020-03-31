using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class ValueTupleTransformerCreator : ICanCreateTransformer
    {
        private readonly ITransformerStore _transformerStore;
        public ValueTupleTransformerCreator(ITransformerStore transformerStore) => _transformerStore = transformerStore;

        public ITransformer<TTuple> CreateTransformer<TTuple>()
        {
            if (!TryGetTupleElements(typeof(TTuple), out var elementTypes) || elementTypes == null)
                throw new NotSupportedException($"Type {typeof(TTuple).GetFriendlyName()} is not supported by {GetType().Name}");

            var transType = (elementTypes.Length) switch
            {
                1 => typeof(ValueTuple1Transformer<>),
                2 => typeof(ValueTuple2Transformer<,>),
                3 => typeof(ValueTuple3Transformer<,,>),
                4 => typeof(ValueTuple4Transformer<,,,>),
                5 => typeof(ValueTuple5Transformer<,,,,>),
                6 => typeof(ValueTuple6Transformer<,,,,,>),
                7 => typeof(ValueTuple7Transformer<,,,,,,>),
                8 => typeof(ValueTupleRestTransformer<,,,,,,,>),
                _ => throw new NotSupportedException($"Only ValueTuple with arity 1..{MAX_ARITY} are supported"),
            };
            transType = transType.MakeGenericType(elementTypes);

            return (ITransformer<TTuple>)Activator.CreateInstance(transType, _transformerStore);
        }

        public sealed class ValueTuple1Transformer<T1> : TransformerBase<ValueTuple<T1>>
        {
            private readonly ITransformer<T1> _transformer1;

            private const byte ARITY = 1;

            public ValueTuple1Transformer(ITransformerStore transformerStore) =>
                _transformer1 = transformerStore.GetTransformer<T1>();

            protected override ValueTuple<T1> ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseEnd(ref enumerator, ARITY);

                return new ValueTuple<T1>(t1);
            }

            public override string Format(ValueTuple<T1> element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()})";

            public override ValueTuple<T1> GetEmpty() =>
                new ValueTuple<T1>(
                    _transformer1.GetEmpty()
                );
        }

        public sealed class ValueTuple2Transformer<T1, T2> : TransformerBase<(T1, T2)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;

            private const byte ARITY = 2;

            public ValueTuple2Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
            }

            protected override (T1, T2) ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);


                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2);
            }

            public override string Format((T1, T2) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);


                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()})";

            public override ValueTuple<T1, T2> GetEmpty() =>
                new ValueTuple<T1, T2>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty()
                );
        }

        public sealed class ValueTuple3Transformer<T1, T2, T3> : TransformerBase<(T1, T2, T3)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;

            private const byte ARITY = 3;

            public ValueTuple3Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
            }

            protected override (T1, T2, T3) ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3);
            }

            public override string Format((T1, T2, T3) element)
            {
                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                Helper.StartFormat(ref accumulator);

                Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer3, element.Item3, ref accumulator);


                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3> GetEmpty() =>
                new ValueTuple<T1, T2, T3>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty()
                );
        }

        public sealed class ValueTuple4Transformer<T1, T2, T3, T4> : TransformerBase<(T1, T2, T3, T4)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;

            private const byte ARITY = 4;

            public ValueTuple4Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
                _transformer4 = transformerStore.GetTransformer<T4>();
            }

            protected override (T1, T2, T3, T4) ParseCore(in ReadOnlySpan<char> input)
            {
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
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3, T4> GetEmpty() =>
                new ValueTuple<T1, T2, T3, T4>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty(),
                    _transformer4.GetEmpty()
                );
        }

        public sealed class ValueTuple5Transformer<T1, T2, T3, T4, T5> : TransformerBase<(T1, T2, T3, T4, T5)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;

            private const byte ARITY = 5;

            public ValueTuple5Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
                _transformer4 = transformerStore.GetTransformer<T4>();
                _transformer5 = transformerStore.GetTransformer<T5>();
            }

            protected override (T1, T2, T3, T4, T5) ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5);
            }

            public override string Format((T1, T2, T3, T4, T5) element)
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
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer5, element.Item5, ref accumulator);


                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3, T4, T5> GetEmpty() =>
                new ValueTuple<T1, T2, T3, T4, T5>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty(),
                    _transformer4.GetEmpty(),
                    _transformer5.GetEmpty()
                );
        }

        public sealed class ValueTuple6Transformer<T1, T2, T3, T4, T5, T6> : TransformerBase<(T1, T2, T3, T4, T5, T6)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;

            private const byte ARITY = 6;

            public ValueTuple6Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
                _transformer4 = transformerStore.GetTransformer<T4>();
                _transformer5 = transformerStore.GetTransformer<T5>();
                _transformer6 = transformerStore.GetTransformer<T6>();
            }

            protected override (T1, T2, T3, T4, T5, T6) ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5, t6);
            }

            public override string Format((T1, T2, T3, T4, T5, T6) element)
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
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer5, element.Item5, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer6, element.Item6, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3, T4, T5, T6> GetEmpty() =>
                new ValueTuple<T1, T2, T3, T4, T5, T6>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty(),
                    _transformer4.GetEmpty(),
                    _transformer5.GetEmpty(),
                    _transformer6.GetEmpty()
                );
        }

        public sealed class ValueTuple7Transformer<T1, T2, T3, T4, T5, T6, T7> : TransformerBase<(T1, T2, T3, T4, T5, T6, T7)>
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;
            private readonly ITransformer<T7> _transformer7;

            private const byte ARITY = 7;

            public ValueTuple7Transformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
                _transformer4 = transformerStore.GetTransformer<T4>();
                _transformer5 = transformerStore.GetTransformer<T5>();
                _transformer6 = transformerStore.GetTransformer<T6>();
                _transformer7 = transformerStore.GetTransformer<T7>();
            }

            protected override (T1, T2, T3, T4, T5, T6, T7) ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseNext(ref enumerator, 7);
                var t7 = Helper.ParseElement(ref enumerator, _transformer7);

                Helper.ParseEnd(ref enumerator, ARITY);

                return (t1, t2, t3, t4, t5, t6, t7);
            }

            public override string Format((T1, T2, T3, T4, T5, T6, T7) element)
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
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer5, element.Item5, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer6, element.Item6, ref accumulator);
                Helper.AddDelimiter(ref accumulator);

                Helper.FormatElement(_transformer7, element.Item7, ref accumulator);

                Helper.EndFormat(ref accumulator);
                var text = accumulator.AsSpan().ToString();
                accumulator.Dispose();
                return text;
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()},{typeof(T7).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3, T4, T5, T6, T7> GetEmpty() =>
                new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty(),
                    _transformer4.GetEmpty(),
                    _transformer5.GetEmpty(),
                    _transformer6.GetEmpty(),
                    _transformer7.GetEmpty()
                );
        }

        public sealed class ValueTupleRestTransformer<T1, T2, T3, T4, T5, T6, T7, TRest> : TransformerBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>> where TRest : struct
        {
            private readonly ITransformer<T1> _transformer1;
            private readonly ITransformer<T2> _transformer2;
            private readonly ITransformer<T3> _transformer3;
            private readonly ITransformer<T4> _transformer4;
            private readonly ITransformer<T5> _transformer5;
            private readonly ITransformer<T6> _transformer6;
            private readonly ITransformer<T7> _transformer7;
            private readonly ITransformer<TRest> _transformerRest;

            private const byte ARITY = 8;

            public ValueTupleRestTransformer(ITransformerStore transformerStore)
            {
                _transformer1 = transformerStore.GetTransformer<T1>();
                _transformer2 = transformerStore.GetTransformer<T2>();
                _transformer3 = transformerStore.GetTransformer<T3>();
                _transformer4 = transformerStore.GetTransformer<T4>();
                _transformer5 = transformerStore.GetTransformer<T5>();
                _transformer6 = transformerStore.GetTransformer<T6>();
                _transformer7 = transformerStore.GetTransformer<T7>();
                _transformerRest = transformerStore.GetTransformer<TRest>();
            }

            protected override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> ParseCore(in ReadOnlySpan<char> input)
            {
                var enumerator = Helper.ParseStart(input, ARITY);

                var t1 = Helper.ParseElement(ref enumerator, _transformer1);

                Helper.ParseNext(ref enumerator, 2);
                var t2 = Helper.ParseElement(ref enumerator, _transformer2);

                Helper.ParseNext(ref enumerator, 3);
                var t3 = Helper.ParseElement(ref enumerator, _transformer3);

                Helper.ParseNext(ref enumerator, 4);
                var t4 = Helper.ParseElement(ref enumerator, _transformer4);

                Helper.ParseNext(ref enumerator, 5);
                var t5 = Helper.ParseElement(ref enumerator, _transformer5);

                Helper.ParseNext(ref enumerator, 6);
                var t6 = Helper.ParseElement(ref enumerator, _transformer6);

                Helper.ParseNext(ref enumerator, 7);
                var t7 = Helper.ParseElement(ref enumerator, _transformer7);

                Helper.ParseNext(ref enumerator, 8);
                var tRest = Helper.ParseElement(ref enumerator, _transformerRest);

                Helper.ParseEnd(ref enumerator, ARITY);

                return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(t1, t2, t3, t4, t5, t6, t7, tRest);
            }

            public override string Format(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> element)
            {
                var initialBuffer = ArrayPool<char>.Shared.Rent(32);
                try
                {
                    var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
                    Helper.StartFormat(ref accumulator);

                    Helper.FormatElement(_transformer1, element.Item1, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer2, element.Item2, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer3, element.Item3, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer4, element.Item4, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer5, element.Item5, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer6, element.Item6, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformer7, element.Item7, ref accumulator);
                    Helper.AddDelimiter(ref accumulator);

                    Helper.FormatElement(_transformerRest, element.Rest, ref accumulator);

                    Helper.EndFormat(ref accumulator);
                    var text = accumulator.AsSpan().ToString();
                    accumulator.Dispose();
                    return text;
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(initialBuffer);
                }
            }

            public override string ToString() => $"Transform ({typeof(T1).GetFriendlyName()},{typeof(T2).GetFriendlyName()},{typeof(T3).GetFriendlyName()},{typeof(T4).GetFriendlyName()},{typeof(T5).GetFriendlyName()},{typeof(T6).GetFriendlyName()},{typeof(T7).GetFriendlyName()},{typeof(TRest).GetFriendlyName()})";

            public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> GetEmpty() =>
                new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(
                    _transformer1.GetEmpty(),
                    _transformer2.GetEmpty(),
                    _transformer3.GetEmpty(),
                    _transformer4.GetEmpty(),
                    _transformer5.GetEmpty(),
                    _transformer6.GetEmpty(),
                    _transformer7.GetEmpty(),
                    _transformerRest.GetEmpty()
                );
        }

        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
        [SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
        public static readonly TupleHelper Helper = new TupleHelper(
                tupleDelimiter: ',', nullElementMarker: '∅', escapingSequenceStart: '\\',
                tupleStart: '(', tupleEnd: ')');

        private const byte MAX_ARITY = 8;

        public bool CanHandle(Type type) =>
            TryGetTupleElements(type, out var elementTypes) &&
            elementTypes != null &&
            elementTypes.Length is { } arity && arity <= MAX_ARITY && arity >= 1 &&
            elementTypes.All(t => _transformerStore.IsSupportedForTransformation(t))
            ;

        private static bool TryGetTupleElements(Type type, out Type[] elementTypes)
        {
            bool isValueTuple = type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition &&
#if NETSTANDARD2_0 || NETFRAMEWORK
            type.Namespace == "System" &&
            type.Name.StartsWith("ValueTuple`") &&
            typeof(ValueType).IsAssignableFrom(type);
#else
            typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(type);
#endif

            elementTypes = isValueTuple ? type.GenericTypeArguments : null;

            return isValueTuple;
        }

        public sbyte Priority => 12;
    }
}
