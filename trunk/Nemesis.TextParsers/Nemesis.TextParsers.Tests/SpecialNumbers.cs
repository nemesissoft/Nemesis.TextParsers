﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    struct LowPrecisionFloat : IEquatable<LowPrecisionFloat>
    {
        public float Value { get; }

        public LowPrecisionFloat(float value) => Value = value;

        public bool Equals(LowPrecisionFloat other) => AlmostEqualUlps(Value, other.Value);

        public override bool Equals(object obj) => !(obj is null) && obj is LowPrecisionFloat other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(LowPrecisionFloat left, LowPrecisionFloat right) => left.Equals(right);

        public static bool operator !=(LowPrecisionFloat left, LowPrecisionFloat right) => !left.Equals(right);

        public override string ToString() => Value.ToString("G17", CultureInfo.InvariantCulture);

        [UsedImplicitly]
        public static LowPrecisionFloat FromText(string text) => new LowPrecisionFloat(
            float.Parse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture)
                );

        private static bool AlmostEqualUlps(float a, float b, uint maxUlpsDiff = 100 * 1024)
        {
            uint aNum = Unsafe.As<float, uint>(ref a);
            uint bNum = Unsafe.As<float, uint>(ref b);

            bool aNegative = (aNum >> 31) == 1;
            bool bNegative = (bNum >> 31) == 1;

            // Different signs means they do not match.
            if (aNegative != bNegative)
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return a == b; // Check for equality to make sure +0==-0

            // Find the difference in ULPs.
            long ulpsDiff = Math.Abs((long)aNum - bNum);
            return ulpsDiff <= maxUlpsDiff;
        }
    }

    struct CarrotAndOnionFactors : IEquatable<CarrotAndOnionFactors>
    {
        public float Carrot { get; }
        public float[] OnionFactors { get; }

        public CarrotAndOnionFactors(float carrot, [NotNull] float[] onionFactors)
        {
            Carrot = carrot;
            OnionFactors = onionFactors ?? throw new ArgumentNullException(nameof(onionFactors));
        }

        public bool Equals(CarrotAndOnionFactors other) => Carrot.Equals(other.Carrot) && OnionFactors.SequenceEqual(other.OnionFactors);

        public override bool Equals(object obj) => !(obj is null) && obj is CarrotAndOnionFactors other && Equals(other);

        public override int GetHashCode() => unchecked((Carrot.GetHashCode() * 397) ^ (OnionFactors?.GetHashCode() ?? 0));

        public static bool operator ==(CarrotAndOnionFactors left, CarrotAndOnionFactors right) => left.Equals(right);

        public static bool operator !=(CarrotAndOnionFactors left, CarrotAndOnionFactors right) => !left.Equals(right);

        public override string ToString() => FormattableString.Invariant(
            $"{Carrot:G9};{string.Join(",", OnionFactors.Select(of => of.ToString("G9", CultureInfo.InvariantCulture)))}"
            );

        [UsedImplicitly]
        public static CarrotAndOnionFactors FromText(string text)
        {
            var stream = text.AsSpan().Split(';').GetEnumerator();
            var floatParser = TextTransformer.Default.GetTransformer<float>();

            if (!stream.MoveNext()) throw new FormatException($"At least one element is expected to parse {nameof(CarrotAndOnionFactors)}");
            var carrot = floatParser.Parse(stream.Current);

            byte onionCount = 0;
            Span<float> onionFactors = stackalloc float[10];

            if (stream.MoveNext())
            {
                var onionStream = stream.Current.Split(',').GetEnumerator();
                while (onionStream.MoveNext())
                {
                    float onion = floatParser.Parse(onionStream.Current);
                    onionFactors[onionCount++] = onion;
                }
            }

            return new CarrotAndOnionFactors(carrot, onionFactors.Slice(0,onionCount).ToArray());
        }
    }

    [TestFixture]
    public class SpecialNumbersTests
    {
        private readonly ITransformer<LowPrecisionFloat> _sut = TextTransformer.Default.GetTransformer<LowPrecisionFloat>();

        private static IEnumerable<(string, float)> ValidValuesForFactory()
            => new[]
            {
                ("3.1415", 3.1415f),
                ("0.000000000001", 0.000000000001f),
                ("-0.000000000001", -0.000000000001f),
            };

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void LowPrecisionFloat_FromText_ShouldParse((string inputText, float expected) data)
        {
            LowPrecisionFloat actual = _sut.Parse(data.inputText.AsSpan());

            Assert.That(actual.Value, Is.EqualTo(data.expected).Within(3).Ulps);
            Assert.That(actual, Is.EqualTo(new LowPrecisionFloat(data.expected)));
        }

        [Test]
        public void LowPrecisionFloat_ParseList()
        {
            var actual = SpanCollectionSerializer.DefaultInstance.ParseCollection<LowPrecisionFloat>("3.14|1|2|3.0005");

            Assert.That(actual, Has.Count.EqualTo(4));
            Assert.That(actual.Select(lpf => lpf.Value), Is.EquivalentTo(new[] { 3.14f, 1f, 2f, 3.0005f }));
        }


        [Test]
        public void CarrotAndOnionFactors_ParseList()
        {
            var list = new List<CarrotAndOnionFactors>()
            {
                new CarrotAndOnionFactors(1.1f, new[]{2.1f, 3.1f, 4.1f}),
                new CarrotAndOnionFactors(10.1f, new[]{20.1f, 30.1f, 40.1f}),
            };

            var formatted = SpanCollectionSerializer.DefaultInstance.FormatCollection(list);
            Assert.That(formatted, Is.EqualTo("1.10000002;2.0999999,3.0999999,4.0999999|10.1000004;20.1000004,30.1000004,40.0999985"));

            var parsed = SpanCollectionSerializer.DefaultInstance.ParseCollection<CarrotAndOnionFactors>(formatted);

            Assert.That(parsed, Has.Count.EqualTo(2));
            Assert.That(parsed, Is.EquivalentTo(list));
        }
    }
}
