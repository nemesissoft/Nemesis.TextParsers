#if NETSTANDARD2_0

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}

#endif