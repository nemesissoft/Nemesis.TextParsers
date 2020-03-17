using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

/*TODO
 more than 8 params 
tests 
recursive tests + exploratory tests 
    */

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>()
        {
            var type = typeof(TDeconstructable);

            if (!TryGetDeconstruct(type, out var genesisPair) ||
                genesisPair.deconstruct.GetParameters().Length == 0 ||
                genesisPair.ctor.GetParameters().Length == 0
               )
                throw new NotSupportedException($"{nameof(DeconstructionTransformerCreator)} supports cases with at lease one non-nullary {DECONSTRUCT} method with matching constructor");

            var transType = typeof(DeconstructionTransformer<>).MakeGenericType(type);

            return (ITransformer<TDeconstructable>)Activator
                .CreateInstance(transType, genesisPair.deconstruct, genesisPair.ctor);
        }

        private sealed class DeconstructionTransformer<TDeconstructable> : TransformerBase<TDeconstructable>
        {
            private delegate TDeconstructable ParserDelegate(ReadOnlySpan<char> input, TupleHelper helper, ITransformer[] transformers);
            private delegate string FormatterDelegate(TDeconstructable element, TupleHelper helper, ITransformer[] transformers);

            private readonly ConstructorInfo _ctor;
            private readonly ITransformer[] _transformers;
            private readonly ParserDelegate _parser;
            private readonly FormatterDelegate _formatter;

            public DeconstructionTransformer(MethodInfo deconstruct, ConstructorInfo ctor)
            {
                _ctor = ctor;
                _transformers = ctor.GetParameters()
                 .Select(p => TextTransformer.Default.GetTransformer(p.ParameterType))
                 .ToArray();
                //TODO add debug sanctify check deconstruct ~= ctor

                _parser = CreateParser(ctor);
                _formatter = CreateFormatter(deconstruct);
            }

            private static ParserDelegate CreateParser(ConstructorInfo ctor)
            {
                var @params = ctor.GetParameters();
                byte arity = (byte)@params.Length;

                var input = Expression.Parameter(typeof(ReadOnlySpan<char>), "input");
                var helper = Expression.Parameter(typeof(TupleHelper), "helper");
                var transformers = Expression.Parameter(typeof(ITransformer[]), "transformers");

                var enumerator = Expression.Variable(typeof(TokenSequence<char>.TokenSequenceEnumerator), "enumerator");
                var fields = @params.Select((p, i) => Expression.Variable(p.ParameterType, $"t{i + 1}")).ToList();

                var expressions = new List<Expression>(5 + arity * 2)
                {
                    Expression.Assign(enumerator,
                        Expression.Call(helper, nameof(TupleHelper.ParseStart), null, input, Expression.Constant(arity))
                        )
                };

                for (int i = 0; i < arity; i++)
                {
                    if (i > 0)
                        expressions.Add(
                            Expression.Call(helper, nameof(TupleHelper.ParseNext), null,
                                enumerator, Expression.Constant((byte)(i + 1)))
                        );

                    var field = fields[i];

                    var trans = Expression.Convert(
                        Expression.ArrayIndex(transformers, Expression.Constant(i)),
                        typeof(ISpanParser<>).MakeGenericType(field.Type)
                    );


                    var assignment = Expression.Assign(
                        field,
                        Expression.Call(helper, nameof(TupleHelper.ParseElement), new[] { field.Type }, enumerator, trans)
                    );
                    expressions.Add(assignment);
                }

                expressions.Add(
                    Expression.Call(helper, nameof(TupleHelper.ParseEnd), null, enumerator, Expression.Constant(arity))
                );
                expressions.Add(
                    Expression.New(ctor, fields)
                    );

                var body = Expression.Block(
                    new[] { enumerator }.Concat(fields),
                    expressions);

                var λ = Expression.Lambda<ParserDelegate>(body, input, helper, transformers);
                return λ.Compile();
            }

            private FormatterDelegate CreateFormatter(MethodInfo deconstruct)
            {
                //TODO
                return null;
            }

            /*public override (T1, T2, T3, T4) Parse(in ReadOnlySpan<char> input)
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
            }*/

            public override TDeconstructable Parse(in ReadOnlySpan<char> input) =>
                input.IsEmpty ? default : _parser(input, _helper, _transformers);

            public override string Format(TDeconstructable element) =>
                element is null ? null : _formatter(element, _helper, _transformers);

            public override string ToString() =>
                $"Transform {typeof(TDeconstructable).GetFriendlyName()} by deconstruction into ({string.Join(", ", _ctor?.GetParameters().Select(p => p.ParameterType.GetFriendlyName()) ?? Enumerable.Empty<string>())})";
        }

        //TODO: make non-static add option for transformer: char? bracketChar, char delimiter ...
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue")]
        [SuppressMessage("ReSharper", "ArgumentsStyleLiteral")]
        private static readonly TupleHelper _helper = new TupleHelper(
            tupleDelimiter: ',',
            nullElementMarker: '∅',
            escapingSequenceStart: '\\',
            tupleStart: '(',
            tupleEnd: ')');

        private const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private const string DECONSTRUCT = "Deconstruct";
        //TODO should we support also Deconstruct extension methods ?
        //TODO support only non-byref types in ctor
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
                    static Type FlattenRef(Type type) =>
                        type.IsByRef ? type.GetElementType() : type;

                    // ReSharper disable once LoopCanBeConvertedToQuery
                    for (var i = 0; i < left.Count; i++)
                        if (FlattenRef(left[i].ParameterType)
                                        !=
                            FlattenRef(right[i].ParameterType)
                           )
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

        public sbyte Priority => 126; //TODO should that be the last transformer in responsibility chain ?
    }
}
