using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Nemesis.TextParsers.Tests.Utils;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;
using TCD = NUnit.Framework.TestCaseData;

namespace Nemesis.TextParsers.Tests.Infrastructure;

[TestFixture]
public class InfrastructureTests
{
    private static IEnumerable<TCD> IsSupportedForTransformation_Data() => new[]
    {
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
    };
    [TestCaseSource(nameof(IsSupportedForTransformation_Data))]
    public void IsSupportedForTransformation(Type type, bool expected) =>
        Assert.That(
            Sut.DefaultStore.IsSupportedForTransformation(type),
            Is.EqualTo(expected)
        );

    private static readonly IReadOnlyList<Type> _simpleTypes = new[]
    {
        typeof(string), typeof(bool), typeof(char),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal),
        typeof(TimeSpan), typeof(DateTime), typeof(DateTimeOffset),
        typeof(Guid), typeof(FileMode),
        typeof(BigInteger), typeof(Complex)
    };

    private static IEnumerable<(Type type, bool expected)> GetIsSupportedCases()
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
                        tupleType.MakeGenericType(new[] { type }.Concat(Enumerable.Repeat(0, arity - 1).Select(i => GetRandomType())).ToArray()),
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

    [Test]
    public void IsSupportedForTransformation_Exploratory()
    {
        //static string ToTick(bool result) => result ? "✔" : "✖";
        //Console.WriteLine($"{ToTick(actual)} as{(pass ? " " : " NOT ")}expected for {type.GetFriendlyName()}");

        var expected = GetIsSupportedCases().ToList();

        var actual = expected
            .Select(td => td.type)
            .Select(type => (
                type,
                actual: Sut.DefaultStore.IsSupportedForTransformation(type)
            )).ToList();


        Assert.That(actual, Is.EqualTo(expected));
    }


    //for type => empty, null
    private static IEnumerable<TCD> GetEmptyNullInstance_Data() => new[]
    {
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
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger?, bool>>("", 0, 0M, null, new List<byte>(), Array.Empty<sbyte>(), new Dictionary<string, int>(), new ValueTuple<FileMode, BigInteger?, bool>(0, null, false)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger?, bool>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode, BigInteger?, bool>(0, null, false))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>("", 0, 0M, null, new List<byte>(), Array.Empty<sbyte>(), new Dictionary<string, int>(), new ValueTuple<FileMode, BigInteger>(0, 0)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode, BigInteger>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode, BigInteger>(0, 0))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>("", 0, 0M, null, new List<byte>(), Array.Empty<sbyte>(), new Dictionary<string, int>(), new ValueTuple<FileMode>(0)),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>, ValueTuple<FileMode>>(null, 0, 0M, null, null, null, null, new ValueTuple<FileMode>(0))
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>("", 0, 0M, null, new List<byte>(), Array.Empty<sbyte>(), new Dictionary<string, int>()),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[], Dictionary<string,int>>(null, 0, 0M, null, null, null, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>("", 0, 0M, null, new List<byte>(), Array.Empty<sbyte>()),
            new ValueTuple<string, int, decimal, double?, List<byte>, sbyte[]>(null, 0, 0M, null, null, null)
        ),
        new TCD(typeof(ValueTuple<string, int, decimal, double?, List<byte>>),
            new ValueTuple<string, int, decimal, double?, List<byte>>("", 0, 0M, null, new List<byte>()),
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
    };

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
    internal static IEnumerable<TCD> EmptyNullParsingData() => new[]
    {
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
    };
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
}
