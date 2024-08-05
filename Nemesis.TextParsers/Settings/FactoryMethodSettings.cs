#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nemesis.TextParsers.Settings;

public sealed record FactoryMethodSettings(
    string FactoryMethodName = "FromText",
    string EmptyPropertyName = "Empty",
    string NullPropertyName = "Null"
) : ISettings<FactoryMethodSettings>
{
    public bool IsValid([NotNullWhen(false)] out string? error)
    {
        error = null;
        return true;
    }

    public FactoryMethodSettings DeepClone() => this with { };

    public static FactoryMethodSettings Default { get; } = new();

    public override string ToString() =>
        $"Parsed by {FactoryMethodName} Empty: {EmptyPropertyName} Null: {NullPropertyName}";
}