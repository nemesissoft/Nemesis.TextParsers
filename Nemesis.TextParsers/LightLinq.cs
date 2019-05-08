using System;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    public static class LightLinq
    {
        public static (bool success, double result) Sum(this ParsedSequence<double> values)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double sum = 0;

            do
                sum += enumerator.Current;
            while (enumerator.MoveNext());


            return (true, sum);
        }

        public static (bool success, double result) Average(this ParsedSequence<double> values)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double avg = enumerator.Current;
            int count = 1;

            while (enumerator.MoveNext())
                avg += (enumerator.Current - avg) / ++count;
            return (true, avg);
        }

        public static (bool success, double result) Variance(this ParsedSequence<double> values)
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, 0);

            double mean = 0;
            // ReSharper disable once InconsistentNaming
            double Σ = 0;
            uint i = 0;

            double current;
            do
            {
                current = enumerator.Current;
                i++;

                var δ = current - mean;

                mean = mean + δ / i;
                Σ += δ * (current - mean);
            } while (enumerator.MoveNext());

            return (true,
                i == 1 ?
                    current :
                    Σ / (i - 1)
                );
        }
        
        public static (bool success, double result) StdDev(this ParsedSequence<double> values)
        {
            var (success, result) = Variance(values);

            return (success, success ? Math.Sqrt(result) : 0);
        }


        public static (bool success, double result) Max(this ParsedSequence<double> values)
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, 0);

            double max = e.Current;

            while (double.IsNaN(max))
            {
                if (!e.MoveNext())
                    return (true, max);
                max = e.Current;
            }

            while (e.MoveNext())
            {
                double current = e.Current;
                if (max < current)
                    max = current;
            }

            return (true, max);
        }

        public static (bool success, double result) Min(this ParsedSequence<double> values)
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, 0);

            double min = e.Current;
            while (e.MoveNext())
            {
                double current = e.Current;
                if (min > current)
                    min = current;
                else if (double.IsNaN(current))
                    return (true, current);
            }

            return (true, min);
        }

        public static (bool success, TSource result)  Aggregate<TSource>(this ParsedSequence<TSource> source, [NotNull] Func<TSource, TSource, TSource> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            var e = source.GetEnumerator();

            if (!e.MoveNext())
                return (false, default);

            TSource result = e.Current;
            while (e.MoveNext())
                result = func(result, e.Current);

            return (true, result);
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this ParsedSequence<TSource> source, TAccumulate seed, [NotNull] Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            TAccumulate result = seed;
            foreach (TSource element in source)
                result = func(result, element);

            return result;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this ParsedSequence<TSource> source, TAccumulate seed,
            [NotNull] Func<TAccumulate, TSource, TAccumulate> func, [NotNull] Func<TAccumulate, TResult> resultSelector)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            TAccumulate result = seed;
            foreach (TSource element in source)
                result = func(result, element);

            return resultSelector(result);
        }
    }
}

