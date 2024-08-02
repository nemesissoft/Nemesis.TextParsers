using ISettings = Nemesis.TextParsers.Settings.ISettings;

namespace Nemesis.TextParsers.Tests.Arch.Domain;

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
                    if (string.Equals(p.Name, "EqualityContract", StringComparison.Ordinal))
                        continue;

                    var property = p;
                    string desc = $"{type.Name}.{property.Name}";
                    if (property.DeclaringType is not null && property.DeclaringType != type)
                    {
                        desc = $"{property.DeclaringType.Name}.{property.Name}";
                        property = property.DeclaringType.GetProperty(property.Name, BindingAttr)
                         ?? throw new MissingMemberException(desc);
                    }

                    desc = $"{desc} should have at least init/private setter to be accessible for value binding";

                    Assert.That(property.CanWrite, Is.True, desc);
                    Assert.That(property.SetMethod, Is.Not.Null, desc);
                    Assert.That(property.SetMethod.ReturnParameter.GetRequiredCustomModifiers() is { Length: > 0 } reqMods &&
                                Array.IndexOf(reqMods, typeof(IsExternalInit)) > -1,
                        Is.True, desc);
                }
        });
    }

    /*
    //#if NET
    //   [System.Text.Json.Serialization.JsonConstructor]
    //#endif 
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
        }*/
}