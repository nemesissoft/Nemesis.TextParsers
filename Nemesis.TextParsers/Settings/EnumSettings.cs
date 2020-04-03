namespace Nemesis.TextParsers.Settings
{
    //TODO
    public readonly struct EnumSettings : ISettings
    {
        public bool CaseInsensitive { get; }
        public bool AllowParsingNumerics { get; }

        public EnumSettings(bool caseInsensitive, bool allowParsingNumerics)
        {
            CaseInsensitive = caseInsensitive;
            AllowParsingNumerics = allowParsingNumerics;
        }

        public static EnumSettings Default { get; } =
            new EnumSettings(true, true);

        public override string ToString() => $"{nameof(CaseInsensitive)}: {CaseInsensitive}, {nameof(AllowParsingNumerics)}: {AllowParsingNumerics}";
    }
}