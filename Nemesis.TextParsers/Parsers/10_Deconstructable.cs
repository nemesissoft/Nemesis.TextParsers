using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;
using Builder = Nemesis.TextParsers.Parsers.DeconstructionTransformerBuilder;
using PublicAPI = JetBrains.Annotations.PublicAPIAttribute;
#if !NET
using NotNull = JetBrains.Annotations.NotNullAttribute;
#else
using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#endif


namespace Nemesis.TextParsers.Parsers;

[JetBrains.Annotations.UsedImplicitly]
public sealed class DeconstructionTransformerHandler : ITransformerHandler
{
    private readonly ITransformerStore _transformerStore;
    public DeconstructionTransformerHandler(ITransformerStore transformerStore) => _transformerStore = transformerStore;


    public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>()
        => (typeof(TDeconstructable).GetCustomAttribute<DeconstructableSettingsAttribute>(true) is { } attribute
            ? Builder.FromSettings(attribute.ToSettings(), _transformerStore)
            : Builder.GetDefault(_transformerStore)
    ).ToTransformer<TDeconstructable>();


    public bool CanHandle(Type type) =>
        Builder.TryGetDefaultDeconstruct(type, out _, out var ctor) &&
        ctor.GetParameters() is { Length: > 0 } cp && cp.All(pi => _transformerStore.IsSupportedForTransformation(pi.ParameterType))
    ;

    public sbyte Priority => 110;

    public override string ToString() =>
        $"Create transformer for Deconstructable aspect with DEFAULT settings:{_transformerStore.SettingsStore.GetSettingsFor<DeconstructableSettings>()}. Specify own settings using {nameof(DeconstructableSettingsAttribute)}";

    string ITransformerHandler.DescribeHandlerMatch() => "Deconstructable with properties types supported for transformation";
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

public sealed class DeconstructionTransformerBuilder
{
    private readonly ITransformerStore _transformerStore;

    //settings
    public char Delimiter { get; private set; }
    public char NullElementMarker { get; private set; }
    public char EscapingSequenceStart { get; private set; }
    public char? Start { get; private set; }
    public char? End { get; private set; }
    public bool UseDeconstructableEmpty { get; private set; }
    //settings

    public DeconstructionMethod Mode { get; private set; } = DeconstructionMethod.DefaultConstructorDeconstructPair;
    public MethodInfo Deconstruct { get; private set; }
    public ConstructorInfo Ctor { get; private set; }

    public override string ToString() =>
        $@"{Start}Item1{Delimiter}Item2{Delimiter}…{Delimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}' Mode = {Mode}. 
Deconstructed by {(Deconstruct == null ? "<default>" : $"{Deconstruct.DeclaringType.GetFriendlyName()}.{Deconstruct.Name}({string.Join(", ", Deconstruct.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})")}. 
Constructed by {(Ctor == null ? "<default>" : $"new {Ctor.DeclaringType.GetFriendlyName()}({string.Join(", ", Ctor.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})")}. 
{(UseDeconstructableEmpty ? "With" : "Without")} deconstructable empty generator. ";

    #region Ctor and factories

    public static Builder GetDefault(ITransformerStore transformerStore)
    {
        var settings = transformerStore.SettingsStore.GetSettingsFor<DeconstructableSettings>();
        return FromSettings(settings, transformerStore);
    }

    public static Builder FromSettings(DeconstructableSettings settings, ITransformerStore transformerStore) =>
        new(transformerStore,
            settings.Delimiter,
            settings.NullElementMarker,
            settings.EscapingSequenceStart,
            settings.Start,
            settings.End,
            settings.UseDeconstructableEmpty
        );

    private DeconstructionTransformerBuilder([NotNull] ITransformerStore transformerStore, char delimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end, bool useDeconstructableEmpty)
    {
        _transformerStore = transformerStore ?? throw new ArgumentNullException(nameof(transformerStore));
        Delimiter = delimiter;
        NullElementMarker = nullElementMarker;
        EscapingSequenceStart = escapingSequenceStart;
        Start = start;
        End = end;
        UseDeconstructableEmpty = useDeconstructableEmpty;
    }
    #endregion


