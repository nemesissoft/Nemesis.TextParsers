using System;
using JetBrains.Annotations;

namespace Nemesis.TextParsers.Utils
{
    [PublicAPI]
    public static class LightLinq
    {
        public static (bool success, double result) Sum(this ParsingSequence values, ITransformer<double> transformer)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double sum = 0;

            do
                sum += enumerator.Current.ParseWith(transformer);
            while (enumerator.MoveNext());


            return (true, sum);
        }

        public static (bool success, double result) Average(this ParsingSequence values, ITransformer<double> transformer)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double avg = enumerator.Current.ParseWith(transformer);
            int count = 1;

            while (enumerator.MoveNext())
                avg += (enumerator.Current.ParseWith(transformer) - avg) / ++count;
            return (true, avg);
        }

        public static (bool success, double result) Variance(this ParsingSequence values, ITransformer<double> transformer)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double mean = 0;
            // ReSharper disable once InconsistentNaming
            double sum = 0;
            uint i = 0;

            double current;
            do
            {
                current = enumerator.Current.ParseWith(transformer);
                i++;

                var delta = current - mean;

                mean += delta / i;
                sum += delta * (current - mean);
            } while (enumerator.MoveNext());

            return (true,
                i == 1 ?
                    current :
                    sum / (i - 1)
                );
        }

        public static (bool success, double result) StdDev(this ParsingSequence values, ITransformer<double> transformer)
        {
            var (success, result) = Variance(values, transformer);

            return (success, success ? Math.Sqrt(result) : 0);
        }


        public static (bool success, double result) Max(this ParsingSequence values, ITransformer<double> transformer)
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, 0);

            double max = e.Current.ParseWith(transformer);

            while (double.IsNaN(max))
            {
                if (!e.MoveNext())
                    return (true, max);
                max = e.Current.ParseWith(transformer);
            }

            while (e.MoveNext())
            {
                double current = e.Current.ParseWith(transformer);
                if (max < current)
                    max = current;
            }

            return (true, max);
        }

        public static (bool success, double result) Min(this ParsingSequence values, ITransformer<double> transformer)
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, 0);

            double min = e.Current.ParseWith(transformer);
            while (e.MoveNext())
            {
                double current = e.Current.ParseWith(transformer);
                if (min > current)
                    min = current;
                else if (double.IsNaN(current))
                    return (true, current);
            }

            return (true, min);
        }

        public static (bool success, TSource result) Aggregate<TSource>(this ParsingSequence source, ITransformer<TSource> transformer, [NotNull] Func<TSource, TSource, TSource> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            var e = source.GetEnumerator();

            if (!e.MoveNext())
                return (false, default);

            TSource result = e.Current.ParseWith(transformer);
            while (e.MoveNext())
                result = func(result, e.Current.ParseWith(transformer));

            return (true, result);
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this ParsingSequence source, ITransformer<TSource> transformer, TAccumulate seed, [NotNull] Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            TAccumulate result = seed;
            foreach (var element in source)
            {
                var parsed = element.ParseWith(transformer);
                result = func(result, parsed);
            }

            return result;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this ParsingSequence source,
            ITransformer<TSource> transformer, TAccumulate seed,
            [NotNull] Func<TAccumulate, TSource, TAccumulate> func, [NotNull] Func<TAccumulate, TResult> resultSelector)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            TAccumulate result = seed;
            foreach (var element in source)
            {
                var parsed = element.ParseWith(transformer);
                result = func(result, parsed);
            }

            return resultSelector(result);
        }
    }
}

