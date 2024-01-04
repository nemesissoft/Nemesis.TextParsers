﻿using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Tests.Utils;
using Nemesis.TextParsers.Utils;
using static Nemesis.TextParsers.Tests.Utils.TestHelper;

namespace Nemesis.TextParsers.Tests.Collections;

[TestFixture]
class CollectionTransformerTests
{
    private static readonly Type[] _collectionTypes =
    [
        typeof(int[]),
        typeof(int[][]),
        typeof(List<int>),
        typeof(IList<int>),
        typeof(ICollection<int>),
        typeof(IEnumerable<int>),
        typeof(ReadOnlyCollection<int>),
        typeof(IReadOnlyCollection<int>),
        typeof(IReadOnlyList<int>),
        typeof(ISet<int>),
        typeof(HashSet<int>),
        typeof(SortedSet<int>),
        typeof(LinkedList<int>),
        typeof(Stack<int>),
        typeof(Queue<int>),
        typeof(ObservableCollection<int>),
        typeof(ReadOnlyObservableCollection<float>),
        //dictionary like collection
        typeof(IEnumerable<KeyValuePair<int, string>>),
        typeof(ICollection<KeyValuePair<int, string>>),
        typeof(IReadOnlyCollection<KeyValuePair<int, string>>),
        typeof(IReadOnlyList<KeyValuePair<int, string>>),

        typeof(Dictionary<int, string>),
        typeof(IDictionary<int, string>),
        typeof(ReadOnlyDictionary<int, string>),
        typeof(IReadOnlyDictionary<int, string>),
        typeof(SortedList<int, string>),
        typeof(SortedDictionary<int, string>),
        //custom collections
        typeof(StringList),
        typeof(Times10NumberList),
        typeof(ImmutableIntCollection),
        typeof(ImmutableNullableIntCollection),
        //custom dictionaries 
        typeof(StringKeyedDictionary<int>),
        typeof(FloatValuedDictionary<int>),
        typeof(ImmutableDecimalValuedDictionary<int>),
        typeof(CaseInsensitiveDictionary<int>),
    ];


    private static IEnumerable<(Type contractType, string input, int cardinality, Type expectedType)>
        CorrectData() => new[]
        {
            //array
            (typeof(float[][]), @"1\|2\|3 | 4\|5\|6\|7", 2, typeof(float[][])),
            (typeof(int[]), @"10|2|3", 3, typeof(int[])),


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
            (typeof(Stack<int>), @"1|22|333|444|5555", 5, typeof(Stack<int>)),
            (typeof(Queue<int>), @"37|2|3|5|36", 5, typeof(Queue<int>)),

            (typeof(ObservableCollection<int>), @"18|14|12|13|10", 5, typeof(ObservableCollection<int>)),
            (typeof(ReadOnlyObservableCollection<float>), @"18.1|14.2|12.3|13.4|10.5", 5, typeof(ReadOnlyObservableCollection<float>)),


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
            

            //custom collections 
            (typeof(StringList), @"ABC|DEF|GHI", 3, typeof(StringList)),
            (typeof(Times10NumberList), @"2|5|99", 3, typeof(Times10NumberList)),
            (typeof(ImmutableIntCollection), @"1|22|333|4444|55555", 5, typeof(ImmutableIntCollection)),
            (typeof(ImmutableNullableIntCollection), @"1|2|3|4|5||7|∅|9", 9, typeof(ImmutableNullableIntCollection)),

            //custom dictionary
            (typeof(StringKeyedDictionary<int>), @"One=1;Two=2;Zero=0;Four=4", 4, typeof(StringKeyedDictionary<int>)),
            (typeof(FloatValuedDictionary<int>), @"1=1.1;2=2.2;0=0.0;4=4.4", 4, typeof(FloatValuedDictionary<int>)),
            (typeof(ImmutableDecimalValuedDictionary<int>), @"1=1.1;2=2.2;0=0.0;4=4.4", 4, typeof(ImmutableDecimalValuedDictionary<int>)),
            (typeof(CaseInsensitiveDictionary<int>), @"A=1;B=2;C=3;a=10;b=20;c=30", 3, typeof(CaseInsensitiveDictionary<int>)),
        }
        .Concat(_collectionTypes.Select(t => (t, (string)null, -1, (Type)null))) //null values
        .Concat(_collectionTypes.Select(t => (t, "", 0, GetExpectedTypeFor(t)))) //empty
    ;

