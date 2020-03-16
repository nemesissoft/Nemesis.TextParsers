using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class SimpleTransformerCreator : ICanCreateTransformer
    {
        private readonly IReadOnlyDictionary<Type, object> _simpleTransformers;

        public SimpleTransformerCreator() : this(GetDefaultTransformers()) { }

        public SimpleTransformerCreator([NotNull] IReadOnlyDictionary<Type, object> simpleTransformers) =>
            _simpleTransformers = simpleTransformers ?? throw new ArgumentNullException(nameof(simpleTransformers));

        private static IReadOnlyDictionary<Type, object> GetDefaultTransformers()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var simpleTransformers = new Dictionary<Type, object>(30);

            foreach (var type in types)
                if (typeof(ICanTransformType).IsAssignableFrom(type) &&
                    type.DerivesOrImplementsGeneric(typeof(ITransformer<>)))
                {
                    var instance = (ICanTransformType)Activator.CreateInstance(type);

                    var elementType = instance.Type;
                    if (simpleTransformers.ContainsKey(elementType))
                        throw new NotSupportedException($"Automatic registration does not support multiple simple transformers to handle type {elementType}");

                    simpleTransformers[elementType] = instance;
                }

            return simpleTransformers;
        }

        public ITransformer<TSimpleType> CreateTransformer<TSimpleType>() =>
            _simpleTransformers.TryGetValue(typeof(TSimpleType), out var transformer)
                ? (ITransformer<TSimpleType>)transformer
                : throw new InvalidOperationException(
                    $"Internal state of {nameof(SimpleTransformerCreator)} was compromised");

        public bool CanHandle(Type type) => _simpleTransformers.ContainsKey(type);

        public sbyte Priority => 10;
    }

    internal static class Culture
    {
        internal static CultureInfo InvCult => CultureInfo.InvariantCulture;
        internal static NumberFormatInfo InvInfo = NumberFormatInfo.InvariantInfo;
    }

    public abstract class SimpleTransformer<TElement> : TransformerBase<TElement>, ICanTransformType
    {
        //public bool CanHandle(Type type) => typeof(TElement) == type;
        public Type Type => typeof(TElement);

        public override string ToString() => $"Transform {Type.Name}";
    }

    [UsedImplicitly]
    public sealed class StringParser : SimpleTransformer<string>
    {
        public override string Parse(in ReadOnlySpan<char> input) => input.ToString();

        public override string Format(string element) => element;
    }

    #region Structs

    [UsedImplicitly]
    public sealed class BooleanParser : SimpleTransformer<bool>
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (span.Length != value.Length)
                return false;
            if (value.Length == 0)  // span.Length == value.Length == 0
                return true;
            for (int i = span.Length - 1; i >= 0; i--)
                if (char.ToUpperInvariant(span[i]) != char.ToUpperInvariant(value[i]))
                    return false;

            return true;
        }

        internal const string TRUE_LITERAL = "True";
        internal const string FALSE_LITERAL = "False";
        public static bool TryParseBool(ReadOnlySpan<char> value, out bool result)
        {
            ReadOnlySpan<char> trueSpan = TRUE_LITERAL.AsSpan();
            if (EqualsOrdinalIgnoreCase(trueSpan, value))
            {
                result = true;
                return true;
            }

            ReadOnlySpan<char> falseSpan = FALSE_LITERAL.AsSpan();
            if (EqualsOrdinalIgnoreCase(falseSpan, value))
            {
                result = false;
                return true;
            }

            // Special case: Trim whitespace as well as null characters.
            value = TrimWhiteSpaceAndNull(value);

            if (EqualsOrdinalIgnoreCase(trueSpan, value))
            {
                result = true;
                return true;
            }

            if (EqualsOrdinalIgnoreCase(falseSpan, value))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        public static bool ParseBool(ReadOnlySpan<char> value) =>
            TryParseBool(value, out bool result) ? result : throw new FormatException($"Boolean supports only case insensitive '{TRUE_LITERAL}' or '{FALSE_LITERAL}'");

        private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
        {
            const char NULL_CHAR = (char)0x0000;

            int start = 0;
            while (start < value.Length)
            {
                if (!char.IsWhiteSpace(value[start]) && value[start] != NULL_CHAR)
                    break;
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!char.IsWhiteSpace(value[end]) && value[end] != NULL_CHAR)
                    break;
                end--;
            }

            return value.Slice(start, end - start + 1);
        }
