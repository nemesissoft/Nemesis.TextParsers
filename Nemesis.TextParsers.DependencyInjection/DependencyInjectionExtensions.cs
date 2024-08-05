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

                //TODO clone settings records
                //settings = settings with { };

                section.Bind(settings, bo => { bo.BindNonPublicProperties = true; bo.ErrorOnUnknownConfiguration = true; });

                settingsStoreBuilder.AddOrUpdate(settings);
            }
        }

        configureSettingsStoreBuilder?.Invoke(settingsStoreBuilder);

        settingsStoreBuilder.Build();

        foreach (var (type, settings) in settingsStoreBuilder)
            services.AddSingleton(type, settings);

        services.AddSingleton<SettingsStore>(settingsStoreBuilder.Build());

        return services;
    }


    public static IServiceCollection AddNemesisTextParsers(this IServiceCollection services)
    {
        services.AddSingleton<ITransformerStore>(
            provider => TextTransformer.GetDefaultStoreWith(provider.GetRequiredService<SettingsStore>())
        );

        //TODO add settings/parameters to determine whether to register additional types 
        services.AddSingleton<TextSyntaxProvider>();


        //TODO add generic transformer 
        /*services.AddSingleton(typeof(ITransformer<>), 
            provider => TextTransformer.GetDefaultStoreWith(provider.GetRequiredService<SettingsStore>())
        );*/


        return services;
    }
}
