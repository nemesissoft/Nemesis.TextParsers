#nullable enable
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

public sealed class TextFactoryTransformerHandler(FactoryMethodSettings settings) : FactoryMethodTransformerHandler(settings)
{
    protected override Type? GetFactoryMethodContainingType(Type type)
    {
        var factoryType = type.GetCustomAttribute<TextFactoryAttribute>()?.FactoryType;
        if (factoryType == null) return null;
        if (!factoryType.IsGenericTypeDefinition) return factoryType;

        if (type.IsGenericTypeDefinition)
            throw new NotSupportedException($"Text transformation for GenericTypeDefinition is not supported: {type.GetFriendlyName()}");

        factoryType = type.IsGenericType ? factoryType.MakeGenericType(type.GenericTypeArguments) : factoryType.MakeGenericType(type);
        return factoryType;
    }

    protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType)
    {
        if (method.IsGenericMethodDefinition)
        {
            method = elementType.IsGenericType ?
                method.MakeGenericMethod(elementType.GenericTypeArguments) :
                method.MakeGenericMethod(elementType);
        }

        return method;
    }

    public override sbyte Priority => 21;

    public override string ToString() =>
        $"Create transformer using {nameof(TextFactoryAttribute)}.{nameof(TextFactoryAttribute.FactoryType)}.{FactoryMethodName}(ReadOnlySpan<char> or string)";

    protected override string DescribeHandlerMatch() => $"Type decorated with {nameof(TextFactoryAttribute)} pointing to factory with method {FactoryMethodName}(ReadOnlySpan<char> or string)";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class TextFactoryAttribute(Type factoryType) : Attribute
{
    public Type FactoryType { get; } = factoryType;
}
