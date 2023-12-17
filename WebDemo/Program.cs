using WebDemo;
using WebDemo.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options => { options.SchemaFilter<EnumSchemaFilter>(); });

var parsingSettings = builder.Configuration.GetRequiredSection(nameof(ParsingSettings))
    .Get<ParsingSettings>(op => { op.BindNonPublicProperties = true; op.ErrorOnUnknownConfiguration = true; })!;
// TODO add validation after (make records) + check if validation triggers after Bind()


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
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/showConfigurations", () => parsingSettings)
    .WithName("ShowConfigurations").WithOpenApi();

app.MapPost("/parse/{type}", (string type, [FromQuery] string text) =>
{
    var t = Type.GetType(type);
    if (t is null) return Results.BadRequest($"Type '{type}' cannot be parsed");

    ITransformer transformer;
    try
    {
        transformer = transformerStore.GetTransformer(t);
    }
    catch (Exception ex) when (ex.InnerException is NotSupportedException)
    {
        return Results.NotFound($"Type '{type}' is not supported for text transformation");
    }

    try
    {
        var parsed = transformer.ParseObject(text);
        return parsed;
    }
    catch (Exception e)
    {
        return Results.Problem($"Text '{text}' failed to parse with message: {e.Message}");
    }
}).WithName("Parse").WithOpenApi();

app.Run();



[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(ParsingSettings))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }