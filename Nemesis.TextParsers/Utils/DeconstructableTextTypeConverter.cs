using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Utils
{
    public sealed class DeconstructableTextTypeConverter<TDeconstructable> : BaseTextConverter<TDeconstructable>
    {
        private static readonly ITransformer<TDeconstructable> _transformer = Sett.Default.ToTransformer<TDeconstructable>();

        public override TDeconstructable ParseString(string text) => _transformer.ParseFromText(text);
        

        public override string FormatToString(TDeconstructable value) => _transformer.Format(value);
    }

    public sealed class DeconstructableNullableTextTypeConverter<TDeconstructable> : BaseNullableTextConverter<TDeconstructable>
    {
        private static readonly ITransformer<TDeconstructable> _transformer = Sett.Default.ToTransformer<TDeconstructable>();

        protected override TDeconstructable ParseNull() => default;

        protected override TDeconstructable ParseString(string text) => _transformer.ParseFromText(text);
        

        protected override string FormatNull() => null;

        protected override string FormatToString(TDeconstructable value) => _transformer.Format(value);
    }
}