using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.CodeGen.Sample;

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
public partial record SpaceAndTime(double X, double Y, double Z, DateTime Time) : RecordPoint3d(X, Y, Z)
{
}
