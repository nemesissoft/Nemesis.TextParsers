using System;
using System.Reflection;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class ConventionTransformer : FactoryMethodTransformer
    {
        protected override Type GetFactoryMethodContainer(Type type) => type;

        protected override MethodInfo PrepareParseMethod(MethodInfo method, Type elementType) => method;

        public override sbyte Priority => 20;

        public override string ToString() => $"Generate transformer using this.{FACTORY_METHOD_NAME}(string or ReadOnlySpan<char>)";
    }
}
