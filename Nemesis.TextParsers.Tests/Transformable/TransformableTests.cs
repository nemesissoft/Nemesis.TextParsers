using Nemesis.TextParsers.Tests.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;

namespace Nemesis.TextParsers.Tests.Transformable;

[TestFixture]
class TransformableTests
{
    private static IEnumerable<TCD> CorrectData() => new[]
    {
        new TCD(new ParsleyAndLeekFactors(100, [200, 300, 400]), @"100;200,300,400"),
        new TCD(new ParsleyAndLeekFactors(1000, [2]), @"1000;2"),
        new TCD(new ParsleyAndLeekFactors(10, [20.0f, 30.0f]), @""), //overriden in transformer
        new TCD(new ParsleyAndLeekFactors(0, [0f, 0f]), null), //overriden in transformer
        new TCD(new ParsleyAndLeekFactors(555.5f, null), @"555.5;∅"),
        new TCD(new ParsleyAndLeekFactors(666.6f, []), @"666.6;"),
    };

    [TestCaseSource(nameof(CorrectData))]
    public void Transformable_ParseAndFormat(ParsleyAndLeekFactors instance, string text)
    {
        var sut = Sut.GetTransformer<ParsleyAndLeekFactors>();

        var actualParsed1 = sut.Parse(text);

        string formattedInstance = sut.Format(instance);
        string formattedActualParsed = sut.Format(actualParsed1);
        Assert.That(formattedInstance, Is.EqualTo(formattedActualParsed));

        var actualParsed2 = sut.Parse(formattedInstance);

        IsMutuallyEquivalent(actualParsed1, instance);
        IsMutuallyEquivalent(actualParsed2, instance);
        IsMutuallyEquivalent(actualParsed1, actualParsed2);
    }


    private static IEnumerable<TCD> GenericTransformable_Data() => new[]
    {
        new TCD(new CustomList<float>([100, 200, 300, 400]), @"100;200;300;400"),
        new TCD(new CustomList<float>([1000]), @"1000"),
        new TCD(new CustomList<float>([]), @""),//overriden in transformer
        new TCD(new CustomList<float>(null), null),//overriden in transformer 
    };

    [TestCaseSource(nameof(GenericTransformable_Data))]
    public void GenericTransformable_ParseAndFormat(ICustomList<float> instance, string text)
    {
        var sut = Sut.GetTransformer<ICustomList<float>>();

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
