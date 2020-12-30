using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using Nemesis.TextParsers.Runtime;

#if !NET
namespace System.Runtime.CompilerServices
{
    // ReSharper disable RedundantNameQualifier
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    // ReSharper restore RedundantNameQualifier
    // ReSharper disable once UnusedMember.Global
    internal static class IsExternalInit { }
}
#endif

namespace Nemesis.TextParsers.Settings
{
    public interface ISettings
    {
        void Validate();
    }

    [PublicAPI]
    public sealed class SettingsStoreBuilder
    {
        private readonly IDictionary<Type, ISettings> _settings;

        public SettingsStoreBuilder() => _settings = new Dictionary<Type, ISettings>();

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public SettingsStoreBuilder(IEnumerable<ISettings> settings)
        {
            if (settings is not null)
                foreach (var s in settings) s.Validate();

            _settings = settings?.ToDictionary(s => s.GetType()) ?? new Dictionary<Type, ISettings>();
        }

        public static SettingsStoreBuilder GetDefault(Assembly fromAssembly = null)
        {
            const BindingFlags PUB_STAT_FLAGS = BindingFlags.Public | BindingFlags.Static;

            var types = (fromAssembly ?? Assembly.GetExecutingAssembly())
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && !t.IsGenericTypeDefinition &&
                            typeof(ISettings).IsAssignableFrom(t)
                );

            var defaultInstances = types
                .Select(t => t.GetProperty("Default", PUB_STAT_FLAGS) is { } defaultProperty &&
                             typeof(ISettings).IsAssignableFrom(defaultProperty.PropertyType)
                    ? (ISettings)defaultProperty.GetValue(null)
                    : throw new NotSupportedException(
                        $"Automatic settings store builder supports {nameof(ISettings)} instances with public static property named 'Default' assignable to {nameof(ISettings)}")
                );

            return new SettingsStoreBuilder(defaultInstances);
        }

        public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings =>
            _settings.TryGetValue(typeof(TSettings), out var s)
                ? (TSettings)s
                : throw new NotSupportedException($"No settings registered for {typeof(TSettings).GetFriendlyName()}");

        public SettingsStoreBuilder AddOrUpdate<TSettings>(TSettings settings) where TSettings : ISettings
        {
            settings.Validate();

            _settings[settings.GetType()] = settings;
            return this;
        }

        public SettingsStore Build() => new(new ReadOnlyDictionary<Type, ISettings>(_settings));
    }

    public sealed class SettingsStore
    {
        private readonly IReadOnlyDictionary<Type, ISettings> _settings;

        public SettingsStore(IReadOnlyDictionary<Type, ISettings> settings)
        {
            foreach (var s in settings.Values) s.Validate();

            _settings = settings;
        }

        public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings =>
            (TSettings)GetSettingsFor(typeof(TSettings));

        public ISettings GetSettingsFor(Type settingsType) =>
            _settings.TryGetValue(settingsType, out var s)
                ? s
                : throw new NotSupportedException($"No settings registered for {settingsType.GetFriendlyName()}");
    }
}
