using System.ComponentModel;
using System.Diagnostics;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;
using Sut = Nemesis.TextParsers.Tests.Utils.Sut;

namespace Nemesis.TextParsers.Tests.Arch.Infrastructure;

[TestFixture]
public class InfrastructureTests
{
    private static IEnumerable<TCD> IsSupportedForTransformation_Data() =>
    [
        new TCD(typeof(PointWithConverter), true),
        new TCD(typeof(string), true),
        new TCD(typeof(TimeSpan), true),
        new TCD(typeof(TimeSpan[]), true),
        new TCD(typeof(TimeSpan[][]), true),
        new TCD(typeof(KeyValuePair<string, int>), true),
        new TCD(typeof(KeyValuePair<string, int>[]), true),
        new TCD(typeof(KeyValuePair<KeyValuePair<string, int>[], int>[]), true),
        new TCD(typeof(Dictionary<string[], Dictionary<int?, float[][]>>), true),
        new TCD(typeof(LeanCollection<string>), true),
        new TCD(typeof(string[][][][][][]), true),


        new TCD(typeof(PointWithBadConverter), false),
        new TCD(typeof(PointWithoutConverter), false),
        new TCD(typeof(object), false),
        new TCD(typeof(object[]), false),
        new TCD(typeof(object[][]), false),
        new TCD(typeof(object[,]), false),
        new TCD(typeof(string[,]), false),
        new TCD(typeof(string[][][,][][][]), false),
        new TCD(typeof(ICollection<object>), false),
        new TCD(typeof(LeanCollection<object>), false),
        new TCD(typeof(IDictionary<object, object>), false),
        new TCD(typeof(IReadOnlyList<object>), false),
        new TCD(typeof(List<object>), false),
        new TCD(typeof(PointWithoutConverter?), false),
        new TCD(typeof(PointWithBadConverter?), false),
        new TCD(typeof(ValueTuple<object, object>), false),
        new TCD(typeof(ValueTuple<string, object, object, object, object, object, object>), false),
        new TCD(typeof(KeyValuePair<object, object>), false),
        new TCD(typeof(KeyValuePair<object, object>[]), false),
    ];
    [TestCaseSource(nameof(IsSupportedForTransformation_Data))]
    public void IsSupportedForTransformation(Type type, bool expected) =>
        Assert.That(
            Sut.DefaultStore.IsSupportedForTransformation(type),
            Is.EqualTo(expected)
        );

    private static readonly IReadOnlyList<Type> _simpleTypes =
    [
        typeof(string), typeof(bool), typeof(char),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
        typeof(TimeSpan), typeof(DateTime), typeof(DateTimeOffset),
        typeof(Guid), typeof(FileMode),
        typeof(BigInteger), typeof(Complex)
    ];

    [Test]
    public void IsSupportedForTransformation_Exploratory()
    {
        var expected = GetIsSupportedCases().ToList();

        var actual = expected
            .Select(td => td.type)
            .Select(type => (
                type,
                actual: Sut.DefaultStore.IsSupportedForTransformation(type)
            )).ToList();

        Assert.That(actual, Is.EqualTo(expected));

        static IEnumerable<(Type type, bool expected)> GetIsSupportedCases()
        {
            static IEnumerable<(Type type, bool expected)> GetCases(IReadOnlyList<Type> types, bool expected)
            {
                var rand = new Random();

                Type GetRandomType() => types[rand.Next(types.Count)];

                var nullable = typeof(Nullable<>);
                var coll = typeof(ICollection<>);
                var dict = typeof(IDictionary<,>);
                var kvp = typeof(KeyValuePair<,>);
                var tupleTypes = new (int arity, Type tupleType)[]
                {
                (1, typeof(ValueTuple<>)),
                (2, typeof(ValueTuple<,>)),
                (3, typeof(ValueTuple<,,>)),
                (4, typeof(ValueTuple<,,,>)),
                (5, typeof(ValueTuple<,,,,>)),
                (6, typeof(ValueTuple<,,,,,>)),
                (7, typeof(ValueTuple<,,,,,,>)),
                };


                foreach (var type in types)
                {
                    yield return (type, expected);
                    yield return (type.MakeArrayType(), expected);
                    yield return (type.MakeArrayType().MakeArrayType(), expected);
                    yield return (coll.MakeGenericType(type), expected);

                    if (type.IsValueType)
                        yield return (nullable.MakeGenericType(type), expected);


                    yield return (kvp.MakeGenericType(type, GetRandomType()), expected);
                    yield return (kvp.MakeGenericType(GetRandomType(), type), expected);

                    yield return (dict.MakeGenericType(type, GetRandomType()), expected);
                    yield return (dict.MakeGenericType(GetRandomType(), type), expected);

                    foreach ((int arity, var tupleType) in tupleTypes)
                        yield return (
                            tupleType.MakeGenericType([type, .. Enumerable.Repeat(0, arity - 1).Select(i => GetRandomType())]),
                            expected
                       );
                }
            }

            foreach (var @case in GetCases(_simpleTypes, true))
                yield return @case;


            var badTypes = new[] { typeof(object), typeof(PointWithBadConverter), typeof(PointWithoutConverter) };
            foreach (var @case in GetCases(badTypes, false))
                yield return @case;


            foreach (var type in _simpleTypes)
            {
                yield return (type.MakeArrayType(2), false);
                yield return (type.MakeArrayType(3), false);
            }
        }
    }


