using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Nemesis.TextParsers.Parsers;
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
            var sut = Sut.GetTransformer<PointWithConverter>();


            var actualTexts = data.Select(pwc => sut.Format(pwc)).ToList();
            var actual = actualTexts.Select(text => sut.Parse(text)).ToList();


            Assert.That(actual, Is.EqualTo(data));
        }

        [Test]
        public void CorrectUsage_Dict()
        {
            IDictionary<PointWithConverter, int> data = Enumerable.Range(1, 9)
                .Select(i => new PointWithConverter(i * 11, i * 100))
                .ToDictionary(p => p, p => p.X + p.Y);
            var sut = Sut.GetTransformer<IDictionary<PointWithConverter, int>>();


            var actualText = sut.Format(data);
            var actual = sut.Parse(actualText);


            Assert.That(actual, Is.EqualTo(data));
        }

        [Test]
        public void BadConverter_NegativeTest()
        {
            var conv = TypeDescriptor.GetConverter(typeof(PointWithBadConverter));
            Assert.That(conv, Is.TypeOf<BadPointConverter>());

            Assert.That(
                Sut.GetTransformer<PointWithBadConverter>,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo("Type 'PointWithBadConverter' is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
                );
        }

        [Test]
        public void NoConverter_NegativeTest()
        {
            var sut = new TypeConverterTransformerCreator();

            Assert.That(
                () => sut.CreateTransformer<PointWithoutConverter>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'PointWithoutConverter' is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
                );

            Assert.That(
                () => sut.CreateTransformer<object>(),
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'object' is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
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
                Sut.GetTransformer<PointWithoutConverter>,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'PointWithoutConverter' is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
            );

            Assert.That(
                Sut.GetTransformer<object>,
                Throws.TypeOf<NotSupportedException>()
                    .With.Message.EqualTo(@"Type 'object' is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
            );
        }
    }
}
