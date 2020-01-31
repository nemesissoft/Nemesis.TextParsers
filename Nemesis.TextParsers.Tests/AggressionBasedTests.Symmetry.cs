using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dss = System.Collections.Generic.Dictionary<string, string>;
using FacInt = Nemesis.TextParsers.Tests.AggressionBasedFactory<int>;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(IAggressionBased<>))]
    internal partial class AggressionBasedTests
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        

        private static IEnumerable<(Type elemenType, string input, IEnumerable expectedOutput)> ValidValuesFor_FromText_Complex22() => new[]
        {
            (typeof(int), @"1#2#3#4#5#6#7#8#9", (IEnumerable)new[]{1,2,3,4,5,6,7,8,9}),

            (typeof(int), @"1#1#1#4#4#4#7#7#7", new[]{1,4,7} ),
            (typeof(int), @"1#4#7", new[]{1,4,7} ),

            (typeof(int), @"1#1#1#1#1#1#1#1#1", new[]{1}),
            (typeof(int), @"1", new[]{1}),

            (typeof(List<int>), @"1|2|3#4|5|6#7|8|9", new[]{new List<int>{1,2,3},new List<int>{4,5,6},new List<int>{7,8,9}}),
            (typeof(List<int>), @"1|2|3#1|2|3#1|2|3", new[]{new List<int>{1,2,3}}),

            (typeof(IAggressionBased<int>), @"1\#2\#4#40\#50\#70#7\#8\#9", new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,4),
                FacInt.FromPassiveNormalAggressive(40,50,70),
                FacInt.FromPassiveNormalAggressive(7,8,9),
            }),
            (typeof(IAggressionBased<int>), @"1\#2\#40#1\#2\#40#1\#2\#40",new[]
            {
                FacInt.FromPassiveNormalAggressive(1,2,40)
            }),

            (typeof(Dss), @"Key=Text", new[] { new Dss { { "Key", @"Text" } } }),
            //TODO: fix or remove
            //(typeof(Dss), @"Key=Text\;Text", new[] { new Dss { { "Key", @"Text;Text" } } }),
        };

        

        /*private static void AggressionBasedFactory_FromText_ShouldParseComplexCasesHelper<TElement>(string input, IEnumerable expectedOutput)
        {
            var actual = AggressionBasedFactory<TElement>.FromText(input);

            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Is.AssignableTo<IAggressionValuesProvider<TElement>>());

            var values = ((IAggressionValuesProvider<TElement>)actual).Values;
            Assert.That(values, Is.EquivalentTo(expectedOutput));
        }*/

        

        private static IEnumerable<(string inputText, int[] expectedValues, Type contractType, Type expectedType)> TextTransformer_Symmetry()
            => new[]
            {
                ("", new []{0}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("", new []{0}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("123",new []{123}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("123",new []{123}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("123#456#789", new []{123,456,789}, typeof(IAggressionBased<int>), typeof(AggressionBased3<int>)),
                ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(AggressionBased3<int>)),

                ("123#123#123", new []{123}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("123#123#123", new []{123}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),


                ("1#1#1#1#1#1#1#1#1", new []{1}, typeof(IAggressionBased<int>), typeof(AggressionBased1<int>)),
                ("1#1#1#1#1#1#1#1#1", new []{1}, typeof(AggressionBased1<int>), typeof(AggressionBased1<int>)),

                ("1#1#1#4#4#4#7#7#7", new []{1,4,7}, typeof(IAggressionBased<int>), typeof(AggressionBased3<int>)),
                ("1#1#1#4#4#4#7#7#7", new []{1,4,7}, typeof(AggressionBased3<int>), typeof(AggressionBased3<int>)),


                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(IAggressionBased<int>), typeof(AggressionBased9<int>)),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(AggressionBased9<int>)),
            };

        [TestCaseSource(nameof(TextTransformer_Symmetry))]
        public void TextTransformer_ShouldParseAndFormatAppropriateTypes((string inputText, int[] expectedValues, Type contractType, Type expectedType) data)
        {
            var transformer = TextTransformer.Default.GetTransformer(data.contractType);
            var parsed = transformer.ParseObject(data.inputText);

            Assert.That(parsed, Is.TypeOf(data.expectedType));
            Assert.That(((IAggressionValuesProvider<int>)parsed).Values, Is.EquivalentTo(data.expectedValues));


            string formatted = transformer.FormatObject(parsed);
            var parsed2 = transformer.ParseObject(formatted);

            Assert.That(
                ((IAggressionValuesProvider<int>)parsed).Values,
                Is.EquivalentTo(
                    ((IAggressionValuesProvider<int>)parsed2).Values
                    ));
        }


        private static IEnumerable<(string expectedOutput, object inputValues, Type expectedAggBasedType, Type expectedElementType)> TextTransformer_Format()
         => new[]
         {
                ("0", (object)new []{0}, typeof(AggressionBased1<int>), typeof(int)),

                ("123",new []{123}, typeof(AggressionBased1<int>), typeof(int)),

                ("123#456#789", new []{123,456,789}, typeof(AggressionBased3<int>), typeof(int)),
                ("123",         new []{123,123,123}, typeof(AggressionBased1<int>), typeof(int)),

                ("1",                 new []{1,1,1,1,1,1,1,1,1}, typeof(AggressionBased1<int>), typeof(int)),
                ("1#4#7",             new []{1,1,1,4,4,4,7,7,7}, typeof(AggressionBased3<int>), typeof(int)),
                ("1#2#3#4#5#6#7#8#9", new []{1,2,3,4,5,6,7,8,9}, typeof(AggressionBased9<int>), typeof(int)),

                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
                //equal values:
                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2", (object)new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", } }, typeof(AggressionBased1<Dss>), typeof(Dss) ),
                //distinct values
                (@"Key\\;1=T\\=ext\\;1;Key\\;2=Text\\;2#Key\\;3=T\\=ext\\;3;Key\\;4=Text\\;4#Key\\;5=T\\=ext\\;5;Key\\;6=Text\\;6", new []{ new Dss{["Key;1"]= "T=ext;1", ["Key;2"]= "Text;2", }, new Dss{["Key;3"]= "T=ext;3", ["Key;4"]= "Text;4", }, new Dss{["Key;5"]= "T=ext;5", ["Key;6"]= "Text;6", } }, typeof(AggressionBased3<Dss>), typeof(Dss) ),

                // GH #1 merged with cases from TextTransformer_Complex_ShouldFormat 
                (@"Key=Value", new []{new Dss{ { "Key", "Value" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
                //TODO: fix or remove
                //(@"Key=Va\;lue", new []{new Dss { { "Key", "Va;lue" } } }, typeof(AggressionBased1<Dss>), typeof(Dss)),
         };

        [TestCaseSource(nameof(TextTransformer_Format))]
        public void TextTransformer_ShouldFormat((string expectedOutput, object inputValues, Type expectedAggBasedType, Type expectedElementType) data)
        {
            var tester = (
                GetType().GetMethod(nameof(TextTransformer_ShouldFormatHelper), ALL_FLAGS) ??
                throw new MissingMethodException(GetType().FullName, nameof(TextTransformer_ShouldFormatHelper))
            ).MakeGenericMethod(data.expectedElementType);

            tester.Invoke(null, new[] { data.expectedOutput, data.inputValues, data.expectedAggBasedType });
        }

        private static void TextTransformer_ShouldFormatHelper<TElement>(string expectedOutput, IEnumerable<TElement> inputValues, Type expectedAggBasedType)
        {
            var aggBased = AggressionBasedFactory<TElement>.FromValues(inputValues);
            Assert.That(aggBased, Is.TypeOf(expectedAggBasedType));


            var transformer = TextTransformer.Default.GetTransformer<IAggressionBased<TElement>>();
            string formatted = transformer.Format(aggBased);


            Assert.That(formatted, Is.EqualTo(expectedOutput));
        }
    }
}
