using System.ComponentModel;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
using Builder = Nemesis.TextParsers.Parsers.DeconstructionTransformerBuilder;

namespace Benchmarks;

/* sample execution
| Method                  | Runtime  | N    | Mean       | Ratio    | Gen0    | Allocated |
|------------------------ |--------- |----- |-----------:|---------:|--------:|----------:|-
| TypeConverter_Standard  | .NET 6.0 | 10   |   2.585 us | baseline |  0.3433 |    2160 B |
| TypeConverter_Dedicated | .NET 6.0 | 10   |   2.422 us |      -6% |  0.2899 |    1840 B |
| Convention              | .NET 6.0 | 10   |   2.032 us |     -21% |       - |         - |
| Deconstructable         | .NET 6.0 | 10   |   3.788 us |     +47% |       - |         - |
| CodeGen                 | .NET 6.0 | 10   |   3.607 us |     +40% |       - |         - |
|                         |          |      |            |          |         |           |
| TypeConverter_Standard  | .NET 8.0 | 10   |   2.266 us | baseline |  0.3433 |    2160 B |
| TypeConverter_Dedicated | .NET 8.0 | 10   |   2.286 us |      +1% |  0.2899 |    1840 B |
| Convention              | .NET 8.0 | 10   |   1.997 us |     -12% |       - |         - |
| Deconstructable         | .NET 8.0 | 10   |   3.135 us |     +38% |       - |         - |
| CodeGen                 | .NET 8.0 | 10   |   2.961 us |     +31% |       - |         - |
|                         |          |      |            |          |         |           |
| TypeConverter_Standard  | .NET 6.0 | 1000 | 250.097 us | baseline | 34.1797 |  216003 B |
| TypeConverter_Dedicated | .NET 6.0 | 1000 | 235.980 us |      -6% | 29.2969 |  184001 B |
| Convention              | .NET 6.0 | 1000 | 201.695 us |     -19% |       - |         - |
| Deconstructable         | .NET 6.0 | 1000 | 376.592 us |     +51% |       - |       3 B |
| CodeGen                 | .NET 6.0 | 1000 | 363.073 us |     +45% |       - |       3 B |
|                         |          |      |            |          |         |           |
| TypeConverter_Standard  | .NET 8.0 | 1000 | 244.608 us | baseline | 34.1797 |  216000 B |
| TypeConverter_Dedicated | .NET 8.0 | 1000 | 235.898 us |      -4% | 29.2969 |  184000 B |
| Convention              | .NET 8.0 | 1000 | 210.782 us |     -14% |       - |         - |
| Deconstructable         | .NET 8.0 | 1000 | 320.726 us |     +31% |       - |         - |
| CodeGen                 | .NET 8.0 | 1000 | 305.235 us |     +25% |       - |         - |
*/
[MemoryDiagnoser]
public class DeconstructablesBench
{
    private static readonly TypeConverter _standard =
        TypeDescriptor.GetConverter(typeof(CarrotAndOnionFactorsStandard));

    private static readonly CarrotAndOnionFactorsStandardConverter _dedicated = new();

    private static readonly ITransformer<CarrotAndOnionFactorsConvention> _convention =
        TextTransformer.Default.GetTransformer<CarrotAndOnionFactorsConvention>();

    private static readonly ITransformer<CarrotAndOnionFactorsDeconstructable> _deconstructable =
        Builder.GetDefault(TextTransformer.Default)
           .WithoutBorders().ToTransformer<CarrotAndOnionFactorsDeconstructable>();

    private static readonly ITransformer<CarrotAndOnionFactorsCodeGen> _codegen =
        TextTransformer.Default.GetTransformer<CarrotAndOnionFactorsCodeGen>();

    [Params(10, 1000)]
    public int N;

