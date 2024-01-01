//HEAD
using System;
using System.Numerics;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(LargeTransformer))]
    partial class Large 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class LargeTransformer : TransformerBase<Large>
    {
        private readonly ITransformer<double> _transformer_n1 = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<float> _transformer_n2 = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<int> _transformer_n3 = TextTransformer.Default.GetTransformer<int>();
        private readonly ITransformer<uint> _transformer_n4 = TextTransformer.Default.GetTransformer<uint>();
        private readonly ITransformer<short> _transformer_n5 = TextTransformer.Default.GetTransformer<short>();
        private readonly ITransformer<ushort> _transformer_n6 = TextTransformer.Default.GetTransformer<ushort>();
        private readonly ITransformer<byte> _transformer_n7 = TextTransformer.Default.GetTransformer<byte>();
        private readonly ITransformer<sbyte> _transformer_n8 = TextTransformer.Default.GetTransformer<sbyte>();
        private readonly ITransformer<long> _transformer_n9 = TextTransformer.Default.GetTransformer<long>();
        private readonly ITransformer<ulong> _transformer_n10 = TextTransformer.Default.GetTransformer<ulong>();
        private readonly ITransformer<decimal> _transformer_n11 = TextTransformer.Default.GetTransformer<decimal>();
        private readonly ITransformer<BigInteger> _transformer_n12 = TextTransformer.Default.GetTransformer<BigInteger>();
        private readonly ITransformer<Complex> _transformer_n13 = TextTransformer.Default.GetTransformer<Complex>();
        private const int ARITY = 13;


        private readonly TupleHelper _helper;

        public LargeTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override Large ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_n1);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_n2, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_n3, 3);

            var t4 = _helper.ParseElement(ref enumerator, _transformer_n4, 4);

            var t5 = _helper.ParseElement(ref enumerator, _transformer_n5, 5);

            var t6 = _helper.ParseElement(ref enumerator, _transformer_n6, 6);

            var t7 = _helper.ParseElement(ref enumerator, _transformer_n7, 7);

            var t8 = _helper.ParseElement(ref enumerator, _transformer_n8, 8);

            var t9 = _helper.ParseElement(ref enumerator, _transformer_n9, 9);

            var t10 = _helper.ParseElement(ref enumerator, _transformer_n10, 10);

            var t11 = _helper.ParseElement(ref enumerator, _transformer_n11, 11);

            var t12 = _helper.ParseElement(ref enumerator, _transformer_n12, 12);

            var t13 = _helper.ParseElement(ref enumerator, _transformer_n13, 13);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Large(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public override string Format(Large element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13) = element;
                _helper.FormatElement(_transformer_n1, n1, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n2, n2, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n3, n3, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n4, n4, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n5, n5, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n6, n6, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n7, n7, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n8, n8, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n9, n9, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n10, n10, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n11, n11, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n12, n12, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n13, n13, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}