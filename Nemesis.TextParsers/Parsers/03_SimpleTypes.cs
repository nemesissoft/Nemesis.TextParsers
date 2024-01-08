using System.Globalization;
using System.Net;

using JetBrains.Annotations;

using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class SimpleTransformerHandler : ITransformerHandler
{
    private readonly IReadOnlyDictionary<Type, ITransformer> _simpleTransformers;

    public SimpleTransformerHandler() => _simpleTransformers = GetDefaultTransformers();

    private static IReadOnlyDictionary<Type, ITransformer> GetDefaultTransformers(Assembly fromAssembly = null)
    {
        var types = (fromAssembly ?? Assembly.GetExecutingAssembly())
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition);

        var simpleTransformers = new Dictionary<Type, ITransformer>(30);

        foreach (var type in types)
        {
            if (type.DerivesOrImplementsGeneric(typeof(ITransformer<>)) &&
                TypeMeta.TryGetGenericRealization(type, typeof(SimpleTransformer<>), out var simpleType)
              )
            {
                var elementType = simpleType.GenericTypeArguments[0];
                var transformerElementType = typeof(ITransformer<>).MakeGenericType(elementType);


                if (simpleTransformers.ContainsKey(elementType))
                    throw new NotSupportedException($"Automatic registration does not support multiple simple transformers to handle type {elementType}");

                simpleTransformers[elementType] = (ITransformer)ReflectionUtils.GetInstanceOrCreate(type, transformerElementType);
            }
        }

        return simpleTransformers;
    }

    public ITransformer<TSimpleType> CreateTransformer<TSimpleType>()
    {
        return _simpleTransformers.TryGetValue(typeof(TSimpleType), out var transformer)
            ? (ITransformer<TSimpleType>)transformer
            : throw new InvalidOperationException(
                $"Internal state of {nameof(SimpleTransformerHandler)} was compromised");
    }

    public bool CanHandle(Type type) => _simpleTransformers.ContainsKey(type);

    public sbyte Priority => 10;

    public override string ToString() =>
        $"Create transformer for simple system types: {string.Join(", ", _simpleTransformers.Keys.Select(t => t.GetFriendlyName()))}";

    string ITransformerHandler.DescribeHandlerMatch() => "Simple built-in type";
}

public static class NumberTransformerCache
{
    private static readonly IReadOnlyDictionary<Type, ITransformer> _cache = BuildCache();

    private static IReadOnlyDictionary<Type, ITransformer> BuildCache(Assembly fromAssembly = null)
    {
        var transformerTypes = (fromAssembly ?? typeof(NumberTransformer<>).Assembly)
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                        t.DerivesOrImplementsGeneric(typeof(NumberTransformer<>))
            );

        var cache = transformerTypes.ToDictionary(
            tt => TypeMeta.GetGenericRealization(tt, typeof(NumberTransformer<>)).GenericTypeArguments[0],
            tt => (ITransformer)ReflectionUtils.GetInstanceOrCreate(tt, typeof(ITransformer))
        );

        return cache;
    }

    public static NumberTransformer<TNumber> GetNumberHandler<TNumber>()
        where TNumber : struct, IComparable, IComparable<TNumber>, IConvertible, IEquatable<TNumber>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TNumber>
#endif
        => _cache.TryGetValue(typeof(TNumber), out var numOp) ? (NumberTransformer<TNumber>)numOp : null;

    public static object GetNumberHandler(Type numberType) =>
        _cache.TryGetValue(numberType, out var numOp) ? numOp : null;
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

#region System types

[UsedImplicitly]
public sealed class StringTransformer : SimpleTransformer<string>
{
    protected override string ParseCore(in ReadOnlySpan<char> input) => input.ToString();

    public override string Format(string element) => element;

    public override string GetEmpty() => "";

    public static readonly ITransformer<string> Instance = new StringTransformer();

    private StringTransformer() { }
}

[UsedImplicitly]
public sealed class BooleanTransformer : SimpleTransformer<bool>
{
#if NETSTANDARD2_0 || NETFRAMEWORK
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
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

