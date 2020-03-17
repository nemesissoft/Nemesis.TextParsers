using System;
using System.Diagnostics.CodeAnalysis;
using Nemesis.Essentials.Design;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class DeconstructableTests
    {
        [Test]
        public void CorrectUsage_Parse()
        {
            var sut = TextTransformer.Default.GetTransformer<CarrotAndOnionFactors>();

            var actual = sut.ParseFromText("(123.456789, 1|2|3|3.14, 12:34:56)");

            var expected = new CarrotAndOnionFactors(
                123.456789M,
                new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, 
                TimeSpan.Parse("12:34:56")
            );
            Assert.That(actual, Is.EqualTo(expected));
        }


        [Test]
        public void NegativeTest()
        {

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
    }
}
