#nullable enable
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

public sealed class ConventionTransformerHandler(FactoryMethodSettings settings) : FactoryMethodTransformerHandler(settings)
{
    protected override Type GetFactoryMethodContainingType(Type type) => type;

    protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType) => method;

    public override sbyte Priority => 20;

    public override string ToString() =>
        $"Create transformer using this.{FactoryMethodName}(ReadOnlySpan<char> or string)";

    protected override string DescribeHandlerMatch() => $"Type with method {FactoryMethodName}(ReadOnlySpan<char> or string)";
}