    private const string TRUE_LITERAL = "True";
    private const string FALSE_LITERAL = "False";
    private static bool TryParseBool(ReadOnlySpan<char> value, out bool result)
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

    private static bool ParseBool(ReadOnlySpan<char> value) =>
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

    public static readonly ITransformer<bool> Instance = new BooleanTransformer();

    private BooleanTransformer() { }
}

[UsedImplicitly]
public sealed class CharTransformer : SimpleTransformer<char>
{
    protected override char ParseCore(in ReadOnlySpan<char> input) => input.Length switch
    {
        0 => '\0',
        1 => input[0],
        _ => throw new FormatException($"\"{input.ToString()}\" is not a valid value for char")
    };

    public override string Format(char element) => element == '\0' ? "" : char.ToString(element);


    public static readonly ITransformer<char> Instance = new CharTransformer();

    private CharTransformer() { }
}

public abstract class SimpleFormattableTransformer<TElement> : SimpleTransformer<TElement>
    where TElement : struct, IFormattable
{
    public sealed override string Format(TElement element) =>
        element.ToString(FormatString, Culture.InvCult);

    protected virtual string FormatString { get; }
}

public abstract class NumberTransformer<TNumber> : SimpleTransformer<TNumber>
    where TNumber : struct, IComparable, IComparable<TNumber>, IConvertible, IEquatable<TNumber>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TNumber>
#endif
{
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

    public sealed override string Format(TNumber element) => element.ToString(null, Culture.InvCult);

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

#if NET

[UsedImplicitly]
public sealed class HalfTransformer : SimpleFormattableTransformer<Half>
{
    protected override Half ParseCore(in ReadOnlySpan<char> input) =>
        input.Length switch
        {
            1 when input[0] == '∞' => Half.PositiveInfinity,
            2 when input[0] == '-' && input[1] == '∞' => Half.NegativeInfinity,
            _ => Half.Parse(input, NumberStyles.Float | NumberStyles.AllowThousands, Culture.InvCult)
        };

    protected override string FormatString { get; } = "G17";

    public static readonly ITransformer<Half> Instance = new HalfTransformer();

    private HalfTransformer() { }
}

#endif

[UsedImplicitly]
public sealed class SingleTransformer : SimpleFormattableTransformer<float>
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

    public static readonly ITransformer<float> Instance = new SingleTransformer();

    private SingleTransformer() { }
}

[UsedImplicitly]
public sealed class DoubleTransformer : SimpleFormattableTransformer<double>
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

    public static readonly ITransformer<double> Instance = new DoubleTransformer();

    private DoubleTransformer() { }
}

[UsedImplicitly]
public sealed class DecimalTransformer : SimpleFormattableTransformer<decimal>
{
    protected override decimal ParseCore(in ReadOnlySpan<char> input) =>
        decimal.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , NumberStyles.Number, Culture.InvCult);

    public static readonly ITransformer<decimal> Instance = new DecimalTransformer();

    private DecimalTransformer() { }
}

[UsedImplicitly]
public sealed class TimeSpanTransformer : SimpleFormattableTransformer<TimeSpan>
{
    protected override TimeSpan ParseCore(in ReadOnlySpan<char> input) =>
        TimeSpan.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
            input
#endif
            , Culture.InvCult);


    public static readonly ITransformer<TimeSpan> Instance = new TimeSpanTransformer();

    private TimeSpanTransformer() { }
}

[UsedImplicitly]
public sealed class DateTimeTransformer : SimpleFormattableTransformer<DateTime>
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

    public static readonly ITransformer<DateTime> Instance = new DateTimeTransformer();

    private DateTimeTransformer() { }
}

[UsedImplicitly]
public sealed class DateTimeOffsetTransformer : SimpleFormattableTransformer<DateTimeOffset>
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

    public static readonly ITransformer<DateTimeOffset> Instance = new DateTimeOffsetTransformer();

    private DateTimeOffsetTransformer() { }
}

