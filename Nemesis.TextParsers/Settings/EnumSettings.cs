namespace Nemesis.TextParsers.Settings;

#if NET
[method: System.Text.Json.Serialization.JsonConstructor]
#endif 
public sealed class EnumSettings(bool caseInsensitive, bool allowParsingNumerics) : ISettings
{
    public bool CaseInsensitive { get; private set; } = caseInsensitive;
    public bool AllowParsingNumerics { get; private set; } = allowParsingNumerics;

    public static EnumSettings Default { get; } = new(true, true);

    public override string ToString() => $"Value{(CaseInsensitive ? "≡" : "≠")}vAluE ; Text {(AllowParsingNumerics ? "and" : "but no")} №";
}