    //for type => empty, null
    private static IEnumerable<TCD> GetEmptyNullInstance_Data() =>
    [
        new TCD(typeof(string), "", null),
        new TCD(typeof(bool), false, false),
        new TCD(typeof(char), '\0', '\0'),

        new TCD(typeof(byte), (byte) 0, (byte) 0),
        new TCD(typeof(sbyte), (sbyte) 0, (sbyte) 0),
        new TCD(typeof(short), (short) 0, (short) 0),
        new TCD(typeof(ushort), (ushort) 0, (ushort) 0),
        new TCD(typeof(int), 0, 0),
        new TCD(typeof(uint), (uint) 0, (uint) 0),
        new TCD(typeof(long), (long) 0, (long) 0),
        new TCD(typeof(ulong), (ulong) 0, (ulong) 0),
        new TCD(typeof(float), 0.0f, 0.0f),
        new TCD(typeof(double), 0.0, 0.0),

        new TCD(typeof(uint?), null, null),
        new TCD(typeof(double?), null, null),

        new TCD(typeof(FileMode), (FileMode) 0, (FileMode) 0),


        new TCD(typeof(KeyValuePair<int, float>), default(KeyValuePair<int, float>), default(KeyValuePair<int, float>)),
        new TCD(typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", null), new KeyValuePair<string, float?>(null, null)),



        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger?, bool>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger?, bool>>("", 0, 0M, null, [], [], [], new ValueTuple<FileMode, BigInteger?, bool>(0, null, false)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger?, bool>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode, BigInteger?, bool>(0, null, false))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>("", 0, 0M, null, [], [], [], new ValueTuple<FileMode, BigInteger>(0, 0)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode, BigInteger>(0, 0))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>("", 0, 0M, null, [], [], [], new ValueTuple<FileMode>(0)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode>(0))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>("", 0, 0M, null, [], [], []),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>(null, 0, 0M, null, null, null, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>("", 0, 0M, null, [], []),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>(null, 0, 0M, null, null, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>>),
            new ValueTuple<string, int, decimal, double?, List<byte>>("", 0, 0M, null, []),
            new ValueTuple<string, int, decimal, double?, List<byte>>(null, 0, 0M, null, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?>),
            new ValueTuple<string, int, decimal, double?>("", 0, 0M, null),
            new ValueTuple<string, int, decimal, double?>(null, 0, 0M, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal>),
            new ValueTuple<string, int, decimal>("", 0, 0M),
            new ValueTuple<string, int, decimal>(null, 0, 0M)
        ),
        new TCD(typeof(ValueTuple<string, int>),
            new ValueTuple<string, int>("", 0),
            new ValueTuple<string, int>(null, 0)
        ),
        new TCD(typeof(ValueTuple<string>),
            new ValueTuple<string>(""),
            new ValueTuple<string>(null)
        ),

        new TCD(typeof(List<string>), new List<string>(), null),
        new TCD(typeof(IReadOnlyList<int>), new List<int>(), null),
        new TCD(typeof(Dictionary<string, float?>), new Dictionary<string, float?>(), null),
        new TCD(typeof(decimal[]), Array.Empty<decimal>(), null),
        new TCD(typeof(BigInteger[][]), Array.Empty<BigInteger[]>(), null),
        new TCD(typeof(Complex), new Complex(0.0, 0.0), new Complex(0.0, 0.0)),


        new TCD(typeof(LotsOfDeconstructableData), LotsOfDeconstructableData.EmptyInstance, null),
        new TCD(typeof(EmptyFactoryMethodConvention), EmptyFactoryMethodConvention.Empty, null),
    ];

    [TestCaseSource(nameof(GetEmptyNullInstance_Data))]
    public void GetEmptyAndNullInstanceTest(Type type, object expectedEmpty, object expectedNull)
    {
        IsMutuallyEquivalent(
            Sut.GetTransformer(type).GetEmptyObject(),
            expectedEmpty, "empty value should be as expected");


        IsMutuallyEquivalent(
            Sut.GetTransformer(type).GetNullObject(),
            expectedNull, "null value should be as expected");
    }


    //type, input, expectedOutput
    private static IEnumerable<TCD> EmptyNullParsingData() =>
    [
        new TCD(typeof(ValueTuple<int, int>), "", (0, 0)),
        new TCD(typeof(ValueTuple<int, int>), null, (0, 0)),
        new TCD(typeof(ValueTuple<int, int>), "(,)", (0, 0)),
        new TCD(typeof(ValueTuple<int, int>), "(1,)", (1, 0)),
        new TCD(typeof(ValueTuple<int, int>), "(,2)", (0, 2)),


        new TCD(typeof(ValueTuple<string, int?>), @"", ("", (int?) null)),
        new TCD(typeof(ValueTuple<string, int?>), @"(,)", ("", (int?) null)),
        new TCD(typeof(ValueTuple<string, int?>), @"(∅,)", ((string) null, (int?) null)),
        new TCD(typeof(ValueTuple<string, int?>), @"(\∅,)", ("∅", (int?) null)),
        new TCD(typeof(ValueTuple<string, int?>), @"(∅ABC,)", ("∅ABC", (int?) null)),

        new TCD(typeof(ValueTuple<int, int?>), "", (0, (int?) null)),


        new TCD(typeof(List<ValueTuple<int, int?>>), @"(0,\∅)", new List<(int, int?)> {default}),
        new TCD(typeof(List<ValueTuple<int, int?>>), @"(0,\∅)|(0,\∅)", new List<(int, int?)> {default, default}),
        new TCD(typeof(List<ValueTuple<int, int?>>), @"(0,\∅)|∅", new List<(int, int?)> {default, default}),
        new TCD(typeof(List<ValueTuple<int, int?>>), @"(0,\∅)|(0,\∅)|", new List<(int, int?)> {default, default, default}),


        new TCD(typeof(List<int?>), @"∅", new List<int?> {default}),
        new TCD(typeof(List<int?>), @"∅|∅", new List<int?> {default, default}),
        new TCD(typeof(List<int?>), @"∅|∅|", new List<int?> {default, default, default}),
    ];
    [TestCaseSource(nameof(EmptyNullParsingData))]
    public void EmptyNullParsingTest(Type type, string input, object expectedOutput)
    {
        var emptyNull = MakeDelegate<Action<string, object>>(
            (p1, p2) => EmptyNullParsingTest_Helper<int>(p1, p2), type
        );

        emptyNull(input, expectedOutput);
    }

    private static void EmptyNullParsingTest_Helper<T>(string input, object expectedOutput)
    {
        var sut = Sut.GetTransformer<T>();

        var parsed1 = sut.Parse(input);

        Assert.That(parsed1, Is.EqualTo(expectedOutput));

        var text = sut.Format(parsed1);
        var parsed2 = sut.Parse(text);

        Assert.That(parsed2, Is.EqualTo(parsed1));
        Assert.That(parsed2, Is.EqualTo(expectedOutput));
    }

    [TestCase(typeof(float), "Handled by SimpleTransformerHandler: Simple built-in type")]
    [TestCase(typeof(Dictionary<decimal, string[]>), """
        MISS -- Simple built-in type
        MISS -- Key-value pair with properties supported for transformation
        MISS -- Value tuple with properties supported for transformation
        MISS -- Type decorated with TransformerAttribute pointing to valid transformer
        MISS -- Type with method FromText(ReadOnlySpan<char> or string)
        MISS -- Type decorated with TextFactoryAttribute pointing to factory with method FromText(ReadOnlySpan<char> or string)
        MISS -- Enum based on system primitive number types
        MISS -- Nullable with value supported for transformation
        Handled by DictionaryTransformerHandler: Dictionary-like structure with key/value supported for transformation
        """)]
    [TestCase(typeof(PointWithoutConverter), """
        MISS -- Simple built-in type
        MISS -- Key-value pair with properties supported for transformation
        MISS -- Value tuple with properties supported for transformation
        MISS -- Type decorated with TransformerAttribute pointing to valid transformer
        MISS -- Type with method FromText(ReadOnlySpan<char> or string)
        MISS -- Type decorated with TextFactoryAttribute pointing to factory with method FromText(ReadOnlySpan<char> or string)
        MISS -- Enum based on system primitive number types
        MISS -- Nullable with value supported for transformation
        MISS -- Dictionary-like structure with key/value supported for transformation
        MISS -- Custom dictionary structure with key/value type supported for transformation
        MISS -- Arrays (single dimensional and jagged) with element type supported for transformation
        MISS -- Collections with element type supported for transformation
        MISS -- LeanCollection with element type supported for transformation
        MISS -- Custom collection with element type supported for transformation
        MISS -- Deconstructable with properties types supported for transformation
        MISS -- Type decorated with TypeConverter
        """)]
    public void DescribeHandlerMatch_ShouldReturnValudDiagnostics(Type type, string expectedDiagnostics)
    {
        var actual = Sut.DefaultStore.DescribeHandlerMatch(type);
        Assert.That(actual, Is.EqualTo(expectedDiagnostics).Using(Utils.IgnoreNewLinesComparer.EqualityComparer));
    }



    [Test]
    public void Positive_SimpleType()
    {
        var data = Enumerable.Range(1, 10).Select(i => new PointWithConverter(i * 10, i * -100)).ToList();
        var sut = Sut.GetTransformer<PointWithConverter>();
        Assert.That(sut, Is.TypeOf<ConverterTransformer<PointWithConverter>>());


        var actualTexts = data.Select(sut.Format).ToList();
        var actual = actualTexts.Select(sut.Parse).ToList();


        Assert.That(actual, Is.EqualTo(data));
    }

    [Test]
    public void Positive_Dictionary()
    {
        IDictionary<PointWithConverter, int> data = Enumerable.Range(1, 9)
            .Select(i => new PointWithConverter(i * 11, i * 100))
            .ToDictionary(p => p, p => p.X + p.Y);
        var sut = Sut.GetTransformer<IDictionary<PointWithConverter, int>>();


        var actualText = sut.Format(data);
        var actual = sut.Parse(actualText);


        Assert.That(actual, Is.EqualTo(data));
    }

    [Test]
    public void Negative_BadConverter()
    {
        var conv = TypeDescriptor.GetConverter(typeof(PointWithBadConverter));
        Assert.Multiple(() =>
        {
            Assert.That(conv, Is.TypeOf<NotTextConverter>());

            Assert.That(
                Sut.GetTransformer<PointWithBadConverter>,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo("Type 'PointWithBadConverter' is not supported for text transformations. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
                );
        });
    }

    [Test]
    public void Negative_WhenNoConverterIsSpecified_ExceptionShouldBeThrown()
    {
        var sut = new TypeConverterTransformerHandler();
        Assert.Multiple(() =>
        {
            Assert.That(
                () => sut.CreateTransformer<PointWithoutConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'PointWithoutConverter' is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );

            Assert.That(
                () => sut.CreateTransformer<object>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'object' is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );
        });
    }

    [Test]
    public void Negative_TypeConverterHandler_ShouldNotHandleTypesWithoutTransformerStrategy() => Assert.Multiple(() =>
    {
        Assert.That(
            TypeDescriptor.GetConverter(typeof(PointWithoutConverter)),
            Is.TypeOf<TypeConverter>()
        );

        Assert.That(
            TypeDescriptor.GetConverter(typeof(object)),
            Is.TypeOf<TypeConverter>()
        );

        Assert.That(
            Sut.GetTransformer<PointWithoutConverter>,
            Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo(@"Type 'PointWithoutConverter' is not supported for text transformations. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
        );

        Assert.That(
            Sut.GetTransformer<object>,
            Throws.TypeOf<NotSupportedException>()
                .With.Message.EqualTo(@"Type 'object' is not supported for text transformations. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
        );
    });


    [TypeConverter(typeof(PointConverter))]
    [DebuggerDisplay("X = {" + nameof(X) + "}, Y = {" + nameof(Y) + "}")]
    readonly struct PointWithConverter(int x, int y) : IEquatable<PointWithConverter>
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        public bool Equals(PointWithConverter other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is PointWithConverter other && Equals(other);

        public override int GetHashCode() => unchecked(X * 397 ^ Y);
    }

    sealed class PointConverter : BaseTextConverter<PointWithConverter>
    {
        private static PointWithConverter FromText(ReadOnlySpan<char> text)
        {
            var enumerator = text.Split(';').GetEnumerator();

            if (!enumerator.MoveNext()) return default;
            int x = Int32Transformer.Instance.Parse(enumerator.Current);

            if (!enumerator.MoveNext()) return default;
            int y = Int32Transformer.Instance.Parse(enumerator.Current);

            return new PointWithConverter(x, y);
        }

        public override PointWithConverter ParseString(string text) => FromText(text.AsSpan());

        public override string FormatToString(PointWithConverter pwc) => $"{pwc.X};{pwc.Y}";
    }

    [TypeConverter(typeof(NotTextConverter))]
    readonly struct PointWithBadConverter(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
    }

    sealed class NotTextConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType != typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType != typeof(string);
    }

    readonly struct PointWithoutConverter(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
    }
}
