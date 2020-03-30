using NUnit.Framework;
using System;
using System.Collections.Generic;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;
using System.Collections;
using Nemesis.TextParsers.Utils;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using static Nemesis.TextParsers.Tests.TestHelper;
using TCD = NUnit.Framework.TestCaseData;

// ReSharper disable once CheckNamespace
namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
        private static IEnumerable<(int number, Type elemenType, string input, IEnumerable expectedOutput)> ValidValuesFor_FromText_Complex() => new[]
        {
            (01, typeof(int), @"1#2#3#4#5#6#7#8#9", (IEnumerable)new[]{1,2,3,4,5,6,7,8,9}),

            (02, typeof(int), @"1#1#1#4#4#4#7#7#7", new[]{1,1,1,4,4,4,7,7,7} ),
            (03, typeof(int), @"1#4#7", new[]{1,4,7} ),

            (04, typeof(int), @"1#1#1#1#1#1#1#1#1", new[]{1,1,1,1,1,1,1,1,1}),
            (05, typeof(int), @"1", new[]{1}),

            (06, typeof(List<int>), @"1|2|3#4|5|6#7|8|9", new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}),
            (07, typeof(List<int>), @"1|2|3#1|2|3#1|2|3", new[]{new List<int>{1,2,3},new List<int>{1,2,3},new List<int>{1,2,3}}),

            (08, typeof(IAggressionBased<int>), @"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }),
            (09, typeof(IAggressionBased<int>), @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40),
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }),

            (10, typeof(Dss), @"Key=Text", new[] { new Dss { { "Key", @"Text" } } }),
        };

        [TestCaseSource(nameof(ValidValuesFor_FromText_Complex))]
        public void AggressionBasedFactory_FromText_ShouldParseComplexCases((int number, Type elementType, string input, IEnumerable expectedOutput) data)
        {
            var fromText = MakeDelegate<Action<string, IEnumerable>>(
                (p1, p2) => AggressionBasedFactory_ShouldParseComplexCasesHelper<int>(p1, p2), data.elementType
            );

            fromText(data.input, data.expectedOutput);
        }

        private static void AggressionBasedFactory_ShouldParseComplexCasesHelper<TElement>(string input, IEnumerable expectedOutput)
        {
            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<TElement>>();
            var actual = transformer.Parse(input);

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Is.AssignableTo<IAggressionValuesProvider<TElement>>());

            var values = ((IAggressionValuesProvider<TElement>)actual).Values;
            Assert.That(values, Is.EqualTo(expectedOutput));
        }

        private static IEnumerable<(string inputText, IEnumerable<int> inputValues, string expectedOutput, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues)> ValidValuesForFactory()
            => new (string, IEnumerable<int>, string, IEnumerable<int>, IEnumerable<int>)[]
            {
                (null,          null, "0", new []{0}, new []{0}),
                ("",            new int [0], @"0",new []{0},new []{0}),
                ("123",         new []{123}, @"123",new []{123},new []{123}),
                ("123#456#789", new []{123,456,789}, @"123#456#789",new []{123,456,789},new []{123,456,789}),
                ("123#123#123", new []{123,123,123}, @"123",new []{123},new []{123, 123, 123}),

                ("1#1#1#1#1#1#1#1#1", new []{1,1,1,1,1,1,1,1,1}, @"1",new []{1},new []{1,1,1,1,1,1,1,1,1}),
                ("1#1#1#4#4#4#7#7#7", new []{1,1,1,4,4,4,7,7,7}, @"1#4#7",new []{1,4,7},new []{1,1,1,4,4,4,7,7,7}),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, @"1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, new []{1,2,3,4,5,6,7,8,9}),
            };

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_FromValues_ShouldCreateAndCompactValues((string _, IEnumerable<int> inputValues, string expectedOutput, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues) data)
        {
            var actual = FacInt.FromValuesCompact(data.inputValues);

            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.ToString(), Is.EqualTo(data.expectedOutput));

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EqualTo(data.expectedValuesCompacted));
        }

        [TestCaseSource(nameof(ValidValuesForFactory))]
        public void AggressionBasedFactory_ShouldParse((string inputText, IEnumerable<int> _, string _s, IEnumerable<int> expectedValuesCompacted, IEnumerable<int> expectedValues) data)
        {
            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<int>>();
            var actual = transformer.Parse(data.inputText);

            Assert.That(actual, Is.Not.Null);

            Assert.That(((IAggressionValuesProvider<int>)actual).Values, Is.EqualTo(data.expectedValues));
        }

        private static IEnumerable<TCD> FromValues_Invalid() => new []
        {
            new TCD("1#2", "0, 1, 3 or 9 elements, but contained 2"),
            new TCD("1#2#3#4", "0, 1, 3 or 9 elements, but contained 4"),
            new TCD("1#2#3#4#5", "0, 1, 3 or 9 elements, but contained 5"),
            new TCD("1#2#3#4#5#6", "0, 1, 3 or 9 elements, but contained 6"),
            new TCD("1#2#3#4#5#6#7", "0, 1, 3 or 9 elements, but contained 7"),
            new TCD("1#2#3#4#5#6#7#8", "0, 1, 3 or 9 elements, but contained 8"),
            new TCD("1#2#3#4#5#6#7#8#9#10", "0, 1, 3 or 9 elements, but contained more than 9"),
            new TCD("1#2#3#4#5#6#7#8#9#10#11", "0, 1, 3 or 9 elements, but contained more than 9"),
        };

        [TestCaseSource(nameof(FromValues_Invalid))]
        public void AggressionBasedTransformer_Parse_NegativeTests(string input, string expectedMessagePart)
        {
            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<int>>();

            var ex = Assert.Throws<ArgumentException>(() => transformer.Parse(input.AsSpan()));

            Assert.That(ex.Message, Does.Contain(expectedMessagePart));
        }

        private const string AGG_BASED_STRING_SYNTAX =
            @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#""and '\' with double backslash ""\\""
Elements syntax:
UTF-16 character string";

        private const string AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX = @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#""and '\' with double backslash ""\\""
Elements syntax:
Elements separated with pipe ('|') i.e.
1|2|3
(escape '|' with ""\|""and '\' with double backslash ""\\"")
Element syntax:
Whole number from -2147483648 to 2147483647 or null";

        private static IEnumerable<TCD> GetSyntaxData() => new[]
        {
            new TCD(typeof(IAggressionBased<string>), AGG_BASED_STRING_SYNTAX),
            new TCD(typeof(AggressionBased3<string>), AGG_BASED_STRING_SYNTAX),
            new TCD(typeof(AggressionBased3<int?[]>), AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX),
        };



        [TestCaseSource(nameof(GetSyntaxData))]
        public void AggressionBased_GetSyntax(Type type, string expectedSyntax) =>
            Assert.That(
                TextConverterSyntaxAttribute.GetConverterSyntax(type),
                Is.EqualTo(
                    expectedSyntax
                    )
                .Using(IgnoreNewLinesComparer.EqualityComparer)
                );
    }
}
