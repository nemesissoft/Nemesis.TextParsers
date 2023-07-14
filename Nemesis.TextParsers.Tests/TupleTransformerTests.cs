using System;
using System.Collections.Generic;
using System.IO;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class TupleTransformerTests
    {
        private static ITransformer<TTuple> GetSut<TTuple>()
        {
            Type tupleType = typeof(TTuple);

            bool isValueTuple =
                tupleType.IsValueType && tupleType.IsGenericType && !tupleType.IsGenericTypeDefinition &&
                tupleType.Namespace == "System" && tupleType.Name.StartsWith("ValueTuple`");
            bool isKvp = tupleType.IsGenericType && !tupleType.IsGenericTypeDefinition &&
                         tupleType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

            Assert.That(isValueTuple || isKvp, Is.True, "isValueTuple || isKvp");

            var transformer = Sut.GetTransformer<TTuple>();

            return transformer;
        }

        private static IEnumerable<(ValueType instance, string input)> Correct_Tuple_Data() => new (ValueType, string)[]
        {
            (new KeyValuePair<TimeSpan, int>(new TimeSpan(1,2,3,4), 0), @"1.02:03:04=∅"),
            (new KeyValuePair<TimeSpan, int>(TimeSpan.Zero, 15), @"∅=15"),
            (new KeyValuePair<TimeSpan?, int>(null, 15), @"∅=15"),


            (new KeyValuePair<string, float?>("PI", 3.14f), @"PI=3.14"),
            (new KeyValuePair<string, float?>("PI", null), @"PI=∅"),
            (new KeyValuePair<string, float?>("", 3.14f), @"=3.14"),
            (new KeyValuePair<string, float?>("", null), @"=∅"),
            (new KeyValuePair<string, float?>(null, 3.14f), @"∅=3.14"),


            (new KeyValuePair<int, float?>(0, null), null),
            (new KeyValuePair<int, float?>(0, null), @""),
            (new KeyValuePair<string, float?>(null, null), null),
            (new KeyValuePair<string, float?>("", null), @""),
            (new KeyValuePair<string, float?>(null, null), @"∅=∅"),
            (new KeyValuePair<string, float>(null, 0), @"∅=∅"),


            (new KeyValuePair<float?, string>(3.14f, "PI"), @"3.14=PI"),
            (new KeyValuePair<float?, string>(null, "PI"), @"∅=PI"),
            (new KeyValuePair<float?, string>(3.14f, ""), @"3.14="),
            (new KeyValuePair<float?, string>(3.14f, null), @"3.14=∅"),
        
            //escaping tests
            (new KeyValuePair<string, string>(null, null), @"∅=∅"),
            (new KeyValuePair<string, string>(@"∅", null), @"\∅=∅"),
            (new KeyValuePair<string, string>(@" ∅ ", null), @" ∅ =∅"),
            (new KeyValuePair<string, string>(@" ∅ ", null), @" \∅ =∅"),
            (new KeyValuePair<string, string>(@"=", null), @"\==∅"),
            (new KeyValuePair<string, string>(@" = ", null), @" \= =∅"),
            (new KeyValuePair<string, string>(@"∅=\∅=\", @"\=∅"), @"\∅\=\\\∅\=\\=\\\=\∅"),
            (new KeyValuePair<string, string>(@"∅=\∅=\,", @"\=∅"), @"\∅\=\\\∅\=\\,=\\\=\∅"),
            (new KeyValuePair<string, string>(@"∅=\∅=\, ", @" \=∅"), @"\∅\=\\\∅\=\\, = \\\=\∅"),


            //Tuples
            (new ValueTuple<TimeSpan>(new TimeSpan(3,14,15,9)), @"(3.14:15:09)"),
            ((new TimeSpan(3,14,15,9), 3), @"(3.14:15:09,3)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f), @"(3.14:15:09,3,3.14)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi"), @"(3.14:15:09,3,3.14,Pi)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @"(3.14:15:09,3,3.14,Pi,3.14)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true), @"(3.14:15:09,3,3.14,Pi,3.14,true)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15), @"(3.14:15:09,3,3.14,Pi,3.14,true,15)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15, FileMode.CreateNew), @"(3.14:15:09,3,3.14,Pi,3.14,True,15,(CreateNew))"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15, FileMode.CreateNew, 89), @"(3.14:15:09,3,3.14,Pi,3.14,True,15,(CreateNew\,89))"),

            ((TimeSpan.Zero, 0, 0f, "", 0m), @"(00:00:00,0,0,,0)"),
            ((TimeSpan.Zero, 0, 0f, (string)null, 0m), @"(00:00:00,0,0,∅,0)"),

            ((TimeSpan.Zero, 0, 0f, "", 0m), @""),
            ((TimeSpan.Zero, 0, 0f, (string)null, 0m), (string)null),

            ((TimeSpan.Zero, 0, 0f, "", 0m, (byte)0), @""),
            ((TimeSpan.Zero, 0, 0f, (string)null, 0m, (byte)0), (string)null),

            ((TimeSpan.Zero, 0, 0f, "", 0m, (byte)0, false), @""),
            ((TimeSpan.Zero, 0, 0f, (string)null, 0m, (byte)0, false), (string)null),

            ((@"∅\,", @",∅\", @"∅,\", @"\∅,", @",\∅"), @"(\∅\\\,,\,\∅\\,\∅\,\\,\\\∅\,,\,\\\∅)"),

            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @" (3.14:15:09,3,3.14,Pi,3.14)"),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @" (3.14:15:09,3,3.14,Pi,3.14)  "),
            ((new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @"(3.14:15:09,3,3.14,Pi,3.14)  "),

            (("A","B"), @"(A,B)"),
            (("A","B","C"), @"(A,B,C)"),
            (("A","B","C","D"), @"(A,B,C,D)"),
            (("A","B","C","D","E"), @"(A,B,C,D,E)"),
            //rare case then brackets are actually used inside tuple values 
            (("(A","B)"), @"((A,B))"),
            (("(A","B","C)"), @"((A,B,C))"),
            (("(A","B","(C)","D)"), @"((A,B,(C),D))"),
            (("(A","(B","C)","D","E)"), @"((A,(B,C),D,E))"),
            //Tuple with tuple fields
            ((("N", "e"), ("s", "t") , "ed"), @"((N\,e),(s\,t),ed)"),
        };

        [TestCaseSource(nameof(Correct_Tuple_Data))]
        public void TupleTransformer_CompoundTest((ValueType instance, string input) data)
        {
            var tester = Method.OfExpression<Action<int, string>>(
                (t, i) => TupleTransformer_CompoundTestHelper(t, i)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(data.instance.GetType());

            tester.Invoke(null, new object[] { data.instance, data.input });
        }

        private static void TupleTransformer_CompoundTestHelper<TTuple>(TTuple tuple, string input) where TTuple : struct
        {
            var sut = GetSut<TTuple>();

            string textActual = sut.Format(tuple);


            var parsed1 = sut.Parse(input);
            Assert.That(parsed1, Is.EqualTo(tuple));


            string text = sut.Format(parsed1);


            var parsed2 = sut.Parse(text);
            Assert.That(parsed2, Is.EqualTo(tuple));


            var parsed3 = sut.Parse(textActual);
            Assert.Multiple(() =>
            {
                Assert.That(parsed3, Is.EqualTo(tuple));
                Assert.That(parsed1, Is.EqualTo(parsed2));
                Assert.That(parsed1, Is.EqualTo(parsed3));
            });
        }

        private const string NO_PARENTHESES_ERROR =
            "Tuple representation has to start with '(' and end with ')' optionally lead in the beginning or trailed in the end by whitespace";
        private static IEnumerable<(Type tupleType, string input, Type expectedException, string expectedErrorMessagePart)>
            Bad_Tuple_Data() => new[]
        {
            (typeof(KeyValuePair<float?, string>), @"abc=ABC", typeof(FormatException),
#if NET7_0_OR_GREATER
                @"The input string 'abc' was not in a correct format."
#else
                @"Input string was not in a correct format"
#endif
            ),
            (typeof(KeyValuePair<float?, string>), @" ", typeof(FormatException), 
#if NET7_0_OR_GREATER
                @"The input string ' ' was not in a correct format."
#else
                @"Input string was not in a correct format"
#endif
            ),
            (typeof(KeyValuePair<float?, string>), @" =", typeof(FormatException), 
#if NET7_0_OR_GREATER
                @"The input string ' ' was not in a correct format."
#else
                @"Input string was not in a correct format"
#endif
            ),

            (typeof(KeyValuePair<float?, string>), @"15=ABC=TooMuch", typeof(ArgumentException), @"Key-value pair of arity=2 separated by '=' cannot have more than 2 elements: 'TooMuch'"),
            (typeof(KeyValuePair<float?, string>), @"15", typeof(ArgumentException), @"2nd element was not found after '15'"),
            (typeof(KeyValuePair<float?, string>), @"∅", typeof(ArgumentException), @"2nd element was not found after '∅'"),
            (typeof(KeyValuePair<string, float?>), @" ", typeof(ArgumentException), @"2nd element was not found after ' '"),


            (typeof((TimeSpan, int, float, string, decimal)), @" ",typeof(ArgumentException), NO_PARENTHESES_ERROR),
#if NETCOREAPP3_1_OR_GREATER
            (typeof((TimeSpan, int, float, string, decimal)), @"( )",typeof(FormatException), @"String ' ' was not recognized as a valid TimeSpan."),                        
#else
            (typeof((TimeSpan, int, float, string, decimal)), @"( )",typeof(FormatException), @"String was not recognized as a valid TimeSpan"),
#endif

            (typeof((TimeSpan, int, float, string, decimal, bool, byte, FileMode, int)), @"3.14:15:09,3,3.14,Pi,3.14,True,15,(CreateNew\,89)", typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal, bool, string)), @"3.14:15:16,3,3.14,Pi,3.14,true,ABC",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal, bool)), @"3.14:15:16,3,3.14,Pi,3.14,true",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:16,3,3.14,Pi,3.14",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:99,3,3.14,Pi,3.14",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:09,3,3.14,Pi",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:09,3,3.14",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:09,3",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:09",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof((TimeSpan, int, float, string, decimal)), @"3.14:15:09,3,3.14,Pi,3.14,MorePie",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            (typeof(ValueTuple<TimeSpan>), @"3.14:15:09",typeof(ArgumentException), NO_PARENTHESES_ERROR),
            
#if NETCOREAPP3_1_OR_GREATER
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:99,3,3.14,Pi,3.14)",typeof(OverflowException), @"The TimeSpan string '3.14:15:99' could not be parsed because at least one of the numeric components is out of range or contains too many digits."),
            (typeof((TimeSpan, int, float, string, decimal)), @" (3.14:15:99,3,3.14,Pi,3.14) ",typeof(OverflowException), @"The TimeSpan string '3.14:15:99' could not be parsed because at least one of the numeric components is out of range or contains too many digits."),                  
#else
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:99,3,3.14,Pi,3.14)",typeof(OverflowException), @"The TimeSpan could not be parsed because at least one of the numeric components is out of range or contains too many digits"),
            (typeof((TimeSpan, int, float, string, decimal)), @" (3.14:15:99,3,3.14,Pi,3.14) ",typeof(OverflowException), @"The TimeSpan could not be parsed because at least one of the numeric components is out of range or contains too many digits"),
#endif
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14,Pi)",typeof(ArgumentException), @"5th element was not found after"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14)",typeof(ArgumentException), @"4th element was not found after"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3)",typeof(ArgumentException), @"3rd element was not found after"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09)",typeof(ArgumentException), @"2nd element was not found after"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14,Pi,3.14,MorePie)",typeof(ArgumentException), @"Tuple of arity=5 separated by ',' cannot have more than 5 elements: 'MorePie'"),
            
#if !NETCOREAPP3_1_OR_GREATER //core 3.1 removed overflow errors for float to be consistent with IEEE
            (typeof(KeyValuePair<float?, string>), @"9999999999999999999999999999999999999999999999999999=OK", typeof(OverflowException), @"Value was either too large or too small for a Single"),

#endif

        };

        [TestCaseSource(nameof(Bad_Tuple_Data))]
        public void TupleTransformer_NegativeTest((Type tupleType, string input, Type expectedException, string expectedErrorMessagePart) data)
        {
            var negative = MakeDelegate<Action<string, Type, string>>(
                (p1, p2, p3) => TupleTransformer_NegativeTest_Helper<int>(p1, p2, p3), data.tupleType
            );

            negative(data.input, data.expectedException, data.expectedErrorMessagePart);
        }

        private static void TupleTransformer_NegativeTest_Helper<TTuple>(string input, Type expectedException, string expectedErrorMessagePart)
        {
            var sut = GetSut<TTuple>();

            bool passed = false;
            object parsed = null;
            try
            {
                parsed = sut.Parse(input);
                passed = true;
            }
            catch (Exception actual)
            {
                AssertException(actual, expectedException, expectedErrorMessagePart);
            }
            if (passed)
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }
    }
}
