using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class AnyTypeConverterTests
    {
        [Test]
        public void CorrectUsage_SingleInstance()
        {
            var data = Enumerable.Range(1, 10).Select(i => new PointWithConverter(i * 10, i * -100)).ToList();
            var sut = TextTransformer.Default.GetTransformer<PointWithConverter>();


            var actualTexts = data.Select(pwc => sut.Format(pwc)).ToList();
            var actual = actualTexts.Select(text => sut.ParseFromText(text)).ToList();


            Assert.That(actual, Is.EquivalentTo(data));
        }

        [Test]
        public void CorrectUsage_Dict()
        {
            IDictionary<PointWithConverter, int> data = Enumerable.Range(1, 9)
                .Select(i => new PointWithConverter(i * 11, i * 100))
                .ToDictionary(p => p, p => p.X + p.Y);
            var sut = TextTransformer.Default.GetTransformer<IDictionary<PointWithConverter, int>>();


            var actualText = sut.Format(data);
            var actual = sut.ParseFromText(actualText);


            Assert.That(actual, Is.EquivalentTo(data));
        }

        [Test]
        public void BadConverter_NegativeTest()
        {
            var conv = TypeDescriptor.GetConverter(typeof(PointWithBadConverter));
            Assert.That(conv, Is.TypeOf<BadPointConverter>());

            Assert.That(
                () => TextTransformer.Default.GetTransformer<PointWithBadConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo("PointWithBadConverter is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
                );
        }

        [Test]
        public void NoConverter_NegativeTest()
        {
            var sut = new TypeConverterTransformerCreator();

            Assert.That(
                () => sut.CreateTransformer<PointWithoutConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"PointWithoutConverter is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );

            Assert.That(
                () => sut.CreateTransformer<object>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"object is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );
        }

        [Test]
        public void AnyHandler_NegativeTest()
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
                () => TextTransformer.Default.GetTransformer<PointWithoutConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"PointWithoutConverter is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
            );

            Assert.That(
                () => TextTransformer.Default.GetTransformer<object>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"object is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
            );
        }

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
                var tuple4 = typeof(ValueTuple<,,,>);

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

                    yield return (tuple4.MakeGenericType(type, GetRandomType(), GetRandomType(), GetRandomType()), expected);
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

                Console.WriteLine($"{ToTick(actual)} as{(pass ? " " : " NOT " )}expected for {type.GetFriendlyName()}");

                if (!pass)
                    allPassed = false;
            }
            Assert.IsTrue(allPassed);
        }


        [TypeConverter(typeof(PointConverter))]
        [DebuggerDisplay("X = {" + nameof(X) + "}, Y = {" + nameof(Y) + "}")]
        struct PointWithConverter : IEquatable<PointWithConverter>
        {
            public int X { get; }
            public int Y { get; }

            public PointWithConverter(int x, int y) => (X, Y) = (x, y);

            public bool Equals(PointWithConverter other) => X == other.X && Y == other.Y;

            public override bool Equals(object obj) => obj is PointWithConverter other && Equals(other);

            public override int GetHashCode() => unchecked((X * 397) ^ Y);
        }

        sealed class PointConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
                sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
                destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
                value is string text ? FromText(text) : default;

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
                destinationType == typeof(string) && value is PointWithConverter pwc ?
                    $"{pwc.X};{pwc.Y}" :
                    base.ConvertTo(context, culture, value, destinationType);

            private static PointWithConverter FromText(string text) =>
                text.Split(';') is { } arr && arr.Length == 2
                    ? new PointWithConverter(int.Parse(arr[0]), int.Parse(arr[1]))
                    : default;
        }

        [TypeConverter(typeof(BadPointConverter))]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        struct PointWithBadConverter
        {
            public int X { get; }
            public int Y { get; }

            public PointWithBadConverter(int x, int y) => (X, Y) = (x, y);
        }

        sealed class BadPointConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
                sourceType != typeof(string);

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
                destinationType != typeof(string);
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        struct PointWithoutConverter
        {
            public int X { get; }
            public int Y { get; }

            public PointWithoutConverter(int x, int y) => (X, Y) = (x, y);
        }
    }
}
