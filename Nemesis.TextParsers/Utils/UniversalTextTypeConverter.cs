using System;
using System.ComponentModel;
using System.Globalization;

namespace Nemesis.TextParsers.Utils
{
    public sealed class UniversalTextTypeConverter<TValue> : TextTypeConverter
    {
        private static readonly ITransformer<TValue> _transformer =
            TextTransformer.Default.GetTransformer<TValue>();

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
             _transformer.Parse(value as string);
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string)
                ? _transformer.Format((TValue)value)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}