using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests.Arch.Infrastructure;

[TestFixture]
internal class StaticRegistrationTests
{
    [Test]
    public void CheckTypesCompatibility_SimpleTransformerHandler() => CheckTypesCompatibility(new SimpleTransformerHandler());

    [Test]
    public void CheckTypesCompatibility_NumberTransformerCache() => CheckTypesCompatibility(NumberTransformerCache.Instance);

    private static void CheckTypesCompatibility(ITestTransformerRegistrations testTransformerRegistrations)
    {
        var registrations = testTransformerRegistrations.GetTransformerRegistrationsForTests();

        Assert.That(registrations, Is.Not.Empty);
        foreach (var kvp in registrations)
        {
            var elementType = kvp.Key;
            var transformerInstance = kvp.Value;

            var transformerElementType = typeof(ITransformer<>).MakeGenericType(elementType);

            Assert.That(transformerInstance, Is.AssignableTo(transformerElementType), () =>
                $"Cannot transform {elementType} with {transformerInstance.GetType().GetFriendlyName()}: {(
                TypeMeta.TryGetGenericRealization(transformerInstance.GetType(), typeof(ITransformer<>), out var iTransformerType)
                    ? iTransformerType.GetFriendlyName()
                    : "NOT SUPPORTED"
                )}");
        }
    }


    [Test]
    public void CheckIfAllTransformersWereRegistered_SimpleTransformerHandler() =>
        CheckIfAllTransformersWereRegistered(new SimpleTransformerHandler(), typeof(SimpleTransformer<>));

    [Test]
    public void CheckIfAllTransformersWereRegistered_NumberTransformerCache() =>
        CheckIfAllTransformersWereRegistered(NumberTransformerCache.Instance, typeof(NumberTransformer<>));

    private static void CheckIfAllTransformersWereRegistered(ITestTransformerRegistrations testTransformerRegistrations, Type baseTransformerType)
    {
        var expectedTransformerTypes = baseTransformerType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                        t.DerivesOrImplementsGeneric(baseTransformerType)
            ).ToList();

        Assert.That(expectedTransformerTypes, Is.Not.Empty);

        var registrations = testTransformerRegistrations.GetTransformerRegistrationsForTests();

        var notProperlyRegistered = expectedTransformerTypes.Where(t => !IsProperlyRegistered(t, baseTransformerType, registrations)).ToList();

        Assert.That(notProperlyRegistered, Is.Empty, () =>
            $"There were {notProperlyRegistered.Count} transformers not properly registered:{Environment.NewLine}{
                string.Join(Environment.NewLine,
                    notProperlyRegistered.Select(t => $"\t{t.FullName}")
                )}");

        static bool IsProperlyRegistered(Type transformerType, Type baseTransformerType, IReadOnlyDictionary<Type, ITransformer> registrations)
        {
            var concreteBaseType = TypeMeta.GetGenericRealization(transformerType, baseTransformerType);
            var elementType = concreteBaseType.GenericTypeArguments[0];

            return registrations.TryGetValue(elementType, out var transformerInstance) &&
                   transformerInstance is not null &&
                   concreteBaseType.IsAssignableFrom(transformerInstance.GetType()) &&
                   transformerInstance.GetType() == transformerType;
        }
    }

    [Test]
    public void CheckIfAllTransformersWereRegistered_Settings() =>
        Data_CheckIfAllTransformersWereRegistered(typeof(ISettings),
            TextTransformer.DefaultSettings.Select(s => s.GetType()).ToList());

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