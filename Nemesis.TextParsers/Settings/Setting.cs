#nullable enable
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Settings;

public interface ISettings
{
    bool IsValid([NotNullWhen(false)] out string? error);
}

public interface ISettings<T> : ISettings
    where T : ISettings<T>
{
#if NET7_0_OR_GREATER
    static abstract T Default { get; }
#endif
}

[JetBrains.Annotations.PublicAPI]
public sealed class SettingsStoreBuilder
{
    private readonly Dictionary<Type, ISettings> _settings;

    public SettingsStoreBuilder() => _settings = [];

    public SettingsStoreBuilder(IEnumerable<ISettings> settings)
    {
        if (settings.Any(s => !s.IsValid(out var _)))
        {
            throw new NotSupportedException("Settings are not valid is not valid:" + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    settings
                        .Where(s => !s.IsValid(out var _))
                        .Select(s =>
                        {
                            s.IsValid(out var err);
                            return $"\t{err} @ {s.GetType().Name} @ {s}";
                        })
            ));
        }

        _settings = settings?.ToDictionary(s => s.GetType()) ?? [];
    }

    public static SettingsStoreBuilder GetDefault(Assembly? fromAssembly = null)
    {
        var types = (fromAssembly ?? Assembly.GetExecutingAssembly())
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                        typeof(ISettings).IsAssignableFrom(t) &&
                        t.DerivesOrImplementsGeneric(typeof(ISettings<>))
            );

        var defaultInstances = types.Select(t =>
            t.GetProperty("Default", BindingFlags.Public | BindingFlags.Static) is { } defaultProperty &&
            defaultProperty.PropertyType.DerivesOrImplementsGeneric(typeof(ISettings<>)) &&
            defaultProperty.GetValue(null) is ISettings s
            ? s
            : throw new NotSupportedException(
                $"Automatic settings store builder supports {nameof(ISettings)} instances with public static property named 'Default' assignable to {nameof(ISettings)}")
            );

        return new SettingsStoreBuilder(defaultInstances);
    }

    /*public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings =>
        _settings.TryGetValue(typeof(TSettings), out var s)
            ? (TSettings)s
            : throw new NotSupportedException($"No settings registered for {typeof(TSettings).GetFriendlyName()}");*/

    public SettingsStoreBuilder AddOrUpdate<TSettings>(TSettings settings)
        where TSettings : ISettings
    {
        if (!settings.IsValid(out var err))
            throw new ArgumentException($"{settings.GetType().FullName}: {err}");

        _settings[settings.GetType()] =
            settings ?? throw new ArgumentNullException(nameof(settings));
        return this;
    }

    public SettingsStore Build() => new(new ReadOnlyDictionary<Type, ISettings>(_settings));
}

public sealed class SettingsStore
{
    private readonly IReadOnlyDictionary<Type, ISettings> _settings;

    public SettingsStore(IReadOnlyDictionary<Type, ISettings> settings) => _settings = settings;

    public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings<TSettings> =>
        (TSettings)GetSettingsFor(typeof(TSettings));

    public ISettings GetSettingsFor(Type settingsType) =>
        _settings.TryGetValue(settingsType, out var s)
            ? s
            : throw new NotSupportedException($"No settings registered for {settingsType.GetFriendlyName()}");
}