    #region With
    [PublicAPI]
    public Builder WithDelimiter(char delimiter) { Delimiter = delimiter; return this; }

    [PublicAPI]
    public Builder WithNullElementMarker(char nullElementMarker) { NullElementMarker = nullElementMarker; return this; }

    [PublicAPI]
    public Builder WithEscapingSequenceStart(char escapingSequenceStart) { EscapingSequenceStart = escapingSequenceStart; return this; }

    [PublicAPI]
    public Builder WithStart(char start) { Start = start; return this; }

    [PublicAPI]
    public Builder WithEnd(char end) { End = end; return this; }

    [PublicAPI]
    public Builder WithBorders(char start, char end) { (Start, End) = (start, end); return this; }

    [PublicAPI]
    public Builder WithoutBorders() { (Start, End) = (null, null); return this; }

    [PublicAPI]
    public Builder WithDefaultDeconstruction()
    {
        Mode = DeconstructionMethod.DefaultConstructorDeconstructPair;
        Deconstruct = null;
        Ctor = null;

        return this;
    }

    [PublicAPI]
    public Builder WithCustomDeconstruction([NotNull] MethodInfo deconstruct, [NotNull] ConstructorInfo ctor)
    {
        Mode = DeconstructionMethod.ProvidedDeconstructMethod;
        Deconstruct = deconstruct ?? throw new ArgumentNullException(nameof(deconstruct));
        Ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));

