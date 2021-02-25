using System;

using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.CodeGen.Sample
{
    [Auto.AutoDeconstructable]
    [DeconstructableSettings('_', '∅', '%', '〈', '〉')]
    readonly partial struct StructPoint3d
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }       

        public StructPoint3d(double x, double y, double z) { X = x; Y = y; Z = z; }

        public void Deconstruct(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
    }

    //[Auto.AutoDeconstructable] public class BadPoint2d { }

    public record RecordPoint2d(double X, double Y) { }

    [Auto.AutoDeconstructable]
    [Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute(',', '␀', '/', '⟪', '⟫')]
    public partial record RecordPoint3d(double X, double Y, double Z) : RecordPoint2d(X, Y)
    {
        public double Magnitude { get; init; } //will NOT be subject to deconstruction
    }

    [Auto.AutoDeconstructable]
    [Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute(',', '␀', '/', '⟪', '⟫')]
    public partial record SpaceAndTime(double X, double Y, double Z, DateTime Time) : RecordPoint3d(X, Y, Z)
    {
    }

    //generated
    //[Transformer(typeof(Point3dTransformer))]
    //readonly partial struct Point3d { }

    /*[System.CodeDom.Compiler.GeneratedCode("AutoDeconstructableGenerator", "1.0")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class SamplePoint3dTransformer : TransformerBase<StructPoint3d>
    {
        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');
        private readonly ITransformer<double> _transformerX = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformerY = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformerZ = TextTransformer.Default.GetTransformer<double>();

        private const int ARITY = 3;

        protected override StructPoint3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);

            var t1 = _helper.ParseElement(ref enumerator, _transformerX);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformerY);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformerZ);

            _helper.ParseEnd(ref enumerator, ARITY);

            return new StructPoint3d(t1, t2, t3);
        }

        public override string Format(StructPoint3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            try
            {
                _helper.StartFormat(ref accumulator);

                var (x, y, z) = element;
                _helper.FormatElement(_transformerX, x, ref accumulator);
                _helper.AddDelimiter(ref accumulator);

                _helper.FormatElement(_transformerY, y, ref accumulator);
                _helper.AddDelimiter(ref accumulator);

                _helper.FormatElement(_transformerZ, z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }

        public override StructPoint3d GetEmpty() => new StructPoint3d(_transformerX.GetEmpty(), _transformerY.GetEmpty(), _transformerZ.GetEmpty());
    }*/

    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    //sealed class AutoDeconstructableAttribute : Attribute { }
}
