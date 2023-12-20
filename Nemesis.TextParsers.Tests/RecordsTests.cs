using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Tests.Utils;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests;

[TestFixture]
class RecordsAutomaticTransformation
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
        var collector = new AnimalCollector("Mike", 36,
        [
            new ReptileWithName("Comodo Dragon", Habitat.Terrestrial),
            new TerrestrialLizard(Diet.Carnivorous)
        ]);
        var sut = Sut.GetTransformer<AnimalCollector>();


        var formatted = sut.Format(collector);

        Assert.That(formatted, Is.EqualTo("(Mike;36;(Comodo Dragon)|(Lizard))"));

        var parsed = sut.Parse(formatted);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.Name, Is.EqualTo("Mike"));
            Assert.That(parsed.Age, Is.EqualTo(36));
            Assert.That(parsed.Animals[0].Name, Is.EqualTo("Comodo Dragon"));
            Assert.That(parsed.Animals[1].Name, Is.EqualTo("Lizard"));
        });
    }

    enum Habitat { Terrestrial/*, Aquatic, Amphibian*/ }
    enum Diet { Carnivorous/*, Herbivorous, Omnivorous*/ }

    record Vertebrate(string Name)
    {
        [JetBrains.Annotations.UsedImplicitly]
        public Vertebrate() : this("") { }
    }

    //repeat Name property to become part of contract
    record ReptileWithName(string Name, Habitat Habitat) : Vertebrate(Name);

    //record ReptileWithoutName(Habitat Habitat) : Vertebrate; //this will loose Name property

    //omit repetitions in case you want to fix them - but pass fixed values as argument list for base class specification 
    record TerrestrialLizard(Diet Diet) : ReptileWithName("Lizard", Habitat.Terrestrial);

    record AnimalCollector(string Name, byte Age, Vertebrate[] Animals/*serialization of this array will loose data not available on Vertebrate level*/);
}

[TestFixture]
class RecordsTransformersTests
{
    [Test]
    public void ShouldBeAbleToReparse_WithEscapedSequence()
    {
        var writer = new Person("Antoine", "de Saint-Exupéry", 44);

        var sut = Sut.GetTransformer<Person>();
        var formatted = sut.Format(writer);

        Assert.That(formatted, Is.EqualTo(@"Antoine-de Saint\-Exupéry-44"));

        var parsed = sut.Parse(formatted);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.FirstName, Is.EqualTo("Antoine"));
            Assert.That(parsed.FamilyName, Is.EqualTo("de Saint-Exupéry"));
            Assert.That(parsed.Age, Is.EqualTo(44));

            Assert.That(parsed, Is.EqualTo(writer));
        });
    }

    [Transformer(typeof(PersonTransformer))]
    record Person(string FirstName, string FamilyName, int Age);

    class PersonTransformer : CustomDeconstructionTransformer<Person>
    {
        public PersonTransformer(ITransformerStore transformerStore) : base(transformerStore) { }

        protected override DeconstructionTransformerBuilder BuildSettings(DeconstructionTransformerBuilder prototype) =>
            prototype
                .WithoutBorders()
                .WithDelimiter('-')
                .WithNullElementMarker('␀')
                .WithDeconstructableEmpty();
    }
}

[TestFixture]
class RecordsCustomTransformerTests
{
    [TestCase(null, double.NaN, double.NaN, double.NaN)]
    [TestCase("", 0.0, 0.0, 0.0)]
    [TestCase("123456.789", 123456.789, 123456.789, 123456.789)]
    [TestCase("1.4#2.5#3.6", 1.4, 2.5, 3.6)]
    public void ShouldBeAbleToReparse(string input, double expectedFirst, double expectedSecond, double expectedThird)
    {
        var sut = Sut.GetTransformer<Triplet<double>>();

        var parsed = sut.Parse(input);
        Assert.Multiple(() =>
        {
            Assert.That(parsed?.First ?? double.NaN, Is.EqualTo(expectedFirst));
            Assert.That(parsed?.Second ?? double.NaN, Is.EqualTo(expectedSecond));
            Assert.That(parsed?.Third ?? double.NaN, Is.EqualTo(expectedThird));
        });
        var formatted = sut.Format(parsed);
        var parsed2 = sut.Parse(formatted);
        Assert.That(parsed, Is.EqualTo(parsed2));
    }

    [TestCase("1.5#2.6", typeof(FormatException), "Cannot parse triples from 2 values only")]
    [TestCase("1.5#2.6#3.7#4.8", typeof(FormatException), "Cannot parse triples from more than 3 values")]
    [TestCase("3#ABC", typeof(FormatException),
#if NET7_0_OR_GREATER
        "The input string 'ABC' was not in a correct format."
#else
        "Input string was not in a correct format."
#endif

        )]
    public void ShouldBeAbleToParse_Negative(string input, Type expectedException, string expectedMessage)
    {
        var sut = Sut.GetTransformer<Triplet<double>>();

        Assert.That(() => sut.Parse(input), Throws.TypeOf(expectedException).And.Message.EqualTo(expectedMessage));
    }

    [Transformer(typeof(TripletTransformer<>))]
    record Triplet<T>(T First, T Second, T Third) where T : struct;

    class TripletTransformer<TValue>(ITransformer<TValue> elementTransformer/*When used in standard way - this parameter gets injected by default*/)
        : TransformerBase<Triplet<TValue>> where TValue : struct
    {
        private const char ELEMENT_DELIMITER = '#';
        private const char ESCAPING_SEQUENCE_START = '\\';

        public override Triplet<TValue> GetEmpty() => new(default, default, default);

        protected override Triplet<TValue> ParseCore(in ReadOnlySpan<char> input)
        {
            var tokens = input.Tokenize(ELEMENT_DELIMITER, ESCAPING_SEQUENCE_START, true);
            //var parsed = tokens.PreParse(ESCAPING_SEQUENCE_START, '\0', ELEMENT_DELIMITER);

            var enumerator = tokens.GetEnumerator();

            if (!enumerator.MoveNext()) return new Triplet<TValue>(default, default, default);
            var first = elementTransformer.Parse(enumerator.Current);

            if (!enumerator.MoveNext()) return new Triplet<TValue>(first, first, first);
            var second = elementTransformer.Parse(enumerator.Current);

            if (!enumerator.MoveNext()) throw new FormatException("Cannot parse triples from 2 values only");
            var third = elementTransformer.Parse(enumerator.Current);

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
            string text = elementTransformer.Format(value);

            foreach (char c in text)
            {
                if (c is ELEMENT_DELIMITER or ESCAPING_SEQUENCE_START)
                    accumulator.Append(ESCAPING_SEQUENCE_START);
                accumulator.Append(c);
            }
        }
    }
}


