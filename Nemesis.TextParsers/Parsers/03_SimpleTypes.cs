using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class SimpleTransformerCreator : ICanCreateTransformer
    {
        private readonly IReadOnlyDictionary<Type, ITransformer> _simpleTransformers;

        public SimpleTransformerCreator() => _simpleTransformers = GetDefaultTransformers();

        private static IReadOnlyDictionary<Type, ITransformer> GetDefaultTransformers(Assembly fromAssembly = null)
        {
            const BindingFlags PUB_STAT_FLAGS = BindingFlags.Public | BindingFlags.Static;

            var types = (fromAssembly ?? Assembly.GetExecutingAssembly())
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

            var simpleTransformers = new Dictionary<Type, ITransformer>(30);

            foreach (var type in types)
                if (type.DerivesOrImplementsGeneric(typeof(ITransformer<>)) &&
                    TypeMeta.TryGetGenericRealization(type, typeof(SimpleTransformer<>), out var simpleType)
                )
                {
                    var elementType = simpleType.GenericTypeArguments[0];
                    var transformerElementType = typeof(ITransformer<>).MakeGenericType(elementType);


                    object instance;
                    if (type.GetProperty("Instance", PUB_STAT_FLAGS) is { } singletonProperty && singletonProperty.GetMethod != null &&
                        transformerElementType.IsAssignableFrom(singletonProperty.PropertyType)
                    )
                    {
                        instance = singletonProperty.GetValue(null);
                    }
                    else if (type.GetField("Instance", PUB_STAT_FLAGS) is { } singletonField &&
                             transformerElementType.IsAssignableFrom(singletonField.FieldType)
                    )
                    {
                        instance = singletonField.GetValue(null);
                    }
                    else
                        instance = Activator.CreateInstance(type, false);



                    if (simpleTransformers.ContainsKey(elementType))
                        throw new NotSupportedException($"Automatic registration does not support multiple simple transformers to handle type {elementType}");

                    simpleTransformers[elementType] = (ITransformer)instance;
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

    public abstract class SimpleTransformer<TElement> : TransformerBase<TElement>
    {
        public sealed override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()}";
    }

    [UsedImplicitly]
    public sealed class StringParser : SimpleTransformer<string>
    {
        protected override string ParseCore(in ReadOnlySpan<char> input) => input.ToString();

        public override string Format(string element) => element;

        public override string GetEmpty() => "";

        public static readonly ITransformer<string> Instance = new StringParser();

        private StringParser() { }
    }

    #region Structs

    //TODO rename parser -> Transformer 
    [UsedImplicitly]
    public sealed class BooleanParser : SimpleTransformer<bool>
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
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

        protected override bool ParseCore(in ReadOnlySpan<char> input)
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
                var seq = input.ToString();
                throw new FormatException($"{nameof(Boolean)} type does not recognize sequence {seq}", e);
            }
        }

        public override string Format(bool element) => element ? "True" : "False";

        public static readonly ITransformer<bool> Instance = new BooleanParser();

        private BooleanParser() { }
    }

    [UsedImplicitly]
    public sealed class CharParser : SimpleTransformer<char>
    {
        protected override char ParseCore(in ReadOnlySpan<char> input) => input.Length switch
        {
            0 => '\0',
            1 => input[0],
            _ => throw new FormatException($"\"{input.ToString()}\" is not a valid value for char")
        };

        public override string Format(char element) => element == '\0' ? "" : char.ToString(element);


        public static readonly ITransformer<char> Instance = new CharParser();

        private CharParser() { }
    }

    public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement>
        where TElement : struct, IFormattable
    {
        public sealed override string Format(TElement element) =>
            element.ToString(FormatString, Culture.InvCult);

        protected virtual string FormatString { get; } = null;
    }

    [UsedImplicitly]
    public sealed class ByteParser : SimpleFormattableTransformer<byte>
    {
        protected override byte ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.ByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            byte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<byte> Instance = new ByteParser();

        private ByteParser() { }
    }

    [UsedImplicitly]
    public sealed class SByteParser : SimpleFormattableTransformer<sbyte>
    {
        protected override sbyte ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.SByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            sbyte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<sbyte> Instance = new SByteParser();

        private SByteParser() { }
    }

    [UsedImplicitly]
    public sealed class Int16Parser : SimpleFormattableTransformer<short>
    {
        protected override short ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Int16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            short.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<short> Instance = new Int16Parser();

        private Int16Parser() { }
    }

    [UsedImplicitly]
    public sealed class UInt16Parser : SimpleFormattableTransformer<ushort>
    {
        protected override ushort ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.UInt16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            ushort.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<ushort> Instance = new UInt16Parser();

        private UInt16Parser() { }
    }

    [UsedImplicitly]
    public sealed class Int32Parser : SimpleFormattableTransformer<int>
    {
        protected override int ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            int.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<int> Instance = new Int32Parser();
        private Int32Parser() { }
    }

    [UsedImplicitly]
    public sealed class UInt32Parser : SimpleFormattableTransformer<uint>
    {
        protected override uint ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseUInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            uint.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<uint> Instance = new UInt32Parser();

        private UInt32Parser() { }
    }

    [UsedImplicitly]
    public sealed class Int64Parser : SimpleFormattableTransformer<long>
    {
        protected override long ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            long.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<long> Instance = new Int64Parser();

        private Int64Parser() { }
    }

    [UsedImplicitly]
    public sealed class UInt64Parser : SimpleFormattableTransformer<ulong>
    {
        protected override ulong ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
            Legacy.Number.ParseUInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            ulong.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

        public static readonly ITransformer<ulong> Instance = new UInt64Parser();

        private UInt64Parser() { }
    }

    [UsedImplicitly]
    public sealed class SingleParser : SimpleFormattableTransformer<float>
    {
        protected override float ParseCore(in ReadOnlySpan<char> input) =>
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

        public static readonly ITransformer<float> Instance = new SingleParser();

        private SingleParser() { }
    }

    [UsedImplicitly]
    public sealed class DoubleParser : SimpleFormattableTransformer<double>
    {
        protected override double ParseCore(in ReadOnlySpan<char> input) =>
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

        public static readonly ITransformer<double> Instance = new DoubleParser();

        private DoubleParser() { }
    }

    [UsedImplicitly]
    public sealed class DecimalParser : SimpleFormattableTransformer<decimal>
    {
        protected override decimal ParseCore(in ReadOnlySpan<char> input) =>
            decimal.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , NumberStyles.Number, Culture.InvCult);

        public static readonly ITransformer<decimal> Instance = new DecimalParser();

        private DecimalParser() { }
    }

    [UsedImplicitly]
    public sealed class TimeSpanParser : SimpleFormattableTransformer<TimeSpan>
    {
        protected override TimeSpan ParseCore(in ReadOnlySpan<char> input) =>
            TimeSpan.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult);


        public static readonly ITransformer<TimeSpan> Instance = new TimeSpanParser();

        private TimeSpanParser() { }
    }

    [UsedImplicitly]
    public sealed class DateTimeParser : SimpleFormattableTransformer<DateTime>
    {
        protected override DateTime ParseCore(in ReadOnlySpan<char> input) =>
            DateTime.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";

        public static readonly ITransformer<DateTime> Instance = new DateTimeParser();

        private DateTimeParser() { }
    }

    [UsedImplicitly]
    public sealed class DateTimeOffsetParser : SimpleFormattableTransformer<DateTimeOffset>
    {
        protected override DateTimeOffset ParseCore(in ReadOnlySpan<char> input) =>
            DateTimeOffset.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , Culture.InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";

        public static readonly ITransformer<DateTimeOffset> Instance = new DateTimeOffsetParser();

        private DateTimeOffsetParser() { }
    }

    [UsedImplicitly]
    public sealed class GuidParser : SimpleFormattableTransformer<Guid>
    {
        protected override Guid ParseCore(in ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            );

        protected override string FormatString { get; } = "D";


        public static readonly ITransformer<Guid> Instance = new GuidParser();

        private GuidParser() { }
    }

    [UsedImplicitly]
    public sealed class BigIntegerParser : SimpleFormattableTransformer<BigInteger>
    {
        protected override BigInteger ParseCore(in ReadOnlySpan<char> input) => BigInteger.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
                input.ToString()
#else
            input
#endif
            , NumberStyles.Integer, Culture.InvCult);

        protected override string FormatString { get; } = "R";


        public static readonly ITransformer<BigInteger> Instance = new BigIntegerParser();

        private BigIntegerParser() { }
    }

    [UsedImplicitly]
    public sealed class ComplexParser : SimpleTransformer<Complex>
    {
        private const string TYPE_NAME = "Complex number";

        private static readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');

        protected override Complex ParseCore(in ReadOnlySpan<char> input)
        {
            var doubleParser = DoubleParser.Instance;

            var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

            double real = _helper.ParseElement(ref enumerator, doubleParser);

            _helper.ParseNext(ref enumerator, 2, TYPE_NAME);
            double imaginary = _helper.ParseElement(ref enumerator, doubleParser);


            _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

            return new Complex(real, imaginary);
        }

        public override string Format(Complex c)
        {
            var doubleParser = DoubleParser.Instance;
            Span<char> initialBuffer = stackalloc char[16];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                _helper.StartFormat(ref accumulator);

                _helper.FormatElement(doubleParser, c.Real, ref accumulator);
                _helper.AddDelimiter(ref accumulator);
                accumulator.Append(' ');//this is pure cosmetics 

                _helper.FormatElement(doubleParser, c.Imaginary, ref accumulator);

                _helper.EndFormat(ref accumulator);

                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }

        public static readonly ITransformer<Complex> Instance = new ComplexParser();

        private ComplexParser() { }
    }

    #endregion
}
