using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using AutoFixture;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public sealed class ExploratoryTests
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly Random _rand = new Random();

        private static readonly IReadOnlyCollection<(ExploratoryTestCategory category, Type type, string friendlyName)> _allTestCases =
            ExploratoryTestsData.GetAllTestTypes(
                ExploratoryTestsData.GetStandardTypes().Concat(
                    new[]
                    {
                        typeof(Fruits), typeof(Enum1), typeof(Enum2), typeof(Enum3), typeof(ByteEnum), typeof(SByteEnum), typeof(Int64Enum), typeof(UInt64Enum),
                        typeof(LowPrecisionFloat), typeof(CarrotAndOnionFactors),

                        typeof(Fruits[]),typeof(Dictionary<Fruits, double>),
                        typeof(SortedDictionary<Fruits, float>), typeof(SortedList<Fruits, int>),
                        typeof(ReadOnlyDictionary<Fruits, IList<TimeSpan>>),

                        typeof(IAggressionBased<int>), typeof(IAggressionBased<string>), typeof(IAggressionBased<LowPrecisionFloat>), typeof(IAggressionBased<bool>),
                        typeof(AggressionBased3<float?>),
                        typeof(AggressionBased3<int[]>), typeof(AggressionBased3<TimeSpan>),
                        typeof(AggressionBased3<List<string>>), typeof(List<AggressionBased3<string>>),
                        typeof(AggressionBased3<List<float>>), typeof(List<AggressionBased3<float>>),
                        typeof(AggressionBased3<List<TimeSpan>>), typeof(List<AggressionBased3<TimeSpan>>),
                        typeof(AggressionBased3<List<AggressionBased3<TimeSpan[]>>>),
                    }
                    ).ToList());

        [OneTimeSetUp]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        public void BeforeEveryTest()
        {
            static void RegisterAllAggressionBased(Fixture fixture, IEnumerable<Type> simpleTypes)
            {
                var registerMethod = Method.OfExpression<Action<Fixture>>(fix => RegisterAggressionBased<int>(null))
                    .GetGenericMethodDefinition();

                foreach (var elementType in simpleTypes)
                {
                    var concreteMethod = registerMethod.MakeGenericMethod(elementType);
                    concreteMethod.Invoke(null, new object[] { fixture });
                }
            }

            static void RegisterAllNullable(Fixture fixture, Random rand, IEnumerable<Type> structs)
            {
                var registerMethod = Method.OfExpression<Action<Fixture>>(fix => RegisterNullable<int>(null, null))
                    .GetGenericMethodDefinition();

                foreach (var elementType in structs)
                {
                    var concreteMethod = registerMethod.MakeGenericMethod(elementType);
                    concreteMethod.Invoke(null, new object[] { fixture, rand });
                }
            }

            static string GetRandomString(Random rand, char start, char end, int length = 10)
            {
                var chars = new char[length];
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = (char)rand.Next(start, end + 1);
                return new string(chars);
            }

            static double GetRandomDouble(Random rand, int magnitude = 1000, bool generateSpecialValues = true)
            {
                if (generateSpecialValues && rand.NextDouble() is { } chance && chance < 0.1)
                {
                    if (chance < 0.045) return double.PositiveInfinity;
                    else if (chance < 0.09) return double.NegativeInfinity;
                    else return double.NaN;
                }
                else
                    return Math.Round((rand.NextDouble() - 0.5) * 2 * magnitude, 3);
            }

            _fixture.Register<string>(() => GetRandomString(_rand, 'A', 'Z'));

            _fixture.Register<double>(() => GetRandomDouble(_rand));
            _fixture.Register<float>(() => (float)GetRandomDouble(_rand));
            _fixture.Register<decimal>(() => (decimal)GetRandomDouble(_rand, 10000, false));
            _fixture.Register<Complex>(() => new Complex(GetRandomDouble(_rand, 1000, false), GetRandomDouble(_rand, 1000, false)));

            _fixture.Register<BigInteger>(() => BigInteger.Parse(GetRandomString(_rand, '0', '9', 30)));
            _fixture.Register<Enum1>(() => (Enum1)_rand.Next(0, 100));



            var simpleTypes = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.Structs || d.category == ExploratoryTestCategory.Enums)
                .Select(d => d.type)
                .Concat(new[] { typeof(string) });
            RegisterAllAggressionBased(_fixture, simpleTypes);


            var nonNullableStructs = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.Structs || d.category == ExploratoryTestCategory.Enums)
                .Select(d => d.type)
                .Where(t => t.IsValueType && Nullable.GetUnderlyingType(t) == null);
            RegisterAllNullable(_fixture, _rand, nonNullableStructs);
        }

        private static void RegisterNullable<TElement>(IFixture fixture, Random rand) where TElement : struct
        {
            TElement? Creator() => rand.NextDouble() < 0.1 ? (TElement?)null : fixture.Create<TElement>();

            fixture.Register(Creator);
        }

        private static void RegisterAggressionBased<TElement>(IFixture fixture)
        {
            AggressionBased1<TElement> Creator1() => new AggressionBased1<TElement>(fixture.Create<TElement>());

            AggressionBased3<TElement> Creator3()
            {
                TElement pass = fixture.Create<TElement>(),
                    norm = fixture.Create<TElement>(),
                    aggr = fixture.Create<TElement>();

                int i = 0;
                while (StructuralEquality.Equals(pass, norm) && StructuralEquality.Equals(pass, aggr))
                {
                    norm = fixture.Create<TElement>();
                    aggr = fixture.Create<TElement>();

                    if (i++ > 100)
                        throw new InvalidOperationException($"Cannot create instance for {typeof(TElement).GetFriendlyName()}");
                }

                return new AggressionBased3<TElement>(pass, norm, aggr);
            }

            fixture.Register(Creator1);

            fixture.Register(Creator3);

            fixture.Register<IAggressionBased<TElement>>(Creator3);
        }


        private static IEnumerable<string> GetTypeNamesForCategory(ExploratoryTestCategory category) =>
            _allTestCases.Where(d => d.category == category).Select(d => d.friendlyName);


        private static IEnumerable<string> GetEnums() => GetTypeNamesForCategory(ExploratoryTestCategory.Enums);
        private static IEnumerable<string> GetStructs() => GetTypeNamesForCategory(ExploratoryTestCategory.Structs);
        private static IEnumerable<string> GetValueTuples() => GetTypeNamesForCategory(ExploratoryTestCategory.ValueTuples);

        private static IEnumerable<string> GetArrays() => GetTypeNamesForCategory(ExploratoryTestCategory.Arrays);
        private static IEnumerable<string> GetDictionaries() => GetTypeNamesForCategory(ExploratoryTestCategory.Dictionaries);
        private static IEnumerable<string> GetCollections() => GetTypeNamesForCategory(ExploratoryTestCategory.Collections);
        private static IEnumerable<string> GetAggressionBased() => GetTypeNamesForCategory(ExploratoryTestCategory.AggressionBased);
        private static IEnumerable<string> GetClasses() => GetTypeNamesForCategory(ExploratoryTestCategory.Classes);

        [TestCaseSource(nameof(GetEnums))]
        public void Enums(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetStructs))]
        public void Structs(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetValueTuples))]
        public void ValueTuples(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetArrays))]
        public void Arrays(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetDictionaries))]
        public void Dictionaries(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetCollections))]
        public void Collections(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetAggressionBased))]
        public void AggressionBased(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetClasses))]
        public void Classes(string typeName) => ShouldParseAndFormat(typeName);

        [Test]
        public void Remaining() =>
            CollectionAssert.IsEmpty(GetTypeNamesForCategory(ExploratoryTestCategory.Remaining));


        private void ShouldParseAndFormat(string typeName)
        {
            var type = _allTestCases.Where(d => d.friendlyName == typeName).Select(d => d.type).SingleOrDefault();

            Assert.That(type, Is.Not.Null, "Type not found");

            ShouldParseAndFormat(type);
        }

        private static readonly MethodInfo _tester = Method.OfExpression<Action<ExploratoryTests>>(
            test => test.ShouldParseAndFormatHelper<int>()
        ).GetGenericMethodDefinition();

        private void ShouldParseAndFormat(Type testType)
        {
            var tester = _tester.MakeGenericMethod(testType);

            tester.Invoke(this, null);
        }

        private void ShouldParseAndFormatHelper<T>()
        {
            Type testType = typeof(T);


            ITransformer transformer = TextTransformer.Default.GetTransformer<T>();
            Assert.That(transformer, Is.Not.Null);
            Console.WriteLine(transformer);

            //nulls
            var parsedNull1 = ParseAndAssert(null);
            var nullText = transformer.FormatObject(parsedNull1);

            Console.WriteLine($"NULL:{nullText ?? "<NULL>"}");

            var parsedNull2 = ParseAndAssert(nullText);
            IsMutuallyEquivalent(parsedNull1, parsedNull2);

            //instances
            var instances = _fixture.CreateMany<T>(30);
            int i = 1;
            foreach (var instance in instances)
            {
                string text = transformer.FormatObject(instance);
                Console.WriteLine("{0:00}. {1}", i++, text);

                var parsed1 = ParseAndAssert(text);
                var parsed2 = ParseAndAssert(text);

                IsMutuallyEquivalent(parsed1, parsed2);
                IsMutuallyEquivalent(parsed1, instance);


                string text3 = transformer.FormatObject(parsed1);
                var parsed3 = ParseAndAssert(text3);
                IsMutuallyEquivalent(parsed1, parsed3);
            }

            object ParseAndAssert(string text)
            {
                var parsed = transformer.ParseObject(text);

                if (parsed == null) return null;


                if (Nullable.GetUnderlyingType(testType) is { } underlyingType)
                    Assert.That(parsed, Is.TypeOf(underlyingType));
                else if (testType.DerivesOrImplementsGeneric(typeof(IAggressionBased<>)))
                    Assert.That(parsed.GetType().DerivesOrImplementsGeneric(typeof(IAggressionBased<>)), $"{parsed} != {testType.GetFriendlyName()}");
                else if (testType.IsInterface)
                    Assert.That(parsed, Is.AssignableTo(testType));
                else
                    Assert.That(parsed, Is.TypeOf(testType));

                return parsed;
            }
        }
    }

    public enum ExploratoryTestCategory : byte
    {
        Enums,
        Structs,
        ValueTuples,

        Arrays,
        Dictionaries,
        Collections,

        AggressionBased,

        Classes,
        Remaining
    }

    static class ExploratoryTestsData
    {
        public static IReadOnlyCollection<(ExploratoryTestCategory category, Type type, string friendlyName)>
            GetAllTestTypes(IList<Type> allTypes)
        {
            var typeComparer = Comparer<Type>.Create((t1, t2) =>
                string.Compare(t1.GetFriendlyName(), t2.GetFriendlyName(), StringComparison.OrdinalIgnoreCase)
            );

            SortedSet<Type> Carve(Predicate<Type> condition)
            {
                var result = new SortedSet<Type>(typeComparer);

                for (int i = allTypes.Count - 1; i >= 0; i--)
                {
                    var elem = allTypes[i];
                    if (condition(elem))
                    {
                        result.Add(elem);
                        allTypes.RemoveAt(i);
                    }
                }
                return result;
            }

            var enums = Carve(t => t.IsEnum);
            var structs = Carve(t => t.IsValueType && !t.IsEnum);
            var arrays = Carve(t => t.IsArray);

            var dictionaries = Carve(t => t.DerivesOrImplementsGeneric(typeof(IDictionary<,>)));
            var collections = Carve(t => t.DerivesOrImplementsGeneric(typeof(IEnumerable<>)) && t != typeof(string));
            var aggressionBased = Carve(t => t.DerivesOrImplementsGeneric(typeof(IAggressionBased<>)));

            var classes = Carve(t => !t.IsValueType && !t.IsArray);
            var remaining = allTypes;

            var simpleTypes = new[] { typeof(string) }.Concat(enums).Concat(structs).ToList();

            var rand = new Random();
            Type GetRandomSimpleType() => simpleTypes[rand.Next(simpleTypes.Count)];

            var valueTuples = new List<(int arity, Type tupleType)>
            {
                (1, typeof(ValueTuple<>)),
                (2, typeof(ValueTuple<,>)),
                (3, typeof(ValueTuple<,,>)),
                (4, typeof(ValueTuple<,,,>)),
                (5, typeof(ValueTuple<,,,,>)),
                (6, typeof(ValueTuple<,,,,,>)),
                (7, typeof(ValueTuple<,,,,,,>)),
            }.Select(pair =>
                pair.tupleType.MakeGenericType(
                    Enumerable.Repeat(0, pair.arity)
                        .Select(i => GetRandomSimpleType()).ToArray())
            ).ToList();

            static Type GetNullableCounterpart(Type t) => t.IsNullable(out var underlyingType)
                ? underlyingType
                : typeof(Nullable<>).MakeGenericType(t);

            structs.UnionWith(structs.Select(GetNullableCounterpart).ToList());

            var originalAggressionBased = aggressionBased.ToList();

            aggressionBased.UnionWith(
                simpleTypes
                    .SelectMany(t => new[] { typeof(AggressionBased1<>).MakeGenericType(t), typeof(AggressionBased3<>).MakeGenericType(t) })
                );

            simpleTypes = simpleTypes.Concat(originalAggressionBased).ToList();

            arrays.UnionWith(simpleTypes.Select(t => t.MakeArrayType()));
            arrays.UnionWith(simpleTypes.Select(t => t.MakeArrayType().MakeArrayType()));


            collections.UnionWith(simpleTypes.Select(t => typeof(List<>).MakeGenericType(t)));


            dictionaries.UnionWith(
                new[] { typeof(float), typeof(string) }
                .SelectMany(keyType => simpleTypes.Select(val => (Key: keyType, Value: val)))
                .Select(kvp => typeof(Dictionary<,>).MakeGenericType(kvp.Key, kvp.Value))
                );



            var @return = new List<(ExploratoryTestCategory category, Type type, string friendlyName)>();

            void ProjectAndAdd(ExploratoryTestCategory category, IEnumerable<Type> types) =>
                @return.AddRange(types.Select(t => (category, t, t.GetFriendlyName())).Distinct());

            ProjectAndAdd(ExploratoryTestCategory.Enums, enums);
            ProjectAndAdd(ExploratoryTestCategory.Structs, structs);
            ProjectAndAdd(ExploratoryTestCategory.ValueTuples, valueTuples);

            ProjectAndAdd(ExploratoryTestCategory.Arrays, arrays);
            ProjectAndAdd(ExploratoryTestCategory.Dictionaries, dictionaries);
            ProjectAndAdd(ExploratoryTestCategory.Collections, collections);
            ProjectAndAdd(ExploratoryTestCategory.AggressionBased, aggressionBased);
            ProjectAndAdd(ExploratoryTestCategory.Classes, classes);
            ProjectAndAdd(ExploratoryTestCategory.Remaining, remaining);

            return @return;
        }

        public static IReadOnlyCollection<Type> GetStandardTypes() => new[]
        {
            //enum
            typeof(FileMode),

            //struct
            typeof(bool), typeof(char),
            typeof(float), typeof(double), typeof(decimal),
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(BigInteger), typeof(Complex),
            typeof(DateTime), typeof(TimeSpan), typeof(DateTimeOffset),
            typeof(Guid), typeof(Guid?),


            //array + collections + dictionaries
            typeof(string[]), typeof(int?[]),
            typeof(List<string>), typeof(ReadOnlyCollection<string>),
            typeof(HashSet<string>), typeof(SortedSet<string>), typeof(ISet<string>),
            typeof(LinkedList<string>), typeof(Stack<TimeSpan>), typeof(Queue<TimeSpan?>),
            typeof(ObservableCollection<string>),

            typeof(Dictionary<string,string>), typeof(IDictionary<string,int>),
            typeof(Dictionary<int, float>), typeof(Dictionary<double, string>), typeof(Dictionary<Fruits, double>),
            typeof(SortedDictionary<decimal, float>), typeof(SortedList<int, Guid>),
            typeof(ReadOnlyDictionary<BigInteger, IList<TimeSpan>>), typeof(IReadOnlyDictionary<string,double>),
            
            //class
            typeof(string), typeof(Uri),
        };
    }
}
