using System.Collections.Generic;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    static class EndToEndCases
    {
        public static IReadOnlyList<(string source, string expectedCode)> AutoDeconstructableCases() => new[]
        {
            (@"public record RecordPoint2d(double X, double Y) { }

               [Auto.AutoDeconstructable]
               [DeconstructableSettings(',', '∅', '\\', '[', ']')]
               public partial record RecordPoint3d(double X, double Y, double Z): RecordPoint2d(X, Y) 
               {
                   public double Magnitude { get; init; } //will NOT be subject to deconstruction
               }", @"//HEAD
using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(RecordPoint3dTransformer))]
    public partial record RecordPoint3d 
    {
#if DEBUG
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode("""", """")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class RecordPoint3dTransformer : TransformerBase<RecordPoint3d>
    {
        private readonly ITransformer<double> _transformer_X = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(',', '∅', '\\', '[', ']');

        public override RecordPoint3d GetEmpty() => new RecordPoint3d(_transformer_X.GetEmpty(), _transformer_Y.GetEmpty(), _transformer_Z.GetEmpty());
        protected override RecordPoint3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_X);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformer_Y);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformer_Z);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new RecordPoint3d(t1, t2, t3);
        }

        public override string Format(RecordPoint3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (X, Y, Z) = element;
                _helper.FormatElement(_transformer_X, X, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Y, Y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Z, Z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),



            (@"[Auto.AutoDeconstructable]
               [DeconstructableSettings(';', '∅', '\\', '(', ')')]
               public readonly partial struct Point3d
               {
                   public double X { get; } public double Y { get; } public double Z { get; }
                   public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }

                   public void Deconstruct(out double x, out System.Double y, out double z) { x = X; y = Y; z = Z; }
               }", @"//HEAD
using System;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(Point3dTransformer))]
    public readonly partial struct Point3d 
    {
#if DEBUG
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode("""", """")]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class Point3dTransformer : TransformerBase<Point3d>
    {
        private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');

        public override Point3d GetEmpty() => new Point3d(_transformer_x.GetEmpty(), _transformer_y.GetEmpty(), _transformer_z.GetEmpty());
        protected override Point3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_x);

            _helper.ParseNext(ref enumerator, 2);
            var t2 = _helper.ParseElement(ref enumerator, _transformer_y);

            _helper.ParseNext(ref enumerator, 3);
            var t3 = _helper.ParseElement(ref enumerator, _transformer_z);

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
                _helper.FormatElement(_transformer_x, x, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_y, y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_z, z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}")
        };
    }
}
