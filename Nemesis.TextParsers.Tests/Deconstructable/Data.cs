using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests.Deconstructable
{
    internal struct CarrotAndOnionFactors : IEquatable<CarrotAndOnionFactors>
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

    internal struct Person
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

    internal readonly struct Address
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

    internal interface IDeconstructable
    {
        float Weight { get; }
        decimal Price { get; }
    }

    internal readonly struct ExternallyDeconstructable : IDeconstructable
    {
        public float Weight { get; }
        public decimal Price { get; }

        public ExternallyDeconstructable(float weight, decimal price)
        {
            Weight = weight;
            Price = price;
        }
    }

    internal struct LargeStruct
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

    internal readonly struct ThreeStrings
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

    [Transformer(typeof(DeconstructableTransformer))]
    internal readonly struct DataWithCustomDeconstructableTransformer : IEquatable<DataWithCustomDeconstructableTransformer>
    {
        public float Number { get; }
        public bool IsEnabled { get; }
        public decimal[] Prices { get; }

        public DataWithCustomDeconstructableTransformer(float number, bool isEnabled, decimal[] prices)
        {
            Number = number;
            IsEnabled = isEnabled;
            Prices = prices;
        }

        [UsedImplicitly]
        public void Deconstruct(out float number, out bool isEnabled, out decimal[] prices)
        {
            number = Number;
            isEnabled = IsEnabled;
            prices = Prices;
        }

        public bool Equals(DataWithCustomDeconstructableTransformer other) =>
            Number.Equals(other.Number) && IsEnabled == other.IsEnabled &&
            EnumerableEqualityComparer<decimal>.DefaultInstance.Equals(Prices, other.Prices);

        public override bool Equals(object obj) =>
            obj is DataWithCustomDeconstructableTransformer other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Number.GetHashCode();
                hashCode = (hashCode * 397) ^ IsEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (Prices != null ? Prices.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    internal class DeconstructableTransformer : CustomDeconstructionTransformer<DataWithCustomDeconstructableTransformer>
    {
        public DeconstructableTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

        protected override DeconstructionTransformerSettings BuildSettings(DeconstructionTransformerSettings prototype)
            => prototype
                .WithBorders('{', '}')
                .WithDelimiter('_')
                .WithNullElementMarker('␀')
                .WithDeconstructableEmpty() //default value but just to be clear 
        ;

        public override DataWithCustomDeconstructableTransformer GetNull() =>
            new DataWithCustomDeconstructableTransformer(666, true, new decimal[] { 6, 7, 8, 9 });
    }


    internal static class DataWithNoDeconstructExt
    {
        public static void Deconstruct(this DataWithNoDeconstruct d, out string name, out string city, out int number, out double fraction)
        {
            name = d.Name;
            city = d.City;
            number = d.Number;
            fraction = d.Fraction;
        }

        private delegate void DeCtorAction(DataWithNoDeconstruct d, out string name, out string city, out int number, out double fraction);

        public static readonly MethodInfo DeconstructMethod = Method.Of<DeCtorAction>(Deconstruct);

        public static readonly ConstructorInfo Constructor = Ctor
            .Of(() => new DataWithNoDeconstruct(default, default, default, default));
    }

    internal static class ExternallyDeconstructableExt
    {
        public static void Deconstruct(this IDeconstructable deco, out float weight, out decimal price)
        {
            weight = deco.Weight;
            price = deco.Price;
        }

        private delegate void DeCtorAction(IDeconstructable d, out float weight, out decimal price);

        public static readonly MethodInfo DeconstructMethod = Method.Of<DeCtorAction>(Deconstruct);

        public static readonly ConstructorInfo Constructor = Ctor
            .Of(() => new ExternallyDeconstructable(default, default));
    }

    #region Recursive

    internal class King : IEquatable<King>
    {
        public string Name { get; }
        public King Successor { get; }

        public King(string name, King successor)
        {
            Name = name;
            Successor = successor;
        }

        public King(string name, IReadOnlyList<string> names)
        {
            Name = name;

            King successor = null;
            if (names != null && names.Count > 0)
                for (int i = names.Count - 1; i >= 0; i--)
                    successor = successor == null ? new King(names[i], (King)null) : new King(names[i], successor);

            Successor = successor;
        }

        public void Deconstruct(out string name, out IReadOnlyList<string> names)
        {
            name = Name;

            var retNames = new List<string>();

            var child = Successor;
            while (child != null)
            {
                retNames.Add(child.Name);
                child = child.Successor;
            }

            names = retNames;
        }

        public bool Equals(King other) =>
            !(other is null) &&
            (ReferenceEquals(this, other) || Name == other.Name && Equals(Successor, other.Successor));

        public override bool Equals(object obj) =>
            !(obj is null) && (ReferenceEquals(this, obj) || obj is King h && Equals(h));

        public override int GetHashCode() =>
            unchecked(((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Successor != null ? Successor.GetHashCode() : 0));

        public override string ToString() => $"{Name}, ({Successor})";
    }

    internal readonly struct Planet
    {
        public string Name { get; }
        public long Population { get; }
        public Country Country { get; }

        public Planet(string name, long population, Country country)
        {
            Name = name;
            Population = population;
            Country = country;
        }

        public void Deconstruct(out string name, out long population, out Country country)
        {
            name = Name;
            population = Population;
            country = Country;
        }
    }

    [Transformer(typeof(CountryTransformer))]
    internal readonly struct Country
    {
        public string Name { get; }
        public int Population { get; }
        public Region Region { get; }

        public Country(string name, int population, Region region)
        {
            Name = name;
            Population = population;
            Region = region;
        }

        public void Deconstruct(out string name, out int population, out Region region)
        {
            name = Name;
            population = Population;
            region = Region;
        }
    }

    [Transformer(typeof(RegionTransformer))]
    internal readonly struct Region
    {
        public string Name { get; }
        public int Population { get; }
        public City City { get; }

        public Region(string name, int population, City city)
        {
            Name = name;
            Population = population;
            City = city;
        }

        public void Deconstruct(out string name, out int population, out City city)
        {
            name = Name;
            population = Population;
            city = City;
        }
    }

    internal readonly struct City
    {
        public string Name { get; }
        public int Population { get; }

        public City(string name, int population)
        {
            Name = name;
            Population = population;
        }

        public void Deconstruct(out string name, out int population)
        {
            name = Name;
            population = Population;
        }
    }

    internal class CountryTransformer : CustomDeconstructionTransformer<Country>
    {
        public CountryTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

        protected override DeconstructionTransformerSettings BuildSettings(DeconstructionTransformerSettings prototype)
            => prototype
                .WithBorders('[', ']')
                .WithDelimiter(',')
        ;
    }

    internal class RegionTransformer : CustomDeconstructionTransformer<Region>
    {
        public RegionTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

        protected override DeconstructionTransformerSettings BuildSettings(DeconstructionTransformerSettings prototype)
            => prototype
                .WithBorders('{', '}')
                .WithDelimiter('_')
        ;
    }

    #endregion

}