#endif

        public override bool Parse(in ReadOnlySpan<char> input)
        {
            try
            {
                return
#if NETSTANDARD2_0 || NETFRAMEWORK || NETFRAMEWORK
                ParseBool(input.Trim());
#else
                bool.Parse(input.Trim());
#endif
            }
            catch (FormatException e)
            {
                throw new FormatException($"{nameof(Boolean)} type does not recognize sequence {input.ToString()}", e);
            }
        }

        public override string Format(bool element) => element ? "True" : "False";
    }

    [UsedImplicitly]
    public sealed class CharParser : SimpleTransformer<char>
    {
        public override char Parse(in ReadOnlySpan<char> input) => input.Length switch
        {
            0 => '\0',
            1 => input[0],
            _ => throw new FormatException($"\"{input.ToString()}\" is not a valid value for char")
        };

        public override string Format(char element) => element == '\0' ? "" : char.ToString(element);
    }

    public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement> where TElement : IFormattable
    {
        public sealed override string Format(TElement element) =>
            element.ToString(FormatString, Culture.InvCult);

        protected virtual string FormatString { get; } = null;
    }

    [UsedImplicitly]
    public sealed class ByteParser : SimpleFormattableTransformer<byte>
    {
        public override byte Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.ByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            byte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class SByteParser : SimpleFormattableTransformer<sbyte>
    {
        public override sbyte Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.SByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            sbyte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int16Parser : SimpleFormattableTransformer<short>
    {
        public override short Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Int16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            short.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    }

    [UsedImplicitly]
    public sealed class UInt16Parser : SimpleFormattableTransformer<ushort>
    {
        public override ushort Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.UInt16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            ushort.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int32Parser : SimpleFormattableTransformer<int>
    {
        public override int Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            int.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt32Parser : SimpleFormattableTransformer<uint>
    {
        public override uint Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseUInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            uint.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int64Parser : SimpleFormattableTransformer<long>
    {
        public override long Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            long.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt64Parser : SimpleFormattableTransformer<ulong>
    {
        public override ulong Parse(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseUInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            ulong.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class SingleParser : SimpleFormattableTransformer<float>
    {
        public override float Parse(in ReadOnlySpan<char> input) => ParseSingle(input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ParseSingle(in ReadOnlySpan<char> input) =>
            input.Length switch
            {
                1 when input[0] == '∞' => float.PositiveInfinity,
                2 when input[0] == '-' && input[1] == '∞' => float.NegativeInfinity,
                _ => float.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                    input.ToString()
#else
                    input
#endif
                    , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
            };

        protected override string FormatString { get; } = "R";
    }

    [UsedImplicitly]
    public sealed class DoubleParser : SimpleFormattableTransformer<double>
    {
        public override double Parse(in ReadOnlySpan<char> input) => ParseDouble(input);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ParseDouble(in ReadOnlySpan<char> input) =>
            input.Length switch
            {
                1 when input[0] == '∞' => double.PositiveInfinity,
                2 when input[0] == '-' && input[1] == '∞' => double.NegativeInfinity,
                _ => double.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                    input.ToString()
#else
                    input
#endif
                    , NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
            };

        protected override string FormatString { get; } = "R";
    }

    [UsedImplicitly]
    public sealed class DecimalParser : SimpleFormattableTransformer<decimal>
    {
        public override decimal Parse(in ReadOnlySpan<char> input) =>
            decimal.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , NumberStyles.Number, Culture.InvCult);
    }

    [UsedImplicitly]
    public sealed class TimeSpanParser : SimpleFormattableTransformer<TimeSpan>
    {
        public override TimeSpan Parse(in ReadOnlySpan<char> input) =>
            TimeSpan.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult);
    }

    [UsedImplicitly]
    public sealed class DateTimeParser : SimpleFormattableTransformer<DateTime>
    {
        public override DateTime Parse(in ReadOnlySpan<char> input) =>
            DateTime.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";
    }

    [UsedImplicitly]
    public sealed class DateTimeOffsetParser : SimpleFormattableTransformer<DateTimeOffset>
    {
        public override DateTimeOffset Parse(in ReadOnlySpan<char> input) =>
            DateTimeOffset.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";
    }

    [UsedImplicitly]
    public sealed class GuidParser : SimpleFormattableTransformer<Guid>
    {
        public override Guid Parse(in ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            );

        protected override string FormatString { get; } = "D";
    }

    [UsedImplicitly]
    public sealed class BigIntegerParser : SimpleFormattableTransformer<BigInteger>
    {
        public override BigInteger Parse(in ReadOnlySpan<char> input) => BigInteger.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            , NumberStyles.Integer, Culture.InvCult);

        protected override string FormatString { get; } = "R";
    }

    [UsedImplicitly]
    public sealed class ComplexParser : SimpleTransformer<Complex>
    {
        private const char DELIMITER = ';';
        private const char ESCAPING_SEQUENCE_START = '\\';
        private const char START = '(';
        private const char END = ')';


        public override Complex Parse(in ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) throw new FormatException("Empty text is not valid complex number representation");

            var tokens = UnParenthesize(input).Tokenize(DELIMITER, ESCAPING_SEQUENCE_START, true);

            var enumerator = tokens.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new FormatException("Real part of complex number was not found");
            var real = DoubleParser.ParseDouble(enumerator.Current);

            if (!enumerator.MoveNext())
                throw new FormatException("Imaginary part of complex number was not found");
            var imaginary = DoubleParser.ParseDouble(enumerator.Current);


            if (enumerator.MoveNext())
                throw new FormatException($"Complex number representation consists of 2 parts separated by delimiter: {START}Real{DELIMITER} Imaginary{END}");

            return new Complex(real, imaginary);
        }

        public override string Format(Complex c) =>
            FormattableString.Invariant($"{START}{c.Real:R}{DELIMITER} {c.Imaginary:R}{END}");


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> UnParenthesize(ReadOnlySpan<char> span)
        {
            int length = span.Length;
            if (length < 2) throw GetStateException();

            int start = 0;
            for (; start < length; start++)
                if (!char.IsWhiteSpace(span[start]))
                    break;

            bool startsWithParenthesis = start < span.Length && span[start] == START;

            if (!startsWithParenthesis) throw GetStateException();

            int end = span.Length - 1;
            for (; end > start; end--)
                if (!char.IsWhiteSpace(span[end]))
                    break;

            bool endsWithParenthesis = end > 0 && span[end] == END;

            if (!endsWithParenthesis) throw GetStateException();

            return span.Slice(start + 1, end - start - 1);

            static Exception GetStateException() => new FormatException(
                "Complex number representation has to start and end with parentheses optionally lead in the beginning or trailed in the end by whitespace");
        }
    }

    #endregion
}
