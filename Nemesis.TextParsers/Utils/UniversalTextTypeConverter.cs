using System;
using System.ComponentModel;
using System.Globalization;

namespace Nemesis.TextParsers.Utils
{
    public sealed class UniversalTextTypeConverter<TValue> : TextTypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value is string text
                ? TextTransformer.Default.GetTransformer<TValue>().Parse(text)
                : base.ConvertFrom(context, culture, value);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string)
                ? TextTransformer.Default.GetTransformer<TValue>().Format((TValue)value)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}