[UsedImplicitly]
public sealed class GuidTransformer : SimpleFormattableTransformer<Guid>
{
    protected override Guid ParseCore(in ReadOnlySpan<char> input) => Guid.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    protected override string FormatString { get; } = "D";


    public static readonly ITransformer<Guid> Instance = new GuidTransformer();

    private GuidTransformer() { }
}

[UsedImplicitly]
public sealed class BigIntegerTransformer : SimpleFormattableTransformer<BigInteger>
{
    protected override BigInteger ParseCore(in ReadOnlySpan<char> input) => BigInteger.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        , NumberStyles.Integer, Culture.InvCult);

    protected override string FormatString { get; } = "R";


    public static readonly ITransformer<BigInteger> Instance = new BigIntegerTransformer();

    private BigIntegerTransformer() { }
}

[UsedImplicitly]
public sealed class VersionTransformer : SimpleTransformer<Version>
{
    protected override Version ParseCore(in ReadOnlySpan<char> input) => Version.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    public override string Format(Version element) => element?.ToString();

    public static readonly ITransformer<Version> Instance = new VersionTransformer();

    private VersionTransformer() { }

    public override Version GetEmpty() => new(0, 0, 0, 0);
}

[UsedImplicitly]
public sealed class IpAddressTransformer : SimpleTransformer<IPAddress>
{
    protected override IPAddress ParseCore(in ReadOnlySpan<char> input) => IPAddress.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    public override string Format(IPAddress element) => element?.ToString();

    public static readonly ITransformer<IPAddress> Instance = new IpAddressTransformer();

    private IpAddressTransformer() { }

    public override IPAddress GetEmpty() => new(0);
}

[UsedImplicitly]
public sealed class RegexTransformer : SimpleTransformer<Regex>
{
    private const string TYPE_NAME = "Regex";
    private static readonly TupleHelper _helper = new(';', '∅', '~', '{', '}');

    protected override Regex ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

        var options = _helper.ParseElement(ref enumerator, RegexOptionsTransformer.Instance);
        var pattern = _helper.ParseElement(ref enumerator, StringTransformer.Instance, 2, TYPE_NAME);

        _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

