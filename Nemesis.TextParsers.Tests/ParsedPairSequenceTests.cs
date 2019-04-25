using System;
using System.Collections.Generic;
using System.Linq;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    internal class ParsedPairSequenceTests
    {
        private static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string text)
        {
            var tokens = text.AsSpan().Tokenize(';', '\\', true);
            var parsed = new ParsedPairSequence<TKey, TValue>(tokens, '\\', '∅', ';', '=');

            var result = new Dictionary<TKey, TValue>();

            foreach (var pair in parsed)
                result.Add(pair.Key, pair.Value);

            return result;
        }

        private static IEnumerable<(string, IEnumerable<KeyValuePair<string, string>>)> ValidDictData() => new (string, IEnumerable<KeyValuePair<string, string>>)[]
        {
            (null, new Dss(0)),
            (@"", new Dss(0)),
            (@"=", new Dss{[""]=""}),
            (@"key1=", new Dss{["key1"]=""}),
            (@"key1=∅", new Dss{["key1"]=null}),
            (@"key1= ∅", new Dss{["key1"]=" ∅"}),
            (@"key1= ∅ ", new Dss{["key1"]=" ∅ "}),
            (@"key1=\∅", new Dss{["key1"]="∅"}),
            (@"key1=\∅ ", new Dss{["key1"]="∅ "}),
            (@"key1=\∅;key2=\∅", new Dss{["key1"]="∅",["key2"]="∅"}),
            (@"key1=∅;key2=\∅", new Dss{["key1"]=null,["key2"]="∅"}),
            (@"key1=value1", new Dss{["key1"]="value1"}),
            (@"key\=1=value\=1", new Dss{["key=1"]="value=1"}),
            (@"\=1=\=2", new Dss{["=1"]="=2"}),
            (@"1\==2\=", new Dss{["1="]="2="}),
            (@"key1=value1;key2=value2", new Dss{["key1"]="value1",["key2"]="value2"}),
            (@"\;key1\;=\;value1\;;\;key2\;=\;value2\;", new Dss{[";key1;"]=";value1;",[";key2;"]=";value2;"}),
            (@"\;key1\=\;=\;val\=ue1\;;\;key2\;=\;value\=2\;", new Dss{[";key1=;"]=";val=ue1;",[";key2;"]=";value=2;"}),
        };

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Parse_Test((string input, IEnumerable<KeyValuePair<string, string>> expectedDict) data)
        {
            IDictionary<string, string> result = ParseDictionary<string, string>(data.input);

            if (data.expectedDict == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EquivalentTo(data.expectedDict));


            if (data.expectedDict == null)
                Console.WriteLine(@"NULL dictionary");
            else if (!data.expectedDict.Any())
                Console.WriteLine(@"Empty dictionary");
            else
                foreach (var kvp in data.expectedDict)
                    Console.WriteLine($@"[{kvp.Key}] = '{kvp.Value ?? "<null>"}'");
        }

        [TestCase(@"key1")]//no value
        [TestCase(@";")]//no values
        [TestCase(@"key1 ; key2")]
        [TestCase(@"key1=value1;")]//non terminated sequence
        [TestCase(@"ke=y1=value1")]//too many separators
        [TestCase(@"key1=value1;key1=value2")] //An item with the same key has already been added.
        [TestCase(@"∅=value")]//Key element in dictionary cannot be null
        [TestCase(@"∅")]//null dictionary can only be mapped as null string 
        public void Dict_Parse_NegativeTest(string input)
        {
            try
            {
                var result = ParseDictionary<string, string>(input).ToList();
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(kvp => $"[{kvp.Key}] = '{kvp.Value}'"))}");
            }
            catch (ArgumentException ae) when (ae.TargetSite?.Name == "ParsePair")
            {
                Console.WriteLine($@"Expected exception from implementation: {ae.Message}");
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine($@"Expected external exception: {ae.Message}");
            }
            catch (Exception e)
            {
                Assert.Fail($@"Unexpected external exception: {e.Message}");
            }

        }

        [Test]
        public void Dict_CompoundTests()
        {
            Dictionary<int, TimeSpan> expected = Enumerable.Range(1, 5)
                .ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var actual = ParseDictionary<int, TimeSpan>("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08");

            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
