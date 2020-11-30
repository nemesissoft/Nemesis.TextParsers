using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoFixture;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Tests.Deconstructable;
using Nemesis.TextParsers.Tests.Entities;
using Nemesis.TextParsers.Tests.Infrastructure;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(typeof(Sut), nameof(Sut.DefaultStore))]
    [TestFixture(typeof(Sut), nameof(Sut.BorderedStore))]
    [TestFixture(typeof(Sut), nameof(Sut.RandomStore))]
    public sealed class ExploratoryTests
    {
        private readonly ITransformerStore _transformerStore;

        public ExploratoryTests(Type containerType, string propertyName)
        {
            var prop = containerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMemberException(containerType.FullName, propertyName);
            _transformerStore = (ITransformerStore)prop.GetValue(null)
                ?? throw new InvalidOperationException("TransformerStore cannot be null");
        }

        private readonly Fixture _fixture = new();
        private readonly RandomSource _randomSource = new();


        private static IReadOnlyCollection<(ExploratoryTestCategory category, Type type)> _allTestCases;

        static void GetTestCases(RandomSource randomSource)
        {
            var baseTypes = ExploratoryTestsData.GetStandardTypes().Concat(
                new[]
                {
                    typeof(ArraySegment<char>), typeof(ArraySegment<byte>), typeof(ArraySegment<string>),
                    typeof(List<string[]>), typeof(List<int[]>), typeof(List<string>[]), typeof(List<int>[]),

                    typeof(Person), typeof(LargeStruct), typeof(ThreeStrings),

                    typeof(Fruits), typeof(Enum1), typeof(Enum2), typeof(Enum3), typeof(ByteEnum), typeof(SByteEnum),
                    typeof(Int64Enum), typeof(UInt64Enum),
                    typeof(LowPrecisionFloat), typeof(CarrotAndOnionFactors), typeof(BasisPoint),

                    typeof(Fruits[]), typeof(Dictionary<Fruits, double>),
                    typeof(SortedDictionary<Fruits, float>), typeof(SortedList<Fruits, int>),
                    typeof(ReadOnlyDictionary<Fruits, IList<TimeSpan>>),

                    typeof(IAggressionBased<int>), typeof(IAggressionBased<string>),
                    typeof(IAggressionBased<LowPrecisionFloat>), typeof(IAggressionBased<bool>),
                    typeof(IAggressionBased<float?>),
                    typeof(IAggressionBased<int[]>),
                    typeof(IAggressionBased<ValueTuple<bool, bool>>),
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
        public void BeforeAnyTest()
        {
            int seed = _randomSource.UseNewSeed();
            Console.WriteLine($"{GetType().Name} initial seed = {seed}");
            GetTestCases(_randomSource);

            string GetRandomString()
            {
                Span<char> special = stackalloc char[]
                    { '\\', '|', ';', '=', '∅', ',', '{', '}', '[', ']', '(', ')' };
                return
                    "" +
                    _randomSource.NextFrom(special) +
                    _randomSource.NextFrom(special) +
                    _randomSource.NextString('A', 'Z', 8) +
                    _randomSource.NextFrom(special) +
                    _randomSource.NextFrom(special) +
                    _randomSource.NextString('a', 'z', 8) +
                    _randomSource.NextFrom(special) +
                    _randomSource.NextFrom(special);
            }
            Uri GetRandomUri()
            {
                var address = _randomSource.NextString('A', 'Z');
                return new Uri(
                    _randomSource.NextDouble() < 0.5 ? $"mailto:{address}@google.com" : $"https://{address}.google.com"
                );
            }

            _fixture.Register(GetRandomString);
            _fixture.Register(GetRandomUri);

            _fixture.Register(() => _randomSource.NextDouble() < 0.5);
            _fixture.Register(() => _randomSource.NextFloatingNumber());
            _fixture.Register(() => (float)_randomSource.NextFloatingNumber());
            _fixture.Register(() => (decimal)_randomSource.NextFloatingNumber(10000, false));
            _fixture.Register(() => new Complex(
                _randomSource.NextFloatingNumber(1000, false),
                _randomSource.NextFloatingNumber(1000, false)
            ));
            _fixture.Register(() => BigInteger.Parse(_randomSource.NextString('0', '9', 30)));

            _fixture.Register(() => BasisPoint.FromBps(
                ((_randomSource.NextDouble() - 0.5) * 2 + 0.1) * (_randomSource.NextDouble() < 0.5 ? 100 : 10)
            ));

            _fixture.Register(() => new LotsOfDeconstructableData(
                _fixture.Create<string>(), _fixture.Create<bool>(), _fixture.Create<int>(), _fixture.Create<uint?>(),
                _fixture.Create<float>(), _fixture.Create<double?>(), _fixture.Create<FileMode>(), _fixture.Create<List<string>>(),
                _fixture.Create<IReadOnlyList<int>>(), _fixture.Create<Dictionary<string, float?>>(), _fixture.Create<decimal[]>(),
                _fixture.Create<BigInteger[][]>(), _fixture.Create<Complex>()
            ));

            _fixture.Register(() => new TimeSpan(
                _randomSource.Next(2), _randomSource.Next(24), _randomSource.Next(60), _randomSource.Next(60), _randomSource.Next(100))
            );

            Regex GetRandomRegex()
            {
                var rs = _randomSource;

                string GetNextPattern() =>
                    (uint)rs.Next(11) switch
                    {
                        0 => @".",
                        1 => @"\w",
                        2 => @"\d",
                        3 => @"\s",
                        4 => @"\p{L}",
                        5 => rs.NextString('A', 'Z', 3),
                        6 => rs.NextString('a', 'z', 3),
                        7 => rs.NextString('0', '9', 2),
                        8 => $"[{(char)rs.Next('a', 'h')}-{(char)rs.Next('h', 'z' + 1)}]",
                        9 => ";",
                        10 => "~",
                        _ => throw new NotSupportedException("Too big number for auto pattern generation")
                    };


                string GetNextRepetition() =>
                    (uint)rs.Next(7) switch
                    {
                        0 => @"",
                        1 => @"*",
                        2 => @"+",
                        3 => @"?",
                        4 => $"{{{rs.Next(4)}}}",
                        5 => $"{{{rs.Next(4)},}}",
                        6 => $"{{{rs.Next(4)},{rs.Next(4, 9)}}}",
                        _ => throw new NotSupportedException("Too big number for auto repetition generation")
                    };

                var pattern = GetNextPattern() + GetNextRepetition() + GetNextPattern() + GetNextRepetition() +
                              GetNextPattern() + GetNextRepetition() + GetNextPattern() + GetNextRepetition();
                RegexOptions options;
                do
                {
                    options = rs.NextEnum<RegexOptions, int>() & ~(RegexOptions)128;
                } while ((options & RegexOptions.ECMAScript) != 0
                         && (options & ~(RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline |
                                         RegexOptions.Compiled | RegexOptions.CultureInvariant)) != 0);

                return new Regex(pattern, options);
            }
            _fixture.Register(GetRandomRegex);

            _fixture.Register(() => new Version(_randomSource.Next(10), _randomSource.Next(10), _randomSource.Next(10), _randomSource.Next(10)));
            _fixture.Register(() => new IPAddress(new[] { (byte)_randomSource.Next(255), (byte)_randomSource.Next(255), (byte)_randomSource.Next(255), (byte)_randomSource.Next(255) }));


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

            FixtureUtils.RegisterArraySegment<byte>(_fixture, _randomSource);
            FixtureUtils.RegisterArraySegment<char>(_fixture, _randomSource);
            FixtureUtils.RegisterArraySegment<string>(_fixture, _randomSource);

            var structs = _allTestCases
                .Where(d => d.category == ExploratoryTestCategory.Structs ||
                            d.category == ExploratoryTestCategory.Enums)
                .Select(d => d.type)
                .ToList();


            var valueTuples = _allTestCases.Select(t => t.type)
                .Where(TypeMeta.IsValueTuple).Distinct().ToList();

            var valueTupleElementTypes =
                _allTestCases.Select(t => t.type).Where(t => t.IsGenericType && !t.IsGenericTypeDefinition)
                .SelectMany(t => t.GenericTypeArguments)
                .Where(TypeMeta.IsValueTuple)
                .Distinct().ToList();

            valueTuples.AddRange(valueTupleElementTypes);

            FixtureUtils.RegisterAllValueTuples(_fixture, valueTuples);

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


            var collectionElementTypes = _allTestCases.Select(d => d.type)
                .Where(t => TypeMeta.TryGetGenericRealization(t, typeof(IEnumerable<>), out _))
                .Select(t => TypeMeta.GetGenericRealization(t, typeof(IEnumerable<>)).GenericTypeArguments[0])
                .Union(structs)
                .Distinct().ToList();
            FixtureUtils.RegisterAllCollections(_fixture, _randomSource, collectionElementTypes);
        }

        [SetUp]
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
                    var ex = e is TargetInvocationException {InnerException: { } inner}
                        ? inner
                        : e;

                    failed.Add($"Case {caseNo:000} {ex.Message}");
                }

            if (failed.Count > 0)
                Assert.Fail($"Failed cases({failed.Count} cases):{Environment.NewLine}{string.Join(Environment.NewLine, failed)}");
        }

        private void ShouldParseAndFormat(Type testType)
        {
            var tester = MakeDelegate<Action<ExploratoryTests, ITransformer>>
                ((test, trans) => test.ShouldParseAndFormatHelper<int>(trans), testType);

            tester(this, _transformerStore.GetTransformer(testType));
        }

        private void ShouldParseAndFormatHelper<T>(ITransformer ngTransformer)
        {
            var transformer = ngTransformer as ITransformer<T>;
            Assert.That(transformer, Is.Not.Null, "Cast failed");

            Type testType = typeof(T);
            string friendlyName = testType.GetFriendlyName();
            string reason = "<none>";

            try
            {
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

                reason = "Parsing empty 1";
                var parsedEmpty = ParseAndAssert("");

                reason = "Formatting empty";
                string emptyText1 = transformer.Format(parsedEmpty);
                string emptyText2 = transformer.Format(emptyInstance);
                Assert.That(emptyText1, Is.EqualTo(emptyText2));

                reason = "Parsing empty 2";
                var parsedEmpty1 = ParseAndAssert(emptyText1);
                var parsedEmpty2 = ParseAndAssert(emptyText2);

                IsMutuallyEquivalent(parsedEmpty, parsedEmpty1);
                IsMutuallyEquivalent(parsedEmpty, parsedEmpty2);
                IsMutuallyEquivalent(parsedEmpty, emptyInstance);



                //instances
                reason = "Creating fixtures";
                IList<T> instances = _fixture.CreateMany<T>(8).ToList();
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
                throw new Exception($"Failed for {friendlyName} during: {reason} due to '{ae.Message}'");
            }
            catch (Exception e)
            {
                throw new Exception($"Failed for {friendlyName} during: {reason} due to '{e.Message}'");
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

            var allTypesCopy = new List<Type>(allTypes);

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

            var simpleTypes = new[] { typeof(string) }.Concat(enums).Concat(
                    structs.Where(t => !(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ArraySegment<>)))
                ).ToList();


            var simpleTypesCopy = simpleTypes;
            Type GetRandomSimpleType() => randomSource.NextElement(simpleTypesCopy);

            Type GetRandomTupleType(int arity, Type tupleType) =>
                tupleType.MakeGenericType(
                    Enumerable.Repeat(0, arity)
                        .Select(_ => GetRandomSimpleType()).ToArray());

            var valueTuples = new List<(int arity, Type tupleType)>
            {
              //(1, typeof(ValueTuple<>)),
                (2, typeof(ValueTuple<,>)),
                (3, typeof(ValueTuple<,,>)),
                (4, typeof(ValueTuple<,,,>)),
                (5, typeof(ValueTuple<,,,,>)),
                (6, typeof(ValueTuple<,,,,,>)),
                (7, typeof(ValueTuple<,,,,,,>)),
            }.SelectMany(pair => Enumerable.Repeat(0, 6).Select(_ => GetRandomTupleType(pair.arity, pair.tupleType))
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


            var ab = typeof(IAggressionBased<>);
            foreach (var type in allTypesCopy)
                if (!type.DerivesOrImplementsGeneric(ab))
                    aggressionBased.Add(ab.MakeGenericType(type));

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

            //special system types
            typeof(Regex), typeof(RegexOptions), typeof(Version), typeof(IPAddress), 
            
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

    internal static class FixtureUtils
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

        private static void RegisterAggressionBased<TElement>(IFixture fixture, RandomSource randomSource)
        {
            AggressionBased1<TElement> Creator1() => new(fixture.Create<TElement>());

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

        private static void RegisterNullable<TUnderlyingType>(IFixture fixture, RandomSource randomSource)
            where TUnderlyingType : struct
        {
            TUnderlyingType? Creator() => randomSource.NextDouble() < 0.1
                ? (TUnderlyingType?)null
                : fixture.Create<TUnderlyingType>();

            fixture.Register(Creator);
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

        private static void RegisterCollections<TElement>(IFixture fixture, RandomSource randomSource)
        {
            List<TElement> ListCreator()
            {
                int length = randomSource.Next(2, 6);
                var list = new List<TElement>(length);
                for (int i = 0; i < length; i++)
                    list.Add(fixture.Create<TElement>());

                return list;
            }

            TElement[] ArrayCreator() => ListCreator().ToArray();
            LinkedList<TElement> LinkedListCreator() => new(ListCreator());
            Stack<TElement> StackCreator() => new(ListCreator());
            Queue<TElement> QueueCreator() => new(ListCreator());

            fixture.Register(ArrayCreator);
            fixture.Register(ListCreator);
            fixture.Register(LinkedListCreator);
            fixture.Register(StackCreator);
            fixture.Register(QueueCreator);
        }


        public static void RegisterArraySegment<TElement>(Fixture fixture, RandomSource randomSource)
        {
            TElement[] ArrayCreator(int length)
            {
                var array = new TElement[length];
                for (int i = 0; i < length; i++)
                    array[i] = fixture.Create<TElement>();
                return array;
            }

            fixture.Register(() => new ArraySegment<TElement>(ArrayCreator(10), randomSource.Next(4), randomSource.Next(1, 5)));
        }

        #region Value Tuple
        public static void RegisterAllValueTuples(Fixture fixture, IEnumerable<Type> tupleTypes)
        {
            foreach (var tupleType in tupleTypes)
            {
                if (!TypeMeta.TryGetValueTupleElements(tupleType, out var elementTypes))
                    throw new NotSupportedException($"{tupleType.GetFriendlyName()} is not supported for value tuple fixture registration");

                var arity = elementTypes.Length;

                var method = typeof(FixtureUtils).GetMethod($"RegisterValueTuple{arity}",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException(nameof(FixtureUtils), $"RegisterValueTuple{arity}");

                method = method.MakeGenericMethod(elementTypes);

                method.Invoke(null, new object[] { fixture });
            }
        }

        [UsedImplicitly]
        private static void RegisterValueTuple1<T1>(IFixture fixture)
        {
            ValueTuple<T1> TupleCreator() => new(fixture.Create<T1>());

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple2<T1, T2>(IFixture fixture)
        {
            (T1, T2) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>()
            );

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple3<T1, T2, T3>(IFixture fixture)
        {
            (T1, T2, T3) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>(),
                fixture.Create<T3>()
            );

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple4<T1, T2, T3, T4>(IFixture fixture)
        {
            (T1, T2, T3, T4) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>(),
                fixture.Create<T3>(),
                fixture.Create<T4>()
            );

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple5<T1, T2, T3, T4, T5>(IFixture fixture)
        {
            (T1, T2, T3, T4, T5) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>(),
                fixture.Create<T3>(),
                fixture.Create<T4>(),
                fixture.Create<T5>()
            );

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple6<T1, T2, T3, T4, T5, T6>(IFixture fixture)
        {
            (T1, T2, T3, T4, T5, T6) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>(),
                fixture.Create<T3>(),
                fixture.Create<T4>(),
                fixture.Create<T5>(),
                fixture.Create<T6>()
            );

            fixture.Register(TupleCreator);
        }
        [UsedImplicitly]
        private static void RegisterValueTuple7<T1, T2, T3, T4, T5, T6, T7>(IFixture fixture)
        {
            (T1, T2, T3, T4, T5, T6, T7) TupleCreator() =>
            (
                fixture.Create<T1>(),
                fixture.Create<T2>(),
                fixture.Create<T3>(),
                fixture.Create<T4>(),
                fixture.Create<T5>(),
                fixture.Create<T6>(),
                fixture.Create<T7>()
            );

            fixture.Register(TupleCreator);
        }
        #endregion
    }
}
