using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    internal abstract class FactoryMethodTransformer : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>()
        {
            var elementType = typeof(TElement);

            var parser = GetParser<TElement>(elementType);
            
            var formatterType = typeof(IFormattable).IsAssignableFrom(elementType)
                                ? typeof(FormattableFormatter<>)
                                : typeof(NormalFormatter<>);
            formatterType = formatterType.MakeGenericType(elementType);
            var formatter = (IFormatter<TElement>)Activator.CreateInstance(formatterType);


            return new CompositionTransformer<TElement>(parser, formatter);
        }

        public bool CanHandle(Type type) =>
            GetFactoryMethodContainer(type) is { } containerType &&
            containerType.GetMethods(STATIC_METHOD_FLAGS)
                .Any(m => FactoryMethodPredicate(m, type));

        protected abstract Type GetFactoryMethodContainer(Type type);

        public abstract sbyte Priority { get; }

        private ISpanParser<TElement> GetParser<TElement>(Type elementType)
        {
            Type factoryMethodContainer = GetFactoryMethodContainer(elementType)
                 ?? throw new InvalidOperationException($"Missing factory declaration for {elementType.GetFriendlyName()}");

            var conversionMethods = factoryMethodContainer.GetMethods(STATIC_METHOD_FLAGS)
                .Where(m => FactoryMethodPredicate(m, elementType)).ToList();

            MethodInfo parseMethod =
                FindMethodWithParameterType(typeof(ReadOnlySpan<char>)) ??
                FindMethodWithParameterType(typeof(string)) ??
                throw new InvalidOperationException($"No proper {FACTORY_METHOD_NAME} method found");

            parseMethod = PrepareParseMethod(parseMethod, elementType);


            var methodInputType = parseMethod.GetParameters()[0].ParameterType;

            var inputParameter = Expression.Parameter(methodInputType, "input");
            Expression body = Expression.Call(parseMethod, inputParameter);

            if (body.Type != elementType)
                body = Expression.Convert(body, elementType);


            return methodInputType == typeof(string)
                ? new InnerStringParser<TElement>(body, inputParameter)
                : (ISpanParser<TElement>)new InnerSpanParser<TElement>(body, inputParameter);


            MethodInfo FindMethodWithParameterType(Type paramType) =>
                conversionMethods.FirstOrDefault(m =>
                    m.GetParameters() is { } @params && @params.Length == 1 &&
                    @params[0].ParameterType == paramType);
        }

        protected abstract MethodInfo PrepareParseMethod(MethodInfo method, Type elementType);


        protected const string FACTORY_METHOD_NAME = nameof(ITextFactorySpan<object>.FromText);

        //TODO more test for non-generic factory with generic methods (i.e. ReturnType.IsGenericTypeDefinition + open generic parse methods)

        private static bool FactoryMethodPredicate(MethodInfo m, Type returnType) =>
            m.Name == FACTORY_METHOD_NAME &&
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

        private const BindingFlags STATIC_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static;

        private abstract class InnerParser<TElement> : ISpanParser<TElement>
        {
            private readonly string _description;

            protected InnerParser(Expression body) =>
                _description = $"Parse {typeof(TElement).GetFriendlyName()} using {GetMethodName(body)}";

            public abstract TElement Parse(in ReadOnlySpan<char> input);

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

        private sealed class InnerStringParser<TElement> : InnerParser<TElement>
        {
            private readonly Func<string, TElement> _delegate;

            public InnerStringParser(Expression body, ParameterExpression inputParameter) : base(body) =>
                _delegate = Expression.Lambda<Func<string, TElement>>(body, inputParameter).Compile();

            public override TElement Parse(in ReadOnlySpan<char> input) => _delegate(input.ToString());
        }

        private sealed class InnerSpanParser<TElement> : InnerParser<TElement>
        {
            private delegate TElement ParserDelegate(ReadOnlySpan<char> input);
            private readonly ParserDelegate _delegate;

            public InnerSpanParser(Expression body, ParameterExpression inputParameter) : base(body) =>
                _delegate = Expression.Lambda<ParserDelegate>(body, inputParameter).Compile();

            public override TElement Parse(in ReadOnlySpan<char> input) => _delegate(input);
        }
    }
}
