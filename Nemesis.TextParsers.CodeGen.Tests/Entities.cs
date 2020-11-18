using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
//GENERATE: append namespaces from source file

namespace Nemesis.TextParsers.CodeGen.Tests
{
    //[AutoDeconstructable(';', '∅', '\\', '(', ')')]
    public readonly partial struct Point3d
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Deconstruct(out double x, out double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }
    }

    //generated
    [Transformer(typeof(Point3dTransformer))]
    public readonly partial struct Point3d
    {

    }

    [System.CodeDom.Compiler.GeneratedCode("AutoDeconstructableGenerator", "1.0")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    public sealed class Point3dTransformer: TransformerBase<Point3d>
    {
        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');
        private readonly ITransformer<double> _transformerX = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformerY = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformerZ = TextTransformer.Default.GetTransformer<double>();

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
    }
}
