using System;
using System.ComponentModel;
using System.Globalization;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers
{
    public sealed class UniversalTextTypeConverter<TValue> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(string) || base.CanConvertTo(context, destinationType);


        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value is string text
                ? TextTransformer.Default.GetTransformer<TValue>().ParseFromText(text)
                : base.ConvertFrom(context, culture, value);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string)
                ? TextTransformer.Default.GetTransformer<TValue>().Format((TValue)value)
                : base.ConvertTo(context, culture, value, destinationType);
    }
}
