#nullable enable
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

public abstract class FactoryMethodTransformerHandler(FactoryMethodSettings settings) : ITransformerHandler
{
    private readonly FactoryMethodSettings _settings = settings;
    protected readonly string FactoryMethodName = settings.FactoryMethodName;

    public ITransformer<TElement> CreateTransformer<TElement>() => GetTransformer<TElement>();

    public bool CanHandle(Type type) =>
        GetFactoryMethodContainingType(type) is { } containerType &&
        containerType.GetMethods(STATIC_MEMBER_FLAGS).Any(m => FactoryMethodPredicate(m, type, FactoryMethodName));


    private Func<TElement>? GetPropertyValueProvider<TElement>(string propertyName)
    {
        var elementType = typeof(TElement);
        var factoryMethodContainer = GetFactoryMethodContainingType(elementType);

        if (factoryMethodContainer?.GetProperty(propertyName, STATIC_MEMBER_FLAGS) is { } property &&
            property.PropertyType.IsAssignableFrom(elementType))
        {
            Expression prop = Expression.Property(null, property);
            if (property.PropertyType != elementType)
                prop = Expression.Convert(prop, elementType);

            return Expression.Lambda<Func<TElement>>(prop).Compile();
        }
        else
            return null;
    }



    protected abstract Type? GetFactoryMethodContainingType(Type type);
    public abstract sbyte Priority { get; }
    protected abstract MethodInfo PrepareParseMethod(MethodInfo method, Type elementType);


    private ITransformer<TElement> GetTransformer<TElement>()
    {
        var elementType = typeof(TElement);


        var formatterType = (
            typeof(IFormattable).IsAssignableFrom(elementType)
            ? typeof(FormattableFormatter<>)
            : typeof(NormalFormatter<>)
            ).MakeGenericType(elementType);
        if (Activator.CreateInstance(formatterType) is not IFormatter<TElement> formatter)
            throw new ArgumentNullException($"Cannot create instance of '{formatterType}'");

        var emptyValueProvider = GetPropertyValueProvider<TElement>(_settings.EmptyPropertyName);
        var nullValueProvider = GetPropertyValueProvider<TElement>(_settings.NullPropertyName);


        Type factoryMethodContainer = GetFactoryMethodContainingType(elementType)
             ?? throw new InvalidOperationException($"Missing factory declaration for {elementType.GetFriendlyName()}");

        var conversionMethods = factoryMethodContainer.GetMethods(STATIC_MEMBER_FLAGS)
            .Where(m => FactoryMethodPredicate(m, elementType, FactoryMethodName)).ToList();

        MethodInfo parseMethod =
            FindMethodWithParameterType(typeof(ReadOnlySpan<char>)) ??
            FindMethodWithParameterType(typeof(string)) ??
            throw new InvalidOperationException($"No proper {FactoryMethodName} method found");

        parseMethod = PrepareParseMethod(parseMethod, elementType);


        var methodInputType = parseMethod.GetParameters()[0].ParameterType;

        var inputParameter = Expression.Parameter(methodInputType, "input");
        Expression body = Expression.Call(parseMethod, inputParameter);

        if (body.Type != elementType)
            body = Expression.Convert(body, elementType);


        return methodInputType == typeof(string)
            ? new StringFactoryTransformer<TElement>(body, inputParameter, formatter, emptyValueProvider, nullValueProvider)
            : new SpanFactoryTransformer<TElement>(body, inputParameter, formatter, emptyValueProvider, nullValueProvider);


        MethodInfo? FindMethodWithParameterType(Type paramType) =>
            conversionMethods.FirstOrDefault(m =>
                m.GetParameters() is { Length: 1 } @params && @params[0].ParameterType == paramType);
    }

    private static bool FactoryMethodPredicate(MethodInfo m, Type returnType, string factoryMethodName) =>
        m.Name == factoryMethodName &&
        m.GetParameters() is [{ParameterType: { } firstParamType}] &&
        (firstParamType == typeof(string) || firstParamType == typeof(ReadOnlySpan<char>)) &&
        (
            m.ReturnType == returnType
            ||
            m.ReturnType.IsGenericTypeDefinition && returnType.DerivesOrImplementsGeneric(m.ReturnType)
            ||
            (m.ReturnType.IsAbstract || m.ReturnType.IsInterface) && returnType.DerivesOrImplementsGeneric(m.ReturnType)
        );
    
    string ITransformerHandler.DescribeHandlerMatch() => DescribeHandlerMatch();
    protected abstract string DescribeHandlerMatch();

    private const BindingFlags STATIC_MEMBER_FLAGS = BindingFlags.Public | BindingFlags.Static;


    private abstract class FactoryTransformer<TElement>(
        Expression body,
        IFormatter<TElement> formatter,
        Func<TElement>? emptyValueProvider,
        Func<TElement>? nullValueProvider) : TransformerBase<TElement>
    {
        private readonly string _description = $"Parse {typeof(TElement).GetFriendlyName()} using {GetMethodName(body)}";

        public override string Format(TElement element) => formatter.Format(element);

        public override TElement GetEmpty() => emptyValueProvider != null ? emptyValueProvider() : base.GetEmpty();

        public override TElement GetNull() => nullValueProvider != null ? nullValueProvider() : base.GetNull();

        private static string GetMethodName(Expression expression)
        {
            var method = expression switch
            {
                MethodCallExpression call1 => call1.Method,
                UnaryExpression convert
                    when expression.NodeType == ExpressionType.Convert && convert.Operand is MethodCallExpression call2
                  => call2.Method,

                _ => throw new NotSupportedException("Only method calls are valid at this point")
            };

            return
                $"{method.DeclaringType?.GetFriendlyName()}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})";
        }

        public sealed override string ToString() => _description;
    }

    private sealed class StringFactoryTransformer<TElement>(
        Expression body,
        ParameterExpression inputParameter,
        IFormatter<TElement> formatter,
        Func<TElement>? emptyValueProvider,
        Func<TElement>? nullValueProvider)
        : FactoryTransformer<TElement>(body, formatter, emptyValueProvider, nullValueProvider)
    {
        private readonly Func<string, TElement> _parserDelegate = Expression.Lambda<Func<string, TElement>>(body, inputParameter).Compile();

        protected override TElement ParseCore(in ReadOnlySpan<char> input) =>
            _parserDelegate(input.ToString());

        protected override TElement ParseText(string text) => _parserDelegate(text);
    }

    private sealed class SpanFactoryTransformer<TElement>(
        Expression body,
        ParameterExpression inputParameter,
        IFormatter<TElement> formatter,
        Func<TElement>? emptyValueProvider,
        Func<TElement>? nullValueProvider)
        : FactoryTransformer<TElement>(body, formatter, emptyValueProvider, nullValueProvider)
    {
        private delegate TElement ParserDelegate(ReadOnlySpan<char> input);

        private readonly ParserDelegate _parserDelegate = Expression.Lambda<ParserDelegate>(body, inputParameter).Compile();

        protected override TElement ParseCore(in ReadOnlySpan<char> input) => _parserDelegate(input);
    }
}
