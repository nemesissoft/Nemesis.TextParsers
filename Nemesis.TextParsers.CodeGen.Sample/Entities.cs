using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.CodeGen.Sample;

[Transformer(typeof(MonthCodeGenTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true, TransformerClassName = "MonthCodeGenTransformer")]
public enum Month : byte
{
    None = 0,
    January = 1,
    February = 2,
    March = 3,
    April = 4,
    May = 5,
    June = 6,
    July = 7,
    August = 8,
    September = 9,
    October = 10,
    November = 11,
    December = 12
}

[Transformer(typeof(SpecialNamespace.MonthsTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false, TransformerClassNamespace = "SpecialNamespace")]
[Flags]
public enum Months : short
{
    None = 0,
    January
        = 0b0000_0000_0001,
    February
        = 0b0000_0000_0010,
    March
        = 0b0000_0000_0100,
    April
        = 0b0000_0000_1000,
    May
        = 0b0000_0001_0000,
    June
        = 0b0000_0010_0000,
    July
        = 0b0000_0100_0000,
    August
        = 0b0000_1000_0000,
    September
        = 0b0001_0000_0000,
    October
        = 0b0010_0000_0000,
    November
        = 0b0100_0000_0000,
    December
        = 0b1000_0000_0000,

    Summer = July | August | September,
    All = January | February | March | April |
          May | June | July | August |
          September | October | November | December
}

[Auto.AutoDeconstructable]
[DeconstructableSettings('_', '∅', '%', '〈', '〉')]
readonly partial struct StructPoint3d(double x, double y, double z)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;

    public void Deconstruct(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
}

//[Auto.AutoDeconstructable] public class BadPoint2d { }

public record RecordPoint2d(double X, double Y) { }

[Auto.AutoDeconstructable]
[DeconstructableSettings(',', '␀', '/', '⟪', '⟫')]
public partial record RecordPoint3d(double X, double Y, double Z) : RecordPoint2d(X, Y)
{
    public double Magnitude { get; init; } //will NOT be subject to deconstruction
}

[Auto.AutoDeconstructable]
[DeconstructableSettings(',', '␀', '/', '⟪', '⟫')]
public partial record SpaceAndTime(double X, double Y, double Z, DateTime Time) : RecordPoint3d(X, Y, Z);