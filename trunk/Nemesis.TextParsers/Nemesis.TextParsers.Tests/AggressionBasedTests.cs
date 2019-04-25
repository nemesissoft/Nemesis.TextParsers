﻿using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FacIntArr = Nemesis.TextParsers.Tests.AggressionBasedFactory<int[]>;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal class AggressionBasedTests
    {
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
            var actual = AggressionBasedFactory<int>.FromValues(data.inputValues);

            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.ToString(), Is.EqualTo(data.expectedOutput));

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValues));
        }

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromText_ShouldParse((string inputText, IEnumerable<int> _, string _s, IEnumerable<int> expectedValues) data)
        {
            var actual = AggressionBasedFactory<int>.FromText(data.inputText);

            Assert.That(actual, Is.Not.Null);

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EquivalentTo(data.expectedValues));
        }

        private static IEnumerable<(Type, string, IEnumerable)> ValidValuesFor_FromText_Complex() => new[]
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
                AggressionBasedFactory<int>.FromPassiveNormalAggressive(1,2,4),
                AggressionBasedFactory<int>.FromPassiveNormalAggressive(40,50,70),
                AggressionBasedFactory<int>.FromPassiveNormalAggressive(7,8,9),
            }),
            (typeof(IAggressionBased<int>), @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                AggressionBasedFactory<int>.FromPassiveNormalAggressive(1,2,40)
            }),
        };

        [TestCaseSource(nameof(ValidValuesFor_FromText_Complex))]
        public void AggressionBasedFactory_FromText_ShouldParseComplexCases((Type elementType, string input, IEnumerable expectedOutput) data)
        {
            var factoryType = typeof(AggressionBasedFactory<>).MakeGenericType(data.elementType);
            var fromTextMethod = factoryType.GetMethods()
                .Single(m => m.Name == nameof(AggressionBasedFactory<object>.FromText) && m.GetParameters()[0].ParameterType == typeof(string)) 
                ?? throw new MissingMethodException();

            var actual = fromTextMethod.Invoke(null, new object[] { data.input });

            Assert.That(actual, Is.Not.Null);

            var aggressionValuesProviderType = typeof(IAggressionValuesProvider<>).MakeGenericType(data.elementType);
            var valuesProperty = aggressionValuesProviderType.GetProperty(nameof(IAggressionValuesProvider<object>.Values)) ?? throw new MissingMemberException();

            var values = valuesProperty.GetValue(actual);

            Assert.That(values, Is.EquivalentTo(data.expectedOutput));
        }

        private static IEnumerable<IEnumerable<int>> InvalidValuesForFactory() => new IEnumerable<int>[]
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

        [TestCaseSource(nameof(InvalidValuesForFactory))]
        public void AggressionBasedFactory_FromValues_NegativeTests(IEnumerable<int> values) =>
            Assert.Throws<ArgumentException>(() => AggressionBasedFactory<int>.FromValues(values));

        #endregion

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

    }
}