        return new(pattern, options);
    }

    public override string Format(Regex re)
    {
        if (re == null) return null;

        Span<char> initialBuffer = stackalloc char[16];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(RegexOptionsTransformer.Instance, re.Options, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(StringTransformer.Instance, re.ToString(), ref accumulator);

            _helper.EndFormat(ref accumulator);

            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public static readonly ITransformer<Regex> Instance = new RegexTransformer();

    private RegexTransformer() { }

    public override Regex GetEmpty() => new("", RegexOptions.None);
}

internal class RegexOptionsTransformer : TransformerBase<RegexOptions>
{
    private static readonly RegexOptions[] _optionValues = [
        RegexOptions.IgnoreCase,
        RegexOptions.Multiline,
        RegexOptions.ExplicitCapture,
        RegexOptions.Compiled,
        RegexOptions.Singleline,
        RegexOptions.IgnorePatternWhitespace,
        RegexOptions.RightToLeft,
        (RegexOptions)128,
        RegexOptions.ECMAScript,
        RegexOptions.CultureInvariant,
#if NET7_0_OR_GREATER
        RegexOptions.NonBacktracking
#endif
    ];

    public static readonly ITransformer<RegexOptions> Instance = new RegexOptionsTransformer();
    private RegexOptionsTransformer() { }

    protected override RegexOptions ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsEmpty) return default;

        var result = RegexOptions.None;

        for (int i = input.Length - 1; i >= 0; i--)
            result |= ParseSingle(input[i]);

        return result;

        static RegexOptions ParseSingle(char element) => element switch
        {
            '0' => RegexOptions.None,
            'i' => RegexOptions.IgnoreCase,
            'm' => RegexOptions.Multiline,
            'n' => RegexOptions.ExplicitCapture,
            'c' => RegexOptions.Compiled,
            's' => RegexOptions.Singleline,
            'x' => RegexOptions.IgnorePatternWhitespace,
            'r' => RegexOptions.RightToLeft,

            'd' => (RegexOptions)128,

            'e' => RegexOptions.ECMAScript,
            'v' => RegexOptions.CultureInvariant,
#if NET7_0_OR_GREATER
            'b' => RegexOptions.NonBacktracking,
#endif
            _ => throw new NotSupportedException($"'{element}' is not supported for parsing RegexOptions")
        };
    }

    public override string Format(RegexOptions element)
    {
        if (element == RegexOptions.None) return "0";

        Span<char> initialBuffer = stackalloc char[8];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            foreach (var option in _optionValues)
                if ((element & option) > 0)
                    accumulator.Append(FormatSingle(option));

            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }

        static char FormatSingle(RegexOptions option) => option switch
        {
            RegexOptions.None => '0',
            RegexOptions.IgnoreCase => 'i',
            RegexOptions.Multiline => 'm',
            RegexOptions.ExplicitCapture => 'n',
            RegexOptions.Compiled => 'c',
            RegexOptions.Singleline => 's',
            RegexOptions.IgnorePatternWhitespace => 'x',
            RegexOptions.RightToLeft => 'r',

            (RegexOptions)128 => 'd',

            RegexOptions.ECMAScript => 'e',
            RegexOptions.CultureInvariant => 'v',

#if NET7_0_OR_GREATER
            RegexOptions.NonBacktracking => 'b',
#endif
            _ => throw new NotSupportedException($"'{option}' is not supported for formatting RegexOptions")
        };
    }

    /*[Flags]
    public enum RegexOptions
    {
        None                    = 0x0000, // '0'
        IgnoreCase              = 0x0001, // 'i'
        Multiline               = 0x0002, // 'm'
        ExplicitCapture         = 0x0004, // 'n'
        Compiled                = 0x0008, // 'c'
        Singleline              = 0x0010, // 's'
        IgnorePatternWhitespace = 0x0020, // 'x'
        RightToLeft             = 0x0040, // 'r'

        ECMAScript              = 0x0100, // 'e'
        CultureInvariant        = 0x0200, // 'v'
        NonBacktracking         = 0x0400, // 'b'
    }*/
}

[UsedImplicitly]
public sealed class ComplexTransformer : SimpleTransformer<Complex>
{
    private const string TYPE_NAME = "Complex number";

    private static readonly TupleHelper _helper = new(';', '∅', '\\', '(', ')');

    protected override Complex ParseCore(in ReadOnlySpan<char> input)
    {
        var doubleParser = DoubleTransformer.Instance;

        var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

        double real = _helper.ParseElement(ref enumerator, doubleParser);
        double imaginary = _helper.ParseElement(ref enumerator, doubleParser, 2, TYPE_NAME);

        _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

        return new Complex(real, imaginary);
    }

    public override string Format(Complex c)
    {
        var doubleParser = DoubleTransformer.Instance;
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

    public static readonly ITransformer<Complex> Instance = new ComplexTransformer();

    private ComplexTransformer() { }
}

#if NET6_0_OR_GREATER

[UsedImplicitly]
public sealed class DateOnlyTransformer : SimpleFormattableTransformer<DateOnly>
{
    protected override DateOnly ParseCore(in ReadOnlySpan<char> input) =>
        DateOnly.Parse(input, Culture.InvCult, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowInnerWhite);

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<DateOnly> Instance = new DateOnlyTransformer();

    private DateOnlyTransformer() { }
}

[UsedImplicitly]
public sealed class TimeOnlyTransformer : SimpleFormattableTransformer<TimeOnly>
{
    protected override TimeOnly ParseCore(in ReadOnlySpan<char> input) =>
        TimeOnly.Parse(input, Culture.InvCult, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowInnerWhite);

    protected override string FormatString { get; } = "o";

    public static readonly ITransformer<TimeOnly> Instance = new TimeOnlyTransformer();

    private TimeOnlyTransformer() { }
}

#endif

#endregion
