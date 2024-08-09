# Nemesis.TextParsers.DependencyInjection

## Intro
This package contains helper methods useful to setup DependencyInjection using [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)

## Example
Given the following setting:
```json
{  
  "ParsingSettings": {
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
}

```

one can use`ConfigureNemesisTextParsers`method to configure settings and`AddNemesisTextParsers`to add NTP services to DI container

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

More info can be found in [example](https://github.com/nemesissoft/Nemesis.TextParsers/blob/93d7c20728f6aad665f253076f5fba9bdad6b33f/WebDemo/Program.cs#L43)