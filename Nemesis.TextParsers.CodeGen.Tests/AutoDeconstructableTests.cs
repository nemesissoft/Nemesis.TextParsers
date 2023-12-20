extern alias original;

using Microsoft.CodeAnalysis;
using Nemesis.TextParsers.CodeGen.Deconstructable;
using static Nemesis.TextParsers.CodeGen.Tests.CodeGenUtils;

namespace Nemesis.TextParsers.CodeGen.Tests;

[TestFixture]
public partial class AutoDeconstructableTests
{
    private static readonly IEnumerable<TCD> _endToEndCases = GetAutoDeconstructableCases()
       .Select((t, i) => new TCD(t.source, t.expectedCode)
           .SetName($"E2E_{i + 1:00}_{t.name}"));

    private static IReadOnlyList<string> GetGeneratedTrees(Compilation compilation, int requiredCardinality = 1)
        => GetGeneratedTreesOnly(compilation,
            new AutoDeconstructableGenerator(),
            AutoDeconstructableGenerator.ATTRIBUTE_NAME,
            requiredCardinality
    );

    [TestCaseSource(nameof(_endToEndCases))]
    public void EndToEndTests(string source, string expectedCode)
    {
        var compilation = CreateValidCompilation(source);

        var generatedTrees = GetGeneratedTrees(compilation);

        var actual = ScrubGeneratorComments(generatedTrees.Single());

        Assert.That(actual, Is.EqualTo(expectedCode).Using(IgnoreNewLinesComparer.EqualityComparer));
    }


