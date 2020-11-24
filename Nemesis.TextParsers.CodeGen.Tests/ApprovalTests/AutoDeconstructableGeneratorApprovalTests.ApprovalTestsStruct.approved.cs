//HEAD
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;
using System;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(Point3dTransformer))]
    public readonly partial struct Point3d 
    {
#if DEBUG
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
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
}