using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers
{
    public abstract class FactoryMethodTransformerCreator : ICanCreateTransformer
    {
        private readonly FactoryMethodSettings _settings;
        protected readonly string FactoryMethodName;

        protected FactoryMethodTransformerCreator([NotNull] FactoryMethodSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            FactoryMethodName = _settings.FactoryMethodName;
        }


        public ITransformer<TElement> CreateTransformer<TElement>() => GetTransformer<TElement>();

        public bool CanHandle(Type type) =>
            GetFactoryMethodContainer(type) is { } containerType &&
            containerType.GetMethods(STATIC_MEMBER_FLAGS).Any(m => FactoryMethodPredicate(m, type, FactoryMethodName));


        private Func<TElement> GetPropertyValueProvider<TElement>(string propertyName)
        {
            var elementType = typeof(TElement);
            Type factoryMethodContainer = GetFactoryMethodContainer(elementType);


            if (factoryMethodContainer != null &&
                factoryMethodContainer.GetProperty(propertyName, STATIC_MEMBER_FLAGS) is { } property &&
                property.PropertyType.IsAssignableFrom(elementType)
               )
            {
                Expression prop = Expression.Property(null, property);
                if (property.PropertyType != elementType)
                    prop = Expression.Convert(prop, elementType);

                return Expression.Lambda<Func<TElement>>(prop).Compile();
            }
            else
                return null;
        }



        protected abstract Type GetFactoryMethodContainer(Type type);
        public abstract sbyte Priority { get; }
        protected abstract MethodInfo PrepareParseMethod(MethodInfo method, Type elementType);


        private ITransformer<TElement> GetTransformer<TElement>()
        {
            var elementType = typeof(TElement);


            var formatterType = typeof(IFormattable).IsAssignableFrom(elementType)
                ? typeof(FormattableFormatter<>)
                : typeof(NormalFormatter<>);
            formatterType = formatterType.MakeGenericType(elementType);
            var formatter = (IFormatter<TElement>)Activator.CreateInstance(formatterType);

            Func<TElement> emptyValueProvider = GetPropertyValueProvider<TElement>(_settings.EmptyPropertyName);
            Func<TElement> nullValueProvider = GetPropertyValueProvider<TElement>(_settings.NullPropertyName);



            Type factoryMethodContainer = GetFactoryMethodContainer(elementType)
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
                : (ITransformer<TElement>)
                  new SpanFactoryTransformer<TElement>(body, inputParameter, formatter, emptyValueProvider, nullValueProvider);


            MethodInfo FindMethodWithParameterType(Type paramType) =>
                conversionMethods.FirstOrDefault(m =>
                    m.GetParameters() is { } @params && @params.Length == 1 &&
                    @params[0].ParameterType == paramType);
        }

        
        private static bool FactoryMethodPredicate(MethodInfo m, Type returnType, string factoryMethodName) =>
            m.Name == factoryMethodName &&
            m.GetParameters() is { } @params && @params.Length == 1 &&
            @params[0].ParameterType is { } firstParamType &&
            (
                firstParamType == typeof(string)
                ||
                firstParamType == typeof(ReadOnlySpan<char>)
            )
            &&
            (
                m.ReturnType == returnType
                ||
                m.ReturnType.IsGenericTypeDefinition && returnType.DerivesOrImplementsGeneric(m.ReturnType)
                ||
                (m.ReturnType.IsAbstract || m.ReturnType.IsInterface) && returnType.DerivesOrImplementsGeneric(m.ReturnType)
            );

        private const BindingFlags STATIC_MEMBER_FLAGS = BindingFlags.Public | BindingFlags.Static;



        private abstract class FactoryTransformer<TElement> : TransformerBase<TElement>
        {
            private readonly string _description;
            private readonly IFormatter<TElement> _formatter;
            private readonly Func<TElement> _emptyValueProvider;
            private readonly Func<TElement> _nullValueProvider;

            protected FactoryTransformer(Expression body, [NotNull] IFormatter<TElement> formatter, Func<TElement> emptyValueProvider, Func<TElement> nullValueProvider)
            {
                _description = $"Parse {typeof(TElement).GetFriendlyName()} using {GetMethodName(body)}";
                _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
                _emptyValueProvider = emptyValueProvider;
                _nullValueProvider = nullValueProvider;
            }

            public override string Format(TElement element) => _formatter.Format(element);

            public override TElement GetEmpty() =>
                _emptyValueProvider != null ? _emptyValueProvider() : base.GetEmpty();

            public override TElement GetNull() =>
                _nullValueProvider != null ? _nullValueProvider() : base.GetNull();


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
                    $"{method.DeclaringType.GetFriendlyName()}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.GetFriendlyName()))})";
            }

            public sealed override string ToString() => _description;
        }

        private sealed class StringFactoryTransformer<TElement> : FactoryTransformer<TElement>
        {
            private readonly Func<string, TElement> _delegate;

            public StringFactoryTransformer(Expression body, ParameterExpression inputParameter, IFormatter<TElement> formatter,
                                            Func<TElement> emptyValueProvider, Func<TElement> nullValueProvider)
                : base(body, formatter, emptyValueProvider, nullValueProvider)
                => _delegate = Expression.Lambda<Func<string, TElement>>(body, inputParameter).Compile();

            protected override TElement ParseCore(in ReadOnlySpan<char> input) =>
                _delegate(input.ToString());

            protected override TElement ParseText(string text) => _delegate(text);
        }

        private sealed class SpanFactoryTransformer<TElement> : FactoryTransformer<TElement>
        {
            private delegate TElement ParserDelegate(ReadOnlySpan<char> input);
            private readonly ParserDelegate _delegate;

            public SpanFactoryTransformer(Expression body, ParameterExpression inputParameter, IFormatter<TElement> formatter,
                                          Func<TElement> emptyValueProvider, Func<TElement> nullValueProvider)
                : base(body, formatter, emptyValueProvider, nullValueProvider)
                => _delegate = Expression.Lambda<ParserDelegate>(body, inputParameter).Compile();

            protected override TElement ParseCore(in ReadOnlySpan<char> input) => _delegate(input);
        }
    }
}
