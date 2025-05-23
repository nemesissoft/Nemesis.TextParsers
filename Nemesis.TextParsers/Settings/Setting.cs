#nullable enable
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Settings;

public interface ISettings
{
    bool IsValid([NotNullWhen(false)] out string? error);
    ISettings DeepClone();
}

public interface ISettings<out T> : ISettings
    where T : ISettings<T>
{
#if NET7_0_OR_GREATER
    static abstract T Default { get; }
#endif    
}

public sealed class SettingsStore : IEnumerable<ISettings>
{
    private readonly ReadOnlyDictionary<Type, ISettings> _settings;

    public SettingsStore(IEnumerable<ISettings>? settings) : this(settings?.ToDictionary(s => s.GetType()) ?? []) { }

    public SettingsStore(IDictionary<Type, ISettings> settingsDictionary)
    {
        if (settingsDictionary.Values.Any(s => !s.IsValid(out _)))
        {
            throw new NotSupportedException("Settings are not valid is not valid:" + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    settingsDictionary.Values
                        .Where(s => !s.IsValid(out _))
                        .Select(s =>
                        {
                            s.IsValid(out string? err);
                            return $"\t{err} @ {s.GetType().Name} @ {s}";
                        })
            ));
        }
        _settings = new(settingsDictionary);
    }

    public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings<TSettings> =>
        (TSettings)GetSettingsFor(typeof(TSettings));

    public ISettings GetSettingsFor(Type settingsType) =>
        _settings.TryGetValue(settingsType, out var s)
            ? s
            : throw new NotSupportedException($"No settings registered for {settingsType.GetFriendlyName()}");

    public IEnumerator<ISettings> GetEnumerator() => _settings.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#if !NETSTANDARD2_0
[CollectionBuilder(typeof(SettingsStoreBuilder), nameof(Create))]
#endif
public sealed class SettingsStoreBuilder(Dictionary<Type, ISettings>? settings) : IEnumerable<ISettings>
{
    private readonly Dictionary<Type, ISettings> _settings = settings ?? [];

#if !NETSTANDARD2_0
    internal static SettingsStoreBuilder Create(ReadOnlySpan<ISettings> items)
    {
        var builder = GetDefault();

        foreach (var settings in items)
            builder.AddOrUpdate(settings);

        return builder;
    }
#endif

    public SettingsStoreBuilder AddOrUpdate(ISettings item)
    {
        if (!item.IsValid(out string? err))
            throw new ArgumentException($"{item.GetType().FullName}: {err}");

        _settings[item.GetType()] =
            item ?? throw new ArgumentNullException(nameof(item));
        return this;
    }

    public SettingsStoreBuilder AddOrUpdateRange(IEnumerable<ISettings> settingsCollection)
    {
        foreach (var settings in settingsCollection)
            AddOrUpdate(settings);
        return this;
    }

    public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings<TSettings> =>
       _settings.TryGetValue(typeof(TSettings), out var s)
            ? (TSettings)s
            : throw new NotSupportedException($"No settings registered for {typeof(TSettings).GetFriendlyName()}");

    public SettingsStore Build() => new(_settings);

    public static SettingsStoreBuilder GetDefault() => new(TextTransformer.DefaultSettings.ToDictionary(s => s.GetType()));

    public IEnumerator<ISettings> GetEnumerator()
    {
        foreach(var settings in _settings.Values)
            yield return settings;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public IEnumerable<(Type Type, ISettings Settings)> GetEntries()
    {
        foreach (var kvp in _settings)
            yield return (kvp.Key, kvp.Value);
    }
}