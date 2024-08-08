using Nemesis.TextParsers.Tests.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;
using MyList = Nemesis.TextParsers.Tests.Transformable.CustomList<float>;
using Parsley = Nemesis.TextParsers.Tests.Transformable.ParsleyAndLeekFactors;

namespace Nemesis.TextParsers.Tests.Transformable;

[TestFixture]
class TransformableTests
{
    //TODO add tests for Transformables aspect in code-gened Deconstruable
    private static TCD[] CorrectData() =>
    [
        new(new Parsley(100, [200, 300, 400]), @"100;200,300,400"),
        new(new Parsley(1000, [2]), @"1000;2"),
        new(new Parsley(10, [20.0f, 30.0f]), @""), //overriden in transformer
        new(new Parsley(0, [0f, 0f]), null), //overriden in transformer
        new(new Parsley(555.5f, null), @"555.5;∅"),
        new(new Parsley(666.6f, []), @"666.6;"),
    ];

    [TestCaseSource(nameof(CorrectData))]
    public void Transformable_ParseAndFormat(Parsley instance, string textToParse)
    {
        var sut = Sut.GetTransformer<Parsley>();

        Assert.That(sut, Is.TypeOf<ParsleyAndLeekFactorsTransformer>());

        var actualParsed1 = sut.Parse(textToParse);

        string formattedInstance = sut.Format(instance);
        string formattedActualParsed = sut.Format(actualParsed1);
        Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

        var actualParsed2 = sut.Parse(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }


    private static TCD[] GenericTransformable_Data() =>
    [
        new(new MyList([100, 200, 300, 400]), @"100;200;300;400"),
        new(new MyList([1000]), @"1000"),
        new(new MyList([]), @""),//overriden in transformer
        new(new MyList(null), null),//overriden in transformer 
    ];

    [TestCaseSource(nameof(GenericTransformable_Data))]
    public void GenericTransformable_ParseAndFormat(ICustomList<float> instance, string textToParse)
    {
        var sut = Sut.GetTransformer<ICustomList<float>>();

        Assert.That(sut, Is.TypeOf<CustomListTransformer<float>>());

        var actualParsed1 = sut.Parse(textToParse);

        string formattedInstance = sut.Format(instance);
        string formattedActualParsed = sut.Format(actualParsed1);
        Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

        var actualParsed2 = sut.Parse(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }
}
