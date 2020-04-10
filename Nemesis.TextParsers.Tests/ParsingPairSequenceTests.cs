using System;
using System.Collections.Generic;
using System.Linq;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    internal class ParsingPairSequenceTests
    {
        private static IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string text)
        {
            var keyTransformer = Sut.GetTransformer<TKey>();
            var valTransformer = Sut.GetTransformer<TValue>();

            var tokens = text.AsSpan().Tokenize(';', '\\', true);
            var parsed = new ParsingPairSequence(tokens, '\\', '∅', ';', '=');

            var result = new Dictionary<TKey, TValue>();

            foreach (var (key, val) in parsed)
                result.Add(
                    (key.ParseWith(keyTransformer)) ?? throw new ArgumentException("Key cannot be null"),
                     val.ParseWith(valTransformer)
                );

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
                Assert.That(result, Is.EqualTo(data.expectedDict));

            /*if (data.expectedDict == null)
                Console.WriteLine(@"NULL dictionary");
            else if (!data.expectedDict.Any())
                Console.WriteLine(@"Empty dictionary");
            else
                foreach (var kvp in data.expectedDict)
                    Console.WriteLine($@"[{kvp.Key}] = '{kvp.Value ?? "<null>"}'");*/
        }

        [TestCase(01, @"key1", "'key1' has no matching value")]//no value
        [TestCase(02, @";", "Key=Value part was not found")]//no values
        [TestCase(03, @"key1 ; key2", "'key1 ' has no matching value")]
        [TestCase(04, @"key1=value1;", "Key=Value part was not found")]//non terminated sequence
        [TestCase(05, @"ke=y1=value1", "ke=y1 pair cannot have more than 2 elements: 'value1'")]//too many separators
        [TestCase(06, @"key1=value1;key1=value2", "An item with the same key has already been added.")] //An item with the same key has already been added.
        [TestCase(07, @"∅=value", "Key cannot be null")]//Key element in dictionary cannot be null
        [TestCase(08, @"∅", "'<DEFAULT>' has no matching value")]//null dictionary can only be mapped as null string 
        public void Dict_Parse_NegativeTest(int _, string input, string expectedMessage) =>
            Assert.That(() => ParseDictionary<string, string>(input), 
                Throws.ArgumentException.And
                    .Message.Contains(expectedMessage)
                );

        [Test]
        public void Dict_CompoundTests()
        {
            Dictionary<int, TimeSpan> expected = Enumerable.Range(1, 5)
                .ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var actual = ParseDictionary<int, TimeSpan>("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08");

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
