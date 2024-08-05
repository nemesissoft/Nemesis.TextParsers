#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Nemesis.TextParsers.Settings;

public sealed record EnumSettings(
    bool CaseInsensitive = true,
    bool AllowParsingNumerics = true
) : ISettings<EnumSettings>
{
    public bool IsValid([NotNullWhen(false)] out string? error)
    {
        error = null;
        return true;
    }

    public EnumSettings DeepClone() => this with { };

    public static EnumSettings Default { get; } = new();

    public override string ToString() => $"Value{(CaseInsensitive ? "≡" : "≠")}vAluE ; Text {(AllowParsingNumerics ? "and" : "but no")} №";
}