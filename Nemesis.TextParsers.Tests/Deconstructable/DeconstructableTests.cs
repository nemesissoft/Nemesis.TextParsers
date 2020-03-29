using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;
using NUnit.Framework;
using TCD = NUnit.Framework.TestCaseData;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;
using S2S = System.Func<Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings, Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings>;
using static Nemesis.TextParsers.Tests.TestHelper;

namespace Nemesis.TextParsers.Tests.Deconstructable
{
    /*TODO
     return to ArrayPool - in finally clause 
    use MakeDelegate<> for all tests
    empty Nullable<> aspect  + tests (person with null marker in name) + tests for null
    update to https://www.nuget.org/packages/Microsoft.SourceLink.GitHub/
        */
    [TestFixture]
    internal class DeconstructableTests
    {
        private static readonly ITransformerStore _transformerStore = TextTransformer.Default;

        internal static IEnumerable<TCD> CorrectData() => new[]
        {
            new TCD(typeof(CarrotAndOnionFactors),
                    new CarrotAndOnionFactors(123.456789M, new[] { 1, 2, 3, (float)Math.Round(Math.PI, 2) }, TimeSpan.Parse("12:34:56")),
                    @"(123.456789;1|2|3|3.14;12:34:56)"),
            new TCD(typeof(Address), new Address("Wrocław", 52200), @"(Wrocław;52200)"),
            new TCD(typeof(Address), new Address("Wrocław)", 52200), @"(Wrocław);52200)"),
            new TCD(typeof(Address), new Address("(Wrocław)", 52200), @"((Wrocław);52200)"),
            new TCD(typeof(Address), new Address("A", 1), @"(A;1)"),
            new TCD(typeof(Address), new Address(null, 1), @"(∅;1)"),
            new TCD(typeof(Address), new Address("", 1), @"(;1)"),
            new TCD(typeof(Person), new Person("Mike", 36, new Address("Wrocław", 52200)), @"(Mike;36;(Wrocław\;52200))"),

            new TCD(typeof(LargeStruct), LargeStruct.Sample, @"(3.14159265;2.718282;-123456789;123456789;-1234;1234;127;-127;-4611686018427387904;9223372036854775807;23.14069263;123456789012345678901234567890;(3.14159265\; 2.71828183))"),

        };

        [TestCaseSource(nameof(CorrectData))]
        public void FormatAndParse(Type type, object instance, string text)
        {
            var tester = Method.OfExpression<Action<object, string, Func<Sett, Sett>>>(
                (i, t, m) => FormatAndParseHelper(i, t, m)
            ).GetGenericMethodDefinition();

            tester = tester.MakeGenericMethod(type);

            tester.Invoke(null, new[] { instance, text, null });
        }

        private static void FormatAndParseHelper<TDeconstructable>(TDeconstructable instance, string text, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualFormatted = sut.Format(instance);
            Assert.That(actualFormatted, Is.EqualTo(text));


            var actualParsed1 = sut.Parse(text);
            var actualParsed2 = sut.Parse(actualFormatted);
            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        private static void ParseHelper<TDeconstructable>(TDeconstructable expected, string input, Func<Sett, Sett> settingsMutator = null)
        {
            var settings = Sett.Default;
            settings = settingsMutator?.Invoke(settings) ?? settings;

            var sut = settings.ToTransformer<TDeconstructable>(_transformerStore);


            var actualParsed1 = sut.Parse(input);
            var formatted1 = sut.Format(actualParsed1);
            var actualParsed2 = sut.Parse(formatted1);


            IsMutuallyEquivalent(actualParsed1, expected);
            IsMutuallyEquivalent(actualParsed2, expected);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }

        [Test]
        public void ParseAndFormat_CustomDeconstruct() => FormatAndParseHelper(
                new DataWithNoDeconstruct("Mike", "Wrocław", 36, 3.14), @"{Mike;Wrocław;36;3.14}",
                s => s.WithBorders('{', '}').WithCustomDeconstruction(DataWithNoDeconstructExt.DeconstructMethod,
                    DataWithNoDeconstructExt.Constructor));

        [Test]
        public void ParseAndFormat_DeconstructContravariance() =>
            FormatAndParseHelper(
                new ExternallyDeconstructable(10.1f, 555.55M), @"{10.1;555.55}",
                s => s.WithBorders('{', '}').WithCustomDeconstruction(ExternallyDeconstructableExt.DeconstructMethod,
                    ExternallyDeconstructableExt.Constructor));


        [Test]
        public void ParseAndFormat_NoBorders()
        {
            //this is just to test normal (bordered) behaviour 
            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    [A;B;C]   ",
                s => s.WithBorders('[', ']'));

            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C",
                s => s.WithoutBorders());

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @" A;B;C ",
                s => s.WithoutBorders());




