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
using Nemesis.TextParsers.Tests.Deconstructable;
using Nemesis.TextParsers.Tests.Infrastructure;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    public sealed class ExploratoryTests
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly RandomSource _randomSource = new RandomSource();


        private static IReadOnlyCollection<(ExploratoryTestCategory category, Type type)> _allTestCases;

        static void GetTestCases(RandomSource randomSource)
        {
            var baseTypes = ExploratoryTestsData.GetStandardTypes().Concat(
                new[]
                {
                    typeof(Person), typeof(LargeStruct), typeof(LotsOfDeconstructableData),

                    typeof(Fruits), typeof(Enum1), typeof(Enum2), typeof(Enum3), typeof(ByteEnum), typeof(SByteEnum),
                    typeof(Int64Enum), typeof(UInt64Enum),
                    typeof(LowPrecisionFloat), typeof(CarrotAndOnionFactors),

                    typeof(Fruits[]), typeof(Dictionary<Fruits, double>),
                    typeof(SortedDictionary<Fruits, float>), typeof(SortedList<Fruits, int>),
                    typeof(ReadOnlyDictionary<Fruits, IList<TimeSpan>>),

                    typeof(IAggressionBased<int>), typeof(IAggressionBased<string>),
                    typeof(IAggressionBased<LowPrecisionFloat>), typeof(IAggressionBased<bool>),
                    typeof(IAggressionBased<float?>),
                    typeof(IAggressionBased<int[]>),
                    typeof(IAggressionBased<List<string>>), typeof(List<IAggressionBased<string>>),
                    typeof(IAggressionBased<List<float>>), typeof(List<IAggressionBased<float>>),
                    typeof(IAggressionBased<List<TimeSpan>>), typeof(List<IAggressionBased<TimeSpan>>),
                    typeof(IAggressionBased<List<IAggressionBased<TimeSpan[]>>>),

                    typeof(IAggressionBased<TimeSpan[]>), typeof(IAggressionBased<TimeSpan>), typeof(IAggressionBased<float>), typeof(IAggressionBased<string>),
                }
            ).ToList();

            _allTestCases = ExploratoryTestsData.GetAllTestTypes(randomSource, baseTypes);
        }

        [OneTimeSetUp]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        public void BeforeAnyTest()
        {
            int seed = _randomSource.UseNewSeed();
            Console.WriteLine($"{GetType().Name} initial seed = {seed}");
            GetTestCases(_randomSource);


            _fixture.Register<string>(() => _randomSource.NextString('A', 'Z'));

            _fixture.Register<double>(() => _randomSource.NextFloatingNumber());
            _fixture.Register<float>(() => (float)_randomSource.NextFloatingNumber());
            _fixture.Register<decimal>(() => (decimal)_randomSource.NextFloatingNumber(10000, false));
            _fixture.Register<Complex>(() => new Complex(
                _randomSource.NextFloatingNumber(1000, false),
                _randomSource.NextFloatingNumber(1000, false)
            ));
            _fixture.Register<BigInteger>(() => BigInteger.Parse(_randomSource.NextString('0', '9', 30)));


            _fixture.Register(() => (EmptyEnum)_randomSource.Next(0, 2));
            _fixture.Register(() => (Enum1)_randomSource.Next(0, 10));
            _fixture.Register(() => (Enum2)_randomSource.Next(0, 10));
            _fixture.Register(() => (Enum3)_randomSource.Next(0, 6));

            _fixture.Register(() => _randomSource.NextEnum<ByteEnum, byte>());
            _fixture.Register(() => _randomSource.NextEnum<SByteEnum, sbyte>());
            _fixture.Register(() => _randomSource.NextEnum<Int64Enum, long>());
            _fixture.Register(() => _randomSource.NextEnum<UInt64Enum, ulong>());
            _fixture.Register(() => _randomSource.NextEnum<Fruits, ushort>());
            _fixture.Register(() => _randomSource.NextEnum<FruitsWeirdAll, short>());
            _fixture.Register(() => _randomSource.NextEnum<FileMode, int>());

            var structs = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.Structs ||
                            d.category == ExploratoryTestCategory.Enums)
                .Select(d => d.type)
                .ToList();


            var aggBasedElements = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.AggressionBased)
                .Select(d => d.type)
                .Where(t => TypeMeta.TryGetGenericRealization(t, typeof(IAggressionBased<>), out _))
                .Select(t => TypeMeta.GetGenericRealization(t, typeof(IAggressionBased<>)).GenericTypeArguments[0])
                .ToList();
            FixtureUtils.RegisterAllAggressionBased(_fixture, _randomSource, aggBasedElements);


            var nonNullableStructs = structs
                .Where(t => t.IsValueType && Nullable.GetUnderlyingType(t) == null);
            FixtureUtils.RegisterAllNullable(_fixture, _randomSource, nonNullableStructs);


            FixtureUtils.RegisterAllCollections(_fixture, _randomSource, structs);
        }

        [SetUp]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        public void BeforeEachTest()
        {
            int seed = _randomSource.UseNewSeed();
            Console.WriteLine($"{GetType().Name}.{TestContext.CurrentContext?.Test?.Name ?? "<no name>"} - seed = {seed}");
        }


        private static IEnumerable<Type> GetTypeNamesFor(ExploratoryTestCategory category) =>
            _allTestCases.Where(d => d.category == category).Select(d => d.type);

        [Test]
        public void Enums() => ShouldParseAndFormat(ExploratoryTestCategory.Enums);

        [Test]
        public void Structs() => ShouldParseAndFormat(ExploratoryTestCategory.Structs);

        [Test]
        public void ValueTuples() => ShouldParseAndFormat(ExploratoryTestCategory.ValueTuples);

        [Test]
        public void Arrays() => ShouldParseAndFormat(ExploratoryTestCategory.Arrays);

        [Test]
        public void Dictionaries() => ShouldParseAndFormat(ExploratoryTestCategory.Dictionaries);

        [Test]
        public void Collections() => ShouldParseAndFormat(ExploratoryTestCategory.Collections);

        [Test]
        public void AggressionBased() => ShouldParseAndFormat(ExploratoryTestCategory.AggressionBased);

        [Test]
        public void Classes() => ShouldParseAndFormat(ExploratoryTestCategory.Classes);

        [Test]
        public void Remaining() =>
            CollectionAssert.IsEmpty(GetTypeNamesFor(ExploratoryTestCategory.Remaining));


        private void ShouldParseAndFormat(ExploratoryTestCategory category)
        {
            var failed = new List<string>();
            var caseNo = 0;

            foreach (var type in GetTypeNamesFor(category))
                try
                {
                    caseNo++;
                    ShouldParseAndFormat(type);
                }
                catch (Exception e)
                {
                    var ex = e is TargetInvocationException tie && tie.InnerException is { } inner
                        ? inner
                        : e;

                    failed.Add($"Case {caseNo:000} {ex.Message}");
                }

            if (failed.Count > 0)
                Assert.Fail($"Failed cases({failed.Count} cases):{Environment.NewLine}{string.Join(Environment.NewLine, failed)}");
        }

        private void ShouldParseAndFormat(Type testType)
        {
            var tester = MakeDelegate<Action<ExploratoryTests>>
                (test => test.ShouldParseAndFormatHelper<int>(), testType);

            tester(this);
        }

        private void ShouldParseAndFormatHelper<T>()
        {
            Type testType = typeof(T);
            string friendlyName = testType.GetFriendlyName();
            string reason = "<none>";


            try
            {
                reason = "Transformer retrieval";
                var transformer = TextTransformer.Default.GetTransformer<T>();
                Assert.That(transformer, Is.Not.Null);

                //nulls
                reason = $"Parsing null with {transformer}";
                var parsedNull1 = ParseAndAssert(null);
                reason = "Formatting null";
                var nullText = transformer.Format(parsedNull1);

                reason = $"NULL:{nullText ?? "<NULL>"}";
                var parsedNull2 = ParseAndAssert(nullText);
                IsMutuallyEquivalent(parsedNull1, parsedNull2);



                //empty
                reason = $"Retrieving empty with {transformer}";
                var emptyInstance = transformer.GetEmpty();

                reason = "Parsing empty";
                var parsedEmpty = ParseAndAssert("");

                reason = "Formatting empty";
                string emptyText1 = transformer.Format(parsedEmpty);
                string emptyText2 = transformer.Format(emptyInstance);
                IsMutuallyEquivalent(emptyText1, emptyText2);

                reason = "Parsing empty";
                var parsedEmpty1 = ParseAndAssert(emptyText1);
                var parsedEmpty2 = ParseAndAssert(emptyText2);

                IsMutuallyEquivalent(parsedEmpty, parsedEmpty1);
                IsMutuallyEquivalent(parsedEmpty, parsedEmpty2);
                IsMutuallyEquivalent(parsedEmpty, emptyInstance);



                //instances
                reason = "Creating fixtures";
                var instances = _fixture.CreateMany<T>(8);
                int i = 1;
                foreach (var instance in instances)
                {
                    reason = $"Transforming {i}";
                    string text = transformer.Format(instance);
                    reason = $"{i++:00}. {text}";

                    var parsed1 = ParseAndAssert(text);
                    var parsed2 = ParseAndAssert(text);

                    IsMutuallyEquivalent(parsed1, parsed2);
                    IsMutuallyEquivalent(parsed1, instance);


                    string text3 = transformer.Format(parsed1);
                    var parsed3 = ParseAndAssert(text3);
                    IsMutuallyEquivalent(parsed1, parsed3);
                }


                T ParseAndAssert(string text)
                {
                    var parsed = transformer.Parse(text);

                    if (parsed == null) return default;


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
            catch (AssertionException ae)
            {
                throw new Exception($"Failed for {friendlyName} during: {reason} due to {ae.Message}");
            }
            catch (Exception e)
            {
                throw new Exception($"Failed for {friendlyName} during: {reason}", e);
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
        public static IReadOnlyCollection<(ExploratoryTestCategory category, Type type)>
            GetAllTestTypes(RandomSource randomSource, IList<Type> allTypes)
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


            var simpleTypesCopy = simpleTypes;
            Type GetRandomSimpleType() => randomSource.NextElement(simpleTypesCopy);

            Type GetRandomTupleType(int arity, Type tupleType) =>
                tupleType.MakeGenericType(
                    Enumerable.Repeat(0, arity)
                        .Select(i => GetRandomSimpleType()).ToArray());

            var valueTuples = new List<(int arity, Type tupleType)>
            {
              //(1, typeof(ValueTuple<>)),
                (2, typeof(ValueTuple<,>)),
                (3, typeof(ValueTuple<,,>)),
                (4, typeof(ValueTuple<,,,>)),
                (5, typeof(ValueTuple<,,,,>)),
                (6, typeof(ValueTuple<,,,,,>)),
                (7, typeof(ValueTuple<,,,,,,>)),
            }.SelectMany(pair => Enumerable.Repeat(0, 6).Select(i => GetRandomTupleType(pair.arity, pair.tupleType))
            ).ToList();

            static Type GetNullableCounterpart(Type t) => t.IsNullable(out var underlyingType)
                ? underlyingType
                : typeof(Nullable<>).MakeGenericType(t);

            structs.UnionWith(structs.Select(GetNullableCounterpart).ToList());

            var originalAggressionBased = aggressionBased.ToList();

            aggressionBased.UnionWith(
                simpleTypes.Union(valueTuples)
                    .Select(t => typeof(IAggressionBased<>).MakeGenericType(t))
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



            var @return = new List<(ExploratoryTestCategory category, Type type)>();

            void ProjectAndAdd(ExploratoryTestCategory category, IEnumerable<Type> types) =>
                @return.AddRange(types.Select(t => (category, t)).Distinct());

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

    static class FixtureUtils
    {
        public static void RegisterAllAggressionBased(Fixture fixture, RandomSource randomSource, IEnumerable<Type> elementTypes)
        {
            foreach (var elementType in elementTypes)
            {
                var register = MakeDelegate<Action<IFixture, RandomSource>>(
                    (fix, rs) => RegisterAggressionBased<int>(fix, rs), elementType
                );

                register(fixture, randomSource);
            }
        }

        public static void RegisterAllNullable(Fixture fixture, RandomSource randomSource, IEnumerable<Type> structs)
        {
            var registerMethod = Method
                .OfExpression<Action<Fixture, RandomSource>>((fix, rs) => RegisterNullable<int>(fix, rs))
                .GetGenericMethodDefinition();

            foreach (var elementType in structs)
            {
                var concreteMethod = registerMethod.MakeGenericMethod(elementType);
                concreteMethod.Invoke(null, new object[] { fixture, randomSource });
            }
        }

        public static void RegisterAllCollections(Fixture fixture, RandomSource randomSource, IEnumerable<Type> elementTypes)
        {
            var registerMethod = Method
                .OfExpression<Action<Fixture, RandomSource>>((fix, rs) => RegisterCollections<int>(fix, rs))
                .GetGenericMethodDefinition();

            foreach (var elementType in elementTypes)
            {
                var concreteMethod = registerMethod.MakeGenericMethod(elementType);
                concreteMethod.Invoke(null, new object[] { fixture, randomSource });
            }
        }

        private static void RegisterAggressionBased<TElement>(IFixture fixture, RandomSource randomSource)
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

            IAggressionBased<TElement> InterfaceCreator() =>
                randomSource.NextDouble() < 0.33 ? (IAggressionBased<TElement>)Creator1() : Creator3();

            fixture.Register(InterfaceCreator);
        }

        private static void RegisterNullable<TUnderlyingType>(Fixture fixture, RandomSource randomSource)
            where TUnderlyingType : struct
        {
            TUnderlyingType? Creator() => randomSource.NextDouble() < 0.1
                ? (TUnderlyingType?)null
                : fixture.Create<TUnderlyingType>();

            fixture.Register(Creator);
        }

        private static void RegisterCollections<TElement>(Fixture fixture, RandomSource randomSource)
        {
            TElement[] ArrayCreator()
            {
                int length = randomSource.Next(2, 6);
                var array = new TElement[length];
                for (int i = 0; i < array.Length; i++)
                    array[i] = fixture.Create<TElement>();

                return array;
            }

            List<TElement> ListCreator()
            {
                int length = randomSource.Next(2, 6);
                var list = new List<TElement>(length);
                for (int i = 0; i < length; i++)
                    list.Add(fixture.Create<TElement>());

                return list;
            }

            fixture.Register(ArrayCreator);
            fixture.Register(ListCreator);
        }
    }
}
