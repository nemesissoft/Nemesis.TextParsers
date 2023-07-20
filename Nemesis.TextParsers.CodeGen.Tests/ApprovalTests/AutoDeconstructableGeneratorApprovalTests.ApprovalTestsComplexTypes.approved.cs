//HEAD
using System;
using System.Collections.Generic;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(ComplexTypesTransformer))]
    public partial record ComplexTypes 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class ComplexTypesTransformer : TransformerBase<ComplexTypes>
    {
        private readonly ITransformer<double[]> _transformer_Doubles = TextTransformer.Default.GetTransformer<double[]>();
        private readonly ITransformer<Number?> _transformer_Nullable = TextTransformer.Default.GetTransformer<Number?>();
        private readonly ITransformer<List<Number>> _transformer_List = TextTransformer.Default.GetTransformer<List<Number>>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper;

        public ComplexTypesTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override ComplexTypes ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_Doubles);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_Nullable, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_List, 3);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new ComplexTypes(t1, t2, t3);
        }

        public override string Format(ComplexTypes element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (Doubles, Nullable, List) = element;
                _helper.FormatElement(_transformer_Doubles, Doubles, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Nullable, Nullable, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_List, List, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}