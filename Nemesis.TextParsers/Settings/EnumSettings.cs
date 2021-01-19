namespace Nemesis.TextParsers.Settings
{
    public readonly struct EnumSettings : ISettings
    {
        public bool CaseInsensitive { get; }
        public bool AllowParsingNumerics { get; }

        public EnumSettings(bool caseInsensitive, bool allowParsingNumerics)
        {
            CaseInsensitive = caseInsensitive;
            AllowParsingNumerics = allowParsingNumerics;
        }

        public static EnumSettings Default { get; } = new(true, true);

        public override string ToString() => $"Value{(CaseInsensitive ? "≡" : "≠")}vAluE ; Text {(AllowParsingNumerics ? "and" : "but no")} №";
    }
}