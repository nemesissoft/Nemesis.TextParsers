// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Nemesis.TextParsers.Tests.TestHelper;
using Dss = System.Collections.Generic.SortedDictionary<string, string>;
// ReSharper restore RedundantUsingDirective

namespace Nemesis.TextParsers.Tests.Collections
{
    [TestFixture]
    public class DictionaryTests
    {
        const string NULL_PLACEHOLDER = "维基百科";
        private static string NormalizeNullMarkers(string text) =>
            text.Replace(@"\∅", NULL_PLACEHOLDER).Replace(@"∅", NULL_PLACEHOLDER).Replace(NULL_PLACEHOLDER, @"\∅");

        private static IEnumerable<(string text, Dss dictionary)> ValidDictData() => new[]
        {
            (null, null),
            (@"", new Dss()),
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

            (@"key1=\∅;key2=∅;key\∅= \∅ ;key\∅2= \\\∅ ", new Dss(StringComparer.Ordinal){["key1"]="∅",["key2"]=null,["key∅"]=" ∅ ",["key∅2"]=@" \∅ ", }),
            (@"key1=\=\;", new Dss{["key1"]="=;"} ),
            (@"key\;\=1=\=\;", new Dss{["key;=1"]="=;"} ),
            (@"\\\;\\\==\\\;\\\=;k\\\=ey2=\;\\\=A\\BC;key\;\=1=\=\;", new Dss{[@"\;\="]=@"\;\=", [@"k\=ey2"]=@";\=A\BC", ["key;=1"]="=;"}),

            (@"Key=Text\;Text", new Dss{{ "Key", @"Text;Text" }})
        };

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Format_SymmetryTests((string expectedOutput, Dss inputDict) data)
        {
            var sut = Sut.GetTransformer<Dss>();

            var result = sut.Format(data.inputDict);

            if (data.expectedOutput == null)
                Assert.That(result, Is.Null);
            else
            {
                string expectedOutput = data.expectedOutput;

                result = NormalizeNullMarkers(result);
                expectedOutput = NormalizeNullMarkers(expectedOutput);
                Assert.That(result, Is.EqualTo(expectedOutput));
            }
            //Console.WriteLine($@"'{result ?? "<null>"}'");
        }

        [Test]
        public void Dict_CompoundTests()
        {
            var sut = Sut.GetTransformer<Dictionary<int, TimeSpan>>();

            var dict = Enumerable.Range(1, 5).ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var text = sut.Format(dict);
            var dict2 = sut.Parse(text);

            Assert.That(text, Is.EqualTo("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08"));
            Assert.That(dict2, Is.EqualTo(dict));
        }

        [Test]
        public void Dict_CompoundTestsArrayAndList()
        {
            var sut = Sut.GetTransformer<Dictionary<double[], List<TimeSpan>>>();

            var dict = Enumerable.Range(0, 4).ToDictionary(
                i => new[] { 10.1 * i, 10.2 * i + 1, 10.3 * i + 2 },
                i => new List<TimeSpan> { new TimeSpan(i, i + 1, i + 2, i + 3), new TimeSpan(10 * i, 10 * i + 1, 10 * i + 2, 10 * i + 3) });

            var text = sut.Format(dict);
            Assert.That(text, Is.EqualTo(@"0|1|2=01:02:03|01:02:03;10.1|11.2|12.3=1.02:03:04|10.11:12:13;20.2|21.4|22.6=2.03:04:05|20.21:22:23;30.299999999999997|31.599999999999998|32.900000000000006=3.04:05:06|31.07:32:33"));

            //dict.Remove(dict.First().Key);

            var deser = sut.Parse(text);
            Assert.That(deser, Is.EquivalentTo(dict));
        }




        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Parse_Test((string input, Dss expectedDict) data)
        {
            var sut = Sut.GetTransformer<Dss>();

            IDictionary<string, string> result = sut.Parse(data.input);

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



        #region Negative tests
        [TestCase(@"key1", typeof(ArgumentException), "'key1' has no matching value")]//no value
        [TestCase(@";", typeof(ArgumentException), "Key=Value part was not found")]//no pairs
        [TestCase(@"key1 ; key2", typeof(ArgumentException), "'key1 ' has no matching value")]//no values
        [TestCase(@"key1=value1;", typeof(ArgumentException), "Key=Value part was not found")]//non terminated sequence
        [TestCase(@"ke=y1=value1", typeof(ArgumentException), "ke=y1 pair cannot have more than 2 elements: 'value1'")]//too many separators
        [TestCase(@"SameKey=value1;SameKey=value2", typeof(ArgumentException), "The key 'SameKey' has already been added")] //An item with the same key has already been added. (DictionaryBehaviour.ThrowOnDuplicate)
        [TestCase(@"∅=value", typeof(ArgumentException), "Key equal to NULL is not supported")]//Key element in dictionary cannot be null
        [TestCase(@"∅", typeof(ArgumentException), "'<DEFAULT>' has no matching value")]//null dictionary can only be mapped as null string  
        #endregion
        public void Dict_Parse_NegativeTest(string input, Type expectedException, string expectedErrorMessagePart)
        {
            var trans = Sut.ThrowOnDuplicateStore.GetTransformer<IDictionary<string, string>>();
            IDictionary<string, string> result = null;
            bool passed = false;
            try
            {
                result = trans.Parse(input);
                passed = true;
            }
            catch (Exception actual) { AssertException(actual, expectedException, expectedErrorMessagePart); }

            if (passed)
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(kvp => $"[{kvp.Key}] = '{kvp.Value}'"))}");
        }
    }
}
