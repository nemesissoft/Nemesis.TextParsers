using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public class InfrastructureTests
    {
        #region Test cases
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
    }
}
