using NUnit.Framework;
using System;
using System.Collections.Generic;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;
using System.Collections;
using Nemesis.TextParsers.Utils;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using static Nemesis.TextParsers.Tests.TestHelper;
using TCD = NUnit.Framework.TestCaseData;
using System.Linq;

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
            var transformer = Sut.GetTransformer<IAggressionBased<TElement>>();
            var actual = transformer.Parse(input);

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Is.AssignableTo<IAggressionBased<TElement>>());

            var values = actual.ToList();
            Assert.That(values, Is.EqualTo(expectedOutput));
        }

        private static IEnumerable<TCD> ValidParsingData()
            => new (string, IEnumerable<int>)[]
            {
                (null,           new []{0}),
                ("",            new []{0}),
                ("123",         new []{123}),
                ("123#456#789", new []{123,456,789}),
                ("123#123#123", new []{123, 123, 123}),

                ("1#22#333#4444#55555", new []{123, 123, 123}),

                ("1#1#1#1#1#1#1#1#1", new []{1,1,1,1,1,1,1,1,1}),
                ("1#1#1#4#4#4#7#7#7", new []{1,1,1,4,4,4,7,7,7}),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}),
            }.Select((t, i) => new TCD(t.Item1, t.Item2).SetName($"Parse{i:00}"));

        [TestCaseSource(nameof(ValidParsingData))]
        public void AggressionBasedFactory_ShouldParse(string inputText, IEnumerable<int> expectedValues)
        {
            var transformer = Sut.GetTransformer<IAggressionBased<int>>();
            var actual = transformer.Parse(inputText);

            Assert.That(actual, Is.Not.Null);

            Assert.That(actual.ToList(),
                Is.EqualTo(expectedValues));
        }

        private static IEnumerable<TCD> InvalidParsingData() => new[]
        {
            new TCD("1#2", "0, 1, 3, 5 or 9 elements, but contained 2"),
            new TCD("1#2#3#4", "0, 1, 3, 5 or 9 elements, but contained 4"),
            new TCD("1#2#3#4#5#6", "0, 1, 3, 5 or 9 elements, but contained 6"),
            new TCD("1#2#3#4#5#6#7", "0, 1, 3, 5 or 9 elements, but contained 7"),
            new TCD("1#2#3#4#5#6#7#8", "0, 1, 3, 5 or 9 elements, but contained 8"),
            new TCD("1#2#3#4#5#6#7#8#9#10", "0, 1, 3, 5 or 9 elements, but contained more than 9"),
            new TCD("1#2#3#4#5#6#7#8#9#10#11", "0, 1, 3, 5 or 9 elements, but contained more than 9"),
        };

        [TestCaseSource(nameof(InvalidParsingData))]
        public void AggressionBasedTransformer_Parse_NegativeTests(string input, string expectedMessagePart)
        {
            var transformer = Sut.GetTransformer<IAggressionBased<int>>();

            var ex = Assert.Throws<ArgumentException>(() => transformer.Parse(input.AsSpan()));

            Assert.That(ex.Message, Does.Contain(expectedMessagePart));
        }

        private const string AGG_BASED_STRING_SYNTAX = @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#"" and '\' by doubling it ""\\""

IAggressionBased`1 elements syntax:
	UTF-16 character string";

        private const string AGG_BASED_DICT = @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#"" and '\' by doubling it ""\\""

AggressionBased3`1 elements syntax:
	KEY=VALUE pairs separated with ';' bound with nothing and nothing i.e.
	key1=value1;key2=value2;key3=value3
	(escape '=' with ""\="", ';' with ""\;"", '∅' with ""\∅"" and '\' by doubling it ""\\"")
	Key syntax:
		Whole number from 0 to 4294967295
	Value syntax:
		One of following: CreateNew, Create, Open, OpenOrCreate, Truncate, Append or null";

        private const string AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX = @"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#"" and '\' by doubling it ""\\""

AggressionBased3`1 elements syntax:
	Elements separated with '|' bound with nothing and nothing i.e.
	1|2|3
	(escape '|' with ""\|"", '∅' with ""\∅"" and '\' by doubling it ""\\"")
	Element syntax:
		Whole number from -2147483648 to 2147483647 or null";

        private static IEnumerable<TCD> GetSyntaxData() => new[]
        {
            new TCD(typeof(IAggressionBased<string>), AGG_BASED_STRING_SYNTAX),
            new TCD(typeof(AggressionBased3<Dictionary<uint, System.IO.FileMode?>>), AGG_BASED_DICT),
            new TCD(typeof(AggressionBased3<int?[]>), AGG_BASED_NULLABLE_INT_ARRAY_SYNTAX),
        };



        [TestCaseSource(nameof(GetSyntaxData))]
        public void AggressionBased_GetSyntax(Type type, string expectedSyntax)
        {
            var actual = TextSyntaxProvider.Default.GetSyntaxFor(type);
            Assert.That(actual,
                Is.EqualTo(expectedSyntax)
                    .Using(IgnoreNewLinesComparer.EqualityComparer)
            );
        }
    }
}
