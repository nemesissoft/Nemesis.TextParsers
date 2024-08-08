//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(eDoubleStructTransformer))]
    readonly partial struct eDoubleStruct 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class eDoubleStructTransformer : TransformerBase<eDoubleStruct>
    {
        private readonly ITransformer<double> _transformer_value;
        private const int ARITY = 1;

        public eDoubleStructTransformer(ITransformerStore store)
        {
            _transformer_value = store.GetTransformer<double>();        
        }

        private readonly TupleHelper _helper;

        public eDoubleStructTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override eDoubleStruct ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_value);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new eDoubleStruct(t1);
        }

        public override string Format(eDoubleStruct element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 double value;
                 element.Deconstruct(out value);

                _helper.FormatElement(_transformer_value, value, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}