using System.Globalization;
using JetBrains.Annotations;

namespace Nemesis.TextParsers.Parsers;

file static class Culture
{
    internal static CultureInfo InvCult => CultureInfo.InvariantCulture;
    internal static NumberFormatInfo InvInfo => NumberFormatInfo.InvariantInfo;
}

public class NumberTransformerCache : ITestTransformerRegistrations
{
    private readonly Dictionary<Type, ITransformer> _cache = new()
    {
        [typeof(byte)] = ByteTransformer.Instance,
        [typeof(sbyte)] = SByteTransformer.Instance,
        [typeof(short)] = Int16Transformer.Instance,
        [typeof(ushort)] = UInt16Transformer.Instance,
        [typeof(int)] = Int32Transformer.Instance,
        [typeof(uint)] = UInt32Transformer.Instance,
        [typeof(long)] = Int64Transformer.Instance,
        [typeof(ulong)] = UInt64Transformer.Instance,
        [typeof(BigInteger)] = BigIntegerTransformer.Instance,
#if NET7_0_OR_GREATER
        [typeof(Int128)] = Int128Transformer.Instance,
        [typeof(UInt128)] = UInt128Transformer.Instance,
#endif        
    };

    public NumberTransformer<TNumber> GetNumberHandler<TNumber>()
        where TNumber : struct, IComparable, IComparable<TNumber>, IEquatable<TNumber>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TNumber>
#endif
        => _cache.TryGetValue(typeof(TNumber), out var numOp)
        ? (NumberTransformer<TNumber>)numOp
        : throw new NotSupportedException($"Type {typeof(TNumber).FullName} is not supported as element for {nameof(NumberTransformer<int>)}");

    public object GetNumberHandler(Type numberType) =>
        _cache.TryGetValue(numberType, out var numOp)
        ? numOp
        : throw new NotSupportedException($"Type {numberType.FullName} is not supported as element for {nameof(NumberTransformer<int>)}");

    public static readonly NumberTransformerCache Instance = new();
    private NumberTransformerCache() { }

    IReadOnlyDictionary<Type, ITransformer> ITestTransformerRegistrations.GetTransformerRegistrationsForTests() => _cache;
}

public abstract class NumberTransformer<TNumber> : SimpleTransformer<TNumber>
    where TNumber : struct, IComparable, IComparable<TNumber>, IEquatable<TNumber>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TNumber>
#endif
{
    #region Metadata and operations
    public abstract bool SupportsNegative { get; }
    public abstract TNumber Zero { get; }
    public abstract TNumber One { get; }
    public abstract TNumber MinValue { get; }
    public abstract TNumber MaxValue { get; }

    public abstract TNumber FromInt64(long value);
    public abstract long ToInt64(TNumber value);

    public abstract TNumber Or(TNumber left, TNumber right);
    public abstract TNumber And(TNumber left, TNumber right);
    public abstract TNumber Not(TNumber value);

    public abstract TNumber ShR(TNumber left, byte right);
    public abstract TNumber ShL(TNumber left, byte right);

    public abstract TNumber Add(TNumber left, TNumber right);
    public abstract TNumber Sub(TNumber left, TNumber right);
    public abstract TNumber Mul(TNumber left, TNumber right);
    public abstract TNumber Div(TNumber left, TNumber right);
    #endregion

    public override string Format(TNumber element) => element.ToString(null, Culture.InvCult);

    public void Deconstruct(out TNumber zero, out TNumber one, out TNumber min, out TNumber max)
    {
        zero = Zero;
        one = One;
        min = MinValue;
        max = MaxValue;
    }
}

[UsedImplicitly]
public sealed class ByteTransformer : NumberTransformer<byte>
{
    #region NumberTransformer
    public override bool SupportsNegative => false;
    public override byte Zero => 0;
    public override byte One => 1;
    public override byte MinValue => byte.MinValue;
    public override byte MaxValue => byte.MaxValue;

    public override byte FromInt64(long value) => (byte)value;
    public override long ToInt64(byte value) => value;

    public override byte Or(byte left, byte right) => (byte)(left | right);
    public override byte And(byte left, byte right) => (byte)(left & right);
    public override byte Not(byte value) => (byte)~value;

    public override byte ShR(byte left, byte right) => (byte)(left >> right);
    public override byte ShL(byte left, byte right) => (byte)(left << right);

    public override byte Add(byte left, byte right) => (byte)(left + right);
    public override byte Sub(byte left, byte right) => (byte)(left - right);
    public override byte Mul(byte left, byte right) => (byte)(left * right);
    public override byte Div(byte left, byte right) => (byte)(left / right);
    #endregion

    protected override byte ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.ByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
        byte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out byte value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.ByteParser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
        byte.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif

