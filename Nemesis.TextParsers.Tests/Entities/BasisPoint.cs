using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Transformable;
using Nemesis.TextParsers.Utils;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;
using TCD = NUnit.Framework.TestCaseData;

namespace Nemesis.TextParsers.Tests.Entities
{
    /// <summary>
    /// Represents BasisPoint. 1bs is 1/100th of 1%
    /// </summary>
    [Transformer(typeof(BasisPointTransformer))]
    [DebuggerDisplay("{" + nameof(BpsValue) + "} bps")]
    public readonly struct BasisPoint : IEquatable<BasisPoint>
    {
        private const double BPS_FACTOR = 10_000d;

        public double RealValue { get; }
        internal double BpsValue => RealValue * BPS_FACTOR;

        public BasisPoint(double realValue) => RealValue = realValue;

        public static BasisPoint FromBps(double bps) => new BasisPoint(bps / BPS_FACTOR);


        #region Equals
        public bool Equals(BasisPoint other) =>
            Math.Abs(RealValue - other.RealValue) < 1E-08;

        public override bool Equals(object obj) => obj is BasisPoint other && Equals(other);

        public override int GetHashCode() => RealValue.GetHashCode();
        #endregion
    }

    //TODO
    [TextConverterSyntax("")]
    internal sealed class BasisPointTransformer : TransformerBase<BasisPoint>
    {
        protected override BasisPoint ParseCore(in ReadOnlySpan<char> input)
        {
            var text = input.Trim();
            var length = text.Length;

            bool IsCharEqual(ReadOnlySpan<char> t, int fromEnd, char upperExpectedChar)
            {
#if DEBUG
                Debug.Assert(char.IsUpper(upperExpectedChar), $"'{upperExpectedChar}' is not in upper case");
                Debug.Assert(fromEnd <= length, $"NOT fromEnd<=length for {fromEnd} <= {length}");
#endif
                return t[length - fromEnd] is { } c &&
                       char.ToUpperInvariant(c) == upperExpectedChar;

            }

            if (length > 3 &&
                IsCharEqual(text, 3, 'B') &&
                IsCharEqual(text, 2, 'P') &&
                IsCharEqual(text, 1, 'S')
               )
                text = text.Slice(0, length - 3);
            else if (length > 2 &&
                     IsCharEqual(text, 2, 'B') &&
                     IsCharEqual(text, 1, 'P')
                    )
                text = text.Slice(0, length - 2);


            var bps = DoubleParser.Instance.Parse(text);
            return BasisPoint.FromBps(bps);
        }

        public override string Format(BasisPoint bp) =>
            FormattableString.Invariant($"{bp.BpsValue:N2} bps");
    }

    [TestFixture]
    public class BasisPointTests
    {
        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            //TODO negative, 0.123456789, 0, 1000, 1000000000 + exploratory tests
            new TCD(BasisPoint.FromBps(1), "1 bp"),
            new TCD(BasisPoint.FromBps(2), "2 bps"),
            new TCD(BasisPoint.FromBps(-1), "-1 bps"),
            new TCD(BasisPoint.FromBps(100), "100"),
            //TODO change precision to 6 places
            //new TCD(BasisPoint.FromBps(Math.PI), "3.141592653589793bpS"),
            //new TCD(BasisPoint.FromBps(Math.E), "2.718281828459045 bPS"),
            new TCD(BasisPoint.FromBps(10.15), "10.15 BpS"),
        };

        [TestCaseSource(nameof(CorrectData))]
        public void ParseAndFormat(BasisPoint instance, string text)
        {
            var sut = TextTransformer.Default.GetTransformer<BasisPoint>();

            var actualParsed1 = sut.Parse(text);

            string formattedInstance = sut.Format(instance);
            string formattedActualParsed = sut.Format(actualParsed1);
            Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

            var actualParsed2 = sut.Parse(formattedInstance);

            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

    }
}
