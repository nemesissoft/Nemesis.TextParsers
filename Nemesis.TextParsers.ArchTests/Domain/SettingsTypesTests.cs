using ISettings = Nemesis.TextParsers.Settings.ISettings;
using JsonConstructorAttribute = System.Text.Json.Serialization.JsonConstructorAttribute;

namespace Nemesis.TextParsers.ArchTests.Domain;

[TestFixture]
public class SettingsTypesTests
{
    private static IEnumerable<Type> GetSettingsTypes() =>
        typeof(ISettings).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface &&
                    !t.IsGenericType && !t.IsGenericTypeDefinition &&
                    typeof(ISettings).IsAssignableFrom(t)
        );


    [Test]
    public void All_ShouldBeSealed()
    {
        var types = GetSettingsTypes();

        Assert.Multiple(() =>
        {
            foreach (var type in types)
                Assert.That(type.IsSealed, Is.True, () => $"{type.Name}.{nameof(Type.IsSealed)} = {type.IsSealed} but should be True");
        });
    }

    [Test]
    public void All_ShouldEndWithSettingsText()
    {
        var types = GetSettingsTypes();

        Assert.Multiple(() =>
        {
            foreach (var type in types)
                Assert.That(type.Name, Does.EndWith("Settings"));
        });
    }

    [Test]
    public void All_AllPropertiesShouldHaveAtLeastPrivateSetters() //to be able to be hydrated via Microsoft.Extensions.Configuration
    {
        const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var types = GetSettingsTypes();

        Assert.Multiple(() =>
        {
            foreach (var type in types)
                foreach (var p in type.GetProperties(BindingAttr))
                {
                    var property = p;
                    string desc = $"{type.Name}.{property.Name}";
                    if (property.DeclaringType is not null && property.DeclaringType != type)
                    {
                        desc = $"{property.DeclaringType.Name}.{property.Name}";
                        property = property.DeclaringType.GetProperty(property.Name, BindingAttr)
                         ?? throw new MissingMemberException(desc);
                    }

                    Assert.That(property.GetSetMethod(true), Is.Not.Null,
                        () => $"{desc} should have (at least) private setter to be accessible for value binding");
                }
        });
    }

    [Test]
    public void All_AtLeastOneConstructorNeedsToBeAnnotatedWithJsonConstructor()
    {
        var types = GetSettingsTypes();

        Assert.Multiple(() =>
        {
            foreach (var type in types)
            {
                var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var attributes = ctors
                    .Select(c => c.GetCustomAttribute<JsonConstructorAttribute>())
                    .ToArray();

                Assert.That(attributes, Has.Some.Not.Null, () => $"{type.Name} should have at least one constructor annotated with {nameof(JsonConstructorAttribute)}");
            }
        });
    }
}