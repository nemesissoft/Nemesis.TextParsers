using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Settings;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

//TODO allow for deserialization of immutable options
var parsingSettings = builder.Configuration.GetRequiredSection("ParsingSettings").Get<ParsingSettings>()!;
var transformerStore = parsingSettings.ToTransformerStore();


services.ConfigureHttpJsonOptions(options =>
{
    //TODO set JsonSerializerIsReflectionEnabledByDefault to false and make use of defined TypeInfoResolver below 
    //options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
    //options.SerializerOptions.TypeInfoResolver = AppJsonSerializerContext.Default;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/showConfigurations", () => parsingSettings)
    .WithName("ShowConfigurations").WithOpenApi();

app.MapPost("/parse/{type}", (string type, [FromQuery] string text) =>
{
    var t = Type.GetType(type);
    var parsed = transformerStore.GetTransformer(t).ParseObject(text);
    return parsed;
}).WithName("Parse").WithOpenApi();

app.Run();

sealed class ParsingSettings
{
    public CollectionSettings CollectionSettings { get; init; } = CollectionSettings.Default;
    public ArraySettings ArraySettings { get; init; } = ArraySettings.Default;
    public DictionarySettings DictionarySettings { get; init; } = DictionarySettings.Default;

    public EnumSettings EnumSettings { get; init; } = EnumSettings.Default;

    public FactoryMethodSettings FactoryMethodSettings { get; init; } = FactoryMethodSettings.Default;

    public ValueTupleSettings ValueTupleSettings { get; init; } = ValueTupleSettings.Default;
    public KeyValuePairSettings KeyValuePairSettings { get; init; } = KeyValuePairSettings.Default;
    public DeconstructableSettings DeconstructableSettings { get; init; } = DeconstructableSettings.Default;

    public ITransformerStore ToTransformerStore()
    {
        var settingsStore = SettingsStoreBuilder.GetDefault()
            .AddOrUpdate(CollectionSettings)
            .AddOrUpdate(ArraySettings)
            .AddOrUpdate(DictionarySettings)
            .AddOrUpdate(EnumSettings)
            .AddOrUpdate(FactoryMethodSettings)
            .AddOrUpdate(ValueTupleSettings)
            .AddOrUpdate(KeyValuePairSettings)
            .AddOrUpdate(DeconstructableSettings)
            .Build();

        return TextTransformer.GetDefaultStoreWith(settingsStore);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
[JsonSerializable(typeof(ParsingSettings))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }