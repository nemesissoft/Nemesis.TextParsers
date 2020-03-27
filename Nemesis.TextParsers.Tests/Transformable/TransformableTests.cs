using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests.Transformable
{
    [TestFixture]
    class TransformableTests
    {
        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            new TCD(new ParsleyAndLeekFactors(100, new float[]{200, 300, 400}), @"100;200,300,400"),
            new TCD(new ParsleyAndLeekFactors(1000, new float[]{2}), @"1000;2"),
            new TCD(new ParsleyAndLeekFactors(10, new[] { 20.0f, 30.0f }), @""), //overriden
            new TCD(new ParsleyAndLeekFactors(0, new[] { 0f, 0f }), null), //overriden
            new TCD(new ParsleyAndLeekFactors(555.5f, null), @"555.5;∅"), 
            new TCD(new ParsleyAndLeekFactors(666.6f, new float[0]), @"666.6;"),
        };

        [TestCaseSource(nameof(CorrectData))]
        public void Transformable_ParseAndFormat(ParsleyAndLeekFactors instance, string text)
        {
            var sut = TextTransformer.Default.GetTransformer<ParsleyAndLeekFactors>();
            
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
