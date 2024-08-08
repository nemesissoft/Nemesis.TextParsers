using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Nemesis.TextParsers.DependencyInjection;
using Nemesis.TextParsers.Settings;
using WebDemo.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.ConfigureHttpJsonOptions(options =>
{
    IJsonTypeInfoResolver resolver = JsonSerializer.IsReflectionEnabledByDefault
            ? new DefaultJsonTypeInfoResolver()
            : AppJsonSerializerContext.Default;

    resolver = resolver.WithAddedModifier(
        static typeInfo =>
        {
            if (typeInfo.Type == typeof(ISettings))
            {
                typeInfo.PolymorphismOptions = new()
                {
                    TypeDiscriminatorPropertyName = "_case",
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(CollectionSettings), nameof(CollectionSettings)),
                        new JsonDerivedType(typeof(ArraySettings), nameof(ArraySettings)),
                        new JsonDerivedType(typeof(DictionarySettings), nameof(DictionarySettings)),
                        new JsonDerivedType(typeof(EnumSettings), nameof(EnumSettings)),
                        new JsonDerivedType(typeof(FactoryMethodSettings), nameof(FactoryMethodSettings)),
                        new JsonDerivedType(typeof(ValueTupleSettings), nameof(ValueTupleSettings)),
                        new JsonDerivedType(typeof(KeyValuePairSettings), nameof(KeyValuePairSettings)),
                        new JsonDerivedType(typeof(DeconstructableSettings), nameof(DeconstructableSettings)),
                    }
                };
            }
        });

    options.SerializerOptions.TypeInfoResolver = resolver;
});

services.AddEndpointsApiExplorer()
        .AddSwaggerGen(options => { options.SchemaFilter<EnumSchemaFilter>(); });

services.ConfigureNemesisTextParsers(builder.Configuration.GetRequiredSection("ParsingSettings"))
        .AddNemesisTextParsers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage()
        .UseSwagger().UseSwaggerUI();
}

app.UseHttpsRedirection();

var configurationGroup = app.MapGroup("parsingConfigurations").WithOpenApi();
configurationGroup.MapGet("", (SettingsStore store) => store.ToList());

configurationGroup.MapGet("{type}", (string type, SettingsStore store) =>
{
    var parsedType = Type.GetType(type) ??
                     Type.GetType($"Nemesis.TextParsers.Settings.{type}") ??
                     typeof(ISettings).Assembly.GetType(type) ??
                     typeof(ISettings).Assembly.GetType($"Nemesis.TextParsers.Settings.{type}");
    if (parsedType is null) return Results.BadRequest($"Type '{type}' cannot be parsed");

    return Results.Ok(store.GetSettingsFor(parsedType));
}
);

app.MapGet("/parseType/{type}", (string type, [FromQuery] string text, ITransformerStore transformerStore) =>
{
    var parsedType = Type.GetType(type);
    if (parsedType is null) return Results.BadRequest($"Type '{type}' cannot be parsed");

    ITransformer transformer;
    try
    {
        transformer = transformerStore.GetTransformer(parsedType);
    }
    catch (Exception ex) when (ex.InnerException is NotSupportedException)
    {
        return Results.NotFound($"Type '{type}' is not supported for text transformation");
    }

    try
    {
        var parsed = transformer.ParseObject(text);
        return Results.Ok(parsed);
    }
    catch (Exception e)
    {
        return Results.Problem($"Text '{text}' failed to parse with message: {e.Message}");
    }
}).WithOpenApi();


//try the following: [␀,{A;B;C},{D;E;F;␀}]
app.MapGet("/parse/listOfStringArrays", ([FromQuery] string text, [FromServices] ITransformer<List<string[]>> transformer) =>
{
    try
    {
        var parsed = transformer.Parse(text);
        return Results.Ok(parsed);
    }
    catch (Exception e)
    {
        return Results.Problem($"Text '{text}' failed to parse with message: {e.Message}");
    }
}).WithOpenApi();

app.Run();

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(ISettings))]
[JsonSerializable(typeof(List<ISettings>))]
[JsonSerializable(typeof(CollectionSettings))]
[JsonSerializable(typeof(ArraySettings))]
[JsonSerializable(typeof(DictionarySettings))]
[JsonSerializable(typeof(EnumSettings))]
[JsonSerializable(typeof(FactoryMethodSettings))]
[JsonSerializable(typeof(ValueTupleSettings))]
[JsonSerializable(typeof(KeyValuePairSettings))]
[JsonSerializable(typeof(DeconstructableSettings))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }