using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System.Reflection;


namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    class TupleTransformerTests
    {
        internal static IEnumerable<(Type, object, string)> Correct_KeyValuePair_Data() => new (Type, object, string)[]
        {
            (typeof(KeyValuePair<TimeSpan, int>), new KeyValuePair<TimeSpan, int>(new TimeSpan(1,2,3,4), 0), @"1.02:03:04,∅"),
            (typeof(KeyValuePair<TimeSpan, int>), new KeyValuePair<TimeSpan, int>(TimeSpan.Zero, 15), @"∅,15"),
            (typeof(KeyValuePair<TimeSpan?, int>), new KeyValuePair<TimeSpan?, int>(null, 15), @"∅,15"),


            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("PI", 3.14f), @"PI,3.14"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("PI", null), @"PI,∅"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", 3.14f), @",3.14"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>("", null), @",∅"),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, 3.14f), @"∅,3.14"),

            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), @""),
            (typeof(KeyValuePair<string, float?>), new KeyValuePair<string, float?>(null, null), @"∅,∅"),
            (typeof(KeyValuePair<string, float>),  new KeyValuePair<string, float>(null, 0), @"∅,∅"),

            
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, "PI"), @"3.14,PI"),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(null, "PI"), @"∅,PI"),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, ""), @"3.14,"),
            (typeof(KeyValuePair<float?, string>), new KeyValuePair<float?, string>(3.14f, null), @"3.14,∅"),

            //escaping tests
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(null, null), @"∅,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>("∅", null), @"\∅,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(" ∅ ", null), @" ∅ ,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@" ∅ ", null), @" \∅ ,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@",", null), @"\,,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@" , ", null), @" \, ,∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅,\∅,\", @"\,∅"), @"\∅\,\\\∅\,\\,\\\,\∅"),
            (typeof(KeyValuePair<string, string>), new KeyValuePair<string, string>(@"∅,\∅,\ ", @" \,∅"), @"\∅\,\\\∅\,\\ , \\\,\∅"),
        };

        [TestCaseSource(nameof(Correct_KeyValuePair_Data))]
        public void KeyValuePairTransformer_CompoundTest((Type kvpType, object pair, string input) data)
        {
            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            var creator = new KeyValuePairTransformerCreator();
            Assert.That(creator.CanHandle(data.kvpType));

            var createTransformer = (creator.GetType().GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                                    mi.Name == nameof(ICanCreateTransformer.CreateTransformer) && mi.IsGenericMethod)
                                ?? throw new MissingMethodException("Method CreateTransformer does not exist"))
                    .MakeGenericMethod(data.kvpType);

            var transformer = createTransformer.Invoke(creator, null);

            var formatMethod = transformer.GetType().GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                    mi.Name == nameof(ITransformer<object>.Format))
                    ?? throw new MissingMethodException("Method Format does not exist");

            var parseMethod = typeof(IParser<>).MakeGenericType(data.kvpType)
                                  .GetMethods(ALL_FLAGS)
                                  .SingleOrDefault(mi => mi.Name == nameof(IParser<object>.ParseText))
                              ?? throw new MissingMethodException("Method ParseText does not exist");


            string textExpected = (string)formatMethod.Invoke(transformer, new[] { data.pair });
            Console.WriteLine(textExpected);

            var parsed = parseMethod.Invoke(transformer, new object[] { data.input });
            Console.WriteLine(data.input);
            Assert.That(parsed, Is.EqualTo(data.pair));


            string text = (string)formatMethod.Invoke(transformer, new[] { parsed });
            Console.WriteLine(text);


            var parsed2 = parseMethod.Invoke(transformer, new object[] { text });
            Assert.That(parsed2, Is.EqualTo(data.pair));


            var parsed3 = parseMethod.Invoke(transformer, new object[] { textExpected });
            Assert.That(parsed3, Is.EqualTo(data.pair));


            Assert.That(parsed, Is.EqualTo(parsed2));
            Assert.That(parsed, Is.EqualTo(parsed3));
        }


        internal static IEnumerable<(Type, string, Type)> Bad_KeyValuePair_Data() => new[]
       {
            (typeof(KeyValuePair<float?, string>), @"abc,ABC", typeof(FormatException)),
            (typeof(KeyValuePair<float?, string>), @" ", typeof(FormatException)),
            
            (typeof(KeyValuePair<float?, string>), @"15,ABC,TooMuch", typeof(ArgumentException)),
            (typeof(KeyValuePair<float?, string>), @"15", typeof(ArgumentException)),
            (typeof(KeyValuePair<float?, string>), @"∅", typeof(ArgumentException)),
            (typeof(KeyValuePair<string, float?>), @" ", typeof(ArgumentException)),

            (typeof(KeyValuePair<float?, string>), @"9999999999999999999999999999999999999999999999999999,OK", typeof(OverflowException)),
        };

        [TestCaseSource(nameof(Bad_KeyValuePair_Data))]
        public void KeyValuePairTransformer_NegativeTest((Type kvpType, string input, Type expectedException) data)
        {
            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            var creator = new KeyValuePairTransformerCreator();
            Assert.That(creator.CanHandle(data.kvpType));

            var createTransformer = (creator.GetType().GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                                         mi.Name == nameof(ICanCreateTransformer.CreateTransformer) && mi.IsGenericMethod)
                                     ?? throw new MissingMethodException("Method CreateTransformer does not exist"))
                .MakeGenericMethod(data.kvpType);

            var transformer = createTransformer.Invoke(creator, null);

            var parseMethod = typeof(IParser<>).MakeGenericType(data.kvpType)
                                  .GetMethods(ALL_FLAGS)
                                  .SingleOrDefault(mi => mi.Name == nameof(IParser<object>.ParseText))
                              ?? throw new MissingMethodException("Method ParseText does not exist");

            bool passed = false;
            object parsed = null;
            try
            {
                parsed = parseMethod.Invoke(transformer, new object[] { data.input });
                passed = true;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException tie)
                    e = tie.InnerException;

                if (data.expectedException == e.GetType())
                {
                    if (e is OverflowException oe)
                        Console.WriteLine("Expected overflow: " + oe.Message);
                    else if (e is FormatException fe)
                        Console.WriteLine("Expected bad format: " + fe.Message);
                    else if (e is InvalidOperationException ioe)
                        Console.WriteLine("Expected invalid operation: " + ioe.Message);
                    else
                        Console.WriteLine("Expected: " + e.Message);
                }
                else
                    Assert.Fail($@"Unexpected external exception: {e}");
            }
            if (passed)
                Assert.Fail($"'{data.input}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }
    }
}
