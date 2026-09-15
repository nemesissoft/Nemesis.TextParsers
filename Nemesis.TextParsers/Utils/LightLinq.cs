#nullable enable

namespace Nemesis.TextParsers.Utils;

public static class LightLinq
{
#if NET7_0_OR_GREATER
    extension(ParsingSequence values)
    {
        public TNumber Sum<TNumber>(ITransformer<TNumber> transformer)
            where TNumber : INumberBase<TNumber>
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return TNumber.Zero;

            var sum = TNumber.Zero;

            do
                sum += enumerator.Current.ParseWith(transformer);
            while (enumerator.MoveNext());

            return sum;
        }

        public TNumber WalkingAverage<TNumber>(ITransformer<TNumber> transformer)
            where TNumber : IFloatingPointIeee754<TNumber>
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return TNumber.NaN;

            var avg = enumerator.Current.ParseWith(transformer);
            var count = 1;

            while (enumerator.MoveNext())
                avg += (enumerator.Current.ParseWith(transformer) - avg)
                       /
                       TNumber.CreateChecked(++count);
            return avg;
        }

        public (bool success, TResult result) Average<TSource, TResult>(ITransformer<TSource> transformer)
            where TSource : INumberBase<TSource>
            where TResult : INumberBase<TResult>
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, TResult.Zero);

            TResult sum = TResult.CreateChecked(e.Current.ParseWith(transformer));
            long count = 1;
            while (e.MoveNext())
            {
                checked
                {
                    sum += TResult.CreateChecked(e.Current.ParseWith(transformer));
                }
                count++;
            }

            return (true, TResult.CreateChecked(sum) / TResult.CreateChecked(count));
        }

        public (bool success, TResult result) Variance<TNumber, TResult>(ITransformer<TNumber> transformer)
            where TNumber : INumberBase<TNumber>
            where TResult : IFloatingPoint<TResult>
        {
            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return (false, TResult.Zero);

            TResult mean = TResult.Zero;
            TResult sum = TResult.Zero;
            uint i = 0;

            TResult current;
            do
            {
                current = TResult.CreateChecked(enumerator.Current.ParseWith(transformer));
                i++;

                var delta = current - mean;

                mean += delta / TResult.CreateChecked(i);
                sum += delta * (current - mean);
            } while (enumerator.MoveNext());

            return (true,
                    i == 1 ? current : sum / TResult.CreateChecked(i - 1)
                );
        }

        public (bool success, TResult result) StdDev<TNumber, TResult>(ITransformer<TNumber> transformer)
            where TNumber : INumberBase<TNumber>
            where TResult : IFloatingPoint<TResult>, IRootFunctions<TResult>
        {
            var (success, result) = values.Variance<TNumber, TResult>(transformer);

            return (success, success ? TResult.Sqrt(result) : TResult.Zero);
        }

        public (bool success, TNumber result) Max<TNumber>(ITransformer<TNumber> transformer)
            where TNumber : INumberBase<TNumber>, IComparisonOperators<TNumber, TNumber, bool>
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, TNumber.Zero);

            TNumber max = e.Current.ParseWith(transformer);
            while (TNumber.IsNaN(max))
            {
                if (!e.MoveNext())
                    return (true, max);
                max = e.Current.ParseWith(transformer);
            }

            while (e.MoveNext())
            {
                TNumber x = e.Current.ParseWith(transformer);
                if (x > max)
                    max = x;
            }

            return (true, max);
        }

        public (bool success, TNumber result) Min<TNumber>(ITransformer<TNumber> transformer)
            where TNumber : INumberBase<TNumber>, IComparisonOperators<TNumber, TNumber, bool>
        {
            var e = values.GetEnumerator();
            if (!e.MoveNext()) return (false, TNumber.Zero);

            TNumber min = e.Current.ParseWith(transformer);
            if (TNumber.IsNaN(min))
                return (true, min);

            while (e.MoveNext())
            {
                TNumber x = e.Current.ParseWith(transformer);
                if (x < min)
                    min = x;
                else if (TNumber.IsNaN(x))
                    return (true, x);
            }

            return (true, min);
        }

        public (bool success, TSource? result) Aggregate<TSource>(ITransformer<TSource> transformer,
            Func<TSource, TSource, TSource> func)
        {
            ArgumentNullException.ThrowIfNull(func);

            var e = values.GetEnumerator();

            if (!e.MoveNext())
                return (false, default);

            TSource result = e.Current.ParseWith(transformer);
            while (e.MoveNext())
                result = func(result, e.Current.ParseWith(transformer));

            return (true, result);
        }

        public TAccumulate Aggregate<TSource, TAccumulate>(ITransformer<TSource> transformer, TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func)
        {
            ArgumentNullException.ThrowIfNull(func);

            TAccumulate result = seed;
            foreach (var element in values)
            {
                var parsed = element.ParseWith(transformer);
                result = func(result, parsed);
            }

            return result;
        }

        public TResult Aggregate<TSource, TAccumulate, TResult>(ITransformer<TSource> transformer, TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            ArgumentNullException.ThrowIfNull(func);
            ArgumentNullException.ThrowIfNull(resultSelector);

            TAccumulate result = seed;
            foreach (var element in values)
            {
                var parsed = element.ParseWith(transformer);
                result = func(result, parsed);
            }

            return resultSelector(result);
        }
    }
#endif
}