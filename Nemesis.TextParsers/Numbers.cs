using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    public static class NumberHandlerCache
    {
        private static readonly IDictionary<Type, object> _cache = BuildCache();

        private static IDictionary<Type, object> BuildCache()
        {
            var numberParsersTypes = typeof(NumberHandlerCache).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                       t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INumber<>))
                );

            return numberParsersTypes.ToDictionary(
                t => t.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INumber<>)).GenericTypeArguments[0],
                Activator.CreateInstance
                );
        }

        public static INumber<TNumber> GetNumberHandler<TNumber>() where TNumber : struct, IComparable, IComparable<TNumber>, IConvertible, IEquatable<TNumber>, IFormattable
            => _cache.TryGetValue(typeof(TNumber), out var numOp) ? (INumber<TNumber>)numOp : null;

        public static object GetNumberHandler(Type numberType) =>
            _cache.TryGetValue(numberType, out var numOp) ? numOp : null;
    }

    public interface INumber<TNumber> where TNumber : struct, IComparable, IComparable<TNumber>, IConvertible, IEquatable<TNumber>, IFormattable
    {
        bool SupportsNegative { get; }
        TNumber Zero { get; }
        TNumber One { get; }
        TNumber MinValue { get; }
        TNumber MaxValue { get; }

        TNumber FromInt64(long value);
        long ToInt64(TNumber value);

        TNumber Or(TNumber left, TNumber right);
        TNumber And(TNumber left, TNumber right);
        TNumber Not(TNumber value);

        TNumber ShR(TNumber left, byte right);
        TNumber ShL(TNumber left, byte right);

        TNumber Add(TNumber left, TNumber right);
        TNumber Sub(TNumber left, TNumber right);
        TNumber Mul(TNumber left, TNumber right);
        TNumber Div(TNumber left, TNumber right);

        bool TryParse(in ReadOnlySpan<char> input, out TNumber value);
    }

    internal static class Culture
    {
        internal static CultureInfo InvCult => CultureInfo.InvariantCulture;
        internal static NumberFormatInfo InvInfo = NumberFormatInfo.InvariantInfo;
    }

    [UsedImplicitly]
    public sealed class ByteNumber : INumber<byte>
    {
        public bool SupportsNegative => false;
        public byte Zero => 0;
        public byte One => 1;
        public byte MinValue => byte.MinValue;
        public byte MaxValue => byte.MaxValue;

        public byte FromInt64(long value) => (byte)value;
        public long ToInt64(byte value) => value;

        public byte Or(byte left, byte right) => (byte)(left | right);
        public byte And(byte left, byte right) => (byte)(left & right);
        public byte Not(byte value) => (byte)~value;

        public byte ShR(byte left, byte right) => (byte)(left >> right);
        public byte ShL(byte left, byte right) => (byte)(left << right);

        public byte Add(byte left, byte right) => (byte)(left + right);
        public byte Sub(byte left, byte right) => (byte)(left - right);
        public byte Mul(byte left, byte right) => (byte)(left * right);
        public byte Div(byte left, byte right) => (byte)(left / right);


        public bool TryParse(in ReadOnlySpan<char> input, out byte value) =>
#if NETSTANDARD2_0
            Legacy.ByteParser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
            byte.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif

    }

    [UsedImplicitly]
    public sealed class SByteNumber : INumber<sbyte>
    {
        public bool SupportsNegative => true;
        public sbyte Zero => 0;
        public sbyte One => 1;
        public sbyte MinValue => sbyte.MinValue;
        public sbyte MaxValue => sbyte.MaxValue;

        public sbyte FromInt64(long value) => (sbyte)value;
        public long ToInt64(sbyte value) => value;

        public sbyte Or(sbyte left, sbyte right) => (sbyte)(left | right);
        public sbyte And(sbyte left, sbyte right) => (sbyte)(left & right);
        public sbyte Not(sbyte value) => (sbyte)~value;

        public sbyte ShR(sbyte left, byte right) => (sbyte)(left >> right);
        public sbyte ShL(sbyte left, byte right) => (sbyte)(left << right);

        public sbyte Add(sbyte left, sbyte right) => (sbyte)(left + right);
        public sbyte Sub(sbyte left, sbyte right) => (sbyte)(left - right);
        public sbyte Mul(sbyte left, sbyte right) => (sbyte)(left * right);
        public sbyte Div(sbyte left, sbyte right) => (sbyte)(left / right);

        public bool TryParse(in ReadOnlySpan<char> input, out sbyte value) =>
#if NETSTANDARD2_0
            Legacy.SByteParser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
            sbyte.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class Int16Number : INumber<short>
    {
        public bool SupportsNegative => true;
        public short Zero => 0;
        public short One => 1;
        public short MinValue => short.MinValue;
        public short MaxValue => short.MaxValue;

        public short FromInt64(long value) => (short)value;
        public long ToInt64(short value) => value;

        public short Or(short left, short right) => (short)(left | right);
        public short And(short left, short right) => (short)(left & right);
        public short Not(short value) => (short)~value;

        public short ShR(short left, byte right) => (short)(left >> right);
        public short ShL(short left, byte right) => (short)(left << right);

        public short Add(short left, short right) => (short)(left + right);
        public short Sub(short left, short right) => (short)(left - right);
        public short Mul(short left, short right) => (short)(left * right);
        public short Div(short left, short right) => (short)(left / right);

        public bool TryParse(in ReadOnlySpan<char> input, out short value) =>
#if NETSTANDARD2_0
            Legacy.Int16Parser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
            short.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt16Number : INumber<ushort>
    {
        public bool SupportsNegative => false;
        public ushort Zero => 0;
        public ushort One => 1;
        public ushort MinValue => ushort.MinValue;
        public ushort MaxValue => ushort.MaxValue;

        public ushort FromInt64(long value) => (ushort)value;
        public long ToInt64(ushort value) => value;

        public ushort Or(ushort left, ushort right) => (ushort)(left | right);
        public ushort And(ushort left, ushort right) => (ushort)(left & right);
        public ushort Not(ushort value) => (ushort)~value;

        public ushort ShR(ushort left, byte right) => (ushort)(left >> right);
        public ushort ShL(ushort left, byte right) => (ushort)(left << right);

        public ushort Add(ushort left, ushort right) => (ushort)(left + right);
        public ushort Sub(ushort left, ushort right) => (ushort)(left - right);
        public ushort Mul(ushort left, ushort right) => (ushort)(left * right);
        public ushort Div(ushort left, ushort right) => (ushort)(left / right);

        public bool TryParse(in ReadOnlySpan<char> input, out ushort value) =>
#if NETSTANDARD2_0
            Legacy.UInt16Parser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
            ushort.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class Int32Number : INumber<int>
    {
        public bool SupportsNegative => true;
        public int Zero => 0;
        public int One => 1;
        public int MinValue => int.MinValue;
        public int MaxValue => int.MaxValue;

        public int FromInt64(long value) => (int)value;
        public long ToInt64(int value) => value;

        public int Or(int left, int right) => left | right;
        public int And(int left, int right) => left & right;
        public int Not(int value) => ~value;

        public int ShR(int left, byte right) => left >> right;
        public int ShL(int left, byte right) => left << right;

        public int Add(int left, int right) => left + right;
        public int Sub(int left, int right) => left - right;
        public int Mul(int left, int right) => left * right;
        public int Div(int left, int right) => left / right;


        public bool TryParse(in ReadOnlySpan<char> input, out int value) =>
#if NETSTANDARD2_0
            Legacy.Number.TryParseInt32(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
            int.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt32Number : INumber<uint>
    {
        public bool SupportsNegative => false;
        public uint Zero => 0;
        public uint One => 1;
        public uint MinValue => uint.MinValue;
        public uint MaxValue => uint.MaxValue;

        public uint FromInt64(long value) => (uint)value;
        public long ToInt64(uint value) => value;

        public uint Or(uint left, uint right) => left | right;
        public uint And(uint left, uint right) => left & right;
        public uint Not(uint value) => ~value;

        public uint ShR(uint left, byte right) => left >> right;
        public uint ShL(uint left, byte right) => left << right;

        public uint Add(uint left, uint right) => left + right;
        public uint Sub(uint left, uint right) => left - right;
        public uint Mul(uint left, uint right) => left * right;
        public uint Div(uint left, uint right) => left / right;

        public bool TryParse(in ReadOnlySpan<char> input, out uint value) =>
#if NETSTANDARD2_0
            Legacy.Number.TryParseUInt32(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
            uint.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class Int64Number : INumber<long>
    {
        public bool SupportsNegative => true;
        public long Zero => 0;
        public long One => 1;
        public long MinValue => long.MinValue;
        public long MaxValue => long.MaxValue;

        public long FromInt64(long value) => value;
        public long ToInt64(long value) => value;

        public long Or(long left, long right) => left | right;
        public long And(long left, long right) => left & right;
        public long Not(long value) => ~value;

        public long ShR(long left, byte right) => left >> right;
        public long ShL(long left, byte right) => left << right;

        public long Add(long left, long right) => left + right;
        public long Sub(long left, long right) => left - right;
        public long Mul(long left, long right) => left * right;
        public long Div(long left, long right) => left / right;

        public bool TryParse(in ReadOnlySpan<char> input, out long value) =>
#if NETSTANDARD2_0
            Legacy.Number.TryParseInt64(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
            long.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }

    [UsedImplicitly]
    public sealed class UInt64Number : INumber<ulong>
    {
        public bool SupportsNegative => false;
        public ulong Zero => 0;
        public ulong One => 1;
        public ulong MinValue => ulong.MinValue;
        public ulong MaxValue => ulong.MaxValue;

        public ulong FromInt64(long value) => (ulong)value;
        public long ToInt64(ulong value) => (long)value;

        public ulong Or(ulong left, ulong right) => left | right;
        public ulong And(ulong left, ulong right) => left & right;
        public ulong Not(ulong value) => ~value;

        public ulong ShR(ulong left, byte right) => left >> right;
        public ulong ShL(ulong left, byte right) => left << right;

        public ulong Add(ulong left, ulong right) => left + right;
        public ulong Sub(ulong left, ulong right) => left - right;
        public ulong Mul(ulong left, ulong right) => left * right;
        public ulong Div(ulong left, ulong right) => left / right;

        public bool TryParse(in ReadOnlySpan<char> input, out ulong value) =>
#if NETSTANDARD2_0
            Legacy.Number.TryParseUInt64(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
            ulong.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif
    }


    /*[UsedImplicitly]
    public sealed class TNumberNumber : INumber<TNumber>
    {
        public TNumber Zero => (TNumber)0;
        public TNumber One => (TNumber)1;
        public TNumber MinValue => TNumber.MinValue;
        public TNumber MaxValue => TNumber.MaxValue;

        public TNumber Or(TNumber left, TNumber right) => (TNumber)(left | right);
        public TNumber And(TNumber left, TNumber right) => (TNumber)(left & right);
        public TNumber Not(TNumber value) => (TNumber)~value;

        public TNumber ShR(TNumber left, byte right) => (TNumber)(left >> right);
        public TNumber ShL(TNumber left, byte right) => (TNumber)(left << right);

        public TNumber Add(TNumber left, TNumber right) => (TNumber)(left + right);
        public TNumber Sub(TNumber left, TNumber right) => (TNumber)(left - right);
        public TNumber Mul(TNumber left, TNumber right) => (TNumber)(left * right);
        public TNumber Div(TNumber left, TNumber right) => (TNumber)(left / right);

        public bool TryParse(in ReadOnlySpan<char> input, out TNumber value) => TNumber.TryParse(input, out value);
    }*/
}
