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
using Nemesis.TextParsers.Settings;

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
    }

    public class TextSyntaxProvider
    {
        private readonly SettingsStore _settingsStore;
        public TextSyntaxProvider([NotNull] SettingsStore settingsStore) => _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

        public static TextSyntaxProvider Default { get; } = new(TextTransformer.Default.SettingsStore);

        [UsedImplicitly]
        public string GetSyntaxFor([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (TryGetGrammarDescription(type, out string message)) return message;
            else if (typeof(string) == type) return GetStringSyntax();
            else if (type.IsValueType && TryGetValueTypeSyntax(type, out string valueTypeSyntax)) return valueTypeSyntax;
            else if (TryGetDictionarySyntax(type, out string dictSyntax)) return dictSyntax;
            else if (TryGetCollectionSyntax(type, out string collSyntax)) return collSyntax;
            else if (typeof(IDictionary).IsAssignableFrom(type)) return "key1=value1;key2=value2;key3=value3";
            else if (typeof(ICollection).IsAssignableFrom(type)) return "Values separated with pipe ('|')";
            return type.GetFriendlyName();
        }

        protected virtual bool TryGetCollectionSyntax(Type type, out string syntax)
        {
            if (TypeMeta.TryGetGenericRealization(type, typeof(ICollection<>), out var collType) || type.IsArray)
            {
                var elemType = (type.IsArray
                        ? type.GetElementType()
                        : collType?.GenericTypeArguments[0]
                    ) ?? throw new NotSupportedException(
                        $"Type {type.GetFriendlyName()} is not supported for formatting");

                string elemSyntax = GetSyntaxFor(elemType);

                var s = type.IsArray ? (CollectionSettingsBase)_settingsStore.GetSettingsFor<ArraySettings>() : _settingsStore.GetSettingsFor<CollectionSettings>();
                char deli = s.ListDelimiter;
                char? start = s.Start, end = s.End;

                syntax = @$"Elements separated with '{deli}' bound with {(start.HasValue ? start.Value.ToString() : "nothing")} and {(end.HasValue ? end.Value.ToString() : "nothing")} i.e.
{start}1{deli}2{deli}3{end}
({GetEscapeSequences(s.EscapingSequenceStart, deli, s.NullElementMarker)})"
                       +
                       (string.IsNullOrWhiteSpace(elemSyntax) ? "" : $"{NL}Element syntax:{NL}{AddIndentation(elemSyntax)}")
                    ;
                return true;
            }
            else
            {
                syntax = null;
                return false;
            }
        }

        protected virtual bool TryGetDictionarySyntax(Type type, out string syntax)
        {
            if (TypeMeta.TryGetGenericRealization(type, typeof(IDictionary<,>), out var dictType))
            {
                string keySyntax = GetSyntaxFor(dictType.GenericTypeArguments[0]),
                       valSyntax = GetSyntaxFor(dictType.GenericTypeArguments[1]);

                var s = _settingsStore.GetSettingsFor<DictionarySettings>();
                char eq = s.DictionaryKeyValueDelimiter, semi = s.DictionaryPairsDelimiter;
                char? start = s.Start, end = s.End;

                syntax = @$"KEY{eq}VALUE pairs separated with '{semi}' bound with {(start.HasValue ? start.Value.ToString() : "nothing")} and {(end.HasValue ? end.Value.ToString() : "nothing")} i.e.
{start}key1{eq}value1{semi}key2{eq}value2{semi}key3{eq}value3{end}
({GetEscapeSequences(s.EscapingSequenceStart, eq, semi, s.NullElementMarker)})"
                         +
                         (string.IsNullOrWhiteSpace(keySyntax) ? "" : $"{NL}Key syntax:{NL}{AddIndentation(keySyntax)}")
                         +
                         (string.IsNullOrWhiteSpace(valSyntax) ? "" : $"{NL}Value syntax:{NL}{AddIndentation(valSyntax)}")
                    ;
                return true;
            }
            else
            {
                syntax = null;
                return false;
            }
        }

        protected virtual bool TryGetValueTypeSyntax(Type type, out string syntax)
        {
            syntax = null;

            bool isNullable = false;
            if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            {
                isNullable = true;
                type = underlyingType;
            }


            (bool isNumeric, var min, var max, bool isFloating) = GetNumberMeta(type);
            if (isNumeric)
                syntax = FormattableString.Invariant(
                    $"{(isFloating ? "Floating" : "Whole")} number from {min} to {max}{(isNullable ? " or null" : "")}");
            else if (typeof(bool) == type)
                syntax = FormattableString.Invariant($"'{true}' or '{false}'{(isNullable ? " or null" : "")}");

            else if (typeof(char) == type)
                syntax = $"Single UTF-16 character{(isNullable ? " or null" : "")}";

            else if (typeof(TimeSpan) == type)
                syntax = $"hh:mm:ss {(isNullable ? " or null" : "")}";

            else if (typeof(DateTime) == type)
                // ReSharper disable once StringLiteralTypo
                syntax =
                    $"ISO 8601 roundtrip datetime literal (yyyy-MM-ddTHH:mm:ss.fffffffK) {(isNullable ? " or null" : "")}";

            else if (type.IsEnum)
                syntax = FormattableString.Invariant(
                    $"One of following: {string.Join(", ", Enum.GetValues(type).Cast<object>())}{(isNullable ? " or null" : "")}");

            return syntax != null;
        }

        protected virtual string GetStringSyntax() => "UTF-16 character string";

        // ReSharper disable SuggestBaseTypeForParameter
        private string GetSyntaxFromAttribute(Type source, Type originalType)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            var attr = source?.GetCustomAttribute<TextConverterSyntaxAttribute>(true);
            if (attr == null) return null;

            (string syntax, var specialChars) = (attr.Syntax, attr.SpecialCharacters);

            string elementsSyntax =
                originalType.IsGenericType && !originalType.IsGenericTypeDefinition &&
                originalType.GenericTypeArguments is {Length: > 0} genArgs
                    ? string.Join(NL, genArgs.Select(GetSyntaxFor))
                    : "";

            return syntax
                   +
                   (specialChars?.Length > 0 ? $"{NL}{GetEscapeSequences('\\', specialChars)}" : "")
                   +
                   (string.IsNullOrWhiteSpace(elementsSyntax) ? "" : $"{NL}{NL}{originalType.Name} elements syntax:{NL}{AddIndentation(elementsSyntax)}")
                ;
        }

        private bool TryGetGrammarDescription(Type type, out string message)
        {
            string fromType = GetSyntaxFromAttribute(type, type);

            string fromConverter =
                type.GetCustomAttribute<TypeConverterAttribute>(true)?.ConverterTypeName is { } converterTypeName &&
                Type.GetType(converterTypeName, false) is { } converterType
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

            var texts = new[] { fromType, fromConverter, fromTextFactory, fromTransformer };

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

        // ReSharper disable once InconsistentNaming
        private static readonly string NL = Environment.NewLine;

        private static string GetEscapeSequences(char escapingSequenceStart, params char[] specialChars) =>
            "escape " + string.Join(", ",
                specialChars.Select(c => $@"'{c}' with ""\{c}""")
            ) + $@" and '{escapingSequenceStart}' by doubling it ""{escapingSequenceStart}{escapingSequenceStart}""";

        private static string AddIndentation(string text)
        {
            var indented = text.Split(new[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None)
                .Select(line => $"\t{line}");
            return string.Join(Environment.NewLine, indented);
        }
    }
}
