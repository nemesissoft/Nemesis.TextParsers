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

        //TODO add generic transformer ?
        /*services.AddSingleton(typeof(ITransformer<>), 
            provider => TextTransformer.GetDefaultStoreWith(provider.GetRequiredService<SettingsStore>())
        );*/

        return services;
    }
}
