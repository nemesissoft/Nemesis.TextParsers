# Nemesis.TextParsers.DependencyInjection

## Intro
This package contains helper methods useful to setup DependencyInjection using [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

```csharp
//use Nemesis.TextParsers.DependencyInjection package
//consider the following ASP.Net demo
var builder = WebApplication.CreateBuilder(args);
//...
builder.Services
    .ConfigureNemesisTextParsers(builder.Configuration.GetRequiredSection("ParsingSettings"))
    .AddNemesisTextParsers();

var app = builder.Build();
app.MapGet("/parsingConfigurations/{type}", (string type, SettingsStore store) => 
{ 
    //use injected SettingsStore 
});

app.MapGet("/parseType/{type}", (string type, [FromQuery] string text, ITransformerStore transformerStore) => 
{
    //use injected ITransformerStore 
});
```