    public static readonly ITransformer<byte> Instance = new ByteTransformer();
    private ByteTransformer() { }
}

[UsedImplicitly]
public sealed class SByteTransformer : NumberTransformer<sbyte>
{
    #region NumberTransformer
    public override bool SupportsNegative => true;
    public override sbyte Zero => 0;
    public override sbyte One => 1;
    public override sbyte MinValue => sbyte.MinValue;
    public override sbyte MaxValue => sbyte.MaxValue;

    public override sbyte FromInt64(long value) => (sbyte)value;
    public override long ToInt64(sbyte value) => value;

    public override sbyte Or(sbyte left, sbyte right) => (sbyte)(left | right);
    public override sbyte And(sbyte left, sbyte right) => (sbyte)(left & right);
    public override sbyte Not(sbyte value) => (sbyte)~value;

    public override sbyte ShR(sbyte left, byte right) => (sbyte)(left >> right);
    public override sbyte ShL(sbyte left, byte right) => (sbyte)(left << right);

    public override sbyte Add(sbyte left, sbyte right) => (sbyte)(left + right);
    public override sbyte Sub(sbyte left, sbyte right) => (sbyte)(left - right);
    public override sbyte Mul(sbyte left, sbyte right) => (sbyte)(left * right);
    public override sbyte Div(sbyte left, sbyte right) => (sbyte)(left / right);
    #endregion

    protected override sbyte ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.SByteParser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
        sbyte.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out sbyte value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.SByteParser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
        sbyte.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif


    public static readonly ITransformer<sbyte> Instance = new SByteTransformer();
    private SByteTransformer() { }
}

[UsedImplicitly]
public sealed class Int16Transformer : NumberTransformer<short>
{
    #region NumberTransformer
    public override bool SupportsNegative => true;
    public override short Zero => 0;
    public override short One => 1;
    public override short MinValue => short.MinValue;
    public override short MaxValue => short.MaxValue;

    public override short FromInt64(long value) => (short)value;
    public override long ToInt64(short value) => value;

    public override short Or(short left, short right) => (short)(left | right);
    public override short And(short left, short right) => (short)(left & right);
    public override short Not(short value) => (short)~value;

    public override short ShR(short left, byte right) => (short)(left >> right);
    public override short ShL(short left, byte right) => (short)(left << right);

    public override short Add(short left, short right) => (short)(left + right);
    public override short Sub(short left, short right) => (short)(left - right);
    public override short Mul(short left, short right) => (short)(left * right);
    public override short Div(short left, short right) => (short)(left / right);
    #endregion

    protected override short ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Int16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
        short.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out short value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Int16Parser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
        short.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif

    public static readonly ITransformer<short> Instance = new Int16Transformer();
    private Int16Transformer() { }
}

[UsedImplicitly]
public sealed class UInt16Transformer : NumberTransformer<ushort>
{
    #region NumberTransformer
    public override bool SupportsNegative => false;
    public override ushort Zero => 0;
    public override ushort One => 1;
    public override ushort MinValue => ushort.MinValue;
    public override ushort MaxValue => ushort.MaxValue;

    public override ushort FromInt64(long value) => (ushort)value;
    public override long ToInt64(ushort value) => value;

    public override ushort Or(ushort left, ushort right) => (ushort)(left | right);
    public override ushort And(ushort left, ushort right) => (ushort)(left & right);
    public override ushort Not(ushort value) => (ushort)~value;

    public override ushort ShR(ushort left, byte right) => (ushort)(left >> right);
    public override ushort ShL(ushort left, byte right) => (ushort)(left << right);

    public override ushort Add(ushort left, ushort right) => (ushort)(left + right);
    public override ushort Sub(ushort left, ushort right) => (ushort)(left - right);
    public override ushort Mul(ushort left, ushort right) => (ushort)(left * right);
    public override ushort Div(ushort left, ushort right) => (ushort)(left / right);
    #endregion


    protected override ushort ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.UInt16Parser.Parse(input, NumberStyles.Integer, Culture.InvCult);
#else
        ushort.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out ushort value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.UInt16Parser.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#else
        ushort.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif

    public static readonly ITransformer<ushort> Instance = new UInt16Transformer();
    private UInt16Transformer() { }
}

[UsedImplicitly]
public sealed class Int32Transformer : NumberTransformer<int>
{
    #region NumberTransformer
    public override bool SupportsNegative => true;
    public override int Zero => 0;
    public override int One => 1;
    public override int MinValue => int.MinValue;
    public override int MaxValue => int.MaxValue;

    public override int FromInt64(long value) => (int)value;
    public override long ToInt64(int value) => value;

    public override int Or(int left, int right) => left | right;
    public override int And(int left, int right) => left & right;
    public override int Not(int value) => ~value;

