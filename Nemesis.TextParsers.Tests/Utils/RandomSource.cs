using JetBrains.Annotations;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests.Utils;

[PublicAPI]
internal class RandomSource
{
    private Random _rand;

    public int Seed { get; private set; }

    public RandomSource() => SetNewSeed();

    public RandomSource(int seed) => SetNewSeed(seed);

    public void SetNewSeed(int? newSeed = null) =>
        _rand = new Random(Seed = newSeed ?? Environment.TickCount);

    public int Next() => _rand.Next();
    public int Next(int maxValue) => _rand.Next(maxValue);
    public int Next(int minValue, int maxValue) => _rand.Next(minValue, maxValue);
    public double NextDouble() => _rand.NextDouble();

    public string NextString(char start, char end, int length = 10) =>
        Enumerable.Range(0, length).Aggregate(
            new StringBuilder(length),
            (sb, i) => sb.Append((char)_rand.Next(start, end + 1)),
            sb => sb.ToString());

    public TElement NextElement<TElement>(IReadOnlyList<TElement> list) => list[Next(list.Count)];

    public TElement NextElement<TElement>(Span<TElement> span) => span[Next(span.Length)];

    public double NextFloatingNumber(int magnitude = 1000, bool generateSpecialValues = true)
    {
        if (generateSpecialValues && _rand.NextDouble() is { } chance && chance < 0.1)
        {
            if (chance < 0.045) return double.PositiveInfinity;
            else if (chance < 0.09) return double.NegativeInfinity;
            else return double.NaN;
        }
        else
            return Math.Round((_rand.NextDouble() - 0.5) * 2 * magnitude, 3);
    }

    public TEnum NextEnum<TEnum, TUnderlying>()
        where TEnum : Enum
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
    {
        var values = Enum.GetValues(typeof(TEnum)).Cast<TUnderlying>().ToList();

        bool isFlag = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);

        TUnderlying value;

        if (values.Count == 0)
        {
            var numberHandler = NumberTransformerCache.GetNumberHandler<TUnderlying>();
            value = _rand.NextDouble() < 0.5 ? numberHandler.Zero : numberHandler.One;
        }
        else if (isFlag)
        {
            var numberHandler = NumberTransformerCache.GetNumberHandler<TUnderlying>();
            var min = (int)numberHandler.ToInt64(values.Min());
            var max = (int)numberHandler.ToInt64(values.Max());
            value = numberHandler.FromInt64(_rand.Next(min, max * 2));
        }
        else
            value = values[_rand.Next(0, values.Count)];

        return Unsafe.As<TUnderlying, TEnum>(ref value);
    }
}