    [Benchmark(Baseline = true)]
    public int TypeConverter_Standard()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            var instance = (CarrotAndOnionFactorsStandard)_standard.ConvertFromInvariantString("15.5;1.1;2.2;3.3");
            count += (int)instance.OnionFactor1;
        }
        return count;
    }

    [Benchmark]
    public int TypeConverter_Dedicated()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            var instance = _dedicated.ParseString("15.5;1.1;2.2;3.3");
            count += (int)instance.OnionFactor1;
        }
        return count;
    }

    [Benchmark]
    public int Convention()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            var instance = _convention.Parse("15.5;1.1;2.2;3.3");
            count += (int)instance.OnionFactor1;
        }
        return count;
    }

    [Benchmark]
    public int Deconstructable()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            var instance = _deconstructable.Parse("15.5;1.1;2.2;3.3");
            count += (int)instance.OnionFactor1;
        }
        return count;
    }

    [Benchmark]
    public int CodeGen()
    {
        int count = 0;
        for (int i = 0; i < N; i++)
        {
            var instance = _codegen.Parse("15.5;1.1;2.2;3.3");
            count += (int)instance.OnionFactor1;
        }
        return count;
    }



    [TypeConverter(typeof(CarrotAndOnionFactorsStandardConverter))]
    readonly record struct CarrotAndOnionFactorsStandard(float Carrot, float OnionFactor1, float OnionFactor2, float OnionFactor3);

    class CarrotAndOnionFactorsStandardConverter : BaseTextConverter<CarrotAndOnionFactorsStandard>
    {
        public override CarrotAndOnionFactorsStandard ParseString(string text)
        {
            var parts = text.Split(';');
            var carrot = float.Parse(parts[0], CultureInfo.InvariantCulture);
            var onionFactors1 = float.Parse(parts[1], CultureInfo.InvariantCulture);
            var onionFactors2 = float.Parse(parts[2], CultureInfo.InvariantCulture);
            var onionFactors3 = float.Parse(parts[3], CultureInfo.InvariantCulture);

            return new(carrot, onionFactors1, onionFactors2, onionFactors3);
        }

        public override string FormatToString(CarrotAndOnionFactorsStandard value) => FormattableString.Invariant(
                $"{value.Carrot:G9};{value.OnionFactor1};{value.OnionFactor2};{value.OnionFactor3}"
            );
    }

    readonly record struct CarrotAndOnionFactorsConvention(float Carrot, float OnionFactor1, float OnionFactor2, float OnionFactor3)
    {
        public override string ToString() => FormattableString.Invariant(
            $"{Carrot:G9};{OnionFactor1};{OnionFactor2};{OnionFactor3}"
        );
        public static CarrotAndOnionFactorsConvention FromText(ReadOnlySpan<char> text)
        {
            static float Parse(ReadOnlySpan<char> span) =>
                float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);

            var stream = SpanSplitExtensions.Split(text, ';').GetEnumerator();

            stream.MoveNext();
            var carrot = Parse(stream.Current);

            stream.MoveNext();
            var onionFactor1 = Parse(stream.Current);

            stream.MoveNext();
            var onionFactor2 = Parse(stream.Current);

            stream.MoveNext();
            var onionFactor3 = Parse(stream.Current);


            return new(carrot, onionFactor1, onionFactor2, onionFactor3);
        }
    }

    //transformation done with expression trees
    readonly record struct CarrotAndOnionFactorsDeconstructable(
        float Carrot, float OnionFactor1, float OnionFactor2, float OnionFactor3);


    //tranformation generated via codegen
    [Transformer(typeof(CarrotAndOnionFactorsCodeGenTransformer))]
    public readonly partial record struct CarrotAndOnionFactorsCodeGen(
        float Carrot, float OnionFactor1, float OnionFactor2, float OnionFactor3);

    sealed class CarrotAndOnionFactorsCodeGenTransformer : TransformerBase<CarrotAndOnionFactorsCodeGen>
    {
        private readonly ITransformer<float> _transformer_Carrot = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<float> _transformer_OnionFactor1 = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<float> _transformer_OnionFactor2 = TextTransformer.Default.GetTransformer<float>();
        private readonly ITransformer<float> _transformer_OnionFactor3 = TextTransformer.Default.GetTransformer<float>();
        private const int ARITY = 4;


        private readonly TupleHelper _helper = new(';', '␀', '\\', null, null);

        public override CarrotAndOnionFactorsCodeGen GetEmpty() => new(_transformer_Carrot.GetEmpty(), _transformer_OnionFactor1.GetEmpty(), _transformer_OnionFactor2.GetEmpty(), _transformer_OnionFactor3.GetEmpty());
        protected override CarrotAndOnionFactorsCodeGen ParseCore(in ReadOnlySpan<char> input)
        {
            var enumerator = _helper.ParseStart(input, ARITY);
            var t1 = _helper.ParseElement(ref enumerator, _transformer_Carrot);

            var t2 = _helper.ParseElement(ref enumerator, _transformer_OnionFactor1, 2);

            var t3 = _helper.ParseElement(ref enumerator, _transformer_OnionFactor2, 3);

            var t4 = _helper.ParseElement(ref enumerator, _transformer_OnionFactor3, 4);

            _helper.ParseEnd(ref enumerator, ARITY);
            return new CarrotAndOnionFactorsCodeGen(t1, t2, t3, t4);
        }

        public override string Format(CarrotAndOnionFactorsCodeGen element)
        {
            Span<char> initialBuffer = stackalloc char[32];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
            try
            {
                _helper.StartFormat(ref accumulator);
                var (Carrot, OnionFactor1, OnionFactor2, OnionFactor3) = element;
                _helper.FormatElement(_transformer_Carrot, Carrot, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_OnionFactor1, OnionFactor1, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_OnionFactor2, OnionFactor2, ref accumulator);

                _helper.AddDelimiter(ref accumulator);
                _helper.FormatElement(_transformer_OnionFactor3, OnionFactor3, ref accumulator);

                _helper.EndFormat(ref accumulator);
                return accumulator.AsSpan().ToString();
            }
            finally { accumulator.Dispose(); }
        }
    }
}
