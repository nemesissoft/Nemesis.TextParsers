using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class InfrastructureTests
    {
        #region IsSupported - Test cases
        [TestCase(typeof(PointWithConverter), true)]
        [TestCase(typeof(string), true)]
        [TestCase(typeof(TimeSpan), true)]
        [TestCase(typeof(TimeSpan[]), true)]
        [TestCase(typeof(TimeSpan[][]), true)]
        [TestCase(typeof(KeyValuePair<string, int>), true)]
        [TestCase(typeof(KeyValuePair<string, int>[]), true)]
        [TestCase(typeof(KeyValuePair<KeyValuePair<string, int>[], int>[]), true)]
        [TestCase(typeof(Dictionary<string[], Dictionary<int?, float[][]>>), true)]
        [TestCase(typeof(LeanCollection<string>), true)]
        [TestCase(typeof(string[][][][][][]), true)]


        [TestCase(typeof(PointWithBadConverter), false)]
        [TestCase(typeof(PointWithoutConverter), false)]
        [TestCase(typeof(object), false)]
        [TestCase(typeof(object[]), false)]
        [TestCase(typeof(object[][]), false)]
        [TestCase(typeof(object[,]), false)]
        [TestCase(typeof(string[,]), false)]
        [TestCase(typeof(string[][][,][][][]), false)]
        [TestCase(typeof(ICollection<object>), false)]
        [TestCase(typeof(LeanCollection<object>), false)]
        [TestCase(typeof(IDictionary<object, object>), false)]
        [TestCase(typeof(IReadOnlyList<object>), false)]
        [TestCase(typeof(List<object>), false)]
        [TestCase(typeof(PointWithoutConverter?), false)]
        [TestCase(typeof(PointWithBadConverter?), false)]
        [TestCase(typeof(ValueTuple<object, object>), false)]
        [TestCase(typeof(ValueTuple<string, object, object, object, object, object, object>), false)]
        [TestCase(typeof(KeyValuePair<object, object>), false)]
        [TestCase(typeof(KeyValuePair<object, object>[]), false)]
        #endregion
        public void IsSupportedForTransformation(Type type, bool expected) =>
            Assert.That(
                TextTransformer.Default.IsSupportedForTransformation(type),
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
            static string ToTick(bool result) => result ? "✔" : "✖";

            var allPassed = true;
            foreach ((var type, bool expected) in GetIsSupportedCases())
            {
                bool actual = TextTransformer.Default.IsSupportedForTransformation(type);

                bool pass = actual == expected;

                Console.WriteLine($"{ToTick(actual)} as{(pass ? " " : " NOT ")}expected for {type.GetFriendlyName()}");

                if (!pass)
                    allPassed = false;
            }
            Assert.IsTrue(allPassed);
        }


        //type, empty, null
        internal static IEnumerable<TCD> GetEmptyNullInstance_Data() => new[]
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
            new TCD(typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), new KeyValuePair<string, float?>(null, null)),
            //TODO what should be empty value ?
            //new TCD(typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", null), new KeyValuePair<string, float?>(null, null)),



            new TCD(typeof(List<string>), new List<string>(), null),
            new TCD(typeof(IReadOnlyList<int>), new List<int>(), null),
            new TCD(typeof(Dictionary<string, float?>), new Dictionary<string, float?>(), null),
            new TCD(typeof(decimal[]), new decimal[0], null),
            new TCD(typeof(BigInteger[][]), new BigInteger[0][], null),
            new TCD(typeof(Complex), new Complex(0.0, 0.0), new Complex(0.0, 0.0)),


            new TCD(typeof(LotsOfDeconstructableData), LotsOfDeconstructableData.EmptyInstance, null),
            new TCD(typeof(EmptyFactoryMethodConvention), EmptyFactoryMethodConvention.Empty, null),
        };


        [TestCaseSource(nameof(GetEmptyNullInstance_Data))]
        public void GetEmptyAndNullInstanceTest(Type type, object expectedEmpty,object expectedNull)
        {
            IsMutuallyEquivalent(
                TextTransformer.Default.GetEmptyInstance(type),
                expectedEmpty, "empty value should be as expected");
            
            
            IsMutuallyEquivalent(
                TextTransformer.Default.GetTransformer(type).GetNullObject(),
                expectedNull, "null value should be as expected");
        }


        class LotsOfDeconstructableData
        {
            public string D1 { get; }
            public bool D2 { get; }
            public int D3 { get; }
            public uint? D4 { get; }
            public float D5 { get; }
            public double? D6 { get; }
            public FileMode D7 { get; }
            public List<string> D8 { get; }
            public IReadOnlyList<int> D9 { get; }
            public Dictionary<string, float?> D10 { get; }
            public decimal[] D11 { get; }
            public BigInteger[][] D12 { get; }
            public Complex D13 { get; }

            public LotsOfDeconstructableData(string d1, bool d2, int d3, uint? d4, float d5, double? d6, FileMode d7, List<string> d8, IReadOnlyList<int> d9, Dictionary<string, float?> d10, decimal[] d11, BigInteger[][] d12, Complex d13)
            {
                D1 = d1;
                D2 = d2;
                D3 = d3;
                D4 = d4;
                D5 = d5;
                D6 = d6;
                D7 = d7;
                D8 = d8;
                D9 = d9;
                D10 = d10;
                D11 = d11;
                D12 = d12;
                D13 = d13;
            }

            [UsedImplicitly]
            public void Deconstruct(out string d1, out bool d2, out int d3, out uint? d4, out float d5, out double? d6, out FileMode d7, out List<string> d8, out IReadOnlyList<int> d9, out Dictionary<string, float?> d10, out decimal[] d11, out BigInteger[][] d12, out Complex d13)
            {
                d1 = D1;
                d2 = D2;
                d3 = D3;
                d4 = D4;
                d5 = D5;
                d6 = D6;
                d7 = D7;
                d8 = D8;
                d9 = D9;
                d10 = D10;
                d11 = D11;
                d12 = D12;
                d13 = D13;
            }

            public static readonly LotsOfDeconstructableData EmptyInstance = new LotsOfDeconstructableData("", false, 0, null, 0.0f, null, 0, new List<string>(),
                new List<int>(), new Dictionary<string, float?>(), new decimal[0], new BigInteger[0][], new Complex(0.0, 0.0)
            );
        }

        //this is to demonstrate support of LSP rule 
        abstract class EmptyConventionBase { }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        sealed class EmptyFactoryMethodConvention : EmptyConventionBase, IEquatable<EmptyFactoryMethodConvention>
        {
            public float Number { get; }
            public DateTime Time { get; }

            public EmptyFactoryMethodConvention(float number, DateTime time)
            {
                Number = number;
                Time = time;
            }


            //it's generally no a good idea for a property not to be deterministic. But this works well for our demo.
            //And have a look at DateTime.Now ;-)
            public static EmptyConventionBase Empty => new EmptyFactoryMethodConvention(3.14f, DateTime.Now);

            //this is just to conform to FactoryMethod convention 
            [UsedImplicitly]
            public static EmptyFactoryMethodConvention FromText(string text) => throw new NotSupportedException("Not used");

            #region Equals
            public bool Equals(EmptyFactoryMethodConvention other) =>
                    !(other is null) && (ReferenceEquals(this, other) ||
                        Number.Equals(other.Number) && Math.Abs(Time.Ticks - other.Time.Ticks) < 2 * TimeSpan.TicksPerMinute
                     );

            public override bool Equals(object obj) =>
                !(obj is null) && (ReferenceEquals(this, obj) || obj is EmptyFactoryMethodConvention ec && Equals(ec));

            public override int GetHashCode() => unchecked((Number.GetHashCode() * 397) ^ Time.GetHashCode());
            #endregion
        }
    }
}
