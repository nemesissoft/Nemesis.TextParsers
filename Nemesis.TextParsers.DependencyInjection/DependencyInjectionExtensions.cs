#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;


namespace Nemesis.TextParsers.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection ConfigureNemesisTextParsers(
        this IServiceCollection services,
        IConfiguration? config = null,
        Action<SettingsStoreBuilder>? configureSettingsStoreBuilder = null
        )
    {
        var settingsStoreBuilder = SettingsStoreBuilder.GetDefault();

        if (config is not null)
        {
            foreach (var (type, settings) in settingsStoreBuilder)
            {
                var section = config.GetSection(type.Name);
                if (!section.GetChildren().Any()) continue;

                var newSettings = settings.DeepClone();

                section.Bind(newSettings, bo => { bo.BindNonPublicProperties = true; bo.ErrorOnUnknownConfiguration = true; });

                settingsStoreBuilder.AddOrUpdate(newSettings);
            }
        }

        configureSettingsStoreBuilder?.Invoke(settingsStoreBuilder);

        foreach (var (type, settings) in settingsStoreBuilder)
        {
            services.AddSingleton(type, settings);
            services.AddSingleton<ISettings>(settings);
        }

        services.AddSingleton<SettingsStore>(settingsStoreBuilder.Build());

        return services;
    }

    public static IServiceCollection AddNemesisTextParsers(
        this IServiceCollection services,
        Action<List<Type>>? configureTransformerHandlers = null
    )
    {
        IEnumerable<Type> transformerHandlerTypes = TextTransformer.DefaultHandlers;
        if (configureTransformerHandlers is not null)
        {
            var list = transformerHandlerTypes.ToList();
            configureTransformerHandlers(list);
            transformerHandlerTypes = list;
        }

        services.AddSingleton<ITransformerStore>(
            provider => StandardTransformerStore.Create(transformerHandlerTypes, provider.GetRequiredService<SettingsStore>())
        );

        services.AddSingleton<TextSyntaxProvider>();

        services.AddSingleton(typeof(ITransformer<>), typeof(TransformerWrapper<>));

        return services;
    }
}

class TransformerWrapper<T>(ITransformerStore transformerStore) : ITransformer<T>
{
    private readonly ITransformer<T> _transformer = transformerStore.GetTransformer<T>();

    public string Format(T element) => _transformer.Format(element);

    public string FormatObject(object element) => _transformer.FormatObject(element);

    public T GetEmpty() => _transformer.GetEmpty();

    public object GetEmptyObject() => _transformer.GetEmptyObject();

    public T GetNull() => _transformer.GetNull();

    public object GetNullObject() => _transformer.GetNullObject();

    public T Parse(string text) => _transformer.Parse(text);

    public T Parse(in ReadOnlySpan<char> input) => _transformer.Parse(input);

    public object ParseObject(string text) => _transformer.ParseObject(text);

    public object ParseObject(in ReadOnlySpan<char> input) => _transformer.ParseObject(input);

    public bool TryParse(in ReadOnlySpan<char> input, out T result) => _transformer.TryParse(input, out result);

    public bool TryParseObject(in ReadOnlySpan<char> input, out object result) => _transformer.TryParseObject(input, out result);
}