    private static Type GetExpectedTypeFor(Type contractType)
    {
        if (contractType.IsInterface && contractType.IsGenericType && !contractType.IsGenericTypeDefinition && contractType.GetGenericTypeDefinition() is { } definition)
        {
            var gta = contractType.GenericTypeArguments;

            if (definition == typeof(IEnumerable<>) ||
                definition == typeof(ICollection<>) ||
                definition == typeof(IList<>))
                return typeof(List<>).MakeGenericType(gta);

            else if (definition == typeof(IReadOnlyCollection<>) ||
                     definition == typeof(IReadOnlyList<>))
                return typeof(ReadOnlyCollection<>).MakeGenericType(gta);

            else if (definition == typeof(ISet<>))
                return typeof(HashSet<>).MakeGenericType(gta);

            else if (definition == typeof(IDictionary<,>))
                return typeof(Dictionary<,>).MakeGenericType(gta);

            else if (definition == typeof(IReadOnlyDictionary<,>))
                return typeof(ReadOnlyDictionary<,>).MakeGenericType(gta);
        }

        return contractType;
    }

    private static IEnumerable<TCD> CollectionTestData(string differentiator) =>
        CorrectData().Select((t, i) =>
            new TCD(t.contractType, t.input, t.cardinality, t.expectedType)
            .SetName($"{differentiator}_{i + 1:000}_{t.contractType.GetFriendlyName().SanitizeTestName()}")
        );

    [TestCaseSource(nameof(CollectionTestData), new object[] { "G" })]
    public void CollectionType_CompoundTest(Type contractType, string input, int cardinality, Type expectedType)
    {
        var collectionCompound = MakeDelegate<Action<string, int, Type>>(
            (p1, p2, p3) => CollectionType_CompoundTestHelper<int>(p1, p2, p3), contractType
        );

        collectionCompound(input, cardinality, expectedType);
    }

    private static void CollectionType_CompoundTestHelper<T>(string input, int expectedCardinality, Type expectedType)
    {
        var sut = Sut.GetTransformer<T>();

        var parsed1 = sut.Parse(input);

        CheckTypeAndCardinality(parsed1, expectedCardinality, expectedType);


        string text = sut.Format(parsed1);

        var parsed2 = sut.Parse(text);

        IsMutuallyEquivalent(parsed1, parsed2);
    }


    [TestCaseSource(nameof(CollectionTestData), new object[] { "NG" })]
    public void CollectionType_CompoundTest_NonGeneric(Type contractType, string input, int cardinality, Type expectedType)
    {
        var transformer = Sut.GetTransformer(contractType);

        var parsed1 = transformer.ParseObject(input);

        CheckTypeAndCardinality(parsed1, cardinality, expectedType);

        string text = transformer.FormatObject(parsed1);


        var parsed2 = transformer.ParseObject(text);


        IsMutuallyEquivalent(parsed1, parsed2);
    }

    private static void CheckTypeAndCardinality(object parsed, int expectedCardinality, Type expectedType)
    {
        if (parsed is null && expectedType is not null)
            Assert.Fail("Not supported test case");
        else if (parsed is not null && expectedType is not null)
        {
            Assert.That(parsed, Is.TypeOf(expectedType));

            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            var cardinalityProp = parsed.GetType().GetProperties(ALL_FLAGS)
                .SingleOrDefault(p => p.Name == "Size" || p.Name == "Count" || p.Name == "Length");
            Assert.That(cardinalityProp, Is.Not.Null, "cardinality property not found");
            var cardinality = cardinalityProp.GetValue(parsed);
            Assert.That(cardinality, Is.EqualTo(expectedCardinality));
        }
        else if (expectedCardinality != -1)
            throw new NotSupportedException("Bad data");
    }


    [TestCase(null, "")]
    [TestCase(new float[0], "")]
    [TestCase(new[] { 15.6f }, "15.6")]
    [TestCase(new[] { 15.5f, 25.6f }, "15.5|25.6")]
    [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }, "15.5|25.6|35.99|50|999")]
    public void LeanCollectionTest(float[] elements, string expectedText) =>
        ParseAndFormat(LeanCollectionFactory.FromArray(elements), expectedText);




    class StringList : List<string> { }
    class Times10NumberList : ICollection<int>, IDeserializationCallback
    {
        private readonly List<int> _items = [];

        public void OnDeserialization(object sender)
        {
            for (int i = 0; i < _items.Count; i++) _items[i] *= 10;
        }


        public IEnumerator<int> GetEnumerator() => _items.Select(i => i / 10).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

        public void Add(int item) => _items.Add(item);

        public void Clear() => _items.Clear();

        public bool Contains(int item) => _items.Contains(item);

        public void CopyTo(int[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public bool Remove(int item) => _items.Remove(item);

        public int Count => _items.Count;

        public bool IsReadOnly => false;
    }

    class ImmutableIntCollection(IList<int> list) : ReadOnlyCollection<int>(list) { }
    class ImmutableNullableIntCollection(IList<int?> list) : ReadOnlyCollection<int?>(list) { }

    class StringKeyedDictionary<TValue> : SortedDictionary<string, TValue>;
    class FloatValuedDictionary<TKey> : Dictionary<TKey, float>;
    class ImmutableDecimalValuedDictionary<TKey>(IDictionary<TKey, decimal> dictionary)
              : ReadOnlyDictionary<TKey, decimal>(dictionary);
    class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
    {
        public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase) { }
    }
}