    public override int ShR(int left, byte right) => left >> right;
    public override int ShL(int left, byte right) => left << right;

    public override int Add(int left, int right) => left + right;
    public override int Sub(int left, int right) => left - right;
    public override int Mul(int left, int right) => left * right;
    public override int Div(int left, int right) => left / right;
    #endregion


    protected override int ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.ParseInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
        int.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif


    public override bool TryParse(in ReadOnlySpan<char> input, out int value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.TryParseInt32(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
        int.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif

    public static readonly ITransformer<int> Instance = new Int32Transformer();
    private Int32Transformer() { }
}

[UsedImplicitly]
public sealed class UInt32Transformer : NumberTransformer<uint>
{
    #region NumberTransformer
    public override bool SupportsNegative => false;
    public override uint Zero => 0;
    public override uint One => 1;
    public override uint MinValue => uint.MinValue;
    public override uint MaxValue => uint.MaxValue;

    public override uint FromInt64(long value) => (uint)value;
    public override long ToInt64(uint value) => value;

    public override uint Or(uint left, uint right) => left | right;
    public override uint And(uint left, uint right) => left & right;
    public override uint Not(uint value) => ~value;

    public override uint ShR(uint left, byte right) => left >> right;
    public override uint ShL(uint left, byte right) => left << right;

    public override uint Add(uint left, uint right) => left + right;
    public override uint Sub(uint left, uint right) => left - right;
    public override uint Mul(uint left, uint right) => left * right;
    public override uint Div(uint left, uint right) => left / right;
    #endregion

    protected override uint ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.ParseUInt32(input, NumberStyles.Integer, Culture.InvInfo);
#else
        uint.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif


    public override bool TryParse(in ReadOnlySpan<char> input, out uint value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.TryParseUInt32(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
        uint.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif


    public static readonly ITransformer<uint> Instance = new UInt32Transformer();
    private UInt32Transformer() { }
}

[UsedImplicitly]
public sealed class Int64Transformer : NumberTransformer<long>
{
    #region NumberTransformer
    public override bool SupportsNegative => true;
    public override long Zero => 0;
    public override long One => 1;
    public override long MinValue => long.MinValue;
    public override long MaxValue => long.MaxValue;

    public override long FromInt64(long value) => value;
    public override long ToInt64(long value) => value;

    public override long Or(long left, long right) => left | right;
    public override long And(long left, long right) => left & right;
    public override long Not(long value) => ~value;

    public override long ShR(long left, byte right) => left >> right;
    public override long ShL(long left, byte right) => left << right;

    public override long Add(long left, long right) => left + right;
    public override long Sub(long left, long right) => left - right;
    public override long Mul(long left, long right) => left * right;
    public override long Div(long left, long right) => left / right;
    #endregion

    protected override long ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.ParseInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
        long.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out long value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.TryParseInt64(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
        long.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif


    public static readonly ITransformer<long> Instance = new Int64Transformer();
    private Int64Transformer() { }
}

[UsedImplicitly]
public sealed class UInt64Transformer : NumberTransformer<ulong>
{
    #region NumberTransformer
    public override bool SupportsNegative => false;
    public override ulong Zero => 0;
    public override ulong One => 1;
    public override ulong MinValue => ulong.MinValue;
    public override ulong MaxValue => ulong.MaxValue;

    public override ulong FromInt64(long value) => (ulong)value;
    public override long ToInt64(ulong value) => (long)value;

    public override ulong Or(ulong left, ulong right) => left | right;
    public override ulong And(ulong left, ulong right) => left & right;
    public override ulong Not(ulong value) => ~value;

    public override ulong ShR(ulong left, byte right) => left >> right;
    public override ulong ShL(ulong left, byte right) => left << right;

    public override ulong Add(ulong left, ulong right) => left + right;
    public override ulong Sub(ulong left, ulong right) => left - right;
    public override ulong Mul(ulong left, ulong right) => left * right;
    public override ulong Div(ulong left, ulong right) => left / right;
    #endregion

    protected override ulong ParseCore(in ReadOnlySpan<char> input) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.ParseUInt64(input, NumberStyles.Integer, Culture.InvInfo);
#else
        ulong.Parse(input, NumberStyles.Integer, Culture.InvCult);
#endif

    public override bool TryParse(in ReadOnlySpan<char> input, out ulong value) =>
#if NETSTANDARD2_0 || NETFRAMEWORK
        Legacy.Number.TryParseUInt64(input, NumberStyles.Integer, Culture.InvInfo, out value);
#else
        ulong.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);
#endif


    public static readonly ITransformer<ulong> Instance = new UInt64Transformer();
    private UInt64Transformer() { }
}

#if NET7_0_OR_GREATER

[UsedImplicitly]
public sealed class Int128Transformer : NumberTransformer<Int128>
{
    #region NumberTransformer
    public override bool SupportsNegative => true;
    public override Int128 Zero => 0;
    public override Int128 One => 1;
    public override Int128 MinValue => Int128.MinValue;
    public override Int128 MaxValue => Int128.MaxValue;

    public override Int128 FromInt64(long value) => value;
    public override long ToInt64(Int128 value) => (long)value;

    public override Int128 Or(Int128 left, Int128 right) => left | right;
    public override Int128 And(Int128 left, Int128 right) => left & right;
    public override Int128 Not(Int128 value) => ~value;

    public override Int128 ShR(Int128 left, byte right) => left >> right;
    public override Int128 ShL(Int128 left, byte right) => left << right;

    public override Int128 Add(Int128 left, Int128 right) => left + right;
    public override Int128 Sub(Int128 left, Int128 right) => left - right;
    public override Int128 Mul(Int128 left, Int128 right) => left * right;
    public override Int128 Div(Int128 left, Int128 right) => left / right;
    #endregion

    protected override Int128 ParseCore(in ReadOnlySpan<char> input) =>
    Int128.Parse(input, NumberStyles.Integer, Culture.InvCult);


    public override bool TryParse(in ReadOnlySpan<char> input, out Int128 value) =>
    Int128.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);


    public static readonly ITransformer<Int128> Instance = new Int128Transformer();
    private Int128Transformer() { }
}

[UsedImplicitly]
public sealed class UInt128Transformer : NumberTransformer<UInt128>
{
    #region NumberTransformer
    public override bool SupportsNegative => false;
    public override UInt128 Zero => 0;
    public override UInt128 One => 1;
    public override UInt128 MinValue => UInt128.MinValue;
    public override UInt128 MaxValue => UInt128.MaxValue;

    public override UInt128 FromInt64(long value) => (UInt128)value;
    public override long ToInt64(UInt128 value) => (long)value;

    public override UInt128 Or(UInt128 left, UInt128 right) => left | right;
    public override UInt128 And(UInt128 left, UInt128 right) => left & right;
    public override UInt128 Not(UInt128 value) => ~value;

    public override UInt128 ShR(UInt128 left, byte right) => left >> right;
    public override UInt128 ShL(UInt128 left, byte right) => left << right;

    public override UInt128 Add(UInt128 left, UInt128 right) => left + right;
    public override UInt128 Sub(UInt128 left, UInt128 right) => left - right;
    public override UInt128 Mul(UInt128 left, UInt128 right) => left * right;
    public override UInt128 Div(UInt128 left, UInt128 right) => left / right;
    #endregion

    protected override UInt128 ParseCore(in ReadOnlySpan<char> input) =>
        UInt128.Parse(input, NumberStyles.Integer, Culture.InvCult);


    public override bool TryParse(in ReadOnlySpan<char> input, out UInt128 value) =>
        UInt128.TryParse(input, NumberStyles.Integer, Culture.InvCult, out value);


    public static readonly ITransformer<UInt128> Instance = new UInt128Transformer();
    private UInt128Transformer() { }
}

#endif


[UsedImplicitly]
public sealed class BigIntegerTransformer : NumberTransformer<BigInteger>
{
    public override bool SupportsNegative => true;

    public override BigInteger Zero => 0;

    public override BigInteger One => 1;

    public override BigInteger MinValue => throw new NotSupportedException();

    public override BigInteger MaxValue => throw new NotSupportedException();

    public override BigInteger FromInt64(long value) => (BigInteger)value;
    public override long ToInt64(BigInteger value) => (long)value;

    public override BigInteger Or(BigInteger left, BigInteger right) => left | right;
    public override BigInteger And(BigInteger left, BigInteger right) => left & right;
    public override BigInteger Not(BigInteger value) => ~value;

    public override BigInteger ShR(BigInteger left, byte right) => left >> right;
    public override BigInteger ShL(BigInteger left, byte right) => left << right;

    public override BigInteger Add(BigInteger left, BigInteger right) => left + right;
    public override BigInteger Sub(BigInteger left, BigInteger right) => left - right;
    public override BigInteger Mul(BigInteger left, BigInteger right) => left * right;
    public override BigInteger Div(BigInteger left, BigInteger right) => left / right;

    public override string Format(BigInteger element) => element.ToString("R", Culture.InvCult);

    protected override BigInteger ParseCore(in ReadOnlySpan<char> input) => BigInteger.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        , NumberStyles.Integer, Culture.InvCult);


    public static readonly ITransformer<BigInteger> Instance = new BigIntegerTransformer();

    private BigIntegerTransformer() { }
}