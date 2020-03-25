using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{

    /*TODO
    perf tests
    //TODO perf test class vs readonly struct 
recursive tests (,,, (,)) + test for no borders 

    static Deconstruct(this IInterface, ...) convariance va contravariance 

exploratory tests 
    
    MetaTransformer.GetByProperties

    test different escaping sequences +  null marker + Delimiter
        address : (Wrocław);52200)   - success where ) is pars of string  
        
    with (', null) sequence. 
    with '' empty tuple. 
    with empty text with no border. 
    with no border and trailing/leading spaces


        
        test all thrown exceptions in DecoTrans
        test TupleHelper +test all thrown exceptions
update to https://www.nuget.org/packages/Microsoft.SourceLink.GitHub/
    */
    [TestFixture]
    internal class DeconstructableTests
    {
        private static readonly ITransformerStore _transformerStore = TextTransformer.Default;

        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            new TCD(typeof(CarrotAndOnionFactors),
                    new CarrotAndOnionFactors(123.456789M, new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, TimeSpan.Parse("12:34:56")),
                    @"(123.456789;1|2|3|3.14;12:34:56)"),
            new TCD(typeof(Address), new Address("Wrocław", 52200), @"(Wrocław;52200)"),
            new TCD(typeof(Address), new Address("A", 1), @"(A;1)"),
            new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200))"),

            new TCD(typeof(LargeStruct), LargeStruct.Sample, @"(3.14159265;2.718282;-123456789;123456789;-1234;1234;127;-127;-4611686018427387904;9223372036854775807;23.14069263;123456789012345678901234567890;(3.14159265\; 2.71828183))"),

        };

        [TestCaseSource(nameof(CorrectData))]
        public void ParseAndFormat(Type type, object instance, string text)
        {
            var tester = Method.OfExpression<Action<object, string, Func<Sett, Sett>>>(
                (i, t, m) => FormatAndParseHelper(i, t, m)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(type);

            tester.Invoke(null, new[] { instance, text, null });
        }

        private static void FormatAndParseHelper<TDeconstructable>(TDeconstructable instance, string text, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed1 = sut.Parse(text);
            var actualParsed2 = sut.Parse(actualFormatted);
            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        private static void ParseHelper<TDeconstructable>(TDeconstructable expected, string input, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualParsed1 = sut.Parse(input);
            var formatted1 = sut.Format(actualParsed1);
            var actualParsed2 = sut.Parse(formatted1);


            IsMutuallyEquivalent(actualParsed1, expected);
            IsMutuallyEquivalent(actualParsed2, expected);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        [Test]
        public void ParseAndFormat_CustomDeconstruct() => FormatAndParseHelper(
                new DataWithNoDeconstruct("Mike", "Wrocław", 36, 3.14), @"{Mike;Wrocław;36;3.14}",
                s => s.WithBorders('{', '}').WithCustomDeconstruction(DataWithNoDeconstructExt.DeconstructMethod,
                    DataWithNoDeconstructExt.Constructor));

        [Test]
        public void ParseAndFormat_NoBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C",
                s => s.WithoutBorders());

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @" A;B;C ",
                s => s.WithoutBorders());
        }

        [Test]
        public void ParseAndFormat_SameBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C/",
                s => s.WithBorders('/', '/'));

            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    /A;B;C/    ",
                s => s.WithBorders('/', '/'));


            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    AA;B;CA    ",
                s => s.WithBorders('A', 'A'));
        }

        [Test]
        public void ParseAndFormat_MixedBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C",
                s => s.WithoutBorders().WithStart('/')
                );

            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
                );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C"), @" A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
            );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @"/ A;B;C ",
                s => s.WithoutBorders().WithStart('/')
            );
        }

        [Test]
        public void Custom_ConventionTransformer_BasedOnDeconstructable()
        {
            var sut = _transformerStore.GetTransformer<DataWithCustomDeconstructableTransformer>();
            var instance = new DataWithCustomDeconstructableTransformer(3.14f, false);
            var text = @"{3.14_False}";

            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed1 = sut.Parse(text);
            var actualParsed2 = sut.Parse(actualFormatted);
            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        [TestCase(@"(Mike;36;(Wrocław;52200))", typeof(ArgumentException), "These requirements were not met in:'(Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław);52200))", typeof(ArgumentException), "2nd element was not found after 'Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław\;52200);123))", typeof(ArgumentException), "cannot have more than 3 elements: '123)'")]
        public void Parse_NegativeTest(string wrongInput, Type expectedException, string expectedMessagePart)
        {
            var sut = Sett.Default
                .ToTransformer<Person>(_transformerStore);

            bool passed = false;
            Person parsed = default;
            try
            {
                parsed = sut.Parse(wrongInput);
                passed = true;
            }
            catch (Exception actual)
            {
                AssertException(actual, expectedException, expectedMessagePart);
            }
            if (passed)
                Assert.Fail($"'{wrongInput}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }


        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private struct CarrotAndOnionFactors : IEquatable<CarrotAndOnionFactors>
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

            [UsedImplicitly]
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

        private struct Person
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

            [UsedImplicitly]
            public void Deconstruct(out string name, out int age, out Address address)
            {
                name = Name;
                age = Age;
                address = Address;
            }
        }

        private readonly struct Address
        {
            public string City { get; }
            public int ZipCode { get; }

            public Address(string city, int zipCode)
            {
                City = city;
                ZipCode = zipCode;
            }

            [UsedImplicitly]
            public void Deconstruct(out string city, out int zipCode)
            {
                city = City;
                zipCode = ZipCode;
            }
        }

        internal struct DataWithNoDeconstruct
        {
            public string Name { get; }
            public string City { get; }
            public int Number { get; }
            public double Fraction { get; }

            public DataWithNoDeconstruct(string name, string city, int number, double fraction)
            {
                Name = name;
                City = city;
                Number = number;
                Fraction = fraction;
            }
        }

        private struct LargeStruct
        {
            public double N1 { get; }
            public float N2 { get; }
            public int N3 { get; }
            public uint N4 { get; }
            public short N5 { get; }
            public ushort N6 { get; }
            public byte N7 { get; }
            public sbyte N8 { get; }
            public long N9 { get; }
            public ulong N10 { get; }
            public decimal N11 { get; }
            public BigInteger N12 { get; }
            public Complex N13 { get; }

            // ReSharper disable once MemberCanBePrivate.Local
            public LargeStruct(double n1, float n2, int n3, uint n4, short n5, ushort n6, byte n7, sbyte n8,
                long n9, ulong n10, decimal n11, BigInteger n12, Complex n13)
            {
                N1 = n1;
                N2 = n2;
                N3 = n3;
                N4 = n4;
                N5 = n5;
                N6 = n6;
                N7 = n7;
                N8 = n8;
                N9 = n9;
                N10 = n10;
                N11 = n11;
                N12 = n12;
                N13 = n13;
            }

            [UsedImplicitly]
            public void Deconstruct(out double n1, out float n2, out int n3, out uint n4, out short n5, out ushort n6,
                out byte n7, out sbyte n8, out long n9, out ulong n10, out decimal n11, out BigInteger n12, out Complex n13)
            {
                n1 = N1;
                n2 = N2;
                n3 = N3;
                n4 = N4;
                n5 = N5;
                n6 = N6;
                n7 = N7;
                n8 = N8;
                n9 = N9;
                n10 = N10;
                n11 = N11;
                n12 = N12;
                n13 = N13;
            }

            public static readonly LargeStruct Sample = new LargeStruct(
                Math.Round(Math.PI, 8), (float)Math.Round(Math.E, 6),
                -123456789, 123456789, -1234, 1234, 127, -127, long.MinValue / 2, ulong.MaxValue / 2,
                Math.Round((decimal)Math.Pow(Math.E, Math.PI), 8),
                BigInteger.Parse("123456789012345678901234567890"), new Complex(Math.Round(Math.PI, 8), Math.Round(Math.E, 8)));
        }

        private readonly struct ThreeStrings
        {
            public string A { get; }
            public string B { get; }
            public string C { get; }

            public ThreeStrings(string a, string b, string c)
            {
                A = a;
                B = b;
                C = c;
            }

            [UsedImplicitly]
            public void Deconstruct(out string a, out string b, out string c)
            {
                a = A;
                b = B;
                c = C;
            }
        }

        readonly struct DataWithCustomDeconstructableTransformer
        {
            public float Number { get; }
            public bool IsEnabled { get; }

            public DataWithCustomDeconstructableTransformer(float number, bool isEnabled)
            {
                Number = number;
                IsEnabled = isEnabled;
            }

            [UsedImplicitly]
            public void Deconstruct(out float number, out bool isEnabled)
            {
                number = Number;
                isEnabled = IsEnabled;
            }

            //create custom deconstructable transformer  
            private static readonly ITransformer<DataWithCustomDeconstructableTransformer> _transformer =
                Sett.Default
                    .WithBorders('{', '}')
                    .WithDelimiter('_')
                    .ToTransformer<DataWithCustomDeconstructableTransformer>(_transformerStore);


            [UsedImplicitly]
            public static DataWithCustomDeconstructableTransformer FromText(string text) => _transformer.Parse(text);
            public override string ToString() => _transformer.Format(this);
        }
    }

    internal static class DataWithNoDeconstructExt
    {
        public static void Deconstruct(this DeconstructableTests.DataWithNoDeconstruct d, out string name, out string city, out int number, out double fraction)
        {
            name = d.Name;
            city = d.City;
            number = d.Number;
            fraction = d.Fraction;
        }

        delegate void DeCtorAction(DeconstructableTests.DataWithNoDeconstruct d, out string name, out string city, out int number, out double fraction);

        public static readonly MethodInfo DeconstructMethod = Method.Of<DeCtorAction>(Deconstruct);

        public static readonly ConstructorInfo Constructor = Ctor
            .Of(() => new DeconstructableTests.DataWithNoDeconstruct(default, default, default, default));
    }
}
