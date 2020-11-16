using System;

using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

using NUnit.Framework;
#if PRE_NULLABLES
using NotNull = JetBrains.Annotations.NotNullAttribute;
#else
using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#endif


namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class Records_AutomaticTransformation
    {
        [TestCase(typeof(Vertebrate), "(Agama)")]
        [TestCase(typeof(ReptileWithName), "(Agama;Terrestrial)")]
        [TestCase(typeof(TerrestrialLizard), "(Carnivorous)")]
        public void ShouldBeAbleToFormat(Type contractType, string expectedResult)
        {
            var reptile = new TerrestrialLizard(Diet.Carnivorous);
            reptile = reptile with { Name = "Agama" };

            var actual = Sut.GetTransformer(contractType).FormatObject(reptile);
            
            Assert.That(actual, Is.EqualTo(expectedResult));
        }

        [Test]
        public void ShouldBeAbleToReparseComplexRecords()
        {
            var collector = new AnimalCollector("Mike", 36, new[]
            {
                new ReptileWithName("Comodo Dragon", Habitat.Terrestrial),
                new TerrestrialLizard(Diet.Carnivorous)
            });
            var sut = Sut.GetTransformer<AnimalCollector>();


            var formatted = sut.Format(collector);

            Assert.That(formatted, Is.EqualTo("(Mike;36;(Comodo Dragon)|(Lizard))"));

            var parsed = sut.Parse(formatted);
            Assert.That(parsed.Name, Is.EqualTo("Mike"));
            Assert.That(parsed.Age, Is.EqualTo(36));
            Assert.That(parsed.Animals[0].Name, Is.EqualTo("Comodo Dragon"));
            Assert.That(parsed.Animals[1].Name, Is.EqualTo("Lizard"));
        }

        enum Habitat { Terrestrial, Aquatic, Amphibian }

        enum Diet { Carnivorous, Herbivorous, Omnivorous }

        record Vertebrate(string Name)
        {
            public Vertebrate() : this("") { }
        }

        //repeat Name property to become part of contract
        record ReptileWithName(string Name, Habitat Habitat) : Vertebrate(Name) { }

        record ReptileWithoutName(Habitat Habitat) : Vertebrate { }

        //omit repetitions in case you want to fix them - but pass fixed values as argument list for base class specification 
        record TerrestrialLizard(Diet Diet) : ReptileWithName("Lizard", Habitat.Terrestrial) { }

        record AnimalCollector(string Name, byte Age, Vertebrate[] Animals/*serialization of this array will loose data not available on Vertebrate level*/) { }
    }

    [TestFixture]
    class Records_Transformables
    {
        [Test]
        public void ShouldBeAbleToReparse()
        {
            var politician = new Person("Janusz", "Korwin-Mikke", 78);

            var sut = Sut.GetTransformer<Person>();
            var formatted = sut.Format(politician);

            Assert.That(formatted, Is.EqualTo(@"Janusz-Korwin\-Mikke-78"));

            var parsed = sut.Parse(formatted);
            Assert.That(parsed.FirstName, Is.EqualTo("Janusz"));
            Assert.That(parsed.FamilyName, Is.EqualTo("Korwin-Mikke"));
            Assert.That(parsed.Age, Is.EqualTo(78));

            Assert.That(parsed, Is.EqualTo(politician));
        }

        [Transformer(typeof(PersonTransformer))]
        record Person(string FirstName, string FamilyName, int Age) { }

        class PersonTransformer : CustomDeconstructionTransformer<Person>
        {
            public PersonTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

            protected override DeconstructionTransformerBuilder BuildSettings(DeconstructionTransformerBuilder prototype) =>
                prototype
                    .WithoutBorders()
                    .WithDelimiter('-')
                    .WithNullElementMarker('␀')
                    .WithDeconstructableEmpty();
        }
    }

    [TestFixture]
    class Records_CustomTransformer
    {
        [TestCase(null, double.NaN, double.NaN, double.NaN)]
        [TestCase("", 0.0, 0.0, 0.0)]
        [TestCase("123456.789", 123456.789, 123456.789, 123456.789)]
        [TestCase("1.4#2.5#3.6", 1.4, 2.5, 3.6)]
        public void ShouldBeAbleToReparse(string input, double expectedFirst, double expectedSecond, double expectedThird)
        {
            var sut = Sut.GetTransformer<Triplet<double>>();

            var parsed = sut.Parse(input);
            Assert.That(parsed?.First ?? double.NaN, Is.EqualTo(expectedFirst));
            Assert.That(parsed?.Second ?? double.NaN, Is.EqualTo(expectedSecond));
            Assert.That(parsed?.Third ?? double.NaN, Is.EqualTo(expectedThird));


            var formated = sut.Format(parsed);
            var parsed2 = sut.Parse(formated);
            Assert.That(parsed, Is.EqualTo(parsed2));
        }

        [TestCase("1.5#2.6", typeof(FormatException), "Cannot parse triples from 2 values only")]
        [TestCase("1.5#2.6#3.7#4.8", typeof(FormatException), "Cannot parse triples from more than 3 values")]
        [TestCase("3#ABC", typeof(FormatException), "Input string was not in a correct format.")]
        public void ShouldBeAbleToParse_Negative(string input, Type expectedException, string expectedMessage)
        {
            var sut = Sut.GetTransformer<Triplet<double>>();

            Assert.That(() => sut.Parse(input), Throws.TypeOf(expectedException).And.Message.EqualTo(expectedMessage));
        }

        [Transformer(typeof(TripletTransformer<>))]
        record Triplet<T>(T First, T Second, T Third) where T : struct { }

        class TripletTransformer<TValue> : TransformerBase<Triplet<TValue>> where TValue : struct
        {
            private readonly ITransformer<TValue> _elementTransformer;

            public TripletTransformer(ITransformerStore transformerStore)//When used in standard way - this parameter gets injected by default
                => _elementTransformer = transformerStore.GetTransformer<TValue>();

            private const char ELEMENT_DELIMITER = '#';
            private const char ESCAPING_SEQUENCE_START = '\\';

            public override Triplet<TValue> GetEmpty() => new Triplet<TValue>(default, default, default);

            protected override Triplet<TValue> ParseCore(in ReadOnlySpan<char> input)
            {
                var tokens = input.Tokenize(ELEMENT_DELIMITER, ESCAPING_SEQUENCE_START, true);
                //var parsed = tokens.PreParse(ESCAPING_SEQUENCE_START, '\0', ELEMENT_DELIMITER);

                var enumerator = tokens.GetEnumerator();

                if (!enumerator.MoveNext()) return new Triplet<TValue>(default, default, default);
                var first = _elementTransformer.Parse(enumerator.Current);

                if (!enumerator.MoveNext()) return new Triplet<TValue>(first, first, first);
                var second = _elementTransformer.Parse(enumerator.Current);

                if (!enumerator.MoveNext()) throw new FormatException("Cannot parse triples from 2 values only");
                var third = _elementTransformer.Parse(enumerator.Current);

                if (!enumerator.MoveNext()) return new Triplet<TValue>(first, second, third);
                else throw new FormatException("Cannot parse triples from more than 3 values");
            }

            public override string Format(Triplet<TValue> element)
            {
                if (element == null) return null;

                Span<char> initialBuffer = stackalloc char[32];
                var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

                try
                {
                    FormatElement(element.First, ref accumulator);
                    accumulator.Append(ELEMENT_DELIMITER);

                    FormatElement(element.Second, ref accumulator);
                    accumulator.Append(ELEMENT_DELIMITER);

                    FormatElement(element.Third, ref accumulator);

                    return accumulator.ToString();
                }
                finally { accumulator.Dispose(); }
            }

            void FormatElement(TValue value, ref ValueSequenceBuilder<char> accumulator)
            {
                string text = _elementTransformer.Format(value);

                foreach (char c in text)
                {
                    if (c is ELEMENT_DELIMITER or ESCAPING_SEQUENCE_START)
                        accumulator.Append(ESCAPING_SEQUENCE_START);
                    accumulator.Append(c);
                }
            }
        }
    }
}

#if !NET
namespace System.Runtime.CompilerServices
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
