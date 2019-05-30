using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Nemesis.Essentials.Design;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class ExploratoryTests
    {
        private static IEnumerable<(string, Type)> GetTestTypes()
        {
            var enums = _existingPropertyTypes.Where(t => t.IsEnum).OrderBy(t => t.Name).ToList();
            var structs = _existingPropertyTypes.Where(t => t.IsValueType && !t.IsEnum).OrderBy(t => t.Name).ToList();
            var arrays = _existingPropertyTypes.Where(t => t.IsArray).OrderBy(t => t.Name).ToList();
            var classes = _existingPropertyTypes.Where(t => !t.IsValueType && !t.IsArray).OrderBy(t => t.Name).ToList();

            var simpleTypes = _existingPropertyTypes.Where(t => t.IsValueType || t == typeof(string)).OrderBy(t => t.Name).ToList();

            var aggressionBased = simpleTypes.SelectMany(t => new[] { typeof(AggressionBased1<>).MakeGenericType(t), typeof(AggressionBased3<>).MakeGenericType(t) }).ToList();

            simpleTypes = simpleTypes.Union(aggressionBased).ToList();

            var customsArrays = simpleTypes.Select(t => t.MakeArrayType()).ToList();

            var collections = simpleTypes.Select(t => typeof(List<>).MakeGenericType(t)).ToList();

            var dictionaries = new[] { typeof(float), typeof(int), typeof(string) }
                .SelectMany(keyType => simpleTypes.Select(val => (Key: keyType, Value: val)))
                .Select(kvp => typeof(Dictionary<,>).MakeGenericType(kvp.Key, kvp.Value))
                .ToList();



            return enums.Union(structs).Union(arrays).Union(classes).Union(aggressionBased).Union(customsArrays).Union(collections).Union(dictionaries)
                .Select(type => (Name: type.GetFriendlyName(), type)).OrderBy(pair => pair.Name)
                .DistinctBy(pair => pair.Name);
        }

        private readonly Fixture _fixture = new Fixture();
        private readonly Random _rand = new Random();
        private readonly MethodInfo _createMethodGeneric = Method.OfExpression<Func<Fixture, int>>(f => f.Create<int>());

        [OneTimeSetUp]
        public void BeforeEveryTest()
        {
            _fixture.Register<string>(() => $"XXX{_rand.Next():D10}");
            _fixture.Register<double>(() => Math.Round(_rand.NextDouble() * 1000,2));
            _fixture.Register<float>(() => (float) Math.Round(_rand.NextDouble() * 1000,2));
        }

        [TestCaseSource(nameof(GetTestTypes))]
        public void AllSupportedTypes_ShouldParseAndFormatProperly((string typeName, Type testType) data)
        {
            var testType = data.testType;
            var transformer = TextTransformer.Default.GetTransformer(testType);
            Assert.That(transformer, Is.Not.Null);
            Console.WriteLine(transformer);


            if (testType.IsGenericType && !testType.IsGenericTypeDefinition && testType.GetGenericTypeDefinition() == typeof(IAggressionBased<>))
            {
                var elementType = testType.GenericTypeArguments[0];
                testType = typeof(AggressionBased3<>).MakeGenericType(elementType);
            }

            var createMethod = _createMethodGeneric.GetGenericMethodDefinition().MakeGenericMethod(testType);


            for (int i = 0; i < 20; i++)
            {
                var instance = createMethod.Invoke(null, new object[] { _fixture });

                string text = transformer.FormatObject(instance);
                Console.WriteLine(text);

                var parsed1 = ParseAndAssert(text);
                var parsed2 = ParseAndAssert(text);

                IsMutuallyEquivalent(parsed1, parsed2);
                IsMutuallyEquivalent(parsed1, instance);


                string text3 = transformer.FormatObject(parsed1);
                var parsed3 = ParseAndAssert(text3);
                IsMutuallyEquivalent(parsed1, parsed3);
            }

            void IsMutuallyEquivalent(object o1, object o2)
            {
                o1.Should().BeEquivalentTo(o2);
                o2.Should().BeEquivalentTo(o1);
            }

            object ParseAndAssert(string text)
            {
                var parsed = transformer.ParseObject(text);
                if (parsed != null && Nullable.GetUnderlyingType(testType) is Type underlyingType)
                    Assert.That(parsed, Is.TypeOf(underlyingType));
                else
                    Assert.That(parsed, Is.TypeOf(testType));
                return parsed;
            }
        }



        private static readonly IEnumerable<Type> _existingPropertyTypes = new[]
        {
            //enum
            
            //array
            typeof(bool[]), typeof(double[]), typeof(int[]), typeof(long[]), typeof(string[]), 
            //struct
            typeof(bool), typeof(double), typeof(int), typeof(long), typeof(double?), typeof(bool?), typeof(int?), 
            //AggressionBased
            typeof(IAggressionBased<int>), typeof(IAggressionBased<string>), typeof(IAggressionBased<LowPrecisionFloat>), typeof(IAggressionBased<bool>), typeof(IAggressionBased<float?>),
            //class
            typeof(Dictionary<string,string>), typeof(Dictionary<string,int>), typeof(Dictionary<string,double>), typeof(List<string>), typeof(string)
        };

        private static readonly IEnumerable<Type> _additionalTypes = new[]
        {
            typeof(Fruits), typeof(Enum1), typeof(Enum2), typeof(Enum3), typeof(ByteEnum), typeof(SByteEnum), typeof(Int64Enum), typeof(UInt64Enum),
            typeof(Fruits[]),

            typeof(LowPrecisionFloat), typeof(CarrotAndOnionFactors),


            typeof(Dictionary<int, float>), typeof(Dictionary<double, string>), typeof(Dictionary<Fruits, double>),
            typeof(SortedDictionary<Fruits, float>), typeof(ReadOnlyDictionary<Fruits, IList<TimeSpan>>),

            typeof(IAggressionBased<int[]>), typeof(IAggressionBased<TimeSpan>),
            typeof(IAggressionBased<List<string>>), typeof(List<IAggressionBased<string>>),
            typeof(IAggressionBased<List<float>>), typeof(List<IAggressionBased<float>>),
            typeof(IAggressionBased<List<TimeSpan>>), typeof(List<IAggressionBased<TimeSpan>>),
            typeof(IAggressionBased<List<IAggressionBased<TimeSpan[]>>>),
            typeof(DateTime), typeof(TimeSpan), typeof(TimeSpan?),
            typeof(char), typeof(char?), typeof(bool?),
        };
    }
}
