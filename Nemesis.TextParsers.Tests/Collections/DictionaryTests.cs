// ReSharper disable RedundantUsingDirective
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Nemesis.TextParsers.Settings;
using NUnit.Framework;
using Dss = System.Collections.Generic.SortedDictionary<string, string>;
using Nemesis.TextParsers.Utils;
using static Nemesis.TextParsers.Tests.TestHelper;
// ReSharper restore RedundantUsingDirective

namespace Nemesis.TextParsers.Tests.Collections
{
    [TestFixture]
    public class DictionaryTests
    {
        private readonly ITransformerStore _transformerStore = TextTransformer.Default;

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


            (@"key\∅= \∅ ;key\∅2= \\\∅ ;key1=\∅;key2=∅", new Dss{["key1"]="∅",["key2"]=null,["key∅"]=" ∅ ",["key∅2"]=@" \∅ ", }),
            (@"key1=\=\;", new Dss{["key1"]="=;"} ),
            (@"key\;\=1=\=\;", new Dss{["key;=1"]="=;"} ),
            (@"\\\;\\\==\\\;\\\=;k\\\=ey2=\;\\\=A\\BC;key\;\=1=\=\;", new Dss{[@"\;\="]=@"\;\=", [@"k\=ey2"]=@";\=A\BC", ["key;=1"]="=;"}),

            (@"Key=Text\;Text", new Dss{{ "Key", @"Text;Text" }})
        };

        [TestCaseSource(nameof(ValidDictData))]
        public void Dict_Format_SymmetryTests((string expectedOutput, Dss inputDict) data)
        {
            var sut = _transformerStore.GetTransformer<Dss>();

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
            var sut = _transformerStore.GetTransformer<Dictionary<int, TimeSpan>>();

            var dict = Enumerable.Range(1, 5).ToDictionary(i => i, i => new TimeSpan(i, i + 1, i + 2, i + 3));

            var text = sut.Format(dict);
            var dict2 = sut.Parse(text);

            Assert.That(text, Is.EqualTo("1=1.02:03:04;2=2.03:04:05;3=3.04:05:06;4=4.05:06:07;5=5.06:07:08"));
            Assert.That(dict2, Is.EqualTo(dict));
        }

        [Test]
        public void Dict_CompoundTestsAggBasedAndList()
        {
            var sut = _transformerStore.GetTransformer<Dictionary<IAggressionBased<float>, List<TimeSpan>>>();

            var dict = Enumerable.Range(0, 4).ToDictionary(
                i => AggressionBasedFactory<float>.FromPassiveNormalAggressive(10 * i, 10 * i + 1, 10 * i + 2),
                i => new List<TimeSpan> { new TimeSpan(i, i + 1, i + 2, i + 3), new TimeSpan(10 * i, 10 * i + 1, 10 * i + 2, 10 * i + 3) });

            var text = sut.Format(dict);
            Assert.That(text, Is.EqualTo("0#1#2=01:02:03|01:02:03;10#11#12=1.02:03:04|10.11:12:13;20#21#22=2.03:04:05|20.21:22:23;30#31#32=3.04:05:06|31.07:32:33"));

            //dict.Remove(dict.First().Key);

            var deser = sut.Parse(text);
            Assert.That(deser, Is.EqualTo(dict));
        }
    }
}
