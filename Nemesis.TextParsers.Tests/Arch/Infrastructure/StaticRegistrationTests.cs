using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests.Arch.Infrastructure;

[TestFixture]
internal class StaticRegistrationTests
{
    [Test]
    public void CheckTypesCompatibility_SimpleTransformerHandler() =>
        StaticRegistrations_CheckTypesCompatibility(new SimpleTransformerHandler());

    [Test]
    public void CheckTypesCompatibility_NumberTransformerCache() =>
        StaticRegistrations_CheckTypesCompatibility(NumberTransformerCache.Instance);

    private static void StaticRegistrations_CheckTypesCompatibility(ITestTransformerRegistrations testTransformerRegistrations)
    {
        var registrations = testTransformerRegistrations.GetTransformerRegistrationsForTests();

        Assert.That(registrations, Is.Not.Empty);
        foreach (var kvp in registrations)
        {
            var elementType = kvp.Key;
            var transformerInstance = kvp.Value;

            var transformerElementType = typeof(ITransformer<>).MakeGenericType(elementType);

            Assert.That(transformerInstance, Is.AssignableTo(transformerElementType), () => $"""
            Cannot transform {elementType} with {transformerInstance.GetType().GetFriendlyName()}: {(
                TypeMeta.TryGetGenericRealization(transformerInstance.GetType(), typeof(ITransformer<>), out var iTransformerType)
                ? iTransformerType.GetFriendlyName()
                : "NOT SUPPORTED"
            )}
            """);
        }
    }



    [Test]
    public void CheckIfAllTransformersWereRegistered_SimpleTransformerHandler() =>
        StaticRegistrations_CheckIfAllTransformersWereRegistered(new SimpleTransformerHandler(), typeof(SimpleTransformer<>));

    [Test]
    public void CheckIfAllTransformersWereRegistered_NumberTransformerCache() =>
        StaticRegistrations_CheckIfAllTransformersWereRegistered(NumberTransformerCache.Instance, typeof(NumberTransformer<>));

    private static void StaticRegistrations_CheckIfAllTransformersWereRegistered(ITestTransformerRegistrations testTransformerRegistrations, Type baseTransformerType)
    {
        var expectedTransformerTypes = baseTransformerType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                        t.DerivesOrImplementsGeneric(baseTransformerType)
            ).ToList();

        Assert.That(expectedTransformerTypes, Is.Not.Empty);

        var registrations = testTransformerRegistrations.GetTransformerRegistrationsForTests();

        foreach (var transformerType in expectedTransformerTypes)
        {
            var concreteBaseType = TypeMeta.GetGenericRealization(transformerType, baseTransformerType);
            var elementType = concreteBaseType.GenericTypeArguments[0];

            var success = registrations.TryGetValue(elementType, out var transformerInstance);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True, () => $"Transformer {transformerType.FullName} was not registered to transform {elementType.FullName}");
                Assert.That(transformerInstance, Is.AssignableTo(concreteBaseType), () => $"{transformerInstance.GetType().FullName} cannot transform {elementType.GetFriendlyName()}");
                Assert.That(transformerInstance, Is.TypeOf(transformerType));
            });
        }
    }


    [Test]
    public void CheckIfAllTransformersWereRegistered_Settings() =>
        Data_CheckIfAllTransformersWereRegistered(typeof(ISettings), TextTransformer.DefaultSettings.Select(s => s.GetType()).ToList());

    [Test]
    public void CheckIfAllTransformersWereRegistered_TransformerHandler() =>
        Data_CheckIfAllTransformersWereRegistered(typeof(ITransformerHandler), TextTransformer.DefaultHandlers);

    private static void Data_CheckIfAllTransformersWereRegistered(Type baseType, IReadOnlyCollection<Type> registeredTypes)
    {
        var expectedTransformerTypes = baseType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                        baseType.IsAssignableFrom(t)
            ).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(registeredTypes, Has.Count.EqualTo(expectedTransformerTypes.Count));
            Assert.That(registeredTypes, Is.EquivalentTo(expectedTransformerTypes.ToList()));
        });
    }
}