using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FacIntArr = Nemesis.TextParsers.Tests.AggressionBasedFactory<int[]>;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal class AggressionBasedTests
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        #region Factory

        private static IEnumerable<(string, IEnumerable<int>, string, IEnumerable<int>)> ValidValuesForFactory()
            => new (string, IEnumerable<int>, string, IEnumerable<int>)[]
        {
            (null,          null, "0", new []{0}),
            ("",            new int [0], @"0",new []{0}),
            ("123",         new []{123}, @"123",new []{123}),
            ("123#456#789", new []{123,456,789}, @"123#456#789",new []{123,456,789}),
            ("123#123#123", new []{123,123,123}, @"123",new []{123}),

            ("1#1#1#1#1#1#1#1#1", new []{1,1,1,1,1,1,1,1,1}, @"1",new []{1}),
            ("1#1#1#4#4#4#7#7#7", new []{1,1,1,4,4,4,7,7,7}, @"1#4#7",new []{1,4,7}),
            ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, @"1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}),
        };

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromValues_ShouldCreateAndCompactValues((string _, IEnumerable<int> inputValues, string expectedOutput, IEnumerable<int> expectedValues) data)
        {
            var actual = FacInt.FromValues(data.inputValues);

            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.ToString(), Is.EqualTo(data.expectedOutput));

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValues));
        }

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromText_ShouldParse((string inputText, IEnumerable<int> _, string _s, IEnumerable<int> expectedValues) data)
        {
            var actual = FacInt.FromText(data.inputText);

            Assert.That(actual, Is.Not.Null);

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValues));
        }

        private static IEnumerable<(Type elemenType, string input, IEnumerable expectedOutput)> ValidValuesFor_FromText_Complex() => new[]
        {
            (typeof(int), @"1#2#3#4#5#6#7#8#9", (IEnumerable)new[]{1,2,3,4,5,6,7,8,9}),

            (typeof(int), @"1#1#1#4#4#4#7#7#7", new[]{1,4,7} ),
            (typeof(int), @"1#4#7", new[]{1,4,7} ),

            (typeof(int), @"1#1#1#1#1#1#1#1#1", new[]{1}),
            (typeof(int), @"1", new[]{1}),

            (typeof(List<int>), @"1|2|3#4|5|6#7|8|9", new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}),
            (typeof(List<int>), @"1|2|3#1|2|3#1|2|3", new[]{new List<int>{1,2,3}}),

            (typeof(IAggressionBased<int>), @"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }),
            (typeof(IAggressionBased<int>), @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }),

            (typeof(Dss), @"Key=Text", new[] { new Dss { { "Key", @"Text" } } }),
            //(typeof(Dss), @"Key=Text\;Text", new[] { new Dss { { "Key", @"Text;Text" } } }),
        };

        [TestCaseSource(nameof(ValidValuesFor_FromText_Complex))]
        public void AggressionBasedFactory_FromText_ShouldParseComplexCases((Type elementType, string input, IEnumerable expectedOutput) data)
        {
            var tester = (
                            GetType().GetMethod(nameof(AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper), ALL_FLAGS) ??
                            throw new MissingMethodException(GetType().FullName, nameof(AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper))
                        ).MakeGenericMethod(data.elementType);

            tester.Invoke(null, new object[] { data.input, data.expectedOutput });
        }

        private static void AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper<TElement>(string input, IEnumerable expectedOutput)
        {
            var actual = AggressionBasedFactory<TElement>.FromText(input);

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Is.AssignableTo<IAggressionValuesProvider<TElement>>());

            var values = ((IAggressionValuesProvider<TElement>)actual).Values;
            Assert.That(values, Is.EquivalentTo(expectedOutput));
        }

        private static IEnumerable<IEnumerable<int>> FromValues_Invalid() => new IEnumerable<int>[]
        {
            new []{1,2},
            new []{1,2,3,4},
            new []{1,2,3,4,5},
            new []{1,2,3,4,5,6},
            new []{1,2,3,4,5,6,7},
            new []{1,2,3,4,5,6,7,8},

            new []{1,2,3,4,5,6,7,8,9,10},
            new []{1,2,3,4,5,6,7,8,9,10,11},
        };

        [TestCaseSource(nameof(FromValues_Invalid))]
        public void AggressionBasedFactory_FromValues_NegativeTests(IEnumerable<int> values) =>
            Assert.Throws<ArgumentException>(() => FacInt.FromValues(values));

        private static IEnumerable<(string, int[], Type, Type)> TextTransformer_Symmetry()
            => new[]
            {
                ("", new []{0}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("", new []{0}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("123",new []{123}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("123",new []{123}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("123#456#789", new []{123,456,789}, typeof(IAggressionBased<int>), typeof(AggressionBased3<int>)),
                ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(AggressionBased3<int>)),

                ("123#123#123", new []{123}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("123#123#123", new []{123}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),


                ("1#1#1#1#1#1#1#1#1", new []{1}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("1#1#1#1#1#1#1#1#1", new []{1}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("1#1#1#4#4#4#7#7#7", new []{1,4,7}, typeof(IAggressionBased<int>), typeof(AggressionBased3<int>)),
                ("1#1#1#4#4#4#7#7#7", new []{1,4,7}, typeof(AggressionBased3<int>), typeof(AggressionBased3<int>)),


                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(IAggressionBased<int>), typeof(AggressionBased9<int>)),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(AggressionBased9<int>)),
            };

        [TestCaseSource(nameof(TextTransformer_Symmetry))]
        public void TextTransformer_ShouldParseAndFormatAppropriateTypes((string inputText, int[] expectedValues, Type contractType, Type expectedType) data)
        {
            var transformer = TextTransformer.Default.GetTransformer(data.contractType);
            var parsed = transformer.ParseObject(data.inputText);

            Assert.That(parsed, Is.TypeOf(data.expectedType));
            Assert.That(((IAggressionValuesProvider<int>)parsed).Values, Is.EquivalentTo(data.expectedValues));


            string formatted = transformer.FormatObject(parsed);
            var parsed2 = transformer.ParseObject(formatted);

            Assert.That(
                ((IAggressionValuesProvider<int>)parsed).Values,
                Is.EquivalentTo(
                    ((IAggressionValuesProvider<int>)parsed2).Values
                    ));
        }


        private static IEnumerable<(string expectedOutput, object inputValues, Type expectedAggBasedType, Type expectedElementType)> TextTransformer_Format()
         => new[]
         {
                ("0", (object)new []{0}, typeof(AggressionBased1<int>), typeof(int)),

                ("123",new []{123}, typeof(AggressionBased1<int>), typeof(int)),

                ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),
                ("123",         new []{123,123,123}, typeof(AggressionBased1<int>), typeof(int)),

                ("1",                 new []{1,1,1,1,1,1,1,1,1}, typeof(AggressionBased1<int>), typeof(int)),
                ("1#4#7",             new []{1,1,1,4,4,4,7,7,7}, typeof(AggressionBased3<int>), typeof(int)),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),

                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
                //equal values:
                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", (object)new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
                //distinct values
                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;3=T\\=ext\\;3;Key\\;4=Text\\;4#Key\\;5=T\\=ext\\;5;Key\\;6=Text\\;6", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;3"]= "T=ext;3", ["Key;4"]= "Text;4", }, new Dss{["Key;5"]= "T=ext;5", ["Key;6"]= "Text;6", } }, typeof(AggressionBased3<Dss>), typeof(Dss) ),

                // GH #1 merged with cases from TextTransformer_Complex_ShouldFormat 
                (@"Key=Value", new []{new Dss{ { "Key", "Value" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
                //(@"Key=Va\;lue", new []{new Dss { { "Key", "Va;lue" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
         };

        [TestCaseSource(nameof(TextTransformer_Format))]
        public void TextTransformer_ShouldFormat((string expectedOutput, object inputValues, Type expectedAggBasedType, Type expectedElementType) data)
        {
            var tester = (
                GetType().GetMethod(nameof(TextTransformer_ShouldFormatHelper), ALL_FLAGS) ??
                throw new MissingMethodException(GetType().FullName, nameof(TextTransformer_ShouldFormatHelper))
            ).MakeGenericMethod(data.expectedElementType);

            tester.Invoke(null, new[] { data.expectedOutput, data.inputValues, data.expectedAggBasedType });
        }

        private static void TextTransformer_ShouldFormatHelper<TElement>(string expectedOutput, TElement[] inputValues, Type expectedAggBasedType)
        {
            var aggBased = AggressionBasedFactory<TElement>.FromValues(inputValues);
            Assert.That(aggBased, Is.TypeOf(expectedAggBasedType));


            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<TElement>>();
            string formatted = transformer.Format(aggBased);


            Assert.That(formatted, Is.EqualTo(expectedOutput));
        }

        #endregion

        #region Equals

        private static IEnumerable<(Type, string, string)> ValidValuesForEquals() => new[]
        {
            (typeof(int), @"1#2#3#4#5#6#7#8#9", @"1#2#3#4#5#6#7#8#9"),

            (typeof(int), @"1#1#1#4#4#4#7#7#7", @"1#4#7"),
            (typeof(int), @"1#1#1#4#4#4#7#7#7", @"1#1#1#4#4#4#7#7#7"),

            (typeof(int), @"1#1#1#1#1#1#1#1#1", @"1"),
            (typeof(int), @"1#1#1#1#1#1#1#1#1", @"1#1#1#1#1#1#1#1#1"),

            (typeof(List<int>), @"1|2|3#4|5|6#7|8|9", @"1|2|3#4|5|6#7|8|9"),
            (typeof(List<int>), @"1|2|3#1|2|3#1|2|3", @"1|2|3"),

            (typeof(IAggressionBased<int>), @"1\#2\#3#4\#5\#6#7\#8\#9", @"1\#2\#3#4\#5\#6#7\#8\#9"),
            (typeof(IAggressionBased<int>), @"1\#2\#3#1\#2\#3#1\#2\#3", @"1\#2\#3"),
        };

        [TestCaseSource(nameof(ValidValuesForEquals))]
        public void EqualsTests((Type elementType, string text1, string text2) data)
        {
            var factoryType = typeof(AggressionBasedFactory<>).MakeGenericType(data.elementType);
            var fromTextMethod = factoryType.GetMethods()
               .Single(m => m.Name == nameof(AggressionBasedFactory<object>.FromText) && m.GetParameters()[0].ParameterType == typeof(string))
               ?? throw new MissingMethodException();

            var ab1 = fromTextMethod.Invoke(null, new object[] { data.text1 });
            var ab2 = fromTextMethod.Invoke(null, new object[] { data.text2 });

            Assert.That(ab1, Is.EqualTo(ab2));
        }

        [Test]
        public void NastyEqualsTests()
        {
            var crazyAggBased = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromPassiveNormalAggressive(
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {10, 11}, new[] {20, 21}, new[] {30, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {40, 41}, new[] {50, 51}, new[] {60, 61}),
                },
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {10, 11}, new[] {20, 21}, new[] {30, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {40, 41}, new[] {50, 51}, new[] {60, 61}),
                },
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {10, 11}, new[] {20, 21}, new[] {30, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {40, 41}, new[] {50, 51}, new[] {60, 61}),
                }
            );
            var text = crazyAggBased.ToString();

            var actual = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromText(text);

            Assert.That(actual, Is.EqualTo(crazyAggBased));


            var crazyAggBased2 = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromPassiveNormalAggressive(
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {10, 11}, new[] {20, 21}, new[] {30, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {40, 41}, new[] {50, 51}, new[] {60, 61}),
                },
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {100, 11}, new[] {200, 21}, new[] {300, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {400, 41}, new[] {500, 51}, new[] {600, 61}),
                },
                new List<IAggressionBased<int[]>>
                {
                    FacIntArr.FromPassiveNormalAggressive(new[] {1000, 11}, new[] {2000, 21}, new[] {3000, 31}),
                    FacIntArr.FromPassiveNormalAggressive(new[] {4000, 41}, new[] {5000, 51}, new[] {6000, 61}),
                }
            );
            var text2 = crazyAggBased2.ToString();

            var actual2 = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromText(text2);

            Assert.That(actual2, Is.EqualTo(crazyAggBased2));
        }

        #endregion
    }
}
