#nullable enable
using System.Collections;
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
    T DeepClone();

#if NET7_0_OR_GREATER
    static abstract T Default { get; }
#endif    
}

public sealed class SettingsStore
{
    private readonly ReadOnlyDictionary<Type, ISettings> _settings;

    public SettingsStore(IEnumerable<ISettings> settings)
        : this(settings?.ToDictionary(s => s.GetType()) ?? [])
    { }

    public SettingsStore(IDictionary<Type, ISettings> settingsDictionary)
    {
        if (settingsDictionary.Values.Any(s => !s.IsValid(out var _)))
        {
            throw new NotSupportedException("Settings are not valid is not valid:" + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    settingsDictionary.Values
                        .Where(s => !s.IsValid(out var _))
                        .Select(s =>
                        {
                            s.IsValid(out var err);
                            return $"\t{err} @ {s.GetType().Name} @ {s}";
                        })
            ));
        }
        _settings = new(settingsDictionary);
    }

    //public SettingsStoreBuilder ToBuilder() => new(_settings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings<TSettings> =>
        (TSettings)GetSettingsFor(typeof(TSettings));

    public ISettings GetSettingsFor(Type settingsType) =>
        _settings.TryGetValue(settingsType, out var s)
            ? s
            : throw new NotSupportedException($"No settings registered for {settingsType.GetFriendlyName()}");


    public static readonly IReadOnlyCollection<ISettings> DefaultSettings = [
        CollectionSettings.Default,
        ArraySettings.Default,
        DictionarySettings.Default,
        EnumSettings.Default,
        FactoryMethodSettings.Default,
        ValueTupleSettings.Default,
        KeyValuePairSettings.Default,
        DeconstructableSettings.Default
    ];

    public static SettingsStore GetDefault() => new(DefaultSettings);
}

public sealed class SettingsStoreBuilder : IEnumerable<(Type Type, ISettings Settings)>
{
    private readonly Dictionary<Type, ISettings> _settings;

    public SettingsStoreBuilder() : this([]) { }

    public SettingsStoreBuilder(Dictionary<Type, ISettings> settings) => _settings = settings;

    public SettingsStoreBuilder AddOrUpdate<TSettings>(TSettings settings)
       where TSettings : ISettings
    {
        if (!settings.IsValid(out var err))
            throw new ArgumentException($"{settings.GetType().FullName}: {err}");

        _settings[settings.GetType()] =
            settings ?? throw new ArgumentNullException(nameof(settings));
        return this;
    }

    public SettingsStore Build() => new(_settings);

    public static SettingsStoreBuilder GetDefault() => new(SettingsStore.DefaultSettings.ToDictionary(s => s.GetType()));

    public IEnumerator<(Type Type, ISettings Settings)> GetEnumerator() =>
        _settings.Select(kvp => (kvp.Key, kvp.Value)).ToList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}