using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public sealed class ExploratoryTests
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly Random _rand = new Random();
        private readonly MethodInfo _createMethodGeneric = Method.OfExpression<Func<Fixture, int>>(f => f.Create<int>()).GetGenericMethodDefinition();

        private static readonly IReadOnlyCollection<(ExploratoryTestCategory category, Type type, string friendlyName)> _allTestCases =
            ExploratoryTestsData.GetAllTestTypes();

        [OneTimeSetUp]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        public void BeforeEveryTest()
        {
            static void RegisterAggressionBased(Fixture fixture, IEnumerable<Type> simpleTypes)
            {
                var getAggBasMethod = Method.OfExpression<Action<Fixture>>(fix => RegisterAllAggressionBased<int>(fixture));
                getAggBasMethod = getAggBasMethod.GetGenericMethodDefinition();

                foreach (var elementType in simpleTypes)
                {
                    var concreteMethod = getAggBasMethod.MakeGenericMethod(elementType);
                    concreteMethod.Invoke(null, new object[] { fixture });
                }
            }

            static string GetRandomString(Random rand, int length = 10)
            {
                var chars = new char[length];
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = (char)rand.Next('A', 'Z' + 1);
                return new string(chars);
            }

            _fixture.Register<string>(() => GetRandomString(_rand));
            _fixture.Register<double>(() => Math.Round(_rand.NextDouble() * 1000, 2));
            _fixture.Register<float>(() => (float)Math.Round(_rand.NextDouble() * 1000, 2));
            _fixture.Register<Enum1>(() => (Enum1)_rand.Next(0, 100));



            var simpleTypes = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.Structs || d.category == ExploratoryTestCategory.Enums)
                .Select(d => d.type)
                .Concat(new[] { typeof(string) })
                .OrderBy(t => t.Name).ToList();

            RegisterAggressionBased(_fixture, simpleTypes);
        }

        private static void RegisterAllAggressionBased<TElement>(Fixture fixture)
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
        private static IEnumerable<string> GetArrays() => GetTypeNamesForCategory(ExploratoryTestCategory.Arrays);
        private static IEnumerable<string> GetDictionaries() => GetTypeNamesForCategory(ExploratoryTestCategory.Dictionaries);
        private static IEnumerable<string> GetCollections() => GetTypeNamesForCategory(ExploratoryTestCategory.Collections);
        private static IEnumerable<string> GetAggressionBased() => GetTypeNamesForCategory(ExploratoryTestCategory.AggressionBased);
        private static IEnumerable<string> GetClasses() => GetTypeNamesForCategory(ExploratoryTestCategory.Classes);

        [TestCaseSource(nameof(GetEnums))]
        public void Enums(string typeName) => ShouldParseAndFormat(typeName);

        [TestCaseSource(nameof(GetStructs))]
        public void Structs(string typeName) => ShouldParseAndFormat(typeName);

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


        private void ShouldParseAndFormat(Type testType)
        {
            bool isGeneric = testType.IsGenericType && !testType.IsGenericTypeDefinition;

            ITransformer transformer = isGeneric && testType.DerivesOrImplementsGeneric(typeof(IAggressionBased<>)) &&
                                       testType.GenericTypeArguments[0] is { } elementType1
                                       ? TextTransformer.Default.GetTransformer(typeof(IAggressionBased<>).MakeGenericType(elementType1))
                                       : TextTransformer.Default.GetTransformer(testType);
            Assert.That(transformer, Is.Not.Null);
            Console.WriteLine(testType);
            Console.WriteLine(transformer);


            if (isGeneric && testType.GetGenericTypeDefinition() == typeof(IAggressionBased<>))
            {
                var elementType = testType.GenericTypeArguments[0];
                testType = typeof(AggressionBased3<>).MakeGenericType(elementType);
            }

            var createMethod = _createMethodGeneric.MakeGenericMethod(testType);

            //nulls
            var parsedNull1 = ParseAndAssert(null);
            var nullText = transformer.FormatObject(parsedNull1);

            Console.WriteLine($"NULL:{nullText ?? "<NULL>"}");

            var parsedNull2 = ParseAndAssert(nullText);
            IsMutuallyEquivalent(parsedNull1, parsedNull2);

            //instances
            for (int i = 1; i <= 30; i++)
            {
                var instance = createMethod.Invoke(null, new object[] { _fixture });

                string text = transformer.FormatObject(instance);
                Console.WriteLine("{0:00}. {1}", i, text);

                var parsed1 = ParseAndAssert(text);
                var parsed2 = ParseAndAssert(text);

                IsMutuallyEquivalent(parsed1, parsed2);
                IsMutuallyEquivalent(parsed1, instance);


                string text3 = transformer.FormatObject(parsed1);
                var parsed3 = ParseAndAssert(text3);
                IsMutuallyEquivalent(parsed1, parsed3);
            }

            static void IsMutuallyEquivalent(object o1, object o2)
            {
                o1.Should().BeEquivalentTo(o2);
                o2.Should().BeEquivalentTo(o1);
            }


            object ParseAndAssert(string text)
            {
                var parsed = transformer.ParseObject(text);
                if (parsed != null && Nullable.GetUnderlyingType(testType) is { } underlyingType)
                    Assert.That(parsed, Is.TypeOf(underlyingType));
                else if (parsed != null && testType.DerivesOrImplementsGeneric(typeof(IAggressionBased<>)))
                    Assert.That(parsed.GetType().DerivesOrImplementsGeneric(typeof(IAggressionBased<>)), $"{parsed} != {testType.GetFriendlyName()}");
                else if (parsed != null)
                    Assert.That(parsed, Is.TypeOf(testType));

                return parsed;
            }
        }
    }

    public enum ExploratoryTestCategory : byte
    {
        Enums,
        Structs,

        Arrays,
        Dictionaries,
        Collections,

        AggressionBased,

        Classes,
        Remaining
    }

    static class ExploratoryTestsData
    {
        public static IReadOnlyCollection<(ExploratoryTestCategory category, Type type, string friendlyName)> GetAllTestTypes()
        {
            var allTypes = GetExistingPropertyTypes()
                .Concat(GetAdditionalTypes())
                .Distinct().ToList();


            List<Type> Carve(Predicate<Type> condition)
            {
                var result = new List<Type>(8);

                for (int i = allTypes.Count - 1; i >= 0; i--)
                {
                    var elem = allTypes[i];
                    if (condition(elem))
                    {
                        result.Add(elem);
                        allTypes.RemoveAt(i);
                    }
                }
                result.Sort((t1, t2) => string.Compare(t1.Name, t2.Name, StringComparison.Ordinal));
                return result;
            }

            var enums = Carve(t => t.IsEnum);
            var structs = Carve(t => t.IsValueType && !t.IsEnum);
            var arrays = Carve(t => t.IsArray);

            var dictionaries = Carve(t => t.DerivesOrImplementsGeneric(typeof(IDictionary<,>)));
            var collections = Carve(t => t.DerivesOrImplementsGeneric(typeof(ICollection<>)));
            var aggressionBased = Carve(t => t.DerivesOrImplementsGeneric(typeof(IAggressionBased<>)));

            var classes = Carve(t => !t.IsValueType && !t.IsArray);
            var remaining = allTypes;

            var simpleTypes = new[] { typeof(string) }.Concat(enums).Concat(structs).OrderBy(t => t.Name).ToList();

            aggressionBased = simpleTypes
                .SelectMany(t => new[] { typeof(AggressionBased1<>).MakeGenericType(t), typeof(AggressionBased3<>).MakeGenericType(t) })
                .Concat(aggressionBased).OrderBy(t => t.Name)
                .ToList();

            simpleTypes = simpleTypes.Concat(aggressionBased).ToList();

            arrays = simpleTypes.Select(t => t.MakeArrayType())
                .Concat(arrays)
                .Distinct().OrderBy(t => t.Name).ToList();

            collections = simpleTypes.Select(t => typeof(List<>).MakeGenericType(t))
                .Concat(collections)
                .Distinct().OrderBy(t => t.Name).ToList();

            dictionaries = new[] { typeof(float), typeof(int), typeof(string) }
                .SelectMany(keyType => simpleTypes.Select(val => (Key: keyType, Value: val)))
                .Select(kvp => typeof(Dictionary<,>).MakeGenericType(kvp.Key, kvp.Value))
                .Concat(dictionaries)
                .Distinct().OrderBy(t => t.Name).ToList();


            var @return = new List<(ExploratoryTestCategory category, Type type, string friendlyName)>();

            void ProjectAndAdd(ExploratoryTestCategory category, IEnumerable<Type> types) =>
                @return.AddRange(types.Select(t => (category, t, t.GetFriendlyName())).Distinct());

            ProjectAndAdd(ExploratoryTestCategory.Enums, enums);
            ProjectAndAdd(ExploratoryTestCategory.Structs, structs);
            ProjectAndAdd(ExploratoryTestCategory.Arrays, arrays);
            ProjectAndAdd(ExploratoryTestCategory.Dictionaries, dictionaries);
            ProjectAndAdd(ExploratoryTestCategory.Collections, collections);
            ProjectAndAdd(ExploratoryTestCategory.AggressionBased, aggressionBased);
            ProjectAndAdd(ExploratoryTestCategory.Classes, classes);
            ProjectAndAdd(ExploratoryTestCategory.Remaining, remaining);

            return @return;
        }

        private static IEnumerable<Type> GetExistingPropertyTypes() => new[]
        {
            //add own types i.e. from configuration model 
            typeof(string)
        };

        private static IEnumerable<Type> GetAdditionalTypes() => new[]
        {
            //enum
            typeof(Fruits), typeof(Enum1), typeof(Enum2), typeof(Enum3), typeof(ByteEnum), typeof(SByteEnum), typeof(Int64Enum), typeof(UInt64Enum),
            


            //array + collections + dictionaries
            typeof(bool[]), typeof(double[]), typeof(int[]), typeof(long[]), typeof(string[]), typeof(Fruits[]),
            typeof(List<string>),
            typeof(Dictionary<string,string>), typeof(Dictionary<string,int>), typeof(Dictionary<string,double>),
            typeof(Dictionary<int, float>), typeof(Dictionary<double, string>), typeof(Dictionary<Fruits, double>),
            typeof(SortedDictionary<Fruits, float>), typeof(ReadOnlyDictionary<Fruits, IList<TimeSpan>>),



            //struct
            typeof(bool), typeof(double), typeof(int), typeof(long), typeof(double?), typeof(bool?), typeof(int?),
            typeof(DateTime), typeof(TimeSpan), typeof(TimeSpan?),
            typeof(char), typeof(char?), typeof(bool?),
            typeof(LowPrecisionFloat), typeof(CarrotAndOnionFactors),
            


            //AggressionBased
            typeof(IAggressionBased<int>), typeof(IAggressionBased<string>), typeof(IAggressionBased<LowPrecisionFloat>), typeof(IAggressionBased<bool>),
            typeof(AggressionBased3<float?>),
            typeof(AggressionBased3<int[]>), typeof(AggressionBased3<TimeSpan>),
            typeof(AggressionBased3<List<string>>), typeof(List<AggressionBased3<string>>),
            typeof(AggressionBased3<List<float>>), typeof(List<AggressionBased3<float>>),
            typeof(AggressionBased3<List<TimeSpan>>), typeof(List<AggressionBased3<TimeSpan>>),
            typeof(AggressionBased3<List<AggressionBased3<TimeSpan[]>>>),


            //class
            typeof(string), typeof(Uri),
        };
    }
}
