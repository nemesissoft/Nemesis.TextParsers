using System;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests
{
    internal static class Sut
    {
        public static ITransformerStore DefaultStore { get; } = TextTransformer.Default;
        public static ITransformerStore ThrowOnDuplicateStore { get; }
        public static ITransformerStore BorderedStore { get; }

        static Sut()
        {
            ThrowOnDuplicateStore = TextTransformer.GetDefaultStoreWith(
                SettingsStoreBuilder.GetDefault()
                .AddOrUpdate(
                    DictionarySettings.Default
                        .With(s => s.Behaviour, DictionaryBehaviour.ThrowOnDuplicate)
                ).Build());

            //F# influenced settings 
            var borderedDictionary = DictionarySettings.Default
                .With(s => s.Start, '{')
                .With(s => s.End, '}')
                .With(s => s.DictionaryKeyValueDelimiter, ',')
                ;
            var borderedCollection = CollectionSettings.Default
                .With(s => s.Start, '[')
                .With(s => s.End, ']')
                .With(s => s.ListDelimiter, ';')
                ;
            var borderedArray = ArraySettings.Default
                .With(s => s.ListDelimiter, ',')
                .With(s => s.Start, '|')
                .With(s => s.End, '|')
                ;
            var weirdTuple = ValueTupleSettings.Default
                    .With(s => s.NullElementMarker, '␀')
                    .With(s => s.Delimiter, '⮿')
                    .With(s => s.Start, '/')
                    .With(s => s.End, '/')
                ;
            var borderedStore = SettingsStoreBuilder.GetDefault()
                .AddOrUpdate(borderedArray)
                .AddOrUpdate(borderedCollection)
                .AddOrUpdate(borderedDictionary)
                .AddOrUpdate(weirdTuple)
                .Build();
            BorderedStore = TextTransformer.GetDefaultStoreWith(borderedStore);
        }

        public static ITransformer<TElement> GetTransformer<TElement>()
            => DefaultStore.GetTransformer<TElement>();

        public static ITransformer GetTransformer(Type type)
            => DefaultStore.GetTransformer(type);
    }
}
