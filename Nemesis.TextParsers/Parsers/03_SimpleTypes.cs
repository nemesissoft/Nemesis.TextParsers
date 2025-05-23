using System.Net;

using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Parsers;

public interface ITestTransformerRegistrations
{
    IReadOnlyDictionary<Type, ITransformer> GetTransformerRegistrationsForTests();
}

public sealed class SimpleTransformerHandler : ITransformerHandler, ITestTransformerRegistrations
{
    private readonly Dictionary<Type, ITransformer> _simpleTransformers = new()
    {
        [typeof(string)] = StringTransformer.Instance,
        [typeof(bool)] = BooleanTransformer.Instance,
        [typeof(char)] = CharTransformer.Instance,
        [typeof(byte)] = ByteTransformer.Instance,
        [typeof(sbyte)] = SByteTransformer.Instance,
        [typeof(short)] = Int16Transformer.Instance,
        [typeof(ushort)] = UInt16Transformer.Instance,
        [typeof(int)] = Int32Transformer.Instance,
        [typeof(uint)] = UInt32Transformer.Instance,
        [typeof(long)] = Int64Transformer.Instance,
        [typeof(ulong)] = UInt64Transformer.Instance,
#if NET7_0_OR_GREATER
        [typeof(Int128)] = Int128Transformer.Instance,
        [typeof(UInt128)] = UInt128Transformer.Instance,
#endif
        [typeof(BigInteger)] = BigIntegerTransformer.Instance,
#if NET
        [typeof(Half)] = HalfTransformer.Instance,
#endif
        [typeof(float)] = SingleTransformer.Instance,
        [typeof(double)] = DoubleTransformer.Instance,
        [typeof(decimal)] = DecimalTransformer.Instance,
        [typeof(TimeSpan)] = TimeSpanTransformer.Instance,
        [typeof(DateTime)] = DateTimeTransformer.Instance,
        [typeof(DateTimeOffset)] = DateTimeOffsetTransformer.Instance,
        [typeof(Guid)] = GuidTransformer.Instance,
        [typeof(Version)] = VersionTransformer.Instance,
        [typeof(IPAddress)] = IpAddressTransformer.Instance,
        [typeof(Regex)] = RegexTransformer.Instance,
        [typeof(RegexOptions)] = RegexOptionsTransformer.Instance,
        [typeof(Complex)] = ComplexTransformer.Instance,
        [typeof(Index)] = IndexTransformer.Instance,
        [typeof(Range)] = RangeTransformer.Instance,
#if NET6_0_OR_GREATER
        [typeof(DateOnly)] = DateOnlyTransformer.Instance,
        [typeof(TimeOnly)] = TimeOnlyTransformer.Instance,
#endif
    };

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

    IReadOnlyDictionary<Type, ITransformer> ITestTransformerRegistrations.GetTransformerRegistrationsForTests() => _simpleTransformers;
}