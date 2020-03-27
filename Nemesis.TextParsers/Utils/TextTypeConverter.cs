using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Utils
{
    public abstract class TextTypeConverter : TypeConverter
    {
        public sealed override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public sealed override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    [PublicAPI]
    public abstract class BaseTextConverter<TValue> : TextTypeConverter
    {
        public sealed override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value is string text ? ParseString(text) : default;

        public abstract TValue ParseString(string text);


        public sealed override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string) ?
                FormatToString((TValue)value) :
                base.ConvertTo(context, culture, value, destinationType);

        public abstract string FormatToString(TValue value);
    }

    [PublicAPI]
    public abstract class BaseNullableTextConverter<TValue> : TextTypeConverter
    {
        public sealed override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value switch
            {
                null => ParseNull(),
                string text => ParseString(text),
                _ => default
            };

        protected abstract TValue ParseNull();

        protected abstract TValue ParseString(string text);



        public sealed override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string) ?
                (value is null ? FormatNull() : FormatToString((TValue)value)) :
                base.ConvertTo(context, culture, value, destinationType);

        protected abstract string FormatNull();

        protected abstract string FormatToString(TValue value);
    }

    //in future this can be taken from text transformers 
    [PublicAPI]
    // ReSharper disable RedundantAttributeUsageProperty
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
    // ReSharper restore RedundantAttributeUsageProperty
    public sealed class TextConverterSyntaxAttribute : Attribute
    {
        public string Syntax { get; }
        public char[] SpecialCharacters { get; }

        public TextConverterSyntaxAttribute(string syntax) => Syntax = syntax;

        public TextConverterSyntaxAttribute(string syntax, params char[] specialCharacters)
        {
            Syntax = syntax;
            SpecialCharacters = specialCharacters;
        }

        // ReSharper disable once InconsistentNaming
        private static readonly string NL = Environment.NewLine;

        private static string GetEscapeSequences(params char[] specialChars) =>
            string.Join(", ",
                specialChars.Select(c => $@"escape '{c}' with ""\{c}""")
            ) + @"and '\' with double backslash ""\\""";

        private static string GetSyntaxFromAttribute(Type source, Type originalType)
        {
            var attr = source?.GetCustomAttribute<TextConverterSyntaxAttribute>(true);
            if (attr == null) return null;

            (string syntax, var specialChars) = (attr.Syntax, attr.SpecialCharacters);

            string elementsSyntax =
                originalType.IsGenericType && !originalType.IsGenericTypeDefinition &&
                originalType.GenericTypeArguments is { } genArgs && genArgs.Length > 0
                  ? string.Join(NL, genArgs.Select(GetConverterSyntax))
                  : "";

            return syntax
                   +
                   (specialChars?.Length > 0 ? $"{NL}{GetEscapeSequences(specialChars)}" : "")
                   +
                   (string.IsNullOrWhiteSpace(elementsSyntax) ? "" : $"{NL}{NL}Elements syntax:{NL}{elementsSyntax}")
                ;
        }

        [UsedImplicitly]
        public static string GetConverterSyntax([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            string fromType = GetSyntaxFromAttribute(type, type);

            string fromConverter =
                type.GetCustomAttribute<TypeConverterAttribute>(true)?.ConverterTypeName is var converterTypeName &&
                converterTypeName != null && Type.GetType(converterTypeName, false) is { } converterType
                    ? GetSyntaxFromAttribute(converterType, type)
                    : null;

            string fromTextFactory =
                type.GetCustomAttribute<TextFactoryAttribute>(true)?.FactoryType is { } factoryType
                    ? GetSyntaxFromAttribute(factoryType, type)
                    : null;

            string fromTransformer =
                type.GetCustomAttribute<TransformerAttribute>(true)?.TransformerType is { } transformerType
                    ? GetSyntaxFromAttribute(transformerType, type)
                    : null;

            if (TryConcat(out string message, fromType, fromConverter, fromTextFactory, fromTransformer))
                return message;
            else if (typeof(string) == type)
                return "UTF-16 character string";
            else
            {
                bool isNullable = false;
                if (Nullable.GetUnderlyingType(type) is { } underlyingType)
                {
                    isNullable = true;
                    type = underlyingType;
                }


                (bool isNumeric, var min, var max, bool isFloating) = GetNumberMeta(type);
                if (isNumeric)
                    return FormattableString.Invariant(
                        $"{(isFloating ? "Floating" : "Whole")} number from {min} to {max}{(isNullable ? " or null" : "")}");
                else if (typeof(bool) == type)
                    return FormattableString.Invariant($"'{true}' or '{false}'{(isNullable ? " or null" : "")}");

                else if (typeof(char) == type)
                    return $"Single UTF-16 character{(isNullable ? " or null" : "")}";

                else if (typeof(TimeSpan) == type)
                    return $"hh:mm:ss {(isNullable ? " or null" : "")}";

                else if (typeof(DateTime) == type)
                    // ReSharper disable once StringLiteralTypo
                    return
                        $"ISO 8601 roundtrip datetime literal (yyyy-MM-ddTHH:mm:ss.fffffffK) {(isNullable ? " or null" : "")}";

                else if (type.IsEnum)
                    return FormattableString.Invariant(
                        $"'{string.Join(", ", Enum.GetValues(type).Cast<object>())}'{(isNullable ? " or null" : "")}");


                else if (TypeMeta.TryGetGenericRealization(type, typeof(IDictionary<,>), out var dictType))
                {
                    string keySyntax = GetConverterSyntax(dictType.GenericTypeArguments[0]),
                        valSyntax = GetConverterSyntax(dictType.GenericTypeArguments[1]);

                    return @$"KEY=VALUE pairs separated with semicolons(';') i.e.
key1=value1;key2=value2;key3=value3
({GetEscapeSequences('=', ';')})"
                           +
                           (string.IsNullOrWhiteSpace(keySyntax) ? "" : $"{NL}Key syntax:{NL}{keySyntax}")
                           +
                           (string.IsNullOrWhiteSpace(valSyntax) ? "" : $"{NL}Value syntax:{NL}{valSyntax}")
                        ;
                }

                else if (TypeMeta.TryGetGenericRealization(type, typeof(ICollection<>), out var collType) ||
                         type.IsArray)
                {
                    var elemType = (type.IsArray
                            ? type.GetElementType()
                            : collType?.GenericTypeArguments[0]
                        ) ?? throw new NotSupportedException(
                            $"Type {type.GetFriendlyName()} is not supported for formatting");

                    string elemSyntax = GetConverterSyntax(elemType);

                    return @$"Elements separated with pipe ('|') i.e.
1|2|3
({GetEscapeSequences('|')})"
                           +
                           (string.IsNullOrWhiteSpace(elemSyntax) ? "" : $"{NL}Element syntax:{NL}{elemSyntax}")
                        ;
                }


                else if (typeof(IDictionary).IsAssignableFrom(type))
                    return "key1=value1;key2=value2;key3=value3";

                else if (typeof(ICollection).IsAssignableFrom(type))
                    return "Values separated with pipe ('|')";

                return null;
            }
        }

        private static bool TryConcat(out string message, params string[] texts)
        {
            message = "";

            foreach (string text in texts)
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (message.Length > 0)
                        message += NL + NL;
                    message += text;
                }

            return !string.IsNullOrWhiteSpace(message);
        }

        private static (bool IsNumeric, object Min, object Max, bool IsFloating) GetNumberMeta(Type type)
        {
            bool notEnum = !type.IsEnum;

            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => (notEnum, sbyte.MinValue, sbyte.MaxValue, false),
                TypeCode.Byte => (notEnum, byte.MinValue, byte.MaxValue, false),
                TypeCode.Int16 => (notEnum, short.MinValue, short.MaxValue, false),
                TypeCode.Int32 => (notEnum, int.MinValue, int.MaxValue, false),
                TypeCode.Int64 => (notEnum, long.MinValue, long.MaxValue, false),
                TypeCode.UInt16 => (notEnum, ushort.MinValue, ushort.MaxValue, false),
                TypeCode.UInt32 => (notEnum, uint.MinValue, uint.MaxValue, false),
                TypeCode.UInt64 => (notEnum, ulong.MinValue, ulong.MaxValue, false),
                TypeCode.Double => (notEnum, double.MinValue, double.MaxValue, true),
                TypeCode.Single => (notEnum, float.MinValue, float.MaxValue, true),
                TypeCode.Decimal => (notEnum, decimal.MinValue, decimal.MaxValue, true),
                _ => default
            };
        }
    }
}
