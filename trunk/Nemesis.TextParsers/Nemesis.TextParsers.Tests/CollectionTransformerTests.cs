using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using System.Linq;
using System.Reflection;


namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class CollectionTransformerTests
    {
        internal static IEnumerable<(Type, string, int, Type)> Correct_Data() => new[]
        {
            //array
            (typeof(float[][]), @"1\|2\|3 | 4\|5\|6\|7", 2, typeof(float[][])),
            (typeof(int[]), @"10|2|3", 3, typeof(int[])),
            (typeof(int[]), @"", 0, typeof(int[])),
            (typeof(int[]), null, 0, typeof(int[])),

            //collections
            (typeof(List<int>), @"10|2|3", 3, typeof(List<int>)),
            (typeof(IList<int>), @"15|2|3|5", 4, typeof(List<int>)),
            (typeof(ICollection<int>), @"44|2|3|5", 4, typeof(List<int>)),
            (typeof(IEnumerable<int>), @"55|2|3|5", 4, typeof(List<int>)),

            (typeof(ReadOnlyCollection<int>), @"16|2|3|5", 4, typeof(ReadOnlyCollection<int>)),
            (typeof(IReadOnlyCollection<int>), @"26|2|3|5", 4, typeof(ReadOnlyCollection<int>)),
            (typeof(IReadOnlyList<int>), @"36|2|3|5", 4, typeof(ReadOnlyCollection<int>)),

            (typeof(ISet<int>), @"17|2|3|5", 4, typeof(HashSet<int>)),
            (typeof(HashSet<int>), @"37|2|3|5", 4, typeof(HashSet<int>)),
            (typeof(SortedSet<int>), @"27|2|3|5", 4, typeof(SortedSet<int>)),

            (typeof(LinkedList<int>), @"37|2|3|5|16", 5, typeof(LinkedList<int>)),
            (typeof(Stack<int>), @"37|2|3|5|26", 5, typeof(Stack<int>)),
            (typeof(Queue<int>), @"37|2|3|5|36", 5, typeof(Queue<int>)),


            //lean collection
            (typeof(LeanCollection<byte>), @"", 0, typeof(LeanCollection<byte>)),
            (typeof(LeanCollection<int>), @"1", 1, typeof(LeanCollection<int>)),
            (typeof(LeanCollection<uint>), @"1|2", 2, typeof(LeanCollection<uint>)),
            (typeof(LeanCollection<short>), @"1|2|3", 3, typeof(LeanCollection<short>)),
            (typeof(LeanCollection<ushort>), @"1|2|3|4", 4, typeof(LeanCollection<ushort>)),
            (typeof(LeanCollection<float>), @"1|2|3|4|5", 5, typeof(LeanCollection<float>)),


            //dictionary like collection
            (typeof(IEnumerable<KeyValuePair<int, string>>), @"1=One|2=Two|0=Zero", 3, typeof(List<KeyValuePair<int, string>>)),
            (typeof(ICollection<KeyValuePair<int, string>>), @"1=One|2=Two|0=Zero", 3, typeof(List<KeyValuePair<int, string>>)),
            (typeof(IReadOnlyCollection<KeyValuePair<int, string>>), @"1=One|2=Two|0=Zero", 3, typeof(ReadOnlyCollection<KeyValuePair<int, string>>)),
            (typeof(IReadOnlyList<KeyValuePair<int, string>>), @"1=One|2=Two|0=Zero", 3, typeof(ReadOnlyCollection<KeyValuePair<int, string>>)),


            //dictionary
            (typeof(Dictionary<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(Dictionary<int, string>)),
            (typeof(IDictionary<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(Dictionary<int, string>)),

            (typeof(ReadOnlyDictionary<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(ReadOnlyDictionary<int, string>)),
            (typeof(IReadOnlyDictionary<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(ReadOnlyDictionary<int, string>)),

            (typeof(SortedList<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(SortedList<int, string>)),
            (typeof(SortedDictionary<int, string>), @"1=One;2=Two;0=Zero", 3, typeof(SortedDictionary<int, string>)),

            (typeof(Dictionary<int, string>), @"", 0, typeof(Dictionary<int, string>)),
            (typeof(IDictionary<int, string>), @"", 0, typeof(Dictionary<int, string>)),
            (typeof(SortedList<int, string>), @"", 0, typeof(SortedList<int, string>)),
            (typeof(SortedDictionary<int, string>), @"", 0, typeof(SortedDictionary<int, string>)),


            //custom collections 
            (typeof(StringList), @"ABC|DEF|GHI", 3, typeof(StringList)),
            (typeof(StringList), @"", 0, typeof(StringList)),
        };

        class StringList : List<string> { }

        [TestCaseSource(nameof(Correct_Data))]
        public void CollectionType_CompoundTest((Type contractType, string input, int cardinality, Type concreteType) data)
        {
            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            var getTransformer = (typeof(ITextTransformer).GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                                    mi.Name == nameof(ITextTransformer.GetTransformer) && mi.IsGenericMethod)
                                ?? throw new MissingMethodException("Method CreateTransformer does not exist"))
                    .MakeGenericMethod(data.contractType);

            var transformer = getTransformer.Invoke(TextTransformer.Default, null);
            Console.WriteLine(transformer);

            var parseMethod = typeof(IParser<>).MakeGenericType(data.contractType)
                                  .GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                    mi.Name == nameof(IParser<object>.ParseText))
                    ?? throw new MissingMethodException("Method ParseText does not exist");

            var formatMethod = transformer.GetType().GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                                   mi.Name == nameof(ITransformer<object>.Format))
                               ?? throw new MissingMethodException("Method Format does not exist");


            var parsed = parseMethod.Invoke(transformer, new object[] { data.input });
            Assert.That(parsed, Is.TypeOf(data.concreteType));


            var cardinalityProp = parsed.GetType().GetProperties(ALL_FLAGS)
                .FirstOrDefault(p => p.Name == "Size" || p.Name == "Count" || p.Name == "Length");
            Assert.That(cardinalityProp, Is.Not.Null, "cardinality property not found");
            var cardinality = cardinalityProp.GetValue(parsed);
            Assert.That(cardinality, Is.EqualTo(data.cardinality));


            string text = (string)formatMethod.Invoke(transformer, new[] { parsed });
            Console.WriteLine(text);


            var parsed2 = parseMethod.Invoke(transformer, new object[] { text });
            if (parsed2 is IEnumerable enumerable2)
                Assert.That(parsed, Is.EquivalentTo(enumerable2));
            else
                Assert.That(parsed, Is.EqualTo(parsed2));
        }
    }
}