        return this;
    }

    [PublicAPI]
    public Builder WithDeconstructableEmpty() { UseDeconstructableEmpty = true; return this; }

    [PublicAPI]
    public Builder WithoutDeconstructableEmpty() { UseDeconstructableEmpty = false; return this; }
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
                           pair.@params.All(p => p.IsOut)
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

    public ITransformer<TDeconstructable> ToTransformer<TDeconstructable>()
    {
        var helper = new TupleHelper(Delimiter, NullElementMarker, EscapingSequenceStart, Start, End);

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
                        throw new NotSupportedException($"Deconstructable for {typeof(TDeconstructable).GetFriendlyName()} cannot be created. Default deconstruction method supports cases with at lease one non-nullary {DECONSTRUCT} method with matching constructor");
                    break;
                }
            case DeconstructionMethod.ProvidedDeconstructMethod:
                deconstruct = Deconstruct;
                ctor = Ctor;
                break;
            default:
                throw new NotSupportedException($"{nameof(Mode)} = {Mode} is not supported");
        }

        CheckCtorAndDeconstruct<TDeconstructable>(ctor, deconstruct, _transformerStore);


        var transformers = ctor.GetParameters()
                 .Select(p => _transformerStore.GetTransformer(p.ParameterType))
                 .ToArray();

        var parser = DeconstructionTransformer<TDeconstructable>.CreateParser(ctor);
        var formatter = DeconstructionTransformer<TDeconstructable>.CreateFormatter(deconstruct);
        var emptyGenerator = UseDeconstructableEmpty
            ? DeconstructionTransformer<TDeconstructable>.CreateEmptyGenerator(ctor)
            : null;

        return new DeconstructionTransformer<TDeconstructable>(helper, transformers, parser, formatter, emptyGenerator);
    }

    private static void CheckCtorAndDeconstruct<TDeconstructable>(ConstructorInfo ctor, MethodInfo deconstruct, ITransformerStore transformerStore)
    {
        if (deconstruct is null || ctor is null)
            throw new NotSupportedException($"{DECONSTRUCT} and constructor have to be provided, depending on {nameof(Mode)} - either manually or object needs to expose compatible ctor/Deconstruct pair");

        if (deconstruct.IsStatic)
        {
            var dp = deconstruct.GetParameters();
            var cp = ctor.GetParameters();

            if (cp.Length == 0 || dp.Length != cp.Length + 1 ||
                !dp[0].ParameterType.IsAssignableFrom(typeof(TDeconstructable)) ||
                !IsCompatible(dp.Skip(1).ToList(), cp)
            )
                throw new NotSupportedException(
                    $"Static {DECONSTRUCT} method has to be compatible with provided constructor and should have one additional parameter in the beginning - deconstructable instance");


            if (dp.Skip(1).Any(p => !p.IsOut))
                throw new NotSupportedException(
                    $"Static {DECONSTRUCT} method must have all but first params as out params (IsOut==true)");


            var notSupportedParams = dp.Skip(1).Select(p =>
                (Type: p.ParameterType,
                 IsSupported: transformerStore.IsSupportedForTransformation(FlattenRef(p.ParameterType))
                )
            ).Where(sp => !sp.IsSupported);

            if (notSupportedParams.Any())
                throw new NotSupportedException(
                    $@"Static {DECONSTRUCT} method must have all parameter types be recognizable by TransformerStore. Not supported types:
{string.Join(", ", notSupportedParams.Select(sp => FlattenRef(sp.Type).GetFriendlyName()))}");
        }
        else
        {
            var dp = deconstruct.GetParameters();

            if (dp.Length == 0 || !IsCompatible(dp, ctor.GetParameters()))
                throw new NotSupportedException(
                    $"Instance {DECONSTRUCT} method has to be compatible with provided constructor and should have same number and type of parameters");


            if (dp.Any(p => !p.IsOut))
                throw new NotSupportedException(
                    $"Instance {DECONSTRUCT} method must have all out params (IsOut==true)");

            var notSupportedParams = dp.Select(p =>
                    (Type: p.ParameterType,
                     IsSupported: transformerStore.IsSupportedForTransformation(FlattenRef(p.ParameterType))
                    )
                ).Where(sp => !sp.IsSupported);

            if (notSupportedParams.Any())
                throw new NotSupportedException(
                    $@"Instance {DECONSTRUCT} method must have all parameter types be recognizable by TransformerStore. Not supported types:
{string.Join(", ", notSupportedParams.Select(sp => FlattenRef(sp.Type).GetFriendlyName()))}");
        }
    }

    private static Type FlattenRef(Type type) => type.IsByRef ? type.GetElementType() : type;

    private static bool IsCompatible(IReadOnlyList<ParameterInfo> left, IReadOnlyList<ParameterInfo> right)
    {
        bool AreEqualByParamTypes()
        {
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
}

internal sealed class DeconstructionTransformer<TDeconstructable> : TransformerBase<TDeconstructable>
{
    public delegate TDeconstructable ParserDelegate(ReadOnlySpan<char> input, TupleHelper helper, ITransformer[] transformers);
    public delegate string FormatterDelegate(TDeconstructable element, ref ValueSequenceBuilder<char> accumulator, TupleHelper helper, ITransformer[] transformers);
    public delegate TDeconstructable EmptyGenerator(ITransformer[] transformers);


    internal TupleHelper Helper { get; }

    private readonly ITransformer[] _transformers;
    private readonly ParserDelegate _parser;
    private readonly FormatterDelegate _formatter;
    private readonly EmptyGenerator _emptyGenerator;

    internal DeconstructionTransformer([NotNull] TupleHelper helper, [NotNull] ITransformer[] transformers,
        [NotNull] ParserDelegate parser, [NotNull] FormatterDelegate formatter, EmptyGenerator emptyGenerator)
    {
        Helper = helper ?? throw new ArgumentNullException(nameof(helper));

        _transformers = transformers ?? throw new ArgumentNullException(nameof(transformers));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

        _emptyGenerator = emptyGenerator;
    }

    /*
     public override (T1, T2, T3, T4) Parse(in ReadOnlySpan<char> input)
     {
         var enumerator = Helper.ParseStart(input, ARITY);

         var t1 = Helper.ParseElement(ref enumerator, _transformer1);
         var t2 = Helper.ParseElement(ref enumerator, _transformer2, 2);
         var t3 = Helper.ParseElement(ref enumerator, _transformer3, 3);
         var t4 = Helper.ParseElement(ref enumerator, _transformer4, 4);

         Helper.ParseEnd(ref enumerator, ARITY);

         return (t1, t2, t3, t4);
     }*/
    internal static ParserDelegate CreateParser(ConstructorInfo ctor)
    {
        string typeName = typeof(TDeconstructable).GetFriendlyName();

        var @params = ctor.GetParameters();
        byte arity = (byte)@params.Length;

        var input = Expression.Parameter(typeof(ReadOnlySpan<char>), "input");
        var helper = Expression.Parameter(typeof(TupleHelper), "helper");
        var transformers = Expression.Parameter(typeof(ITransformer[]), "transformers");

        var enumerator = Expression.Variable(typeof(TokenSequence<char>.TokenSequenceEnumerator), "enumerator");
        var fields = @params.Select((p, i) => Expression.Variable(p.ParameterType, $"t{i + 1}")).ToList();

        var expressions = new List<Expression>(3 + arity)
            {
                Expression.Assign(enumerator,
                    Expression.Call(helper, nameof(TupleHelper.ParseStart), null, input, Expression.Constant(arity), Expression.Constant(typeName))
                    )
            };

        for (int i = 0; i < arity; i++)
        {
            var field = fields[i];

            var trans = Expression.Convert(
                Expression.ArrayIndex(transformers, Expression.Constant(i)),
                typeof(ISpanParser<>).MakeGenericType(field.Type)
            );

            var arguments = new Expression[] {
                enumerator, trans, Expression.Constant(i > 0 ? (byte?)(i + 1) : null, typeof(byte?)), Expression.Constant(typeName)
            };

            var assignment = Expression.Assign(
                field,
                Expression.Call(helper, nameof(TupleHelper.ParseElement), [field.Type], arguments)
            );
            expressions.Add(assignment);
        }

        expressions.Add(
            Expression.Call(helper, nameof(TupleHelper.ParseEnd), null, enumerator, Expression.Constant(arity), Expression.Constant(typeName))
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
        CreateFormatterData(deconstruct, out int arity,
            out var element, out var accumulator,
            out var helper, out var transformers,
            out var temps,
            out var accumulatorToString);


        Expression GetConvertedSource()
        {
            var decoSourceType = deconstruct.GetParameters()[0].ParameterType;

            return element.Type == decoSourceType ? element : Expression.Convert(element, decoSourceType);
        }

        var expressions = new List<Expression>(7 + 2 * arity)
        {
            deconstruct.IsStatic
                ? Expression.Call(deconstruct, //static method
                                  new[] { GetConvertedSource() } //this T instance
                                  .Concat(temps)) //out params
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

            var format = Expression.Call(helper, nameof(TupleHelper.FormatElement), [temp.Type],
                    formatter, temp, accumulator);
            expressions.Add(format);
        }

        expressions.Add(Expression.Call(helper, nameof(TupleHelper.EndFormat), null, accumulator));

        expressions.Add(accumulatorToString);


        var body = Expression.Block(temps, expressions);


        var λ = Expression.Lambda<FormatterDelegate>(body, element, accumulator, helper, transformers);
        return λ.Compile();
    }

    internal static EmptyGenerator CreateEmptyGenerator(ConstructorInfo ctor)
    {
        var transformers = Expression.Parameter(typeof(ITransformer[]), "transformers");

        Expression GetEmptyInstanceExpression(Type parameterType, int index)
        {
            var transformerType = typeof(ITransformer<>).MakeGenericType(parameterType);

            var genericTransformer = Expression.Convert(
                Expression.ArrayIndex(transformers, Expression.Constant(index)),
                transformerType
            );
            var getEmptyMethod = transformerType.GetMethod(nameof(ITransformer<>.GetEmpty)) ??
                throw new MissingMethodException(nameof(ITransformer<>), nameof(ITransformer<>.GetEmpty));
            return Expression.Call(genericTransformer, getEmptyMethod);
        }

        var emptyInstances = ctor.GetParameters()
                .Select(p => p.ParameterType)
                .Select(GetEmptyInstanceExpression);

        var @new = Expression.New(ctor, emptyInstances);

        var λ = Expression.Lambda<EmptyGenerator>(@new, transformers);
        return λ.Compile();
    }


    private static void CreateFormatterData(MethodBase deconstruct, out int arity,
        out ParameterExpression element, out ParameterExpression accumulator,
        out ParameterExpression helper, out ParameterExpression transformers,
        out IReadOnlyList<ParameterExpression> temps,
        out Expression accumulatorToString)
    {
        static Type FlattenRef(Type type) => type.IsByRef ? type.GetElementType() : type;

        var @params = deconstruct.IsStatic
            ? [.. deconstruct.GetParameters().Skip(1)]
            : deconstruct.GetParameters();

        arity = @params.Length;

        element = Expression.Parameter(typeof(TDeconstructable), "element");
        accumulator = Expression.Parameter(typeof(ValueSequenceBuilder<char>).MakeByRefType(), "accumulator");
        helper = Expression.Parameter(typeof(TupleHelper), "helper");
        transformers = Expression.Parameter(typeof(ITransformer[]), "transformers");


        temps = @params.Select((p, i) => Expression.Variable(FlattenRef(p.ParameterType), $"temp{i + 1}")).ToList();



        accumulatorToString = Expression.Call(
            Expression.Call(accumulator, nameof(ValueSequenceBuilder<>.AsSpan), null),
            nameof(ReadOnlySpan<>.ToString), null)
            ;
    }


    protected override TDeconstructable ParseCore(in ReadOnlySpan<char> input) =>
        _parser(input, Helper, _transformers);

    public override string Format(TDeconstructable element)
    {
        if (element is null) return null;
        else
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                return _formatter(element, ref accumulator, Helper, _transformers);
            }
            finally { accumulator.Dispose(); }
        }
    }

    public override TDeconstructable GetEmpty() =>
        _emptyGenerator != null ? _emptyGenerator(_transformers) : base.GetEmpty();

    public override string ToString()
    {
        string GetTupleDefinition()
        {
            var def = _transformers?.Select(t =>
                TypeMeta.TryGetGenericRealization(t.GetType(), typeof(ITransformer<>), out var realization)
                 ? realization.GenericTypeArguments[0].GetFriendlyName()
                 : "object"
                ) ?? [];

            return string.Join(", ", def);
        }

        return
            $"Transform {typeof(TDeconstructable).GetFriendlyName()} by deconstruction into ({GetTupleDefinition()}) formatted as {Helper}.";
    }
}

public abstract class CustomDeconstructionTransformer<TDeconstructable> : TransformerBase<TDeconstructable>
{
    private readonly string _description;
    private readonly ITransformer<TDeconstructable> _transformer;

    /// <summary>
    /// Create base class that bounds 2 aspects - Deconstructable and Transformable 
    /// </summary>
    /// <param name="transformerStore">When used in standard way - this parameter gets injected by default</param>
    protected CustomDeconstructionTransformer([NotNull] ITransformerStore transformerStore)
    {
        ArgumentNullException.ThrowIfNull(transformerStore);
        var builder = Builder.GetDefault(transformerStore);
        builder = BuildSettings(builder);

        _transformer = builder.ToTransformer<TDeconstructable>();

        _description = $"{_transformer} Based on:{Environment.NewLine}{builder}";
    }

    protected abstract Builder BuildSettings(Builder prototype);

    protected sealed override TDeconstructable ParseCore(in ReadOnlySpan<char> input) =>
        _transformer.Parse(input);

    public sealed override string Format(TDeconstructable element) =>
        _transformer.Format(element);

    public override TDeconstructable GetEmpty() =>
        _transformer.GetEmpty();

    public sealed override string ToString() => _description;
}
