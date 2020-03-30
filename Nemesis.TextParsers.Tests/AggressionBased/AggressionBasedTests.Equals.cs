using NUnit.Framework;
using System;
using System.Collections.Generic;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using static Nemesis.TextParsers.Tests.TestHelper;

// ReSharper disable once CheckNamespace
namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
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

            (typeof(Dss), @"A=1;B=2#C=1;D=2#E=1;F=2", @"A=1;B=2#C=1;D=2#E=1;F=2"),
            (typeof(Dss), @"A=1;B=2#A=1;B=2#A=1;B=2", @"A=1;B=2"),
        };

        [TestCaseSource(nameof(ValidValuesForEquals))]
        public void EqualsTests((Type elementType, string text1, string text2) data)
        {
            var equals = MakeDelegate<Action<string, string>>(
                (t1, t2) => EqualsTestsHelper<int>(t1, t2), data.elementType
            );
            
            equals(data.text1, data.text2);
        }

        private static void EqualsTestsHelper<TElement>(string text1, string text2)
        {
            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<TElement>>();

            var ab1 = transformer.Parse(text1.AsSpan());
            var ab2 = transformer.Parse(text2.AsSpan());

            Assert.That(ab1, Is.EqualTo(ab2));
        }

        [Test]
        public void NastyEqualsTests()
        {
            static IAggressionBased<int[]> FromPna(int[] p, int[] n, int[] a) =>
                  AggressionBasedFactory<int[]>.FromPassiveNormalAggressive(p, n, a);

            static List<IAggressionBased<int[]>> SameList() => new List<IAggressionBased<int[]>>
            {
                FromPna(new[] {10, 11}, new[] {20, 21}, new[] {30, 31}),
                FromPna(new[] {40, 41}, new[] {50, 51}, new[] {60, 61}),
            };

            var crazyAggBasedSame = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromPassiveNormalAggressive(
                SameList(),
                SameList(),
                SameList()
            );
            Assert.That(crazyAggBasedSame, Is.TypeOf<AggressionBased1<List<IAggressionBased<int[]>>>>());

            var text = crazyAggBasedSame.ToString();

            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<List<IAggressionBased<int[]>>>>();

            var actual = transformer.Parse(text.AsSpan());

            Assert.That(actual, Is.EqualTo(crazyAggBasedSame));



            var crazyAggBasedNotSame = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromPassiveNormalAggressive(
                SameList(),
                new List<IAggressionBased<int[]>>
                {
                    FromPna(new[] {100, 11}, new[] {200, 21}, new[] {300, 31}),
                    FromPna(new[] {400, 41}, new[] {500, 51}, new[] {600, 61}),
                },
                new List<IAggressionBased<int[]>>
                {
                    FromPna(new[] {1000, 11}, new[] {2000, 21}, new[] {3000, 31}),
                    FromPna(new[] {4000, 41}, new[] {5000, 51}, new[] {6000, 61}),
                }
            );
            var text2 = crazyAggBasedNotSame.ToString();

            var actual2 = transformer.Parse(text2.AsSpan());

            Assert.That(actual2, Is.EqualTo(crazyAggBasedNotSame));



            var crazyAggBasedSameNotCompacted = AggressionBasedFactory<List<IAggressionBased<int[]>>>.FromPassiveNormalAggressiveChecked(
                SameList(),
                SameList(),
                SameList()
            );
            Assert.That(crazyAggBasedSameNotCompacted, Is.TypeOf<AggressionBased3<List<IAggressionBased<int[]>>>>());

            var textNotCompacted = crazyAggBasedSameNotCompacted.ToString();

            var actualNotCompacted = transformer.Parse(textNotCompacted.AsSpan());

            Assert.That(actualNotCompacted, Is.EqualTo(crazyAggBasedSameNotCompacted));
        }
    }
}
