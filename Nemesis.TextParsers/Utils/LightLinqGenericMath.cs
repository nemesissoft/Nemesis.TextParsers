#nullable enable

namespace Nemesis.TextParsers.Utils;

public static class LightLinqGenericMath
{
#if NET7_0_OR_GREATER
    public static (bool success, TNumber result) Sum<TNumber>(this ParsingSequence values, ITransformer<TNumber> transformer)
        where TNumber : INumberBase<TNumber>
    {
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) return (false, TNumber.Zero);

        var sum = TNumber.Zero;

        do
            sum += enumerator.Current.ParseWith(transformer);
        while (enumerator.MoveNext());


        return (true, sum);
    }

    public static (bool success, TNumber result) WalkingAverage<TNumber>(this ParsingSequence values, ITransformer<TNumber> transformer)
        where TNumber : IFloatingPoint<TNumber>
    {
        var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext()) return (false, TNumber.Zero);

        TNumber avg = enumerator.Current.ParseWith(transformer);
        int count = 1;

        while (enumerator.MoveNext())
            avg += (enumerator.Current.ParseWith(transformer) - avg)
                                 /
                   TNumber.CreateChecked(++count);
        return (true, avg);
    }


    public static (bool success, TResult result) Average<TSource, TAccumulator, TResult>(this ParsingSequence values, ITransformer<TSource> transformer)
        where TSource : INumberBase<TSource>
        where TAccumulator : INumberBase<TAccumulator>
        where TResult : INumberBase<TResult>
    {
        var e = values.GetEnumerator();
        if (!e.MoveNext()) return (false, TResult.Zero);

        TAccumulator sum = TAccumulator.CreateChecked(e.Current.ParseWith(transformer));
        long count = 1;
        while (e.MoveNext())
        {
            checked { sum += TAccumulator.CreateChecked(e.Current.ParseWith(transformer)); }
            count++;
        }

        return (true, TResult.CreateChecked(sum) / TResult.CreateChecked(count));
    }

    public static (bool success, TResult result) Variance<TNumber, TResult>(this ParsingSequence values, ITransformer<TNumber> transformer)
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
            i == 1 ?
                current :
                sum / TResult.CreateChecked(i - 1)
            );
    }

    public static (bool success, TResult result) StdDev<TNumber, TResult>(this ParsingSequence values, ITransformer<TNumber> transformer)
          where TNumber : INumberBase<TNumber>
          where TResult : IFloatingPoint<TResult>, IRootFunctions<TResult>
    {
        var (success, result) = Variance<TNumber, TResult>(values, transformer);

        return (success, success ? TResult.Sqrt(result) : TResult.Zero);
    }


    public static (bool success, TNumber result) Max<TNumber>(this ParsingSequence values, ITransformer<TNumber> transformer)
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

    public static (bool success, TNumber result) Min<TNumber>(this ParsingSequence values, ITransformer<TNumber> transformer)
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
#endif
}

