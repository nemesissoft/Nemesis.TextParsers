using System;
using System.Globalization;

namespace Nemesis.TextParsers
{
    public abstract class SimpleTransformer<TElement> : ICanTransformType, ITransformer<TElement>
    {
        public bool CanHandle(Type type) => typeof(TElement) == type;
        public Type Type => typeof(TElement);

        public abstract TElement Parse(ReadOnlySpan<char> input);

        public abstract string Format(TElement element);

        protected static CultureInfo InvCult => CultureInfo.InvariantCulture;

        public override string ToString() => $"Transform {Type.Name}";
    }

    public sealed class StringParser : SimpleTransformer<string>
    {
        public override string Parse(ReadOnlySpan<char> input) => input.ToString();

        public override string Format(string element) => element;
    }

    #region Structs

    public sealed class BooleanParser : SimpleTransformer<bool>
    {
        public override bool Parse(ReadOnlySpan<char> input)
        {
            input = input.Trim();
            try
            {
                return bool.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                    );
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

    public sealed class ByteParser : SimpleFormattableTransformer<byte>
    {
        public override byte Parse(ReadOnlySpan<char> input) =>
            byte.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else           
                input
#endif
          , NumberStyles.Integer, InvCult);

    }

    public sealed class SByteParser : SimpleFormattableTransformer<sbyte>
    {
        public override sbyte Parse(ReadOnlySpan<char> input) =>
            sbyte.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class Int16Parser : SimpleFormattableTransformer<short>
    {
        public override short Parse(ReadOnlySpan<char> input) =>
            short.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class UInt16Parser : SimpleFormattableTransformer<ushort>
    {
        public override ushort Parse(ReadOnlySpan<char> input) =>
            ushort.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class Int32Parser : SimpleFormattableTransformer<int>
    {
        public override int Parse(ReadOnlySpan<char> input) =>
            int.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class UInt32Parser : SimpleFormattableTransformer<uint>
    {
        public override uint Parse(ReadOnlySpan<char> input) =>
            uint.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class Int64Parser : SimpleFormattableTransformer<long>
    {
        public override long Parse(ReadOnlySpan<char> input) =>
            long.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class UInt64Parser : SimpleFormattableTransformer<ulong>
    {
        public override ulong Parse(ReadOnlySpan<char> input) =>
            ulong.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Integer, InvCult);
    }

    public sealed class SingleParser : SimpleFormattableTransformer<float>
    {
        public override float Parse(ReadOnlySpan<char> input) =>
            float.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, InvCult);
    }

    public sealed class DoubleParser : SimpleFormattableTransformer<double>
    {
        public override double Parse(ReadOnlySpan<char> input) =>
            double.Parse(
#if NETSTANDARD2_0
                input.ToString()
#else
                input
#endif
                , NumberStyles.Float | NumberStyles.AllowThousands, InvCult);
    }

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

    /*public sealed class GuidParser : SimpleFormattableTransformer<Guid>
    {
        public override Guid Parse(ReadOnlySpan<char> input) => Guid.Parse(input);

        protected override string FormatString { get; } = "D";
    }

    public sealed class BigIntegerParser : SimpleFormattableTransformer<System.Numerics.BigInteger>
    {
        public override System.Numerics.BigInteger Parse(ReadOnlySpan<char> input) => System.Numerics.BigInteger.Parse(input, NumberStyles.Integer, InvCult);        
    }*/

    #endregion
}
