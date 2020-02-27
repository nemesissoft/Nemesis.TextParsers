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

        private static readonly IDictionary<ExploratoryTestCategory, IReadOnlyList<Type>> _allTestCases = ExploratoryTestsData.GetAllTestTypes();

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

            _fixture.Register<string>(() => $"XXX{_rand.Next():D10}");
            _fixture.Register<double>(() => Math.Round(_rand.NextDouble() * 1000, 2));
            _fixture.Register<float>(() => (float)Math.Round(_rand.NextDouble() * 1000, 2));
            _fixture.Register<Enum1>(() => (Enum1)_rand.Next(0, 100));



            var simpleTypes = _allTestCases[ExploratoryTestCategory.Structs]
                .Concat(_allTestCases[ExploratoryTestCategory.Enums])
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

        private static IEnumerable<Type> GetTypesForCategory(ExploratoryTestCategory category) =>
            _allTestCases.TryGetValue(category, out var list)
                ? list/*.Select(t => (t.GetFriendlyName(), t))*/
                : Enumerable.Empty<Type>();


        private static IEnumerable<Type> GetEnums() => GetTypesForCategory(ExploratoryTestCategory.Enums);
        private static IEnumerable<Type> GetStructs() => GetTypesForCategory(ExploratoryTestCategory.Structs);
        private static IEnumerable<Type> GetArrays() => GetTypesForCategory(ExploratoryTestCategory.Arrays);
        private static IEnumerable<Type> GetDictionaries() => GetTypesForCategory(ExploratoryTestCategory.Dictionaries);
        private static IEnumerable<Type> GetCollections() => GetTypesForCategory(ExploratoryTestCategory.Collections);
        private static IEnumerable<Type> GetAggressionBased() => GetTypesForCategory(ExploratoryTestCategory.AggressionBased);
        private static IEnumerable<Type> GetClasses() => GetTypesForCategory(ExploratoryTestCategory.Classes);
        private static IEnumerable<Type> GetRemaining() => GetTypesForCategory(ExploratoryTestCategory.Remaining);

        [TestCaseSource(nameof(GetEnums))]
        public void Enums(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetStructs))]
        public void Structs(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetArrays))]
        public void Arrays(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetDictionaries))]
        public void Dictionaries(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetCollections))]
        public void Collections(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetAggressionBased))]
        public void AggressionBased(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetClasses))]
        public void Classes(Type data) => ShouldParseAndFormat(data);

        [TestCaseSource(nameof(GetRemaining))]
        public void Remaining(Type data) => ShouldParseAndFormat(data);


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

        Arrays,
        Dictionaries,
        Collections,

        AggressionBased,

        Classes,
        Remaining
    }

    static class ExploratoryTestsData
    {
        public static IDictionary<ExploratoryTestCategory, IReadOnlyList<Type>> GetAllTestTypes()
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


            var dict = new Dictionary<ExploratoryTestCategory, IReadOnlyList<Type>>
            {
                [ExploratoryTestCategory.Enums] = enums,
                [ExploratoryTestCategory.Structs] = structs,
                [ExploratoryTestCategory.Arrays] = arrays,
                [ExploratoryTestCategory.Dictionaries] = dictionaries,
                [ExploratoryTestCategory.Collections] = collections,
                [ExploratoryTestCategory.AggressionBased] = aggressionBased,
                [ExploratoryTestCategory.Classes] = classes,
                [ExploratoryTestCategory.Remaining] = remaining
            };

            return dict;
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
