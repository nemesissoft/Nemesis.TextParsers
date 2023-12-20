#nullable enable
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

public sealed class ConventionTransformerCreator(FactoryMethodSettings settings) : FactoryMethodTransformerCreator(settings)
{
    protected override Type GetFactoryMethodContainingType(Type type) => type;

    protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType) => method;

    public override sbyte Priority => 20;

    public override string ToString() =>
        $"Create transformer using this.{FactoryMethodName}(ReadOnlySpan<char> or string)";
}
