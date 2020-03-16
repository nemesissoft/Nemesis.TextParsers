using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NUnit.Framework;
using JetBrains.Annotations;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class UniversalTextTypeConverterTests
    {
        private static IEnumerable<(ITextBasedObject, string)> Correct_Data() => new (ITextBasedObject, string)[]
        {
            (new CommaSeparated(3.14, 15), "3.14,15"),
            (new CommaSeparated(-50.555555, 111111111), "-50.555555,111111111"),
        };

        [TestCaseSource(nameof(Correct_Data))]
        public void CanTransformTextIntoObjects((ITextBasedObject instance, string text) data)
        {
            var converter = TypeDescriptor.GetConverter(data.instance.GetType());

            var newText = converter.ConvertToInvariantString(data.instance);
            var newInstance = converter.ConvertFromInvariantString(data.text);
            var newText2 = converter.ConvertToInvariantString(newInstance ?? "");

            Assert.That(newInstance, Is.EqualTo(data.instance));
            Assert.That(newText, Is.EqualTo(data.text));
            Assert.That(newText, Is.EqualTo(newText2));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
        public interface ITextBasedObject
        {
            string ToText();
            double Value1 { get; }
            int Value2 { get; }
        }

        [TypeConverter(typeof(UniversalTextTypeConverter<CommaSeparated>))]
        struct CommaSeparated : ITextBasedObject
        {
            public double Value1 { get; }
            public int Value2 { get; }

            public CommaSeparated(double value1, int value2)
            {
                Value1 = value1;
                Value2 = value2;
            }

            public override string ToString() => FormattableString.Invariant($"{Value1},{Value2}");

            [UsedImplicitly]
            public static CommaSeparated FromText(string text) =>
                text.Split(',') is var array && array.Length == 2
                    ? new CommaSeparated(
                        double.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture),
                        int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture)
                        )
                    : default;

            public string ToText() => ToString();

            private bool Equals(CommaSeparated other) => Value1.Equals(other.Value1) && Value2 == other.Value2;

            public override bool Equals(object obj) => obj is CommaSeparated other && Equals(other);

            public override int GetHashCode() => unchecked((Value1.GetHashCode() * 397) ^ Value2);
        }

    }
}
