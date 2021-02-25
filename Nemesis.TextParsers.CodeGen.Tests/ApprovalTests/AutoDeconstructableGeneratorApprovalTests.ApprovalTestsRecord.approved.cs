//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(RecordPoint3dTransformer))]
    public partial record RecordPoint3d 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
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
}