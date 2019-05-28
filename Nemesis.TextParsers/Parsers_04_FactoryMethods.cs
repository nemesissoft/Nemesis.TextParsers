using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    internal abstract class FactoryMethodTransformer : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>()
        {
            var elementType = typeof(TElement);

            var parser = GetParser<TElement>(elementType);

            var formatter = GetFormatter<TElement>(elementType);

            return new CompositionTransformer<TElement>(parser, formatter);
        }

        public abstract bool CanHandle(Type type);

        public abstract sbyte Priority { get; }

        protected abstract ISpanParser<TElement> GetParser<TElement>(Type elementType);

        protected static IFormatter<TElement> GetFormatter<TElement>(Type elementType)
        {
            var formatterType = typeof(IFormattable).IsAssignableFrom(elementType)
                ? typeof(FormattableFormatter<>)
                : typeof(NormalFormatter<>);

            formatterType = formatterType.MakeGenericType(elementType);
            var formatter = (IFormatter<TElement>)Activator.CreateInstance(formatterType);
            return formatter;
        }

        protected const string FACTORY_METHOD_NAME = nameof(ITextFactorySpan<object>.FromText);
        
        protected static bool FactoryMethodPredicate(MethodInfo m, Type returnType) =>
            m.Name == FACTORY_METHOD_NAME &&
            m.GetParameters() is ParameterInfo[] @params && @params.Length == 1 &&
            @params[0].ParameterType is Type firstParamType &&
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
            //TODO more test for non-generic factory with generic methods
            );

        protected const BindingFlags STATIC_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static;

        protected class InnerStringParser<TElement> : ISpanParser<TElement>
        {
            private readonly Func<string, TElement> _delegate;

            public InnerStringParser(MethodCallExpression methodCall, ParameterExpression inputParameter) =>
                _delegate = Expression.Lambda<Func<string, TElement>>(methodCall, inputParameter).Compile();

            public TElement Parse(ReadOnlySpan<char> input) => _delegate(input.ToString());

            public sealed override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()} using {FACTORY_METHOD_NAME}(string input)";
        }

        protected class InnerSpanParser<TElement> : ISpanParser<TElement>
        {
            private delegate TElement ParserDelegate(ReadOnlySpan<char> input);
            private readonly ParserDelegate _delegate;

            public InnerSpanParser(MethodCallExpression methodCall, ParameterExpression inputParameter) =>
                _delegate = Expression.Lambda<ParserDelegate>(methodCall, inputParameter).Compile();

            public TElement Parse(ReadOnlySpan<char> input) => _delegate(input);

            public sealed override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()} using {FACTORY_METHOD_NAME}(ReadOnlySpan<char> input)";
        }
    }

    internal sealed class ConventionTransformer : FactoryMethodTransformer
    {
        protected override ISpanParser<TElement> GetParser<TElement>(Type elementType)
        {
            var conversionMethods = elementType.GetMethods(STATIC_METHOD_FLAGS)
                .Where(m => FactoryMethodPredicate(m, elementType)).ToList();

            MethodInfo method = FindMethodWithParameterType(typeof(ReadOnlySpan<char>)) ??
                                FindMethodWithParameterType(typeof(string)) ??
                                throw new InvalidOperationException($"No proper {FACTORY_METHOD_NAME} method found");

            Type methodInputType = method.GetParameters()[0].ParameterType;

            ParameterExpression inputParameter = Expression.Parameter(methodInputType, "input");
            MethodCallExpression methodCall = Expression.Call(method, inputParameter);

            return methodInputType == typeof(string)
                ? new InnerStringParser<TElement>(methodCall, inputParameter)
                : (ISpanParser<TElement>)
                  new InnerSpanParser<TElement>(methodCall, inputParameter);

            MethodInfo FindMethodWithParameterType(Type paramType) =>
                conversionMethods.FirstOrDefault(m =>
                    m.GetParameters() is ParameterInfo[] @params && @params.Length == 1 &&
                    @params[0].ParameterType == paramType);
        }

        public override bool CanHandle(Type type) => type.GetMethods(STATIC_METHOD_FLAGS)
            .Any(m => FactoryMethodPredicate(m, type));

        public override sbyte Priority => 20;

        public override string ToString() => $"Generate transformer using this.{FACTORY_METHOD_NAME}(string or ReadOnlySpan<char>)";
    }

    // ReSharper disable RedundantAttributeUsageProperty
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    // ReSharper restore RedundantAttributeUsageProperty
    public sealed class TextFactoryAttribute : Attribute
    {
        public Type FactoryType { get; }

        public TextFactoryAttribute([NotNull] Type factoryType) => FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    public interface ITextFactoryString<out TElement> { TElement FromText(string text); }

    public interface ITextFactorySpan<out TElement> { TElement FromText(ReadOnlySpan<char> text); }

    //TODO support non static factory classes via ITextFactoryString+ITextFactorySpan?
    internal sealed class TextFactoryTransformer : FactoryMethodTransformer
    {
        protected override ISpanParser<TElement> GetParser<TElement>(Type elementType)
        {
            if (elementType.IsGenericTypeDefinition)
                throw new NotSupportedException($"Parsing GenericTypeDefinition is not supported: {elementType.FullName}");


            Type factoryType = GetFactoryType(elementType) ??
                 throw new InvalidOperationException($"Missing factory declaration for {elementType.FullName}");

            var conversionMethods = factoryType.GetMethods(STATIC_METHOD_FLAGS)
                 .Where(m => FactoryMethodPredicate(m, elementType)).ToList();

            MethodInfo method = FindMethodWithParameterType(typeof(ReadOnlySpan<char>)) ??
                                FindMethodWithParameterType(typeof(string)) ??
                                throw new InvalidOperationException($"No proper {FACTORY_METHOD_NAME} method found");

            if (method.IsGenericMethodDefinition)
            {
                method = elementType.IsGenericType ?
                    method.MakeGenericMethod(elementType.GenericTypeArguments) :
                    method.MakeGenericMethod(elementType);
            }

            Type methodInputType = method.GetParameters()[0].ParameterType;

            ParameterExpression inputParameter = Expression.Parameter(methodInputType, "input");
            MethodCallExpression methodCall = Expression.Call(method, inputParameter);

            return methodInputType == typeof(string)
                ? new InnerStringParser<TElement>(methodCall, inputParameter)
                : (ISpanParser<TElement>)
                new InnerSpanParser<TElement>(methodCall, inputParameter);

            MethodInfo FindMethodWithParameterType(Type paramType) =>
                conversionMethods.FirstOrDefault(m =>
                    m.GetParameters() is ParameterInfo[] @params && @params.Length == 1 &&
                    @params[0].ParameterType == paramType);
        }

        public override bool CanHandle(Type type) =>
            GetFactoryType(type) is Type factoryType &&
            factoryType.GetMethods(STATIC_METHOD_FLAGS)
            .Any(m => FactoryMethodPredicate(m, type));


        private static Type GetFactoryType(Type type)
        {
            Type factoryType = type.GetCustomAttribute<TextFactoryAttribute>()?.FactoryType;
            if (factoryType == null) return null;
            if (factoryType.IsGenericTypeDefinition)
            {
                if (type.IsGenericTypeDefinition)
                    throw new NotSupportedException($"Parsing GenericTypeDefinition is not supported: {type.FullName}");

                factoryType = type.IsGenericType ?
                    factoryType.MakeGenericType(type.GenericTypeArguments) :
                    factoryType.MakeGenericType(type);
            }
            return factoryType;
        }

        public override sbyte Priority => 21;

        public override string ToString() => $"Generate transformer using {nameof(TextFactoryAttribute)}.{nameof(TextFactoryAttribute.FactoryType)}.{FACTORY_METHOD_NAME}(string or ReadOnlySpan<char>)";
    }
}
