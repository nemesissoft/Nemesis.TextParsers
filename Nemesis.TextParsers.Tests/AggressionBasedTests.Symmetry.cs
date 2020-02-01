using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;


        private static IEnumerable<(string inputText, IEnumerable inputEnumeration, Type expectedAggBasedType, Type elementType)> SymmetryData() => new[]
        {
            (@"1#2#3#4#5#6#7#8#9", (IEnumerable)new[]{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),

            (@"1#1#1#4#4#4#7#7#7", new[]{1,4,7}, typeof(AggressionBased3<int>), typeof(int)),
            (@"1#4#7", new[]{1,4,7}, typeof(AggressionBased3<int>), typeof(int)),

            (@"1#1#1#1#1#1#1#1#1", new[]{1}, typeof(AggressionBased1<int>), typeof(int)),
            (@"1", new[]{1}, typeof(AggressionBased1<int>), typeof(int)),

            (@"1|2|3#4|5|6#7|8|9", new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}, typeof(AggressionBased3<List<int>>), typeof(List<int>)),
            (@"1|2|3#1|2|3#1|2|3", new[]{new List<int>{1,2,3}}, typeof(AggressionBased1<List<int>>), typeof(List<int>)),

            (@"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }, typeof(AggressionBased3<IAggressionBased<int>>), typeof(IAggressionBased<int>)),
            (@"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }, typeof(AggressionBased1<IAggressionBased<int>>), typeof(IAggressionBased<int>)),

            (@"Key=Text", new[] { new Dss { { "Key", @"Text" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
            //GH #1 fix or remove
            //(@"Key=Text\;Text", new[] { new Dss { { "Key", @"Text;Text" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
            
            
            ("0", new []{0}, typeof(AggressionBased1<int>), typeof(int)),

            ("123",new []{123}, typeof(AggressionBased1<int>), typeof(int)),

            ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),
            ("123",         new []{123,123,123}, typeof(AggressionBased1<int>), typeof(int)),

            ("1",                 new []{1,1,1,1,1,1,1,1,1}, typeof(AggressionBased1<int>), typeof(int)),
            ("1#4#7",             new []{1,1,1,4,4,4,7,7,7}, typeof(AggressionBased3<int>), typeof(int)),
            ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),

            (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
            //equal values:
            (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
            //distinct values
            (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;3=T\\=ext\\;3;Key\\;4=Text\\;4#Key\\;5=T\\=ext\\;5;Key\\;6=Text\\;6", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;3"]= "T=ext;3", ["Key;4"]= "Text;4", }, new Dss{["Key;5"]= "T=ext;5", ["Key;6"]= "Text;6", } }, typeof(AggressionBased3<Dss>), typeof(Dss) ),



            ("", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            ("0", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            ("∅", new []{0}, typeof(AggressionBased1<int>), typeof(int)),
            ("∅", new int?[]{null}, typeof(AggressionBased1<int?>), typeof(int?)),

            ("123",new []{123}, typeof(AggressionBased1<int>), typeof(int)),

            ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),

            ("123#123#123", new []{123}, typeof(AggressionBased1<int>), typeof(int)),


            ("1#1#1#1#1#1#1#1#1", new []{1}, typeof(AggressionBased1<int>), typeof(int)),

            ("1#1#1#4#4#4#7#7#7", new []{1,4,7}, typeof(AggressionBased3<int>), typeof(int)),

            ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),
        };

        [TestCaseSource(nameof(SymmetryData))]
        public void SymmetryTests((string inputText, IEnumerable inputEnumeration, Type expectedAggBasedType, Type elementType) data)
        {
            var tester = (
                GetType().GetMethod(nameof(SymmetryHelper), ALL_FLAGS) ??
                throw new MissingMethodException(GetType().FullName, nameof(SymmetryHelper))
            ).MakeGenericMethod(data.elementType);

            tester.Invoke(null, new object[] { data.inputText, data.inputEnumeration, data.expectedAggBasedType });
        }

        private static void SymmetryHelper<TElement>(string inputText, IEnumerable inputEnumeration, Type aggBasedType)
        {
            IReadOnlyList<TElement> inputValues = inputEnumeration.Cast<TElement>().ToList();

            void CheckType(IAggressionBased<TElement> ab)
            {
                Assert.That(ab, Is.Not.Null);
                Assert.That(ab, Is.AssignableTo<IAggressionValuesProvider<TElement>>());
                Assert.That(ab, Is.TypeOf(aggBasedType));

                var actualValues = ((IAggressionValuesProvider<TElement>)ab).Values;

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
                            Assert.That(fullValues, Is.EquivalentTo(compactValues).Using(equalityComparer));
                        break;
                    case 9:
                        Assert.That(fullValues, Is.EquivalentTo(compactValues).Using(equalityComparer));
                        break;
                    default:
                        Assert.Fail("Not expected test case data");
                        break;
                }
            }

            static void CheckEquivalenceAb(IAggressionBased<TElement> ab1, IAggressionBased<TElement> ab2)
            {
                var v1 = ((IAggressionValuesProvider<TElement>)ab1).Values;
                var v2 = ((IAggressionValuesProvider<TElement>)ab2).Values;

                Assert.That(v1, Is.EquivalentTo(v2).Using(StructuralEqualityComparer<TElement>.Instance));
            }

            var ab1 = AggressionBasedFactory<TElement>.FromText(inputText);
            CheckType(ab1);
            var text1 = ab1.ToString();


            var ab2 = AggressionBasedFactory<TElement>.FromValues(inputValues);
            CheckType(ab2);
            var text2 = ab2.ToString();



            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<TElement>>();
            var ab3 = transformer.ParseFromText(inputText);
            CheckType(ab3);
            var text3 = ab3.ToString();
            var text3A = transformer.Format(ab3);


            CheckEquivalenceAb(ab1, ab2);
            CheckEquivalenceAb(ab1, ab3);

            Assert.That(text1, Is.EqualTo(text2));
            Assert.That(text1, Is.EqualTo(text3));
            Assert.That(text1, Is.EqualTo(text3A));

            var parsed1 = AggressionBasedFactory<TElement>.FromText(text1);
            var parsed2 = AggressionBasedFactory<TElement>.FromText(text2);
            var parsed3 = AggressionBasedFactory<TElement>.FromText(text3);
            var parsed3A = AggressionBasedFactory<TElement>.FromText(text3A);

            CheckEquivalenceAb(ab1, parsed1);
            CheckEquivalenceAb(ab1, parsed2);
            CheckEquivalenceAb(ab1, parsed3);
            CheckEquivalenceAb(ab1, parsed3A);
            CheckEquivalenceAb(parsed1, parsed2);
        }

        

        private class StructuralEqualityComparer<T> : IEqualityComparer<T>
        {
            public static readonly IEqualityComparer<T> Instance = new StructuralEqualityComparer<T>();

            private StructuralEqualityComparer() { }

            public bool Equals(T x, T y) => StructuralEquality.Equals(x, y);

            public int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;
        }
    }
}
