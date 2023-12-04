using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests.Infrastructure;

[TestFixture]
public class SettingsPersistenceTests
{
    const string ConfigJsonFile = """
        {
          "CollectionSettings": {
            "ListDelimiter": ",",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "/",
            "Start": "[",
            "End": "]",
            "DefaultCapacity": 60
          },
          "ArraySettings": {
            "ListDelimiter": ";",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "/",
            "Start": "{",
            "End": "}",
            "DefaultCapacity": 70
          },
          "DictionarySettings": {
            "DictionaryPairsDelimiter": "⮿",
            "DictionaryKeyValueDelimiter": ">",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "/",
            "Start": "(",
            "End": ")",
            "Behaviour": "DoNotOverrideKeys",
            "DefaultCapacity": 80
          },
          "EnumSettings": {
            "CaseInsensitive": false,
            "AllowParsingNumerics": false
          },
          "FactoryMethodSettings": {
            "FactoryMethodName": "FromString",
            "EmptyPropertyName": "EmptyProp",
            "NullPropertyName": "NullProp"
          },
          "ValueTupleSettings": {
            "Delimiter": ";",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "/",
            "Start": "_",
            "End": "_"
          },
          "KeyValuePairSettings": {
            "Delimiter": "→",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "/",
            "Start": "-",
            "End": "-"
          },
          "DeconstructableSettings": {
            "UseDeconstructableEmpty": false,
            "Delimiter": ",",
            "NullElementMarker": "␀",
            "EscapingSequenceStart": "_",
            "Start": "/",
            "End": "/"
          }
        }
        """;

    private static IEnumerable<TCD> GetSettingsTestData(string category) => new ISettings[]
    {
        new CollectionSettings(',', '␀', '/', '[', ']', 60),
        new ArraySettings(';', '␀', '/', '{', '}', 70),
        new DictionarySettings('⮿', '>', '␀', '/', '(', ')', DictionaryBehaviour.DoNotOverrideKeys, 80),
        new EnumSettings(false, false),
        new FactoryMethodSettings("FromString", "EmptyProp", "NullProp"),
        new ValueTupleSettings(';', '␀', '/', '_', '_'),
        new KeyValuePairSettings('→', '␀', '/', '-', '-'),
        new DeconstructableSettings(',', '␀', '_', '/', '/', false)
    }.Select((s, i) => new TCD(s).SetName($"{category}_{i + 1:00}_{s.GetType().Name}"));

    [TestCaseSource(nameof(GetSettingsTestData), new object[] { "Refl" })]
    public void SettingsCanBeReadFromAndWrittenToJsonFile_UsingReflection(ISettings settings)
    {
        var option = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        SettingsPersistanceHelper(settings,
            obj => JsonSerializer.Serialize(obj, settings.GetType(), option),
            text => JsonSerializer.Deserialize(text, settings.GetType(), option)
        );
    }

    [TestCaseSource(nameof(GetSettingsTestData), new object[] { "CodeGen" })]
    public void SettingsCanBeReadFromAndWrittenToJsonFile_UsingCodeGen(ISettings settings)
    {
        SettingsPersistanceHelper(settings,
            obj => JsonSerializer.Serialize(obj, settings.GetType(), TestSerializerContext.Default),
            text => JsonSerializer.Deserialize(text, settings.GetType(), TestSerializerContext.Default)
        );
    }

    private static void SettingsPersistanceHelper(ISettings settings,
        Func<object, string> serializer,
        Func<string, object> deserializer)
    {
        var settingsType = settings.GetType();
        var doc = JsonNode.Parse(ConfigJsonFile);
        var section = doc[settingsType.Name].ToString();

        var deser1 = deserializer(section);
        AssertMutualEquivalence(deser1, settings);


        var fromText = serializer(deser1);
        var fromTestData = serializer(settings);

        var deser2 = deserializer(fromText);
        var deser3 = deserializer(fromTestData);

        AssertMutualEquivalence(deser2, settings);
        AssertMutualEquivalence(deser3, settings);
        AssertMutualEquivalence(deser2, deser3);
    }

    [TestCaseSource(nameof(GetSettingsTestData), new object[] { "Config" })]
    public void SettingsCanBeReadFromJsonFile_UsingConfigurationBinder(ISettings settings)
    {
        var settingsType = settings.GetType();
        var configuration = GetConfiguration();

        var actual = configuration.GetRequiredSection(settingsType.Name)
            .Get(settingsType,
            op => { op.BindNonPublicProperties = true; op.ErrorOnUnknownConfiguration = true; }
        )!;

        AssertMutualEquivalence(actual, settings);

        static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ConfigJsonFile));
            builder.AddJsonStream(stream);
            return builder.Build();
        }
    }

    static void AssertMutualEquivalence(object o1, object o2)
    {
        o1.Should().NotBeSameAs(o2);

        o1.Should().BeEquivalentTo(o2, options => options.RespectingRuntimeTypes());
        o2.Should().BeEquivalentTo(o1, options => options.RespectingRuntimeTypes());
    }
}

[JsonSourceGenerationOptions(
     PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
     UseStringEnumConverter = true
 )]
[JsonSerializable(typeof(CollectionSettings))]
[JsonSerializable(typeof(ArraySettings))]
[JsonSerializable(typeof(DictionarySettings))]
[JsonSerializable(typeof(EnumSettings))]
[JsonSerializable(typeof(FactoryMethodSettings))]
[JsonSerializable(typeof(ValueTupleSettings))]
[JsonSerializable(typeof(KeyValuePairSettings))]
[JsonSerializable(typeof(DeconstructableSettings))]
internal partial class TestSerializerContext : JsonSerializerContext { }
