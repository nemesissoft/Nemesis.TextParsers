using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Nemesis.TextParsers;
using Nemesis.TextParsers.Utils;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Benchmarks
{
    [MemoryDiagnoser]
    // ReSharper disable once IdentifierTypo
    public class Deconstructables
    {
        private TypeConverter _standard;

        private CarrotAndOnionFactorsStandardConverter _dedicated;

        private ITransformer<CarrotAndOnionFactorsConvention> _convention;

        private ITransformer<CarrotAndOnionFactorsConventionDeconstructable> _deconstructable;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _standard = TypeDescriptor.GetConverter(typeof(CarrotAndOnionFactorsStandard));

            _dedicated = new CarrotAndOnionFactorsStandardConverter();

            _convention = TextTransformer.Default.GetTransformer<CarrotAndOnionFactorsConvention>();

            _deconstructable = Sett.Default
               .WithoutBorders().ToTransformer<CarrotAndOnionFactorsConventionDeconstructable>(TextTransformer.Default);
        }

        [Params(10, 100, 1000)]
        public int N;

        [Benchmark(Baseline = true)]
        public int Standard()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
            {
                var instance = (CarrotAndOnionFactorsStandard)_standard.ConvertFromInvariantString("15.5;1.1,2.2,3.3");
                count += (int)instance.OnionFactor1;
            }
            return count;
        }

        [Benchmark]
        public int Dedicated()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
            {
                var instance = _dedicated.ParseString("15.5;1.1,2.2,3.3");
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
                var instance = _convention.Parse("15.5;1.1,2.2,3.3");
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



        [TypeConverter(typeof(CarrotAndOnionFactorsStandardConverter))]
        struct CarrotAndOnionFactorsStandard
        {
            public float Carrot { get; }
            public float OnionFactor1 { get; }
            public float OnionFactor2 { get; }
            public float OnionFactor3 { get; }

            public CarrotAndOnionFactorsStandard(float carrot, float onionFactor1, float onionFactor2, float onionFactor3)
            {
                Carrot = carrot;
                OnionFactor1 = onionFactor1;
                OnionFactor2 = onionFactor2;
                OnionFactor3 = onionFactor3;
            }
        }

        class CarrotAndOnionFactorsStandardConverter : BaseTextConverter<CarrotAndOnionFactorsStandard>
        {
            public override CarrotAndOnionFactorsStandard ParseString(string text)
            {
                var parts = text.Split(';');
                float carrot = float.Parse(parts[0], CultureInfo.InvariantCulture);

                var onionFactors = parts[1].Split(',')
                    .Select(t => float.Parse(t, CultureInfo.InvariantCulture))
                    .ToArray();

                return new CarrotAndOnionFactorsStandard(carrot, onionFactors[0], onionFactors[1], onionFactors[2]);

            }

            public override string FormatToString(CarrotAndOnionFactorsStandard value) =>
                FormattableString.Invariant(
                    $"{value.Carrot:G9};{value.OnionFactor1},{value.OnionFactor2},{value.OnionFactor3}"
                );
        }

        struct CarrotAndOnionFactorsConvention
        {
            public float Carrot { get; }
            public float OnionFactor1 { get; }
            public float OnionFactor2 { get; }
            public float OnionFactor3 { get; }

            public CarrotAndOnionFactorsConvention(float carrot, float onionFactor1, float onionFactor2, float onionFactor3)
            {
                Carrot = carrot;
                OnionFactor1 = onionFactor1;
                OnionFactor2 = onionFactor2;
                OnionFactor3 = onionFactor3;
            }

            public override string ToString() => FormattableString.Invariant(
                $"{Carrot:G9};{OnionFactor1},{OnionFactor2},{OnionFactor3}"
            );

            //string is used for tests - normally ReadOnlySpan<char> should be preferred
            public static CarrotAndOnionFactorsConvention FromText(string text)
            {
                static float Parse(ReadOnlySpan<char> span) =>
                    float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);

                var stream = text.AsSpan().Split(';').GetEnumerator();

                stream.MoveNext();

                var carrot = Parse(stream.Current);
                float onionFactor1 = 0.0f, onionFactor2 = 0.0f, onionFactor3 = 0.0f;


                if (stream.MoveNext())
                {
                    var current = stream.Current;

                    var onionStream = current.Split(',', true).GetEnumerator();


                    onionStream.MoveNext();
                    onionFactor1 = Parse(onionStream.Current);

                    onionStream.MoveNext();
                    onionFactor2 = Parse(onionStream.Current);

                    onionStream.MoveNext();
                    onionFactor3 = Parse(onionStream.Current);

                }

                return new CarrotAndOnionFactorsConvention(carrot, onionFactor1, onionFactor2, onionFactor3);
            }
        }

        struct CarrotAndOnionFactorsConventionDeconstructable
        {
            public float Carrot { get; }
            public float OnionFactor1 { get; }
            public float OnionFactor2 { get; }
            public float OnionFactor3 { get; }

            public CarrotAndOnionFactorsConventionDeconstructable(float carrot, float onionFactor1, float onionFactor2, float onionFactor3)
            {
                Carrot = carrot;
                OnionFactor1 = onionFactor1;
                OnionFactor2 = onionFactor2;
                OnionFactor3 = onionFactor3;
            }

            public void Deconstruct(out float carrot, out float onionFactor1, out float onionFactor2, out float onionFactor3)
            {
                carrot = Carrot;
                onionFactor1 = OnionFactor1;
                onionFactor2 = OnionFactor2;
                onionFactor3 = OnionFactor3;
            }
        }
    }
}
