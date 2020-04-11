using System;
using System.Collections.Generic;
using System.Linq;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Settings;

namespace Nemesis.TextParsers.Tests
{
    internal static class Sut
    {
        public static ITransformerStore DefaultStore { get; } = TextTransformer.Default;
        public static ITransformerStore ThrowOnDuplicateStore { get; }
        public static ITransformerStore BorderedStore { get; }
        public static ITransformerStore RandomStore { get; }

        static Sut()
        {
            ThrowOnDuplicateStore = TextTransformer.GetDefaultStoreWith(
                SettingsStoreBuilder.GetDefault()
                .AddOrUpdate(
                    DictionarySettings.Default
                        .With(s => s.Behaviour, DictionaryBehaviour.ThrowOnDuplicate)
                ).Build());


            BorderedStore = BuildBorderedStore();
            RandomStore = BuildRandomStore();
        }

        private static ITransformerStore BuildRandomStore()
        {
            static bool IsChar(Type t) => t == typeof(char) || t == typeof(char?);

            var settingTypes = new[]
            {
                typeof(ArraySettings), typeof(CollectionSettings), typeof(DictionarySettings),
                typeof(DeconstructableSettings), typeof(KeyValuePairSettings), typeof(ValueTupleSettings),
            };
            int seed = Environment.TickCount / 10;
            var special = new[] { '\\', '|', ';', '=', '∅', ',', '{', '}', '[', ']', '(', ')', '⮿', '␀', '!', '@', '#', '$', '%', '&' };

            Stack<char> GetRandomChars(int length, string reason)
            {
                
                seed += 10;
                Console.WriteLine($"Seed for {reason} = {seed}");
                var rand = new Random(seed);
                var result = new HashSet<char>(length);
                do
                {
                    result.Add(
                        special[rand.Next(special.Length)]
                    );
                } while (result.Count < length);

                return new Stack<char>(result);
            }


            var settings = new List<ISettings>();

            foreach (var settingType in settingTypes)
            {
                var ctor = settingType.GetConstructors().Select(c => (Ctor: c, Params: c.GetParameters()))
                    .Where(pair => pair.Params.Length > 0)
                    .OrderByDescending(p => p.Params.Length)
                    .FirstOrDefault().Ctor ?? throw new NotSupportedException($"No suitable constructor found for {settingType}");
                var @params = ctor.GetParameters();
                var charNum = @params.Count(p => IsChar(p.ParameterType));
                var chars = GetRandomChars(charNum, settingType.Name);

                var args = @params
                    .Select(p => p.ParameterType)
                    .Select(t => IsChar(t) ? chars.Pop() : TypeMeta.GetDefault(t))
                    .ToArray();
                settings.Add((ISettings)ctor.Invoke(args));
            }


            var settingsStoreBuilder = SettingsStoreBuilder.GetDefault();

            foreach (var s in settings)
                settingsStoreBuilder.AddOrUpdate(s);

            return TextTransformer.GetDefaultStoreWith(settingsStoreBuilder.Build());
        }

        private static ITransformerStore BuildBorderedStore()
        {
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

            return TextTransformer.GetDefaultStoreWith(borderedStore);
        }

        public static ITransformer<TElement> GetTransformer<TElement>()
            => DefaultStore.GetTransformer<TElement>();

        public static ITransformer GetTransformer(Type type)
            => DefaultStore.GetTransformer(type);
    }
}
