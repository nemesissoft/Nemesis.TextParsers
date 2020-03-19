using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

using S = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>()
            => S.Default.ToTransformer<TDeconstructable>();

        public bool CanHandle(Type type) => S.TryGetDefaultDeconstruct(type, out _, out _);

        public sbyte Priority => 110;
    }

    public enum DeconstructionMethod : byte
    {
        /// <summary>
        /// Use Deconstruct provided in parsed type. Choose overload with larges number of parameters with matching constructor 
        /// </summary>
        DefaultConstructorDeconstructPair = 0,
        /// <summary>
        /// Use deconstruct-like method provided by other parameter i.e. void DeconstructMe(this instance, out field1, out field2). If method is static then first parameter should be of Deconstructed type  
        /// </summary>
        ProvidedDeconstructMethod = 1,
    }

    public sealed class DeconstructionTransformerSettings
    {
        public char Delimiter { get; private set; } = ';';
        public char NullElementMarker { get; private set; } = '∅';
        public char EscapingSequenceStart { get; private set; } = '\\';
        public char? Start { get; private set; } = '(';
        public char? End { get; private set; } = ')';

        public DeconstructionMethod Mode { get; private set; } = DeconstructionMethod.DefaultConstructorDeconstructPair;
        public MethodInfo Deconstruct { get; private set; }
        public ConstructorInfo Ctor { get; private set; }

        public override string ToString() =>
            $@"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}' Mode = {Mode}. 
Deconstructed by {(Deconstruct == null ? "<default>" : $"{Deconstruct.DeclaringType.GetFriendlyName()}.{Deconstruct.Name}({string.Join(", ", Deconstruct.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})")}. 
Constructed by {(Ctor == null ? "<default>" : $"new {Ctor.DeclaringType.GetFriendlyName()}({string.Join(", ", Ctor.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})")}.";

        private DeconstructionTransformerSettings() { }
        /// <summary>
        /// Get default instance. Always return new instance 
        /// </summary>
        public static DeconstructionTransformerSettings Default => new DeconstructionTransformerSettings();


        #region With
        [PublicAPI]
        public S WithDelimiter(char delimiter) { Delimiter = delimiter; return this; }

        [PublicAPI]
        public S WithNullElementMarker(char nullElementMarker) { NullElementMarker = nullElementMarker; return this; }

        [PublicAPI]
        public S WithEscapingSequenceStart(char escapingSequenceStart) { EscapingSequenceStart = escapingSequenceStart; return this; }

        [PublicAPI]
        public S WithStart(char start) { Start = start; return this; }

        [PublicAPI]
        public S WithEnd(char end) { End = end; return this; }

        [PublicAPI]
        public S WithBorders(char start, char end) { (Start, End) = (start, end); return this; }

        [PublicAPI]
        public S WithoutBorders() { (Start, End) = (null, null); return this; }

        [PublicAPI]
        public S WithDefaultDeconstruction()
        {
            Mode = DeconstructionMethod.DefaultConstructorDeconstructPair;
            Deconstruct = null;
            Ctor = null;

            return this;
        }

        [PublicAPI]
        public S WithCustomDeconstruction([NotNull] MethodInfo deconstruct, [NotNull] ConstructorInfo ctor)
        {
            Mode = DeconstructionMethod.ProvidedDeconstructMethod;
            Deconstruct = deconstruct ?? throw new ArgumentNullException(nameof(deconstruct));
            Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));

            return this;
        }

        #endregion


        public const string DECONSTRUCT = "Deconstruct";
        public static bool TryGetDefaultDeconstruct(Type type, out MethodInfo deconstruct, out ConstructorInfo ctor)
        {
            const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            deconstruct = default;
            ctor = default;

            var ctors = type.GetConstructors(ALL_FLAGS).Select(c => (ctor: c, @params: c.GetParameters())).ToList();
            if (ctors.Count == 0) return false;

            var deconstructs = type
                .GetMethods(ALL_FLAGS)
                .Select(m => (method: m, @params: m.GetParameters()))
                .Where(pair => string.Equals(pair.method.Name, DECONSTRUCT, StringComparison.Ordinal) &&
                               pair.@params.Length > 0 &&
                               pair.@params.All(p => p.IsOut) //TODO + check if param type is supported for transformation+recurrence  ?
                )
                .OrderByDescending(p => p.@params.Length);

            foreach (var (method, @params) in deconstructs)
            {
                var ctorPair = ctors.FirstOrDefault(p => IsCompatible(p.@params, @params));
                if (ctorPair.ctor is { } c)
                {
                    deconstruct = method;
                    ctor = c;

                    return true;
                }
            }

            return false;
        }

        private static bool IsCompatible(IReadOnlyList<ParameterInfo> left, IReadOnlyList<ParameterInfo> right)
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

        public ITransformer<TDeconstructable> ToTransformer<TDeconstructable>()
        {
            // ReSharper disable ArgumentsStyleNamedExpression
            var helper = new TupleHelper(tupleDelimiter: Delimiter, nullElementMarker: NullElementMarker,
                escapingSequenceStart: EscapingSequenceStart, tupleStart: Start, tupleEnd: End);
            // ReSharper restore ArgumentsStyleNamedExpression

            MethodInfo deconstruct;
            ConstructorInfo ctor;
            switch (Mode)
            {
                case DeconstructionMethod.DefaultConstructorDeconstructPair:
                    {
                        if (!TryGetDefaultDeconstruct(typeof(TDeconstructable), out deconstruct, out ctor) ||
                            deconstruct is null || deconstruct.GetParameters().Length == 0 ||
                            ctor is null || ctor.GetParameters().Length == 0
                        )
                            throw new NotSupportedException($"Default deconstruction method supports cases with at lease one non-nullary {DECONSTRUCT} method with matching constructor");
                        break;
                    }
                case DeconstructionMethod.ProvidedDeconstructMethod:
                    deconstruct = Deconstruct;
                    ctor = Ctor;
                    break;
                default:
                    throw new NotSupportedException($"{nameof(Mode)} = {Mode} is not supported");
            }
            if (deconstruct is null || ctor is null)
                throw new NotSupportedException($"{DECONSTRUCT} and constructor have to be provided");

            if (deconstruct.IsStatic)
            {
                if (deconstruct.GetParameters() is { } dp && ctor.GetParameters() is { } cp && (
                    dp.Length < 2 ||
                    dp.Length != cp.Length + 1 ||
                    !dp[0].ParameterType.IsAssignableFrom(typeof(TDeconstructable)) ||
                    !IsCompatible(dp.Skip(1).ToList(), cp)
                ))
                    throw new NotSupportedException(
                        $"Static {DECONSTRUCT} method has to be compatible with provided constructor and should have one additional parameter in the beginning - deconstructable instance");

                if (deconstruct.GetParameters().Skip(1).Any(p => !p.IsOut))
                    throw new NotSupportedException(
                        $"Static {DECONSTRUCT} method must have all but first params as out params (IsOut==true)");
            }
            else
            {
                if (deconstruct.GetParameters() is { } dp && (
                    dp.Length == 0 ||
                    !IsCompatible(dp, ctor.GetParameters())
                ))
                    throw new NotSupportedException(
                        $"Instance {DECONSTRUCT} method has to be compatible with provided constructor and should have same number of parameters");

                if (deconstruct.GetParameters().Any(p => !p.IsOut))
                    throw new NotSupportedException(
                        $"Instance {DECONSTRUCT} method must have all out params (IsOut==true)");
            }


            var transformers = ctor.GetParameters()
                     .Select(p => TextTransformer.Default.GetTransformer(p.ParameterType))
                     .ToArray();

            var parser = DeconstructionTransformer<TDeconstructable>.CreateParser(ctor);
            var formatter = DeconstructionTransformer<TDeconstructable>.CreateFormatter(deconstruct);

            return new DeconstructionTransformer<TDeconstructable>(helper, transformers, parser, formatter);
        }
    }

    internal sealed class DeconstructionTransformer<TDeconstructable> : TransformerBase<TDeconstructable>
    {
        public delegate TDeconstructable ParserDelegate(ReadOnlySpan<char> input, TupleHelper helper, ITransformer[] transformers);
        public delegate string FormatterDelegate(TDeconstructable element, TupleHelper helper, ITransformer[] transformers);


        private readonly TupleHelper _helper;
        private readonly ITransformer[] _transformers;
        private readonly ParserDelegate _parser;
        private readonly FormatterDelegate _formatter;

        internal DeconstructionTransformer(TupleHelper helper, ITransformer[] transformers, ParserDelegate parser, FormatterDelegate formatter)
        {
            _helper = helper;
            _transformers = transformers;
            _parser = parser;
            _formatter = formatter;
        }

        /*
         public override (T1, T2, T3, T4) Parse(in ReadOnlySpan<char> input)
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
         }*/
        internal static ParserDelegate CreateParser(ConstructorInfo ctor)
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
        /*
         public string Format((T1, T2, T3) element)
         {
             var initialBuffer = ArrayPool<char>.Shared.Rent(10);
             var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

             try
             {
                 var (temp1, temp2, temp3) = element;

                 Helper.StartFormat(ref accumulator);

                 Helper.FormatElement(_transformer1, temp1, ref accumulator);

                 Helper.AddDelimiter(ref accumulator);
                 Helper.FormatElement(_transformer2, temp2, ref accumulator);

                 Helper.AddDelimiter(ref accumulator);
                 Helper.FormatElement(_transformer3, temp3, ref accumulator);


                 Helper.EndFormat(ref accumulator);
                 return accumulator.AsSpan().ToString();                    
             }
             finally
             {
                 accumulator.Dispose();
                 ArrayPool<char>.Shared.Return(initialBuffer);
             }
         }*/
        internal static FormatterDelegate CreateFormatter(MethodInfo deconstruct)
        {
            CreateFormatterData(deconstruct,
                out int arity, out var element, out var helper,
                out var transformers, out var accumulator, out var initialBuffer,
                out var temps, out var rentInitialBuffer, out var returnInitialBuffer,
                out var accumulatorInit, out var accumulatorDispose, out var accumulatorToString
            );


            var expressions = new List<Expression>(7 + 2 * arity)
            {
                rentInitialBuffer,
                accumulatorInit,
                deconstruct.IsStatic
                    ? Expression.Call(deconstruct, new[] { element }.Concat(temps)) //TODO add convert
                    : Expression.Call(element, deconstruct, temps),
                Expression.Call(helper, nameof(TupleHelper.StartFormat), null, accumulator)
            };

            for (int i = 0; i < arity; i++)
            {
                if (i > 0)
                    expressions.Add(
                        Expression.Call(helper, nameof(TupleHelper.AddDelimiter), null, accumulator)
                    );

                var temp = temps[i];

                var formatter = Expression.Convert(
                    Expression.ArrayIndex(transformers, Expression.Constant(i)),
                    typeof(IFormatter<>).MakeGenericType(temp.Type)
                );

                var format = Expression.Call(helper, nameof(TupleHelper.FormatElement), new[] { temp.Type },
                        formatter, temp, accumulator);
                expressions.Add(format);
            }

            expressions.Add(Expression.Call(helper, nameof(TupleHelper.EndFormat), null, accumulator));

            var text = Expression.Variable(typeof(string), "text");
            expressions.Add(
                Expression.Assign(text, accumulatorToString)
                );
            expressions.Add(returnInitialBuffer);
            expressions.Add(text);


            var tryBody = Expression.Block(
                new[] { accumulator, initialBuffer, text }.Concat(temps),
                expressions);

            var finallyBody = Expression.Block(
                new[] { accumulator },
                accumulatorDispose);

            var tryFinally = Expression.TryFinally(tryBody, finallyBody);


            var λ = Expression.Lambda<FormatterDelegate>(tryFinally, element, helper, transformers);
            return λ.Compile();
        }

        private static void CreateFormatterData(MethodInfo deconstruct,
            out int arity, out ParameterExpression element, out ParameterExpression helper,
            out ParameterExpression transformers, out ParameterExpression accumulator, out ParameterExpression initialBuffer,
            out IReadOnlyList<ParameterExpression> temps, out Expression rentInitialBuffer, out Expression returnInitialBuffer,
            out Expression accumulatorInit, out Expression accumulatorDispose, out Expression accumulatorToString)
        {
            static Type FlattenRef(Type type) => type.IsByRef ? type.GetElementType() : type;

            var @params = deconstruct.IsStatic
                ? deconstruct.GetParameters().Skip(1).ToArray()
                : deconstruct.GetParameters();

            arity = @params.Length;

            element = Expression.Parameter(typeof(TDeconstructable), "element");
            helper = Expression.Parameter(typeof(TupleHelper), "helper");
            transformers = Expression.Parameter(typeof(ITransformer[]), "transformers");

            accumulator = Expression.Variable(typeof(ValueSequenceBuilder<char>), "accumulator");
            initialBuffer = Expression.Variable(typeof(char[]), "initialBuffer");
            temps = @params.Select((p, i) => Expression.Variable(FlattenRef(p.ParameterType), $"temp{i + 1}")).ToList();

            var rentMethod = Method.OfExpression<Func<ArrayPool<char>, int, char[]>>((ap, i) => ap.Rent(i));
            var returnMethod = Method.OfExpression<Action<ArrayPool<char>, char[], bool>>((ap, arr, clear) => ap.Return(arr, clear));

            var arrayPoolAccess = Expression.Property(null, Property.Of((ArrayPool<char> ap) => ArrayPool<char>.Shared));
            rentInitialBuffer = Expression.Assign(initialBuffer,
               Expression.Call(arrayPoolAccess, rentMethod, Expression.Constant(10))
            );
            returnInitialBuffer = Expression.Call(arrayPoolAccess, returnMethod, initialBuffer, Expression.Constant(false));


            accumulatorInit = Expression.Assign(accumulator,
                Expression.New(
                    typeof(ValueSequenceBuilder<char>).GetConstructor(new[] { typeof(Span<char>) }) ?? throw new MissingMemberException($"No proper ctor in {nameof(ValueSequenceBuilder<char>)}"),
                    Expression.Convert(initialBuffer, typeof(Span<char>))
                )
            );

            var disposeMethod = typeof(ValueSequenceBuilder<char>).GetMethod(nameof(ValueSequenceBuilder<char>.Dispose))
                ?? throw new MissingMemberException(typeof(ValueSequenceBuilder<char>).Name, nameof(ValueSequenceBuilder<char>.Dispose));
            accumulatorDispose = Expression.Call(accumulator, disposeMethod);


            accumulatorToString = Expression.Call(
                Expression.Call(accumulator, nameof(ValueSequenceBuilder<char>.AsSpan), null),
                nameof(ReadOnlySpan<char>.ToString), null)
                ;
        }


        public override TDeconstructable Parse(in ReadOnlySpan<char> input) =>
            input.IsEmpty ? default : _parser(input, _helper, _transformers);

        public override string Format(TDeconstructable element) =>
            element is null ? null : _formatter(element, _helper, _transformers);

        public override string ToString() =>
            $"Transform {typeof(TDeconstructable).GetFriendlyName()} by deconstruction into ({GetTupleDefinition()})";

        private string GetTupleDefinition()
        {
            var def = _transformers?.Select(t =>
                TypeMeta.TryGetGenericRealization(t.GetType(), typeof(ITransformer<>), out var realization)
                 ? realization.GenericTypeArguments[0].GetFriendlyName()
                 : "object"
                ) ?? Enumerable.Empty<string>();

            return string.Join(", ", def);

        }
    }
}
