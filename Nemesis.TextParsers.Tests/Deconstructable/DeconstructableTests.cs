using System;
using System.Collections.Generic;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;
using B2B = System.Func<Nemesis.TextParsers.Parsers.DeconstructionTransformerBuilder, Nemesis.TextParsers.Parsers.DeconstructionTransformerBuilder>;
using Builder = Nemesis.TextParsers.Parsers.DeconstructionTransformerBuilder;
using TCD = NUnit.Framework.TestCaseData;

namespace Nemesis.TextParsers.Tests.Deconstructable;

[TestFixture]
internal class DeconstructableTests
{
    private static IEnumerable<TCD> CorrectData() => new[]
    {
        new TCD(typeof(CarrotAndOnionFactors),
                new CarrotAndOnionFactors(123.456789M, new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, TimeSpan.Parse("12:34:56")),
                @"(123.456789;1|2|3|3.14;12:34:56)"),
        new TCD(typeof(Address), new Address("Wrocław", 52200), @"(Wrocław;52200)"),
        new TCD(typeof(Address), new Address("Wrocław)", 52200), @"(Wrocław);52200)"),
        new TCD(typeof(Address), new Address("(Wrocław)", 52200), @"((Wrocław);52200)"),
        new TCD(typeof(Address), new Address("A", 1), @"(A;1)"),
        new TCD(typeof(Address), new Address(null, 1), @"(∅;1)"),
        new TCD(typeof(Address), new Address("", 1), @"(;1)"),
        new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200))"),

        new TCD(typeof(LargeStruct), LargeStruct.Sample, @"(3.14159265;2.718282;-123456789;123456789;-1234;1234;127;-127;-4611686018427387904;9223372036854775807;23.14069263;123456789012345678901234567890;(3.14159265\; 2.71828183))"),

    };

    [TestCaseSource(nameof(CorrectData))]
    public void FormatAndParse(Type type, object instance, string text)
    {
        var tester = Method.OfExpression<Action<object, string, B2B>>(
            (i, t, m) => FormatAndParseHelper(i, t, m)
        ).GetGenericMethodDefinition();

        tester = tester.MakeGenericMethod(type);

        tester.Invoke(null, new[] { instance, text, null });
    }

    private static void FormatAndParseHelper<TDeconstructable>(TDeconstructable instance, string text, B2B settingsMutator = null)
    {
        var settings = Builder.GetDefault(Sut.DefaultStore);
        settings = settingsMutator?.Invoke(settings) ?? settings;

        var sut = settings.ToTransformer<TDeconstructable>();


        var actualFormatted = sut.Format(instance);
        Assert.That(actualFormatted, Is.EqualTo(text));


        var actualParsed1 = sut.Parse(text);
        var actualParsed2 = sut.Parse(actualFormatted);
        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }

    private static void ParseHelper<TDeconstructable>(TDeconstructable expected, string input, B2B settingsMutator = null)
    {
        var settings = Builder.GetDefault(Sut.DefaultStore);
        settings = settingsMutator?.Invoke(settings) ?? settings;

        var sut = settings.ToTransformer<TDeconstructable>();


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
    public void ParseAndFormat_DeconstructContravariance() =>
        FormatAndParseHelper(
            new ExternallyDeconstructable(10.1f, 555.55M), @"{10.1;555.55}",
            s => s.WithBorders('{', '}').WithCustomDeconstruction(ExternallyDeconstructableExt.DeconstructMethod,
                ExternallyDeconstructableExt.Constructor));


    [Test]
    public void ParseAndFormat_NoBorders()
    {
        //this is just to test normal (bordered) behaviour 
        ParseHelper(
            new ThreeStrings("A", "B", "C"), @"    [A;B;C]   ",
            s => s.WithBorders('[', ']'));

        FormatAndParseHelper(
            new ThreeStrings("A", "B", "C"), @"A;B;C",
            s => s.WithoutBorders());

        FormatAndParseHelper(
            new ThreeStrings(" A", "B", "C "), @" A;B;C ",
            s => s.WithoutBorders());




        ParseHelper(
            new ThreeStrings("", "", ""), @"    [;;]   ",
            s => s.WithBorders('[', ']'));

        ParseHelper(
            new ThreeStrings("", "", ""), @";;",
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
    public void ParseAndFormat_MixedBorders_WithOptionalWhitespace()
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
    public void ParseAndFormat_NonStandardControlCharacters()
    {
        //(\∅;∅;\\t123\\ABC\\\∅DEF\∅GHI\;)
        var data = new ThreeStrings("∅␀", null, @"\t123\ABC\∅DEF∅GHI;/:");

        FormatAndParseHelper(data, @"(\∅␀;∅;\\t123\\ABC\\\∅DEF\∅GHI\;/:)");

        FormatAndParseHelper(data, @"(∅\␀;␀;\\t123\\ABC\\∅DEF∅GHI\;/:)",
            s => s.WithNullElementMarker('␀')
            );

        FormatAndParseHelper(data, @"(/∅␀;∅;\t123\ABC\/∅DEF/∅GHI/;//:)",
            s => s.WithEscapingSequenceStart('/')
            );

        FormatAndParseHelper(data, @"(\∅␀:∅:\\t123\\ABC\\\∅DEF\∅GHI;/\:)",
            s => s.WithDelimiter(':')
        );

        FormatAndParseHelper(data, @"(∅/␀:␀:\t123\ABC\∅DEF∅GHI;///:)",
            s => s.WithDelimiter(':')
                  .WithEscapingSequenceStart('/')
                  .WithNullElementMarker('␀')
        );
    }

    [Test]
    public void ParseAndFormat_Recursive()
    {
        //currently cyclic dependencies are not supported but have a look at the following deconstructable 
        // ReSharper disable StringLiteralTypo
        FormatAndParseHelper(new King("Mieszko I", new King("Bolesław I Chrobry", new King("Mieszko II Lambert", new King("Kazimierz I Odnowiciel", new King("Bolesław II Szczodry", (King)null))))),
            @"Mieszko I>Bolesław I Chrobry|Mieszko II Lambert|Kazimierz I Odnowiciel|Bolesław II Szczodry",
            s => s
                .WithoutBorders()
                .WithDelimiter('>')
        );

        //it is possible to influence only top type for automatic deconstructable aspect...
        FormatAndParseHelper(
            new Person("Mike", 36, new Address("Wrocław", 52200)),
            @"Mike,36,(Wrocław;52200)",
            s => s.WithoutBorders()
                  .WithDelimiter(',')
                  .WithEscapingSequenceStart('/')
        );

        //...but inner type can have own registered transformers or simply use automatic settings - here we have
        //1. / : \    - set here in test 
        //2. [ , ]    - from custom transformer
        //3. { _ }    - from custom transformer
        //4. ( ; )    - default
        FormatAndParseHelper(
            new Planet("Earth", 7_774_001_533, new Country("Poland", 37_857_130, new Region("Dolny Śląsk", 2_907_200, new City("Wrocław", 639_258)))),
            @"/Earth:7774001533:[Poland,37857130,{Dolny Śląsk_2907200_(Wrocław;639258)}]\",
            s => s.WithBorders('/', '\\')
                .WithDelimiter(':')
                .WithEscapingSequenceStart('*')
        );

        FormatAndParseHelper(
            new House("My house", 116.2f, new List<Room>
            {
                new Room("Kid/wife room", new Dictionary<string, decimal>{["BRIMNES"]=857,["STRANDMON"]=699}),
                new Room("Kitchen", new Dictionary<string, decimal>{["VADHOLMA"]=999,["RISATORP"]=29.99m}),
            }),
            @"{My house⸗116.2⸗[Kid//wife room,BRIMNES=857;STRANDMON=699]|[Kitchen,VADHOLMA=999;RISATORP=29.99]}",
            s => s.WithBorders('{', '}')
                .WithDelimiter('⸗')
                .WithEscapingSequenceStart('/')
        );

        // ReSharper restore StringLiteralTypo
    }

    [Test]
    public void ParseAndFormat_AttributeProvidedSettings()
    {
        var instance = new Kindergarten(null, new[] { new Child(3, 20.2f), new Child(5, 25.66f) });
        var text = @"␀;{3,20.2}|{5,25.66}";

        var sut = Sut.GetTransformer<Kindergarten>();

        var actualFormatted = sut.Format(instance);
        Assert.That(actualFormatted, Is.EqualTo(text));


        var actualParsed1 = sut.Parse(text);
        var actualParsed2 = sut.Parse(actualFormatted);
        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }

    [Test]
    public void ParseAndFormat_AttributeProvidedSettings_DefaultSettings()
    {
        var instance = new UnderscoreSeparatedProperties("ABC_DEF", "∅NULL∅", @"\ Start:( END:)");
        var text = @"(ABC\_DEF_\∅NULL\∅_\\ Start:( END:))";

        var sut = Sut.GetTransformer<UnderscoreSeparatedProperties>();

        var actualFormatted = sut.Format(instance);
        Assert.That(actualFormatted, Is.EqualTo(text));


        var actualParsed1 = sut.Parse(text);
        var actualParsed2 = sut.Parse(actualFormatted);
        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }

    [Test]
    public void ParseAndFormat_AttributeProvidedSettings_NoAttribute()
    {
        var settings = new DeconstructableSettings('๑', '๒', '๓', '๔', '๕', false);
        var storeStore = SettingsStoreBuilder.GetDefault()
            .AddOrUpdate(settings)
            .Build();
        var sut = TextTransformer.GetDefaultStoreWith(storeStore);


        var transformer = sut.GetTransformer<NoSettings>();


        Assert.That(transformer, Is.TypeOf<DeconstructionTransformer<NoSettings>>());

        var helper = ((DeconstructionTransformer<NoSettings>)transformer).Helper;
        Assert.Multiple(() =>
        {
            Assert.That(helper.TupleDelimiter, Is.EqualTo(settings.Delimiter));
            Assert.That(helper.NullElementMarker, Is.EqualTo(settings.NullElementMarker));
            Assert.That(helper.EscapingSequenceStart, Is.EqualTo(settings.EscapingSequenceStart));
            Assert.That(helper.TupleStart, Is.EqualTo(settings.Start));
            Assert.That(helper.TupleEnd, Is.EqualTo(settings.End));
        });
    }

    private static IEnumerable<TCD> CustomDeconstructable_Data() => new[]
    {
        //instance, input
        new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, new decimal[] {10, 20, 30}),
            @"{3.14_False_10|20|30}"),
        new TCD(new DataWithCustomDeconstructableTransformer(666, true, new decimal[] {6, 7, 8, 9}), null), //overriden by custom transformer 
        new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, Array.Empty<decimal>()), ""), //overriden by deconstructable aspect convention

        new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, null), @"{3.14_False_␀}"),
        new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_False_␀}"),
        new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_␀_␀}"),
        new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, Array.Empty<decimal>()), @"{__}"),
    };

    [TestCaseSource(nameof(CustomDeconstructable_Data))]
    public void Custom_ConventionTransformer_BasedOnDeconstructable(DataWithCustomDeconstructableTransformer instance, string text)
    {
        var sut = Sut.GetTransformer<DataWithCustomDeconstructableTransformer>();


        var actualParsed1 = sut.Parse(text);

        var formattedInstance = sut.Format(instance);
        var formattedActualParsed1 = sut.Format(actualParsed1);

        Assert.That(formattedActualParsed1, Is.EqualTo(formattedInstance));

        var actualParsed2 = sut.Parse(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }

    [Test]
    public void Empty_CheckNested()
    {
        IsMutuallyEquivalent(
            Sut.GetTransformer<House>().GetEmpty(),
            new House("", 0.0f, new List<Room>())); //not overriden  

        IsMutuallyEquivalent(
            Sut.GetTransformer<Room>().GetEmpty(),
            RoomTransformer.Empty); //overriden by transformer
    }

    [Test]
    public void Empty_CheckStability()
    {
        //not overriden 
        var emptyHouse1 = Sut.GetTransformer<House>().GetEmpty();
        var emptyHouse2 = Sut.GetTransformer<House>().GetEmpty();

        Assert.That(emptyHouse1.Rooms, Is.Empty);
        emptyHouse1.Rooms.Add(new Room("XXX", null));
        Assert.Multiple(() =>
        {
            Assert.That(emptyHouse1.Rooms, Is.Not.Empty);
            Assert.That(emptyHouse2.Rooms, Is.Empty);
        });


        //overriden by transformer
        var emptyRoom1 = Sut.GetTransformer<Room>().GetEmpty();
        var emptyRoom2 = Sut.GetTransformer<Room>().GetEmpty();

        Assert.That(emptyRoom1.FurniturePrices, Has.Count.EqualTo(1));
        emptyRoom1.FurniturePrices.Add("New bed", 10000);
        Assert.Multiple(() =>
        {
            Assert.That(emptyRoom1.FurniturePrices, Has.Count.EqualTo(2));
            Assert.That(emptyRoom2.FurniturePrices, Has.Count.EqualTo(1));
        });
    }

    #region Negative tests

    [TestCase(@"(Mike;36;(Wrocław;52200))", typeof(ArgumentException), "These requirements were not met in:'(Wrocław'")]
    [TestCase(@"(Mike;36;(Wrocław);52200))", typeof(ArgumentException), "2nd element was not found after 'Wrocław'")]
    [TestCase(@"(Mike;36;(Wrocław\;52200);123))", typeof(ArgumentException), "cannot have more than 3 elements: '123)'")]
    public void Parse_NegativeTest(string wrongInput, Type expectedException, string expectedMessagePart)
    {
        var sut = Builder.GetDefault(Sut.DefaultStore)
            .ToTransformer<Person>();

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

    private static IEnumerable<(Type type, Type exceptionType, B2B settingsBuilder, string expectedMessagePart)> ToTransformer_NegativeTest_Data() => new[]
    {
        (typeof(NoDeconstruct), typeof(NotSupportedException), (B2B)null, "Deconstructable for NoDeconstruct cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),
        (typeof(DeconstructWithoutMatchingCtor), typeof(NotSupportedException), null, "Deconstructable for DeconstructWithoutMatchingCtor cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),
        (typeof(CtorAndDeCtorOutOfSync), typeof(NotSupportedException), null, "Deconstructable for CtorAndDeCtorOutOfSync cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),


        (typeof(NotCompatibleCtor), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(NotCompatibleCtor.DeconstructMethod, NotCompatibleCtor.Constructor), "Instance Deconstruct method has to be compatible with provided constructor and should have same number and type of parameters"),

        (typeof(NotOutParam), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(NotOutParam.DeconstructMethod, NotOutParam.Constructor), "Instance Deconstruct method must have all out params (IsOut==true)"),

        (typeof(NotSupportedParams), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(NotSupportedParams.DeconstructMethod, NotSupportedParams.Constructor), @"Instance Deconstruct method must have all parameter types be recognizable by TransformerStore. Not supported types:object, object[], List<object>, string[,]"),


        (typeof(StaticNotCompatibleCtor), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(StaticNotCompatibleCtor.DeconstructMethod, StaticNotCompatibleCtor.Constructor), @"Static Deconstruct method has to be compatible with provided constructor and should have one additional parameter in the beginning - deconstructable instance"),

        (typeof(StaticNotOutParam), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(StaticNotOutParam.DeconstructMethod, StaticNotOutParam.Constructor), @"Static Deconstruct method must have all but first params as out params (IsOut==true)"),


        (typeof(StaticNotSupportedParams), typeof(NotSupportedException),
            s => s.WithCustomDeconstruction(StaticNotSupportedParams.DeconstructMethod, StaticNotSupportedParams.Constructor), @"Static Deconstruct method must have all parameter types be recognizable by TransformerStore. Not supported types:object, object[], List<object>, string[,]"),
    };

    [TestCaseSource(nameof(ToTransformer_NegativeTest_Data))]
    public void ToTransformer_NegativeTest((Type type, Type exceptionType, B2B settingsBuilder, string expectedMessagePart) data)
    {
        var negativeTest = MakeDelegate<Func<B2B, Exception>>(
            func => ToTransformer_NegativeTest_Helper<int>(func), data.type
        );

        var ex = negativeTest(data.settingsBuilder);

        Assert.That(ex, Is.Not.Null);

        Assert.That(ex, Is.TypeOf(data.exceptionType));

        Assert.That(ex.Message, Is.EqualTo(data.expectedMessagePart)
            .Using(IgnoreNewLinesComparer.EqualityComparer)
        );
    }

    private static Exception ToTransformer_NegativeTest_Helper<TDeconstructable>(B2B settingsBuilder)
    {
        var settings = Builder.GetDefault(Sut.DefaultStore);

        if (settingsBuilder != null)
            settings = settingsBuilder(settings);

        try
        {
            settings.ToTransformer<TDeconstructable>();
            return null;
        }
        catch (Exception e) { return e; }
    }

    #endregion
}