    private static readonly IEnumerable<TCD> _settingsCases = new (string typeDefinition, string expectedCodePart)[]
    {
        (@"[DeconstructableSettings(':', '␀', '/', '{', '}')]
            readonly partial struct Child
            {
                public byte Age { get; }
                public float Weight { get; }
                public Child(byte age, float weight) { Age = age; Weight = weight; }
                public void Deconstruct(out byte age, out float weight) { age = Age; weight = Weight; }                
            }", "private readonly TupleHelper _helper = new TupleHelper(':', '␀', '/', '{', '}');"),


        (@"[DeconstructableSettings('_', '␀', '*', '<', '>')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '<', '>');"),

        (@"[DeconstructableSettings('_', '␀', '*', '<')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '<', ')');"),

        (@"[DeconstructableSettings('_', '␀', '*')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '*', '(', ')');"),

        (@"[DeconstructableSettings('_', '␀')]
            partial record T(byte B) { }", @"new TupleHelper('_', '␀', '\\', '(', ')');"),

        (@"[DeconstructableSettings('_')]
            partial record T(byte B) { }", @"new TupleHelper('_', '∅', '\\', '(', ')');"),

        (@"[DeconstructableSettings]
            partial record T(byte B) { }", @"new TupleHelper(';', '∅', '\\', '(', ')');"),

        (@"partial record T(byte B) { }", @"_helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();"),


        (@"[DeconstructableSettings(useDeconstructableEmpty: true)]
            partial record T(byte B) { }", @"public override T GetEmpty() => new T(_transformer_B.GetEmpty());"),

        (@"[DeconstructableSettings(useDeconstructableEmpty: false)]
            partial record T(byte B) { }", @"NOT CONTAIN:GetEmpty()"),
    }
       .Select((t, i) => new TCD($@"using Nemesis.TextParsers.Settings; namespace Tests {{ [Auto.AutoDeconstructable] {t.typeDefinition} }}", t.expectedCodePart)
           .SetName($"Settings{i + 1:00}"));

    [TestCaseSource(nameof(_settingsCases))]
    public void SettingsRetrieval_ShouldEmitProperValues(string source, string expectedCodePart)
    {
        bool matchNotContain = expectedCodePart.StartsWith("NOT CONTAIN:");
        if (matchNotContain)
            expectedCodePart = expectedCodePart.Substring(12);

        //arrange
        var compilation = CreateValidCompilation(source);

        //act
        var generatedTrees = GetGeneratedTrees(compilation);
        var actual = generatedTrees.Single();


        //assert
        Assert.That(actual, matchNotContain ? Does.Not.Contain(expectedCodePart) : Does.Contain(expectedCodePart));
    }

    [Test]
    public void Generate_When_StaticUsing_And_Mnemonics()
    {
        var compilation = CreateValidCompilation(@"using SD = System.Double;
using static Tests3.ContainerClass3;

namespace Tests1
{
    [Auto.AutoDeconstructable]
    public partial record Numbers(System.Single FloatNumber, SD DoubleNumber, Tests2.ContainerClass2.NestedClass2 NestedFullyQualified, NestedClass3 NestedUsingStatic) { }
}

namespace Tests2
{
    public static class ContainerClass2 { public class NestedClass2 { } }
}

namespace Tests3
{
    public static class ContainerClass3 { public class NestedClass3 { } }
}");

        var generatedTrees = GetGeneratedTrees(compilation);

        var actual = ScrubGeneratorComments(generatedTrees.Single());

        Assert.That(actual, Is.EqualTo(@"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
using Tests2;
using Tests3;
using static Tests3.ContainerClass3;
using SD = System.Double;

namespace Tests1
{
    [Transformer(typeof(NumbersTransformer))]
    public partial record Numbers 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class NumbersTransformer : TransformerBase<Numbers>
    {
        private readonly ITransformer<float> _transformer_FloatNumber = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<double> _transformer_DoubleNumber = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<ContainerClass2.NestedClass2> _transformer_NestedFullyQualified = TextTransformer.Default.GetTransformer<ContainerClass2.NestedClass2>();
        private readonly ITransformer<ContainerClass3.NestedClass3> _transformer_NestedUsingStatic = TextTransformer.Default.GetTransformer<ContainerClass3.NestedClass3>();
        private const int ARITY = 4;


        private readonly TupleHelper _helper;

        public NumbersTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override Numbers ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_FloatNumber);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_DoubleNumber, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_NestedFullyQualified, 3);

            var t4 = _helper.ParseElement(ref enumerator, _transformer_NestedUsingStatic, 4);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Numbers(t1, t2, t3, t4);
        }

        public override string Format(Numbers element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (FloatNumber, DoubleNumber, NestedFullyQualified, NestedUsingStatic) = element;
                _helper.FormatElement(_transformer_FloatNumber, FloatNumber, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_DoubleNumber, DoubleNumber, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_NestedFullyQualified, NestedFullyQualified, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_NestedUsingStatic, NestedUsingStatic, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}").Using(IgnoreNewLinesComparer.EqualityComparer));
    }


    public static IReadOnlyList<(string name, string source, string expectedCode)> GetAutoDeconstructableCases() =>
        GetData().Select(t =>
        (
          t.name,
          $$"""
          using Nemesis.TextParsers.Settings; 
          namespace Nemesis.TextParsers.CodeGen.Tests; 
          {{t.source}}
          """,
          t.expectedCode)
        ).ToList();

    private static IReadOnlyList<(string name, string source, string expectedCode)> GetData() =>
    [
        ("SimpleWrapperRecord", @"[Auto.AutoDeconstructable] partial record eDoubleRecord(double Value) { }",
            @"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(eDoubleRecordTransformer))]
    partial record eDoubleRecord 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class eDoubleRecordTransformer : TransformerBase<eDoubleRecord>
    {
        private readonly ITransformer<double> _transformer_Value = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 1;


        private readonly TupleHelper _helper;

        public eDoubleRecordTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override eDoubleRecord ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_Value);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new eDoubleRecord(t1);
        }

        public override string Format(eDoubleRecord element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 double Value;
                 element.Deconstruct(out Value);

                _helper.FormatElement(_transformer_Value, Value, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),


        ("SimpleWrapperStruct", @"[Auto.AutoDeconstructable] readonly partial struct eDoubleStruct {
                public double Value { get; }
                public eDoubleStruct(double value) => Value = value;
                public void Deconstruct(out double value) => value = Value;
            }", @"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(eDoubleStructTransformer))]
    readonly partial struct eDoubleStruct 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class eDoubleStructTransformer : TransformerBase<eDoubleStruct>
    {
        private readonly ITransformer<double> _transformer_value = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 1;


        private readonly TupleHelper _helper;

        public eDoubleStructTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override eDoubleStruct ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_value);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new eDoubleStruct(t1);
        }

        public override string Format(eDoubleStruct element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 double value;
                 element.Deconstruct(out value);

                _helper.FormatElement(_transformer_value, value, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),


        ("Record", @"public record RecordPoint2d(double X, double Y) { }

               [Auto.AutoDeconstructable]
               [DeconstructableSettings(',', '∅', '\\', '[', ']')]
               public partial record RecordPoint3d(double X, double Y, double Z): RecordPoint2d(X, Y) 
               {
                   public double Magnitude { get; init; } //will NOT be subject to deconstruction
               }", @"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(RecordPoint3dTransformer))]
    public partial record RecordPoint3d 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class RecordPoint3dTransformer : TransformerBase<RecordPoint3d>
    {
        private readonly ITransformer<double> _transformer_X = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_Z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(',', '∅', '\\', '[', ']');

        public override RecordPoint3d GetEmpty() => new RecordPoint3d(_transformer_X.GetEmpty(), _transformer_Y.GetEmpty(), _transformer_Z.GetEmpty());
        protected override RecordPoint3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_X);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_Y, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_Z, 3);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new RecordPoint3d(t1, t2, t3);
        }

        public override string Format(RecordPoint3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (X, Y, Z) = element;
                _helper.FormatElement(_transformer_X, X, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Y, Y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Z, Z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),




        ("ReadOnlyStruct", @"[Auto.AutoDeconstructable]
               [DeconstructableSettings(';', '∅', '\\', '(', ')')]
               public readonly partial struct Point3d
               {
                   public double X { get; } public double Y { get; } public double Z { get; }
                   public Point3d(double x, double y, double z) { X = x; Y = y; Z = z; }

                   public void Deconstruct(out double x, out System.Double y, out double z) { x = X; y = Y; z = Z; }
               }", @"//HEAD
using System;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(Point3dTransformer))]
    public readonly partial struct Point3d 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class Point3dTransformer : TransformerBase<Point3d>
    {
        private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_y = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<double> _transformer_z = TextTransformer.Default.GetTransformer<double>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper = new TupleHelper(';', '∅', '\\', '(', ')');

        public override Point3d GetEmpty() => new Point3d(_transformer_x.GetEmpty(), _transformer_y.GetEmpty(), _transformer_z.GetEmpty());
        protected override Point3d ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_x);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_y, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_z, 3);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Point3d(t1, t2, t3);
        }

        public override string Format(Point3d element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (x, y, z) = element;
                _helper.FormatElement(_transformer_x, x, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_y, y, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_z, z, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),



        ("Large", @"[Auto.AutoDeconstructable]
               partial class Large
               {
                   public double N1 { get; } public float N2 { get; } public int N3 { get; } public uint N4 { get; }
                   public short N5 { get; } public ushort N6 { get; } public byte N7 { get; } public sbyte N8 { get; }
                   public long N9 { get; } public ulong N10 { get; } public decimal N11 { get; } public System.Numerics.BigInteger N12 { get; }
                   public System.Numerics.Complex N13 { get; }

                   public Large(double n1, float n2, int n3, uint n4, short n5, ushort n6, byte n7, sbyte n8,
                       long n9, ulong n10, decimal n11, System.Numerics.BigInteger n12, System.Numerics.Complex n13)
                   {
                       N1 = n1; N2 = n2; N3 = n3; N4 = n4;
                       N5 = n5; N6 = n6; N7 = n7; N8 = n8;
                       N9 = n9; N10 = n10; N11 = n11; N12 = n12;
                       N13 = n13;
                   }

                   public void Deconstruct(out double n1, out float n2, out int n3, out uint n4, out short n5, out ushort n6,
                       out byte n7, out sbyte n8, out long n9, out ulong n10, out decimal n11, out System.Numerics.BigInteger n12, out System.Numerics.Complex n13)
                   {
                       n1 = N1; n2 = N2; n3 = N3; n4 = N4;
                       n5 = N5; n6 = N6; n7 = N7; n8 = N8;
                       n9 = N9; n10 = N10; n11 = N11; n12 = N12;
                       n13 = N13;
                   }
               }",
            @"//HEAD
using System;
using System.Numerics;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(LargeTransformer))]
    partial class Large 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class LargeTransformer : TransformerBase<Large>
    {
        private readonly ITransformer<double> _transformer_n1 = TextTransformer.Default.GetTransformer<double>();
        private readonly ITransformer<float> _transformer_n2 = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<int> _transformer_n3 = TextTransformer.Default.GetTransformer<int>();
        private readonly ITransformer<uint> _transformer_n4 = TextTransformer.Default.GetTransformer<uint>();
        private readonly ITransformer<short> _transformer_n5 = TextTransformer.Default.GetTransformer<short>();
        private readonly ITransformer<ushort> _transformer_n6 = TextTransformer.Default.GetTransformer<ushort>();
        private readonly ITransformer<byte> _transformer_n7 = TextTransformer.Default.GetTransformer<byte>();
        private readonly ITransformer<sbyte> _transformer_n8 = TextTransformer.Default.GetTransformer<sbyte>();
        private readonly ITransformer<long> _transformer_n9 = TextTransformer.Default.GetTransformer<long>();
        private readonly ITransformer<ulong> _transformer_n10 = TextTransformer.Default.GetTransformer<ulong>();
        private readonly ITransformer<decimal> _transformer_n11 = TextTransformer.Default.GetTransformer<decimal>();
        private readonly ITransformer<BigInteger> _transformer_n12 = TextTransformer.Default.GetTransformer<BigInteger>();
        private readonly ITransformer<Complex> _transformer_n13 = TextTransformer.Default.GetTransformer<Complex>();
        private const int ARITY = 13;


        private readonly TupleHelper _helper;

        public LargeTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override Large ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_n1);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_n2, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_n3, 3);

            var t4 = _helper.ParseElement(ref enumerator, _transformer_n4, 4);

            var t5 = _helper.ParseElement(ref enumerator, _transformer_n5, 5);

            var t6 = _helper.ParseElement(ref enumerator, _transformer_n6, 6);

            var t7 = _helper.ParseElement(ref enumerator, _transformer_n7, 7);

            var t8 = _helper.ParseElement(ref enumerator, _transformer_n8, 8);

            var t9 = _helper.ParseElement(ref enumerator, _transformer_n9, 9);

            var t10 = _helper.ParseElement(ref enumerator, _transformer_n10, 10);

            var t11 = _helper.ParseElement(ref enumerator, _transformer_n11, 11);

            var t12 = _helper.ParseElement(ref enumerator, _transformer_n12, 12);

            var t13 = _helper.ParseElement(ref enumerator, _transformer_n13, 13);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new Large(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public override string Format(Large element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13) = element;
                _helper.FormatElement(_transformer_n1, n1, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n2, n2, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n3, n3, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n4, n4, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n5, n5, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n6, n6, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n7, n7, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n8, n8, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n9, n9, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n10, n10, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n11, n11, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n12, n12, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_n13, n13, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}"),



        ("ComplexType", @" public readonly struct Number{}
                [Auto.AutoDeconstructable]               
                public partial record ComplexTypes(double[] Doubles, Number? Nullable, System.Collections.Generic.List<Number> List) { }",
            @"//HEAD
using System;
using System.Collections.Generic;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.CodeGen.Tests
{
    [Transformer(typeof(ComplexTypesTransformer))]
    public partial record ComplexTypes 
    {
#if DEBUG
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        internal void DebuggerHook() { System.Diagnostics.Debugger.Launch(); }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#endif
    }

    [System.CodeDom.Compiler.GeneratedCode(string.Empty, string.Empty)]
    [System.Runtime.CompilerServices.CompilerGenerated]
    sealed class ComplexTypesTransformer : TransformerBase<ComplexTypes>
    {
        private readonly ITransformer<double[]> _transformer_Doubles = TextTransformer.Default.GetTransformer<double[]>();
        private readonly ITransformer<Number?> _transformer_Nullable = TextTransformer.Default.GetTransformer<Number?>();
        private readonly ITransformer<List<Number>> _transformer_List = TextTransformer.Default.GetTransformer<List<Number>>();
        private const int ARITY = 3;


        private readonly TupleHelper _helper;

        public ComplexTypesTransformer(Nemesis.TextParsers.ITransformerStore transformerStore)
        {
            _helper = transformerStore.SettingsStore.GetSettingsFor<Nemesis.TextParsers.Settings.DeconstructableSettings>().ToTupleHelper();
        }
        protected override ComplexTypes ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_Doubles);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_Nullable, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_List, 3);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new ComplexTypes(t1, t2, t3);
        }

        public override string Format(ComplexTypes element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                 _helper.StartFormat(ref accumulator);
                 var (Doubles, Nullable, List) = element;
                _helper.FormatElement(_transformer_Doubles, Doubles, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_Nullable, Nullable, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_List, List, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}")
    ];
}


