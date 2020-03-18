using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;
using Nemesis.Essentials.Design;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Tests
{

    //TODO change priority with Any trans 
    //TODO add TypeConverter with deconstruct 

    /*TODO
    
tests - more than 8 params
recursive tests (,,, (,)) + test for no borders 

    static Deconstruct(this IInterface, ...) convariance va contravariance 

exploratory tests 
    
    MetaTransformer.GetByProperties

    test escaping sequences 
    test- start and end with same character. 
    with (', null) sequence. 
    with '' empty tuple. 
    with empty text with no border. 
    with no border and trailing/leading spaces

        test all thrown exceptions
update to https://www.nuget.org/packages/Microsoft.SourceLink.GitHub/
    */
    [TestFixture]
    class DeconstructableTests
    {
        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            new TCD(typeof(CarrotAndOnionFactors),
                    new CarrotAndOnionFactors(123.456789M, new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, TimeSpan.Parse("12:34:56")),
                    @"(123.456789;1|2|3|3.14;12:34:56)"),
            new TCD(typeof(Address), new Address("Wrocław", 52200), @"(Wrocław;52200)"),
            new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200))"),
        };

        [TestCaseSource(nameof(CorrectData))]
        public void ParseAndFormat(Type type, object instance, string text)
        {
            var tester = Method.OfExpression<Action<object, string, Func<Sett, Sett>>>(
                (i, t, m) => ParseAndFormatHelper(i, t, m)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(type);

            tester.Invoke(null, new[] { instance, text, null });
        }

        private static void ParseAndFormatHelper<TDeconstructable>(TDeconstructable instance, string text, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>();

            var actualParsed1 = sut.ParseFromText(text);
            IsMutuallyEquivalent(actualParsed1, instance);


            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed2 = sut.ParseFromText(actualFormatted);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        /*[Test]
        public void ParseAndFormat2433434()
        {
            var settings = Sett.Default;

            var instance = new Address("Wrocław", 52200);
            var text = @"(Wrocław;52200)";

            var sut = settings.ToTransformer<Address>();

            var actualParsed1 = sut.ParseFromText(text);
            IsMutuallyEquivalent(actualParsed1, instance);


            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed2 = sut.ParseFromText(actualFormatted);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }*/


        //new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław;52200))"),
        //new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław);52200))"),
        //new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200);123))"),
        [Test]
        public void NegativeTest()
        {

        }


        private static void IsMutuallyEquivalent(object o1, object o2)
        {
            o1.Should().BeEquivalentTo(o2);
            o2.Should().BeEquivalentTo(o1);
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        struct CarrotAndOnionFactors : IEquatable<CarrotAndOnionFactors>
        {
            public decimal Carrot { get; }
            public float[] OnionFactors { get; }
            public TimeSpan Time { get; }

            public CarrotAndOnionFactors(decimal carrot, float[] onionFactors, TimeSpan time)
            {
                Carrot = carrot;
                OnionFactors = onionFactors;
                Time = time;
            }

            public void Deconstruct(out decimal carrot, out float[] onionFactors, out TimeSpan time)
            {
                carrot = Carrot;
                onionFactors = OnionFactors;
                time = Time;
            }

            public bool Equals(CarrotAndOnionFactors other) =>
                Carrot.Equals(other.Carrot) &&
                Time.Equals(other.Time) &&
                EnumerableEqualityComparer<float>.DefaultInstance.Equals(OnionFactors, other.OnionFactors);

            public override bool Equals(object obj) => !(obj is null) && obj is CarrotAndOnionFactors other && Equals(other);

            public override int GetHashCode() => unchecked(
                (Carrot.GetHashCode() * 397) ^
                (Time.GetHashCode() * 397) ^
                (OnionFactors?.GetHashCode() ?? 0)
            );
        }

        struct Person
        {
            public string Name { get; }
            public int Age { get; }
            public Address Address { get; }

            public Person(string name, int age, Address address)
            {
                Name = name;
                Age = age;
                Address = address;
            }

            public void Deconstruct(out string name, out int age, out Address address)
            {
                name = Name;
                age = Age;
                address = Address;
            }
        }

        struct Address
        {
            public string City { get; }
            public int ZipCode { get; }

            public Address(string city, int zipCode)
            {
                City = city;
                ZipCode = zipCode;
            }

            public void Deconstruct(out string city, out int zipCode)
            {
                city = City;
                zipCode = ZipCode;
            }
        }

        public struct DataWithNoDeconstruct
        {
            public string Name { get; }
            public int Number { get; }
            public float Fraction { get; }

            public DataWithNoDeconstruct(string name, int number, float fraction)
            {
                Name = name;
                Number = number;
                Fraction = fraction;
            }
        }
    }

    internal static class DataWithNoDeconstructExt
    {
        public static void Deconstruct(this DeconstructableTests.DataWithNoDeconstruct d, out string name, out int number, out float fraction)
        {
            name = d.Name;
            number = d.Number;
            fraction = d.Fraction;
        }

        delegate void DeCtorAction(DeconstructableTests.DataWithNoDeconstruct d, out string name, out int number, out float fraction);

        public static readonly MethodInfo DeconstructMethod = Method.Of<DeCtorAction>(Deconstruct);

        public static readonly ConstructorInfo Constructor = Ctor
            .Of(() => new DeconstructableTests.DataWithNoDeconstruct(default, default, default));
    }
}
