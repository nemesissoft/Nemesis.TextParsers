using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture]
    internal class ParsedSequenceTests
    {
        private static IReadOnlyList<T> ParseCollection<T>(string text)
        {
            if (text == null) return null;

            var tokens = text.AsSpan().Tokenize('|', '\\', false);
            var parsed = tokens.Parse<T>('\\', '∅', '|');

            var result = new List<T>();

            foreach (var part in parsed)
                result.Add(part);

            return result;
        }

        #region List

        private static IEnumerable<(string, IEnumerable<string>)> ValidListData() => new (string, IEnumerable<string>)[]
        {
            ("", new[]{""}),
            (@"AAA|BBB|CCC", new []{"AAA","BBB","CCC"}),
            (@"|BBB||CCC", new []{"","BBB","","CCC"}),
            (@"|BBB|\|CCC", new []{"","BBB","|CCC"}),
            (@"|B\\BB|\|CCC", new []{"",@"B\BB","|CCC"}),
            (@"|BBB|", new []{"","BBB",""}),
            (@"|BBB\|", new []{"","BBB|"}),
            (@"\|BBB|", new []{"|BBB",""}),
            (@"|∅||∅", new []{"",null,"",null}),
            (@"∅", new []{(string)null}),
            (@"B|∅|A|∅", new []{"B",null,"A",null}),
            (@"|||", new []{"","","",""}),
            (@"|\||", new []{"","|",""}),
            (@"|\\\\\||", new []{"",@"\\|",""}),
            (@"|\\\|\\\||", new []{"",@"\|\|",""}),
            (@"\\\|\\\||", new []{@"\|\|",""}),
            (@"\\|ABC|\\", new []{@"\","ABC", @"\"}),
            (@"\\|ABC|\|", new []{@"\","ABC", @"|"}),
            (@"\||ABC|\\", new []{@"|","ABC", @"\"}),

            (@"\\\\|ABC|\\\\", new []{@"\\","ABC", @"\\"}),
            (@"\\\\|ABC|\|", new []{@"\\","ABC", @"|"}),
            (@"\||ABC|\\\\", new []{@"|","ABC", @"\\"}),

            (@"\\1\\|ABC|\\2\\", new []{@"\1\","ABC", @"\2\"}),
            (@"\\3\\|ABC|\|", new []{@"\3\","ABC", @"|"}),
            (@"\||ABC|\\4\\", new []{@"|","ABC", @"\4\"}),

            (@"\|", new []{@"|"}),
            (@"|", new []{@"", ""}),
            (@" |", new []{@" ", ""}),
            (@"\\", new []{@"\"}),
            (@"\∅", new []{@"∅"}),
            (@"∅", new string[]{null}),
            (@" ∅", new[]{" ∅"}),
            (@" ∅ ", new[]{" ∅ "}),
            (@" \∅ ", new[]{" ∅ "}),
            (@"∅ ", new[]{@"∅ "}),
            (@"\∅ ", new[]{@"∅ "}),
            (@"A|∅|B", new[]{"A",null,"B"}),
            (@"A| ∅ |B", new[]{"A", " ∅ ", "B"}),
            (@"∅|B", new[]{null,"B"}),
            (@"∅ |B", new[]{ "∅ ", "B"}),
            (@"\∅ |B", new[]{ "∅ ", "B"}),
            (@"A| ∅ |B", new[]{"A", " ∅ ", "B"}),
            (@"A| \∅ |B", new[]{"A", " ∅ ", "B"}),

            (@" |\\", new []{@" ", @"\"}),
            (@" |\\\|", new []{@" ", @"\|"}),
            (@" |\\\||A", new []{@" ", @"\|", "A"}),
            (@" |\|", new []{@" ", @"|"}),
        };

        [TestCaseSource(nameof(ValidListData))]
        public void List_Parse_Test((string input, IEnumerable<string> expectedList) data)
        {
            IEnumerable<string> result = ParseCollection<string>(data.input);

            if (data.expectedList == null)
                Assert.That(result, Is.Null);
            else
                Assert.That(result, Is.EquivalentTo(data.expectedList));

            if (data.expectedList == null)
                Console.WriteLine(@"NULL list");
            else if (!data.expectedList.Any())
                Console.WriteLine(@"Empty list");
            else
                foreach (string elem in data.expectedList)
                    Console.WriteLine($@"'{elem ?? "<null>"}'");
        }

        //unfinished escaping sequence
        [TestCase(@"\")]
        [TestCase(@"\\\")]
        [TestCase(@"\\\\\")]
        [TestCase(@"\|\|\|\|\")]
        [TestCase(@"AAA|BBB\")]
        //illegal escaping sequence
        [TestCase(@"AAA|BBB\n")]
        [TestCase(@"\aAAA|BBB\n")]
        [TestCase(@"AAA|BB\\\B")]
        [TestCase(@"\AAA|BB\\\B")]
        [TestCase(@"\r")]
        public void List_Parse_NegativeTest(string input)
        {
            try
            {
                var result = ParseCollection<string>(input).ToList();
                //Console.WriteLine(string.Join(Environment.NewLine, result));
                Assert.Fail($"'{input}' should not be parseable to:{Environment.NewLine} {string.Join(Environment.NewLine, result.Select(r => $"'{r}'"))}");
            }
            catch (ArgumentException ae) when (ae.TargetSite?.Name == "ParseElement")
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

        [TestCaseSource(typeof(SpanCollectionSerializerTests), nameof(SpanCollectionSerializerTests.ListCompoundData))]
        public void List_Parse_CompoundTests((Type elementType, IEnumerable expectedList, string input) data)
        {
            static void CheckEquivalency(IEnumerable left, IEnumerable right)
            {
                if (left is null)
                    Assert.That(right, Is.Null);
                else
                    Assert.That(left, Is.EquivalentTo(right));
            }

            const BindingFlags ALL_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

            var parseMethod = (typeof(ParsedSequenceTests).GetMethods(ALL_FLAGS).SingleOrDefault(mi =>
                    mi.Name == nameof(ParseCollection) && mi.IsGenericMethod)
                    ?? throw new MissingMethodException("Method ParseCollection does not exist"))
                .MakeGenericMethod(data.elementType);

            var deser = (IEnumerable)parseMethod.Invoke(null, new object[] { data.input });
            CheckEquivalency(deser, data.expectedList);

            if (data.expectedList == null)
                Console.WriteLine(@"NULL list");
            else if (!data.expectedList.Cast<object>().Any())
                Console.WriteLine(@"Empty list");
            else
                foreach (object elem in data.expectedList)
                    Console.WriteLine(FormattableString.Invariant($"{elem}"));
        }

        #endregion
    }
}
