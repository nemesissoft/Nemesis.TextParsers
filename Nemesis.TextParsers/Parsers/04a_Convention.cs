using JetBrains.Annotations;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class ConventionTransformerCreator : FactoryMethodTransformerCreator
{
    public ConventionTransformerCreator([NotNull] FactoryMethodSettings settings)
        : base(settings) { }

    protected override Type GetFactoryMethodContainer(Type type) => type;

    protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType) => method;

    public override sbyte Priority => 20;

    public override string ToString() =>
        $"Create transformer using this.{FactoryMethodName}(ReadOnlySpan<char> or string)";
}
