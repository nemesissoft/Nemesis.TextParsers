using System;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests
{
    internal static class Sut
    {
        public static ITransformerStore DefaultStore { get; } = TextTransformer.Default;
        public static ITransformerStore ThrowOnDuplicateStore { get; }

        static Sut()
        {
            var throwOnDuplicateSettings = DictionarySettings.Default
                .With(s => s.Behaviour, DictionaryBehaviour.ThrowOnDuplicate);
            var settingsStore = SettingsStoreBuilder.GetDefault()
                .AddOrUpdate(throwOnDuplicateSettings).Build();
            ThrowOnDuplicateStore = TextTransformer.GetDefaultStoreWith(settingsStore);
        }

        public static ITransformer<TElement> GetTransformer<TElement>()
            => DefaultStore.GetTransformer<TElement>();

        public static ITransformer GetTransformer(Type type)
            => DefaultStore.GetTransformer(type);
    }
}