            ParseHelper(
                new ThreeStrings("", "", ""), @"    [;;]   ",
                s => s.WithBorders('[', ']'));

            ParseHelper(
                new ThreeStrings("", "", ""), @";;",
                s => s.WithoutBorders());
        }

        [Test]
        public void ParseAndFormat_SameBorders()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C/",
                s => s.WithBorders('/', '/'));

            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    /A;B;C/    ",
                s => s.WithBorders('/', '/'));


            ParseHelper(
                new ThreeStrings("A", "B", "C"), @"    AA;B;CA    ",
                s => s.WithBorders('A', 'A'));
        }

        [Test]
        public void ParseAndFormat_MixedBorders_WithOptionalWhitespace()
        {
            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"/A;B;C",
                s => s.WithoutBorders().WithStart('/')
                );

            FormatAndParseHelper(
                new ThreeStrings("A", "B", "C"), @"A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
                );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C"), @" A;B;C/",
                s => s.WithoutBorders().WithEnd('/')
            );

            FormatAndParseHelper(
                new ThreeStrings(" A", "B", "C "), @"/ A;B;C ",
                s => s.WithoutBorders().WithStart('/')
            );
        }

        [Test]
        public void ParseAndFormat_NonStandardControlCharacters()
        {
            //(\∅;∅;\\t123\\ABC\\\∅DEF\∅GHI\;)
            var data = new ThreeStrings("∅␀", null, @"\t123\ABC\∅DEF∅GHI;/:");

            FormatAndParseHelper(data, @"(\∅␀;∅;\\t123\\ABC\\\∅DEF\∅GHI\;/:)");

            FormatAndParseHelper(data, @"(∅\␀;␀;\\t123\\ABC\\∅DEF∅GHI\;/:)",
                s => s.WithNullElementMarker('␀')
                );

            FormatAndParseHelper(data, @"(/∅␀;∅;\t123\ABC\/∅DEF/∅GHI/;//:)",
                s => s.WithEscapingSequenceStart('/')
                );

            FormatAndParseHelper(data, @"(\∅␀:∅:\\t123\\ABC\\\∅DEF\∅GHI;/\:)",
                s => s.WithDelimiter(':')
            );

            FormatAndParseHelper(data, @"(∅/␀:␀:\t123\ABC\∅DEF∅GHI;///:)",
                s => s.WithDelimiter(':')
                      .WithEscapingSequenceStart('/')
                      .WithNullElementMarker('␀')
            );
        }

        [Test]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void ParseAndFormat_Recursive()
        {
            //currently cyclic dependencies are not supported but have a look at the following deconstructable 
            FormatAndParseHelper(new King("Mieszko I", new King("Bolesław I Chrobry", new King("Mieszko II Lambert", new King("Kazimierz I Odnowiciel", new King("Bolesław II Szczodry", (King)null))))),
                @"Mieszko I>Bolesław I Chrobry|Mieszko II Lambert|Kazimierz I Odnowiciel|Bolesław II Szczodry",
                s => s
                    .WithoutBorders()
                    .WithDelimiter('>')
            );


            //it is possible to influence only top type for automatic deconstructable aspect...
            FormatAndParseHelper(
                new Person("Mike", 36, new Address("Wrocław", 52200)),
                @"Mike,36,(Wrocław;52200)",
                s => s.WithoutBorders()
                      .WithDelimiter(',')
                      .WithEscapingSequenceStart('/')
            );

            //...but inner type can have own registered transformers or simply use automatic settings - here we have
            //1. / : \    - set here in test 
            //2. [ , ]    - from custom transformer
            //3. { _ }    - from custom transformer
            //4. ( ; )    - default
            FormatAndParseHelper(
                new Planet("Earth", 7_774_001_533, new Country("Poland", 37_857_130, new Region("Dolny Śląsk", 2_907_200, new City("Wrocław", 639_258)))),
                @"/Earth:7774001533:[Poland,37857130,{Dolny Śląsk_2907200_(Wrocław;639258)}]\",
                s => s.WithBorders('/', '\\')
                    .WithDelimiter(':')
                    .WithEscapingSequenceStart('*')
            );
        }


        internal static IEnumerable<TCD> CustomDeconstructable_Data() => new[]
        {
            new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, new decimal[]{10,20,30}), @"{3.14_False_10|20|30}"),
            new TCD(new DataWithCustomDeconstructableTransformer(666, true, new decimal[]{6,7,8,9 }), null), //overriden by custom transformer 
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, new decimal[0]), ""), //overriden by deconstructable aspect convention
            
            new TCD(new DataWithCustomDeconstructableTransformer(3.14f, false, null), @"{3.14_False_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_False_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, null), @"{␀_␀_␀}"),
            new TCD(new DataWithCustomDeconstructableTransformer(0.0f, false, new decimal[0]), @"{__}"),

        };

        [TestCaseSource(nameof(CustomDeconstructable_Data))]
        public void Custom_ConventionTransformer_BasedOnDeconstructable(DataWithCustomDeconstructableTransformer instance, string text)
        {
            var sut = _transformerStore.GetTransformer<DataWithCustomDeconstructableTransformer>();


            var actualParsed1 = sut.Parse(text);

            var formattedInstance = sut.Format(instance);
            var formattedActualParsed1 = sut.Format(actualParsed1);

            Assert.That(formattedActualParsed1, Is.EqualTo(formattedInstance));

            var actualParsed2 = sut.Parse(formattedInstance);

            IsMutuallyEquivalent(actualParsed1, instance);
            IsMutuallyEquivalent(actualParsed2, instance);
            IsMutuallyEquivalent(actualParsed1, actualParsed2);
        }



        #region Negative tests

        [TestCase(@"(Mike;36;(Wrocław;52200))", typeof(ArgumentException), "These requirements were not met in:'(Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław);52200))", typeof(ArgumentException), "2nd element was not found after 'Wrocław'")]
        [TestCase(@"(Mike;36;(Wrocław\;52200);123))", typeof(ArgumentException), "cannot have more than 3 elements: '123)'")]
        public void Parse_NegativeTest(string wrongInput, Type expectedException, string expectedMessagePart)
        {
            var sut = Sett.Default
                .ToTransformer<Person>(_transformerStore);

            bool passed = false;
            Person parsed = default;
            try
            {
                parsed = sut.Parse(wrongInput);
                passed = true;
            }
            catch (Exception actual)
            {
                AssertException(actual, expectedException, expectedMessagePart);
            }
            if (passed)
                Assert.Fail($"'{wrongInput}' should not be parseable to:{Environment.NewLine}\t{parsed}");
        }

        internal static IEnumerable<(Type type, Type exceptionType, Func<Sett, Sett> settingsBuilder, string expectedMessagePart)> ToTransformer_NegativeTest_Data() => new[]
        {
            (typeof(NoDeconstruct), typeof(NotSupportedException), null, "Deconstructable for NoDeconstruct cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),
            (typeof(DeconstructWithoutMatchingCtor), typeof(NotSupportedException), null, "Deconstructable for DeconstructWithoutMatchingCtor cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),
            (typeof(CtorAndDeCtorOutOfSync), typeof(NotSupportedException), null, "Deconstructable for CtorAndDeCtorOutOfSync cannot be created. Default deconstruction method supports cases with at lease one non-nullary Deconstruct method with matching constructor"),


            (typeof(NotCompatibleCtor), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(NotCompatibleCtor.DeconstructMethod, NotCompatibleCtor.Constructor)), "Instance Deconstruct method has to be compatible with provided constructor and should have same number and type of parameters"),

            (typeof(NotOutParam), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(NotOutParam.DeconstructMethod, NotOutParam.Constructor)), "Instance Deconstruct method must have all out params (IsOut==true)"),

            (typeof(NotSupportedParams), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(NotSupportedParams.DeconstructMethod, NotSupportedParams.Constructor)), @"Instance Deconstruct method must have all parameter types be recognizable by TransformerStore. Not supported types:object, object[], List<object>, string[,]"),



            (typeof(StaticNotCompatibleCtor), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(StaticNotCompatibleCtor.DeconstructMethod, StaticNotCompatibleCtor.Constructor)), @"Static Deconstruct method has to be compatible with provided constructor and should have one additional parameter in the beginning - deconstructable instance"),

            (typeof(StaticNotOutParam), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(StaticNotOutParam.DeconstructMethod, StaticNotOutParam.Constructor)), @"Static Deconstruct method must have all but first params as out params (IsOut==true)"),


            (typeof(StaticNotSupportedParams), typeof(NotSupportedException),
                (S2S)(s=> s.WithCustomDeconstruction(StaticNotSupportedParams.DeconstructMethod, StaticNotSupportedParams.Constructor)), @"Static Deconstruct method must have all parameter types be recognizable by TransformerStore. Not supported types:object, object[], List<object>, string[,]"),
        };

        [TestCaseSource(nameof(ToTransformer_NegativeTest_Data))]
        public void ToTransformer_NegativeTest((Type type, Type exceptionType, S2S settingsBuilder, string expectedMessagePart) data)
        {
            var negativeTest = MakeDelegate<Func<S2S, Exception>>(
                func => ToTransformer_NegativeTest_Helper<int>(func), data.type
            );

            var ex = negativeTest(data.settingsBuilder);

            Assert.That(ex, Is.Not.Null);

            Assert.That(ex, Is.TypeOf(data.exceptionType));

            Assert.That(ex.Message, Is.EqualTo(data.expectedMessagePart)
                .Using(IgnoreNewLinesComparer.EqualityComparer)
            );
        }

        private static Exception ToTransformer_NegativeTest_Helper<TDeconstructable>(S2S settingsBuilder)
        {
            var settings = Sett.Default;
            if (settingsBuilder != null)
                settings = settingsBuilder(settings);

            try
            {
                settings.ToTransformer<TDeconstructable>(_transformerStore);
                return null;
            }
            catch (Exception e) { return e; }
        }

        [UsedImplicitly]
        private readonly struct NoDeconstruct
        {
            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Text { get; }
            public NoDeconstruct(string text) => Text = text;
        }

        [UsedImplicitly]
        private readonly struct DeconstructWithoutMatchingCtor
        {
            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public string Text { get; }
            // ReSharper disable once UnusedMember.Local
            public void Deconstruct(out string text) => text = Text;
        }

        [UsedImplicitly]
        private readonly struct CtorAndDeCtorOutOfSync
        {
            public string Text { get; }
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public int Number { get; }

            public CtorAndDeCtorOutOfSync(string text, int number)
            {
                Text = text;
                Number = number;
            }
            [UsedImplicitly]
            public void Deconstruct(out string text) => text = Text;
        }

        [UsedImplicitly]
        private readonly struct NotCompatibleCtor
        {
            public string Text { get; }

            public NotCompatibleCtor(int text) => Text = text.ToString();

            [UsedImplicitly]
            public void Deconstruct(out string text) => text = Text;

            public static readonly MethodInfo DeconstructMethod =
                typeof(NotCompatibleCtor).GetMethod(nameof(Deconstruct));

            public static readonly ConstructorInfo Constructor = Ctor.Of(() => new NotCompatibleCtor(default));
        }

        [UsedImplicitly]
        private readonly struct NotOutParam
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public string Text { get; }

            public NotOutParam(string text) => Text = text;

            [UsedImplicitly]
            // ReSharper disable RedundantAssignment
            public void Deconstruct(string text) => text = Text;
            // ReSharper restore RedundantAssignment

            public static readonly MethodInfo DeconstructMethod =
                typeof(NotOutParam).GetMethod(nameof(Deconstruct));

            public static readonly ConstructorInfo Constructor = Ctor.Of(() => new NotOutParam(default));
        }

        [UsedImplicitly]
        private readonly struct NotSupportedParams
        {
            public string Text { get; }
            public object Obj { get; }
            public object[] ObjArray { get; }
            public List<object> ObjList { get; }
            public string[,] StringMultiDimArray { get; }

            public NotSupportedParams(string text, object obj, object[] objArray, List<object> objList, string[,] stringMultiDimArray)
            {
                Text = text;
                Obj = obj;
                ObjArray = objArray;
                ObjList = objList;
                StringMultiDimArray = stringMultiDimArray;
            }

            public void Deconstruct(out string text, out object obj, out object[] objArray, out List<object> objList, out string[,] stringMultiDimArray)
            {
                text = Text;
                obj = Obj;
                objArray = ObjArray;
                objList = ObjList;
                stringMultiDimArray = StringMultiDimArray;
            }

            public static readonly MethodInfo DeconstructMethod =
                typeof(NotSupportedParams).GetMethod(nameof(Deconstruct));

            public static readonly ConstructorInfo Constructor = Ctor.Of(() => new NotSupportedParams(default, default, default, default, default));
        }


        [UsedImplicitly]
        internal readonly struct StaticNotCompatibleCtor
        {
            public string Text { get; }

            public StaticNotCompatibleCtor(int text) => Text = text.ToString();

            private static readonly Type _thisType = typeof(StaticNotCompatibleCtor);

            public static readonly MethodInfo DeconstructMethod = typeof(ExternalDeconstruct).GetMethods()
                    .Single(m => m.Name == "Deconstruct" && m.GetParameters().FirstOrDefault()?.ParameterType == _thisType);

            public static readonly ConstructorInfo Constructor = _thisType.GetConstructors().Single();
        }


        [UsedImplicitly]
        internal readonly struct StaticNotOutParam
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public string Text { get; }

            public StaticNotOutParam(string text) => Text = text;

            private static readonly Type _thisType = typeof(StaticNotOutParam);

            public static readonly MethodInfo DeconstructMethod = typeof(ExternalDeconstruct).GetMethods()
                .Single(m => m.Name == "Deconstruct" && m.GetParameters().FirstOrDefault()?.ParameterType == _thisType);

            public static readonly ConstructorInfo Constructor = _thisType.GetConstructors().Single();
        }

        [UsedImplicitly]
        internal readonly struct StaticNotSupportedParams
        {
            public string Text { get; }
            public object Obj { get; }
            public object[] ObjArray { get; }
            public List<object> ObjList { get; }
            public string[,] StringMultiDimArray { get; }

            public StaticNotSupportedParams(string text, object obj, object[] objArray, List<object> objList, string[,] stringMultiDimArray)
            {
                Text = text;
                Obj = obj;
                ObjArray = objArray;
                ObjList = objList;
                StringMultiDimArray = stringMultiDimArray;
            }

            private static readonly Type _thisType = typeof(StaticNotSupportedParams);

            public static readonly MethodInfo DeconstructMethod = typeof(ExternalDeconstruct).GetMethods()
                .Single(m => m.Name == "Deconstruct" && m.GetParameters().FirstOrDefault()?.ParameterType == _thisType);

            public static readonly ConstructorInfo Constructor = _thisType.GetConstructors().Single();
        }

        #endregion
    }

    internal static class ExternalDeconstruct
    {
        public static void Deconstruct(this DeconstructableTests.StaticNotCompatibleCtor instance, out string text)
            => text = instance.Text;

        // ReSharper disable RedundantAssignment
        public static void Deconstruct(this DeconstructableTests.StaticNotOutParam instance, string text) => text = instance.Text;
        // ReSharper restore RedundantAssignment

        public static void Deconstruct(this DeconstructableTests.StaticNotSupportedParams instance, out string text, out object obj, out object[] objArray, out List<object> objList, out string[,] stringMultiDimArray)
        {
            text = instance.Text;
            obj = instance.Obj;
            objArray = instance.ObjArray;
            objList = instance.ObjList;
            stringMultiDimArray = instance.StringMultiDimArray;
        }
    }
}
