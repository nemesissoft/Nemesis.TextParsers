using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers.Settings
{
    //marker interface
    public interface ISettings { }

    [PublicAPI]
    public sealed class SettingsStoreBuilder
    {
        private readonly IDictionary<Type, ISettings> _settings;

        public SettingsStoreBuilder() => _settings = new Dictionary<Type, ISettings>();

        public SettingsStoreBuilder(IEnumerable<ISettings> settings) =>
            _settings = settings?.ToDictionary(s => s.GetType()) ?? new Dictionary<Type, ISettings>();

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

        public SettingsStoreBuilder AddOrUpdate<TSettings>([NotNull] TSettings settings) where TSettings : ISettings
        {
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

        public TSettings GetSettingsFor<TSettings>() where TSettings : ISettings =>
            (TSettings)GetSettingsFor(typeof(TSettings));

        public ISettings GetSettingsFor(Type settingsType) =>
            _settings.TryGetValue(settingsType, out var s)
                ? s
                : throw new NotSupportedException($"No settings registered for {settingsType.GetFriendlyName()}");
    }

    public static class SettingsHelper
    {
        public static TSettings With<TSettings, TProp>(this TSettings settings, Expression<Func<TSettings, TProp>> propertyExpression, TProp newValue)
            where TSettings : ISettings
        {
            static bool EqualNames(string s1, string s2) => string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);

            var property = Property.Of(propertyExpression);
            var ctor = typeof(TSettings).GetConstructors().Select(c => (Ctor: c, Params: c.GetParameters()))
                .Where(pair =>
                    pair.Params.Length > 0 &&
                    pair.Params.Any(p => EqualNames(p.Name, property.Name))
                )
                .OrderByDescending(p => p.Params.Length)
                .FirstOrDefault().Ctor ?? throw new NotSupportedException("No suitable constructor found");

            var allProperties =
                typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            object GetArg(string paramName)
            {
                if (EqualNames(paramName, property.Name))
                    return newValue;
                else
                {
                    var prop = allProperties.FirstOrDefault(p => EqualNames(paramName, p.Name))
                        ?? throw new NotSupportedException($"No suitable property found: {paramName} +/- letter casing");
                    var value = prop.GetValue(settings);
                    return value;
                }
            }

            var arguments = ctor.GetParameters()
                .Select(p => GetArg(p.Name)).ToArray();

            return (TSettings)ctor.Invoke(arguments);
        }
    }
}
