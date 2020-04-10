using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TCD = NUnit.Framework.TestCaseData;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;
using static Nemesis.TextParsers.Tests.TestHelper;

// ReSharper disable once CheckNamespace
namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
        private static IEnumerable<(int num, string inputText, IEnumerable inputEnumeration, Type expectedAggBasedType, Type elementType)>
            SymmetryData() => new[]
        {
            (01, @"1|2|3#4|5|6#7|8|9", (IEnumerable)new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}, typeof(AggressionBased3<List<int>>), typeof(List<int>)),
            (02, @"1|2|3#1|2|3#1|2|3",              new[]{new List<int>{1,2,3},new List<int>{1,2,3},new List<int>{1,2,3}}, typeof(AggressionBased3<List<int>>), typeof(List<int>)),

            (03, @"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }, typeof(AggressionBased3<IAggressionBased<int>>), typeof(IAggressionBased<int>)),
            (04, @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }, typeof(AggressionBased3<IAggressionBased<int>>), typeof(IAggressionBased<int>)),

            (05, "123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),

            (06, "1",                 new []{1}, typeof(AggressionBased1<int>), typeof(int)),
            (07, "1#4#7",             new []{1,4,7}, typeof(AggressionBased3<int>), typeof(int)),
            (08, "1#1#1#4#4#4#7#7#7", new []{1,1,1,4,4,4,7,7,7}, typeof(AggressionBased9<int>), typeof(int)),
            (09, "1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),
            (10, "1#1#1#1#1#1#1#1#1", new []{1,1,1,1,1,1,1,1,1}, typeof(AggressionBased9<int>), typeof(int)),


            (11, @"Key=Text", new[] { new Dss { { "Key", @"Text" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
            (12, @"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
            //equal values:
            (13, @"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased3<Dss>), typeof(Dss) ),
            //distinct values
            (14, @"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;3=T\\=ext\\;3;Key\\;4=Text\\;4#Key\\;5=T\\=ext\\;5;Key\\;6=Text\\;6", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;3"]= "T=ext;3", ["Key;4"]= "Text;4", }, new Dss{["Key;5"]= "T=ext;5", ["Key;6"]= "Text;6", } }, typeof(AggressionBased3<Dss>), typeof(Dss) ),


            (15, "", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            (16, "0", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            (17, "∅", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            (18, "∅", new int?[]{null}, typeof(AggressionBased1<int?>), typeof(int?)),

            (19, "123",new []{123}, typeof(AggressionBased1<int>), typeof(int)),
            (20, "123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),
            (21, "123#123#123", new []{123,123,123}, typeof(AggressionBased3<int>), typeof(int)),


            (22, "5#5#5#5#5#5#5#5#5", new []{5,5,5,5,5,5,5,5,5}, typeof(AggressionBased9<int>), typeof(int)),

            (23, "2#2#2#4#4#4#7#7#7", new []{2,2,2,4,4,4,7,7,7}, typeof(AggressionBased9<int>), typeof(int)),

            (24, "1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),
        };

        [TestCaseSource(nameof(SymmetryData))]
        public void SymmetryTests((int number, string inputText, IEnumerable inputEnumeration, Type expectedAggBasedType, Type elementType) data)
        {
            var symmetry = MakeDelegate<Action<string, IEnumerable, Type>>(
                (p1, p2, p3) => SymmetryHelper<int>(p1, p2, p3), data.elementType
            );

            symmetry(data.inputText, data.inputEnumeration, data.expectedAggBasedType);
        }

        private static void SymmetryHelper<TElement>(string inputText, IEnumerable inputEnumeration, Type aggBasedType)
        {
            IReadOnlyList<TElement> inputValues = inputEnumeration.Cast<TElement>().ToList();

            void CheckType(IAggressionBased<TElement> ab, string stage)
            {
                Assert.That(ab, Is.Not.Null);
                Assert.That(ab, Is.AssignableTo<IAggressionBased<TElement>>());
                Assert.That(ab, Is.TypeOf(aggBasedType), $"CheckType_{stage}");

                var actualValues = ab.GetValues().ToList();

                CheckEquivalence(actualValues, inputValues);
            }

            static void CheckEquivalence(IReadOnlyList<TElement> compactValues, IReadOnlyList<TElement> fullValues)
            {
                var equalityComparer = StructuralEqualityComparer<TElement>.Instance;
                switch (compactValues.Count)
                {
                    case 1:
                        Assert.That(fullValues, Has.All.EqualTo(compactValues.Single()).Using(equalityComparer));
                        break;
                    case 3:
                        if (fullValues.Count == 9)
                        {
                            void Check(int index9, int index3) =>
                                Assert.That(fullValues[index9], Is.EqualTo(compactValues[index3]).Using(equalityComparer));

                            Check(0, 0);
                            Check(1, 0);
                            Check(2, 0);

                            Check(3, 1);
                            Check(4, 1);
                            Check(5, 1);

                            Check(6, 2);
                            Check(7, 2);
                            Check(8, 2);
                        }
                        else
                            Assert.That(fullValues, Is.EqualTo(compactValues).Using(equalityComparer));
                        break;
                    case 9:
                        Assert.That(fullValues, Is.EqualTo(compactValues).Using(equalityComparer));
                        break;
                    default:
                        Assert.Fail("Not expected test case data");
                        break;
                }
            }

            static void CheckEquivalenceAb(IAggressionBased<TElement> ab1, IAggressionBased<TElement> ab2, string stage)
            {
                var v1 = ab1.GetValues().ToList();
                var v2 = ab2.GetValues().ToList();

                Assert.That(v1, Is.EqualTo(v2).Using(StructuralEqualityComparer<TElement>.Instance), $"CheckEquivalenceAb_{stage}");
            }

            var transformer = Sut.GetTransformer<IAggressionBased<TElement>>();

            var ab1 = transformer.Parse(inputText.AsSpan());
            CheckType(ab1, "1");
            var text1 = transformer.Format(ab1);


            var ab2 = AggressionBasedFactory<TElement>.FromValues(inputValues);
            CheckType(ab2, "2");
            var text2 = transformer.Format(ab2);



            var ab3 = transformer.Parse(inputText);
            CheckType(ab3, "3");
            var text3 = transformer.Format(ab3);


            CheckEquivalenceAb(ab1, ab2, "1==2");
            CheckEquivalenceAb(ab1, ab3, "1==3");

            Assert.That(text1, Is.EqualTo(text2), "2");
            Assert.That(text1, Is.EqualTo(text3), "3");

            var parsed1  = transformer.Parse(text1.AsSpan());
            var parsed2  = transformer.Parse(text2.AsSpan());
            var parsed3  = transformer.Parse(text3.AsSpan());

            CheckEquivalenceAb(ab1, parsed1, "1==p1");
            CheckEquivalenceAb(ab1, parsed2, "1==p2");
            CheckEquivalenceAb(ab1, parsed3, "1==p3");
            CheckEquivalenceAb(parsed1, parsed2, "p1==p1");
        }



        [TestCase(typeof(string))]
        [TestCase(typeof(string[]))]
        [TestCase(typeof(int[]))]
        [TestCase(typeof(ICollection<string>))]
        [TestCase(typeof(List<string>))]
        [TestCase(typeof(float?))]
        [TestCase(typeof(Complex?))]
        [TestCase(typeof(int))]
        public void Default_SymmetryTest(Type type)
        {
            var defaultSymmetry = MakeDelegate<Action>(
                () => Default_SymmetryTestHelper<int>(), type
            );

            defaultSymmetry();
        }

        private static void Default_SymmetryTestHelper<TElement>()
        {
            var null1 = AggressionBasedFactory<TElement>.FromOneValue(default);
            var null3 = AggressionBasedFactory<TElement>.FromPassiveNormalAggressiveChecked(default, default, default);
            IAggressionBased<TElement> @null = null;

            var sut = Sut.GetTransformer<IAggressionBased<TElement>>();


            var text1 = sut.Format(null1);
            var text3 = sut.Format(null3);
            // ReSharper disable once ExpressionIsAlwaysNull
            var textNull = sut.Format(@null);


            var parsed1 = sut.Parse(text1);
            var parsed3 = sut.Parse(text3);
            var parsedNull = sut.Parse(textNull);


            IsMutuallyEquivalent(null1, parsed1);
            IsMutuallyEquivalent(null3, parsed3);

            Assert.That(parsedNull, Is.Not.Null);
            IsMutuallyEquivalent(parsedNull, AggressionBasedFactory<TElement>.FromOneValue(default));
        }

        private static IEnumerable<TCD> EmptyData() => new[]
        {
            new TCD(typeof(string), ""),
            new TCD(typeof(int[]), new int[0]),
            new TCD(typeof(ICollection<int>), new List<int>()),
            new TCD(typeof(ICollection<KeyValuePair<int, string>>), new List<KeyValuePair<int, string>>()),
        };

        [TestCaseSource(nameof(EmptyData))]
        public void Empty_SymmetryTest(Type type, IEnumerable emptyValue)
        {
            var emptySymmetry = MakeDelegate<Action<IEnumerable>>(
                p1 => Empty_SymmetryTestHelper<IEnumerable>(p1), type
            );

            emptySymmetry(emptyValue);
        }

        private static void Empty_SymmetryTestHelper<TCollection>(IEnumerable emptyValue)
        {
            var empty1 = AggressionBasedFactory<TCollection>.FromOneValue((TCollection)emptyValue);
            var empty3 = AggressionBasedFactory<TCollection>.FromPassiveNormalAggressiveChecked((TCollection)emptyValue, (TCollection)emptyValue, (TCollection)emptyValue);

            var sut = Sut.GetTransformer<IAggressionBased<TCollection>>();


            string text1 = sut.Format(empty1);
            string text3 = sut.Format(empty3);


            var parsed1 = sut.Parse(text1);
            var parsed3 = sut.Parse(text3);


            IsMutuallyEquivalent(empty1, parsed1);
            IsMutuallyEquivalent(empty3, parsed3);

            IsMutuallyEquivalent(parsed1, AggressionBasedFactory<TCollection>.FromOneValue((TCollection)emptyValue));
        }

        [Test]
        public void TransformConcreteAggressionBased()
        {
            //var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<int>>();
            //var formatter = (IFormatter<AggressionBased1<int>>) transformer;
        }
    }
}
