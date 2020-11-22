extern alias original;

using System;

using DeconstructableSettings = original::Nemesis.TextParsers.Settings.DeconstructableSettingsAttribute;
using Transformer = original::Nemesis.TextParsers.Parsers.TransformerAttribute;

using TextTransformer = original::Nemesis.TextParsers.TextTransformer;
using TransformerBase = original::Nemesis.TextParsers.TransformerBase<Nemesis.TextParsers.CodeGen.Tests.Point3d>;
using DoubleTransformer = original::Nemesis.TextParsers.ITransformer<double>;
using ITransformerStore = original::Nemesis.TextParsers.ITransformerStore;
using TupleHelper = original::Nemesis.TextParsers.Utils.TupleHelper;
using ValueSequenceBuilder = original::Nemesis.TextParsers.Utils.ValueSequenceBuilder<char>;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [AutoDeconstructable]
    [DeconstructableSettings(';', '∅', '\\', '(', ')')]
    readonly partial struct Point3d
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }

        public void Deconstruct(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
    }

    //generated
    [Transformer(typeof(Point3dTransformer))]
    readonly partial struct Point3d { }

    [System.CodeDom.Compiler.GeneratedCode("AutoDeconstructableGenerator", "1.0")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class Point3dTransformer : TransformerBase
    {
        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');
        private readonly DoubleTransformer _transformerX = TextTransformer.Default.GetTransformer<double>();
        private readonly DoubleTransformer _transformerY = TextTransformer.Default.GetTransformer<double>();
        private readonly DoubleTransformer _transformerZ = TextTransformer.Default.GetTransformer<double>();

        private const int ARITY = 3;

        protected override Point3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);

            var t1 = _helper.ParseElement(ref enumerator, _transformerX);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformerY);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformerZ);

            _helper.ParseEnd(ref enumerator, ARITY);

            return new Point3d(t1, t2, t3);
        }

        public override string Format(Point3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder(initialBuffer);

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

        public override Point3d GetEmpty() => new Point3d(_transformerX.GetEmpty(), _transformerY.GetEmpty(), _transformerZ.GetEmpty());
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    sealed class AutoDeconstructableAttribute : Attribute
    {
    }
}
