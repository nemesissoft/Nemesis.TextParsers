using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class SimpleTransformerCreator : ICanCreateTransformer
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
                    simpleTransformers[instance.Type] = instance;
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


    public abstract class SimpleTransformer<TElement> : TransformerBase<TElement>, ICanTransformType
    {
        //public bool CanHandle(Type type) => typeof(TElement) == type;
        public Type Type => typeof(TElement);

        protected static CultureInfo InvCult => CultureInfo.InvariantCulture;

        public override string ToString() => $"Transform {Type.Name}";
    }

    [UsedImplicitly]
    public sealed class StringParser : SimpleTransformer<string>
    {
        public override string Parse(ReadOnlySpan<char> input) => input.ToString();

        public override string Format(string element) => element;
    }

    #region Structs

    [UsedImplicitly]
    public sealed class BooleanParser : SimpleTransformer<bool>
    {
#if NETSTANDARD2_0
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

        internal const string TrueLiteral = "True";
        internal const string FalseLiteral = "False";
        public static bool TryParseBool(ReadOnlySpan<char> value, out bool result)
        {
            ReadOnlySpan<char> trueSpan = TrueLiteral.AsSpan();
            if (EqualsOrdinalIgnoreCase(trueSpan, value))
            {
                result = true;
                return true;
            }

            ReadOnlySpan<char> falseSpan = FalseLiteral.AsSpan();
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
            TryParseBool(value, out bool result) ? result : throw new FormatException($"Boolean supports only case insensitive '{TrueLiteral}' or '{FalseLiteral}'");

        private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
        {
            const char NULL_CHAR = (char)0x0000;

            int start = 0;
            while (start < value.Length)
            {
                if (!Char.IsWhiteSpace(value[start]) && value[start] != NULL_CHAR)
                {
                    break;
                }
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!Char.IsWhiteSpace(value[end]) && value[end] != NULL_CHAR)
                {
                    break;
                }
                end--;
            }

            return value.Slice(start, end - start + 1);
        }
#endif

        public override bool Parse(ReadOnlySpan<char> input)
        {
            input = input.Trim();
            try
            {
                return
#if NETSTANDARD2_0
                ParseBool(input);
#else
                bool.Parse(input);
#endif
            }
            catch (FormatException e)
            {
                throw new FormatException($"{nameof(Boolean)} type does not recognize sequence {input.ToString()}", e);
            }
        }

        public override string Format(bool element) => element ? "True" : "False";
    }

    public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement> where TElement : IFormattable
    {
        public sealed override string Format(TElement element) =>
            element.ToString(FormatString, InvCult);

        protected virtual string FormatString { get; } = null;
    }

    [UsedImplicitly]
    public sealed class ByteParser : SimpleFormattableTransformer<byte>
    {
        public override byte Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.ByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            byte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class SByteParser : SimpleFormattableTransformer<sbyte>
    {
        public override sbyte Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.SByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            sbyte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int16Parser : SimpleFormattableTransformer<short>
    {
        public override short Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.Int16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            short.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    }

    [UsedImplicitly]
    public sealed class UInt16Parser : SimpleFormattableTransformer<ushort>
    {
        public override ushort Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.UInt16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
            ushort.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int32Parser : SimpleFormattableTransformer<int>
    {
        public override int Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.Number.ParseInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            int.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt32Parser : SimpleFormattableTransformer<uint>
    {
        public override uint Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.Number.ParseUInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
            uint.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class Int64Parser : SimpleFormattableTransformer<long>
    {
        public override long Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.Number.ParseInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            long.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt64Parser : SimpleFormattableTransformer<ulong>
    {
        public override ulong Parse(ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0
            Legacy.Number.ParseUInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
            ulong.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif
    }

    [UsedImplicitly]
    public sealed class SingleParser : SimpleFormattableTransformer<float>
    {
        public override float Parse(ReadOnlySpan<char> input)
        {
            if (input.Length == 1 && input[0] == '∞') return float.PositiveInfinity;
            else if (input.Length == 2 && input[0] == '-' && input[1] == '∞') return float.NegativeInfinity;
            else
                return float.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, InvCult);
        }

        protected override string FormatString { get; } = "G9";
    }

    [UsedImplicitly]
    public sealed class DoubleParser : SimpleFormattableTransformer<double>
    {
        public override double Parse(ReadOnlySpan<char> input)
        {
            if (input.Length == 1 && input[0] == '∞') return double.PositiveInfinity;
            else if (input.Length == 2 && input[0] == '-' && input[1] == '∞') return double.NegativeInfinity;
            else
                return double.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, InvCult);
        }

        protected override string FormatString { get; } = "G17";
    }

    [UsedImplicitly]
    public sealed class DecimalParser : SimpleFormattableTransformer<decimal>
    {
        public override decimal Parse(ReadOnlySpan<char> input) =>
            decimal.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Number, InvCult);
    }

    [UsedImplicitly]
    public sealed class TimeSpanParser : SimpleFormattableTransformer<TimeSpan>
    {
        public override TimeSpan Parse(ReadOnlySpan<char> input) =>
            TimeSpan.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , InvCult);
    }

    [UsedImplicitly]
    public sealed class DateTimeParser : SimpleFormattableTransformer<DateTime>
    {
        public override DateTime Parse(ReadOnlySpan<char> input) =>
            DateTime.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";
    }

    [UsedImplicitly]
    public sealed class DateTimeOffsetParser : SimpleFormattableTransformer<DateTimeOffset>
    {
        public override DateTimeOffset Parse(ReadOnlySpan<char> input) =>
            DateTimeOffset.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , InvCult, DateTimeStyles.RoundtripKind);

        protected override string FormatString { get; } = "o";
    }

    [UsedImplicitly]
    public sealed class GuidParser : SimpleFormattableTransformer<Guid>
    {
        public override Guid Parse(ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0
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
        public override BigInteger Parse(ReadOnlySpan<char> input) => BigInteger.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
            input
#endif
            , NumberStyles.Integer, InvCult);

        protected override string FormatString { get; } = "R";
    }

    #endregion
}
