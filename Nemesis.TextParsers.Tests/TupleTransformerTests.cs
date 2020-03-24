using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class TupleTransformerTests
    {
        private static ITransformer<TTuple> GetSut<TTuple>()
        {
            Type tupleType = typeof(TTuple);

            var store = TextTransformer.Default;

            var creator = tupleType.IsGenericType && !tupleType.IsGenericTypeDefinition &&
                          tupleType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                ? new KeyValuePairTransformerCreator(store)
                : (ICanCreateTransformer)new ValueTupleTransformerCreator(store);

            Assert.That(creator.CanHandle(tupleType), $"Type is not supported: {tupleType}");

            var transformer = creator.CreateTransformer<TTuple>();

            return transformer;
        }

        private static IEnumerable<(Type, object, string)> Correct_Tuple_Data() => new (Type, object, string)[]
        {
            (typeof(KeyValuePair<TimeSpan, int>), new KeyValuePair<TimeSpan, int>(new TimeSpan(1,2,3,4), 0), @"1.02:03:04=∅"),
            (typeof(KeyValuePair<TimeSpan, int>), new KeyValuePair<TimeSpan, int>(TimeSpan.Zero, 15), @"∅=15"),
            (typeof(KeyValuePair<TimeSpan?, int>), new KeyValuePair<TimeSpan?, int>(null, 15), @"∅=15"),


            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("PI", 3.14f), @"PI=3.14"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("PI", null), @"PI=∅"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", 3.14f), @"=3.14"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", null), @"=∅"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, 3.14f), @"∅=3.14"),


            (typeof(KeyValuePair<int, float?>), new KeyValuePair<int, float?>(0, null), null),
            (typeof(KeyValuePair<int, float?>), new KeyValuePair<int, float?>(0, null), @""),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), null),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), @""),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), @"∅=∅"),
            (typeof(KeyValuePair<string, float>),  new KeyValuePair<string, float>(null, 0), @"∅=∅"),


            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, "PI"), @"3.14=PI"),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(null, "PI"), @"∅=PI"),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, ""), @"3.14="),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, null), @"3.14=∅"),
        
            //escaping tests
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(null, null), @"∅=∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅", null), @"\∅=∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@" ∅ ", null), @" ∅ =∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@" ∅ ", null), @" \∅ =∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"=", null), @"\==∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@" = ", null), @" \= =∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅=\∅=\", @"\=∅"), @"\∅\=\\\∅\=\\=\\\=\∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅=\∅=\,", @"\=∅"), @"\∅\=\\\∅\=\\,=\\\=\∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅=\∅=\, ", @" \=∅"), @"\∅\=\\\∅\=\\, = \\\=\∅"),


            //Tuples
            (typeof(ValueTuple<TimeSpan>), new ValueTuple<TimeSpan>(new TimeSpan(3,14,15,9)), @"(3.14:15:09)"),
            (typeof((TimeSpan, int)), (new TimeSpan(3,14,15,9), 3), @"(3.14:15:09,3)"),
            (typeof((TimeSpan, int, float)), (new TimeSpan(3,14,15,9), 3, 3.14f), @"(3.14:15:09,3,3.14)"),
            (typeof((TimeSpan, int, float, string)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi"), @"(3.14:15:09,3,3.14,Pi)"),
            (typeof((TimeSpan, int, float, string, decimal)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @"(3.14:15:09,3,3.14,Pi,3.14)"),
            (typeof((TimeSpan, int, float, string, decimal, bool)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true), @"(3.14:15:09,3,3.14,Pi,3.14,true)"),
            (typeof((TimeSpan, int, float, string, decimal, bool, byte)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15), @"(3.14:15:09,3,3.14,Pi,3.14,true,15)"),
            (typeof((TimeSpan, int, float, string, decimal, bool, byte, FileMode)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15, FileMode.CreateNew), @"(3.14:15:09,3,3.14,Pi,3.14,True,15,(CreateNew))"),
            (typeof((TimeSpan, int, float, string, decimal, bool, byte, FileMode, int)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m, true, (byte)15, FileMode.CreateNew, 89), @"(3.14:15:09,3,3.14,Pi,3.14,True,15,(CreateNew\,89))"),

            (typeof((TimeSpan, int, float, string, decimal)), (TimeSpan.Zero, 0, 0f, "", 0m), @"(00:00:00,0,0,,0)"),
            (typeof((TimeSpan, int, float, string, decimal)), (TimeSpan.Zero, 0, 0f, (string)null, 0m), @"(00:00:00,0,0,∅,0)"),
            (typeof((TimeSpan, int, float, string, decimal)), (TimeSpan.Zero, 0, 0f, (string)null, 0m), @""),
            (typeof((TimeSpan, int, float, string, decimal, byte)), (TimeSpan.Zero, 0, 0f, (string)null, 0m, (byte)0), @""),
            (typeof((TimeSpan, int, float, string, decimal, byte, bool)), (TimeSpan.Zero, 0, 0f, (string)null, 0m, (byte)0, false), @""),

            (typeof((string, string, string, string, string)), (@"∅\,", @",∅\", @"∅,\", @"\∅,", @",\∅"), @"(\∅\\\,,\,\∅\\,\∅\,\\,\\\∅\,,\,\\\∅)"),

            (typeof((TimeSpan, int, float, string, decimal)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @" (3.14:15:09,3,3.14,Pi,3.14)"),
            (typeof((TimeSpan, int, float, string, decimal)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @" (3.14:15:09,3,3.14,Pi,3.14)  "),
            (typeof((TimeSpan, int, float, string, decimal)), (new TimeSpan(3,14,15,9), 3, 3.14f, "Pi", 3.14m), @"(3.14:15:09,3,3.14,Pi,3.14)  "),

            (typeof((string, string)), ("A","B"), @"(A,B)"),
            (typeof((string, string, string)), ("A","B","C"), @"(A,B,C)"),
            (typeof((string, string, string, string)), ("A","B","C","D"), @"(A,B,C,D)"),
            (typeof((string, string, string, string, string)), ("A","B","C","D","E"), @"(A,B,C,D,E)"),
            //rare case then brackets are actually used inside tuple values 
            (typeof((string, string)), ("(A","B)"), @"((A,B))"),
            (typeof((string, string, string)), ("(A","B","C)"), @"((A,B,C))"),
            (typeof((string, string, string, string)), ("(A","B","(C)","D)"), @"((A,B,(C),D))"),
            (typeof((string, string, string, string, string)), ("(A","(B","C)","D","E)"), @"((A,(B,C),D,E))"),
            //Tuple with tuple fields
            (typeof( ((string, string), (string, string), string) ), (("N", "e"), ("s", "t") , "ed"), @"((N\,e),(s\,t),ed)"),
        };

        [TestCaseSource(nameof(Correct_Tuple_Data))]
        public void TupleTransformer_CompoundTest((Type tupleType, object tuple, string input) data)
        {
            var tester = Method.OfExpression<Action<int, string>>(
                (t, i) => TupleTransformer_CompoundTestHelper(t, i)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(data.tupleType);

            tester.Invoke(null, new[] { data.tuple, data.input });
        }

        private static void TupleTransformer_CompoundTestHelper<TTuple>(TTuple tuple, string input)
        {
            var transformer = GetSut<TTuple>();

            string textActual = transformer.Format(tuple);


            var parsed1 = transformer.ParseFromText(input);
            Assert.That(parsed1, Is.EqualTo(tuple));


            string text = transformer.Format(parsed1);


            var parsed2 = transformer.ParseFromText(text);
            Assert.That(parsed2, Is.EqualTo(tuple));


            var parsed3 = transformer.ParseFromText(textActual);
            Assert.That(parsed3, Is.EqualTo(tuple));


            Assert.That(parsed1, Is.EqualTo(parsed2));
            Assert.That(parsed1, Is.EqualTo(parsed3));
        }

        private const string NO_PARENTHESES_ERROR =
            "Tuple representation has to start with '(' and end with ')' optionally lead in the beginning or trailed in the end by whitespace";
        internal static IEnumerable<(Type, string, Type, string)> Bad_Tuple_Data() => new[]
        {
            (typeof(KeyValuePair<float?, string>), @"abc=ABC", typeof(FormatException), @"Input string was not in a correct format"),
            (typeof(KeyValuePair<float?, string>), @" ", typeof(FormatException), @"Input string was not in a correct format"),
            (typeof(KeyValuePair<float?, string>), @" =", typeof(FormatException), @"Input string was not in a correct format"),

            (typeof(KeyValuePair<float?, string>), @"15=ABC=TooMuch", typeof(ArgumentException), @"15=ABC pair cannot have more than 2 elements: 'TooMuch'"),
            (typeof(KeyValuePair<float?, string>), @"15", typeof(ArgumentException), @"'15' has no matching value"),
            (typeof(KeyValuePair<float?, string>), @"∅", typeof(ArgumentException), @"'' has no matching value"),
            (typeof(KeyValuePair<string, float?>), @" ", typeof(ArgumentException), @"' ' has no matching value"),
            



            (typeof((TimeSpan, int, float, string, decimal)), @" ",typeof(ArgumentException), NO_PARENTHESES_ERROR),
#if NETCOREAPP3_0 
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
            
#if NETCOREAPP3_0 
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:99,3,3.14,Pi,3.14)",typeof(OverflowException), @"The TimeSpan string '3.14:15:99' could not be parsed because at least one of the numeric components is out of range or contains too many digits."),
            (typeof((TimeSpan, int, float, string, decimal)), @" (3.14:15:99,3,3.14,Pi,3.14) ",typeof(OverflowException), @"The TimeSpan string '3.14:15:99' could not be parsed because at least one of the numeric components is out of range or contains too many digits."),      
#else
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:99,3,3.14,Pi,3.14)",typeof(OverflowException), @"The TimeSpan could not be parsed because at least one of the numeric components is out of range or contains too many digits"),
            (typeof((TimeSpan, int, float, string, decimal)), @" (3.14:15:99,3,3.14,Pi,3.14) ",typeof(OverflowException), @"The TimeSpan could not be parsed because at least one of the numeric components is out of range or contains too many digits"),
#endif
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14,Pi)",typeof(ArgumentException), @"5th tuple element was not found"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14)",typeof(ArgumentException), @"4th tuple element was not found"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3)",typeof(ArgumentException), @"3rd tuple element was not found"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09)",typeof(ArgumentException), @"2nd tuple element was not found"),
            (typeof((TimeSpan, int, float, string, decimal)), @"(3.14:15:09,3,3.14,Pi,3.14,MorePie)",typeof(ArgumentException), @"Tuple of arity=5 separated by ',' cannot have more than 5 elements: 'MorePie'"),
            
#if NETCOREAPP3_0 == false
            (typeof(KeyValuePair<float?, string>), @"9999999999999999999999999999999999999999999999999999=OK", typeof(OverflowException), @"Value was either too large or too small for a Single"),

#endif

        };

        [TestCaseSource(nameof(Bad_Tuple_Data))]
        public void TupleTransformer_NegativeTest((Type tupleType, string input, Type expectedException, string expectedErrorMessagePart) data)
        {
            var tester = Method.OfExpression<Action<string, Type, string>>(
                (p1,p2,p3) => TupleTransformer_NegativeTest_Helper<int>(p1, p2, p3)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(data.tupleType);

            tester.Invoke(null, new object[] { data.input, data.expectedException, data.expectedErrorMessagePart });
        }
        
        private static void TupleTransformer_NegativeTest_Helper<TTuple>(string input, Type expectedException, string expectedErrorMessagePart)
        {
            var transformer = GetSut<TTuple>();

            bool passed = false;
            object parsed = null;
            try
            {
                parsed = transformer.ParseFromText(input);
                passed = true;
            }
            catch (Exception actual)
            {
                TestHelper.AssertException(actual, expectedException, expectedErrorMessagePart);
            }
            if (passed)
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }
    }
}
