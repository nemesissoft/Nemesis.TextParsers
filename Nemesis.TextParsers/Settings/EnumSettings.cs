namespace Nemesis.TextParsers.Settings
{
    public sealed record EnumSettings(bool CaseInsensitive, bool AllowParsingNumerics) : ISettings
    {
        public static EnumSettings Default { get; } = new(true, true);

        public override string ToString() => $"Value{(CaseInsensitive ? "≡" : "≠")}vAluE ; Text {(AllowParsingNumerics ? "and" : "but no")} №";

        public void Validate() { }
    }
}