using System.Buffers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

public sealed class ValueTuple1Transformer<T1> : TransformerBase<ValueTuple<T1>>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;

    private const byte ARITY = 1;

    public ValueTuple1Transformer(TupleHelper helper, ITransformer<T1> transformer1)
    {
        _helper = helper;
        _transformer1 = transformer1;
    }

    protected override ValueTuple<T1> ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);

        _helper.ParseEnd(ref enumerator, ARITY);

        return new ValueTuple<T1>(t1);
    }

    public override string Format(ValueTuple<T1> element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public override ValueTuple<T1> GetEmpty() =>
        new(
            _transformer1.GetEmpty()
        );
}

public sealed class ValueTuple2Transformer<T1, T2> : TransformerBase<(T1, T2)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;

    private const byte ARITY = 2;

    public ValueTuple2Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
    }

    protected override (T1, T2) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple2");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2);
    }

    public override string Format((T1, T2) element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);


            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public override ValueTuple<T1, T2> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty()
        );
}

public sealed class ValueTuple3Transformer<T1, T2, T3> : TransformerBase<(T1, T2, T3)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;

    private const byte ARITY = 3;

    public ValueTuple3Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
    }

    protected override (T1, T2, T3) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple3");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple3");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2, t3);
    }

    public override string Format((T1, T2, T3) element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public override ValueTuple<T1, T2, T3> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty()
        );
}

public sealed class ValueTuple4Transformer<T1, T2, T3, T4> : TransformerBase<(T1, T2, T3, T4)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;
    private readonly ITransformer<T4> _transformer4;

    private const byte ARITY = 4;

    public ValueTuple4Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3, ITransformer<T4> transformer4)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
        _transformer4 = transformer4;
    }

    protected override (T1, T2, T3, T4) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple4");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple4");
        var t4 = _helper.ParseElement(ref enumerator, _transformer4, 4, "ValueTuple4");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2, t3, t4);
    }

    public override string Format((T1, T2, T3, T4) element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer4, element.Item4, ref accumulator);


            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public override ValueTuple<T1, T2, T3, T4> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty(),
            _transformer4.GetEmpty()
        );
}

public sealed class ValueTuple5Transformer<T1, T2, T3, T4, T5> : TransformerBase<(T1, T2, T3, T4, T5)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;
    private readonly ITransformer<T4> _transformer4;
    private readonly ITransformer<T5> _transformer5;

    private const byte ARITY = 5;

    public ValueTuple5Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3, ITransformer<T4> transformer4, ITransformer<T5> transformer5)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
        _transformer4 = transformer4;
        _transformer5 = transformer5;
    }

    protected override (T1, T2, T3, T4, T5) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple5");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple5");
        var t4 = _helper.ParseElement(ref enumerator, _transformer4, 4, "ValueTuple5");
        var t5 = _helper.ParseElement(ref enumerator, _transformer5, 5, "ValueTuple5");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2, t3, t4, t5);
    }

    public override string Format((T1, T2, T3, T4, T5) element)
    {
        var initialBuffer = ArrayPool<char>.Shared.Rent(32);
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer4, element.Item4, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer5, element.Item5, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally
        {
            accumulator.Dispose();
            ArrayPool<char>.Shared.Return(initialBuffer);
        }
    }

    public override ValueTuple<T1, T2, T3, T4, T5> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty(),
            _transformer4.GetEmpty(),
            _transformer5.GetEmpty()
        );
}

public sealed class ValueTuple6Transformer<T1, T2, T3, T4, T5, T6> : TransformerBase<(T1, T2, T3, T4, T5, T6)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;
    private readonly ITransformer<T4> _transformer4;
    private readonly ITransformer<T5> _transformer5;
    private readonly ITransformer<T6> _transformer6;

    private const byte ARITY = 6;

    public ValueTuple6Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3, ITransformer<T4> transformer4, ITransformer<T5> transformer5, ITransformer<T6> transformer6)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
        _transformer4 = transformer4;
        _transformer5 = transformer5;
        _transformer6 = transformer6;
    }

    protected override (T1, T2, T3, T4, T5, T6) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple6");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple6");
        var t4 = _helper.ParseElement(ref enumerator, _transformer4, 4, "ValueTuple6");
        var t5 = _helper.ParseElement(ref enumerator, _transformer5, 5, "ValueTuple6");
        var t6 = _helper.ParseElement(ref enumerator, _transformer6, 6, "ValueTuple6");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2, t3, t4, t5, t6);
    }

    public override string Format((T1, T2, T3, T4, T5, T6) element)
    {
        var initialBuffer = ArrayPool<char>.Shared.Rent(32);
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer4, element.Item4, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer5, element.Item5, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer6, element.Item6, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally
        {
            accumulator.Dispose();
            ArrayPool<char>.Shared.Return(initialBuffer);
        }
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty(),
            _transformer4.GetEmpty(),
            _transformer5.GetEmpty(),
            _transformer6.GetEmpty()
        );
}

public sealed class ValueTuple7Transformer<T1, T2, T3, T4, T5, T6, T7> : TransformerBase<(T1, T2, T3, T4, T5, T6, T7)>
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;
    private readonly ITransformer<T4> _transformer4;
    private readonly ITransformer<T5> _transformer5;
    private readonly ITransformer<T6> _transformer6;
    private readonly ITransformer<T7> _transformer7;

    private const byte ARITY = 7;

    public ValueTuple7Transformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3, ITransformer<T4> transformer4, ITransformer<T5> transformer5, ITransformer<T6> transformer6, ITransformer<T7> transformer7)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
        _transformer4 = transformer4;
        _transformer5 = transformer5;
        _transformer6 = transformer6;
        _transformer7 = transformer7;
    }

    protected override (T1, T2, T3, T4, T5, T6, T7) ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple7");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple7");
        var t4 = _helper.ParseElement(ref enumerator, _transformer4, 4, "ValueTuple7");
        var t5 = _helper.ParseElement(ref enumerator, _transformer5, 5, "ValueTuple7");
        var t6 = _helper.ParseElement(ref enumerator, _transformer6, 6, "ValueTuple7");
        var t7 = _helper.ParseElement(ref enumerator, _transformer7, 7, "ValueTuple7");

        _helper.ParseEnd(ref enumerator, ARITY);

        return (t1, t2, t3, t4, t5, t6, t7);
    }

    public override string Format((T1, T2, T3, T4, T5, T6, T7) element)
    {
        var initialBuffer = ArrayPool<char>.Shared.Rent(32);
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer4, element.Item4, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer5, element.Item5, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer6, element.Item6, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer7, element.Item7, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally
        {
            accumulator.Dispose();
            ArrayPool<char>.Shared.Return(initialBuffer);
        }
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6, T7> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty(),
            _transformer4.GetEmpty(),
            _transformer5.GetEmpty(),
            _transformer6.GetEmpty(),
            _transformer7.GetEmpty()
        );
}

public sealed class ValueTupleRestTransformer<T1, T2, T3, T4, T5, T6, T7, TRest> : TransformerBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>> where TRest : struct
{
    private readonly TupleHelper _helper;
    private readonly ITransformer<T1> _transformer1;
    private readonly ITransformer<T2> _transformer2;
    private readonly ITransformer<T3> _transformer3;
    private readonly ITransformer<T4> _transformer4;
    private readonly ITransformer<T5> _transformer5;
    private readonly ITransformer<T6> _transformer6;
    private readonly ITransformer<T7> _transformer7;
    private readonly ITransformer<TRest> _transformerRest;

    private const byte ARITY = 8;

    public ValueTupleRestTransformer(TupleHelper helper, ITransformer<T1> transformer1, ITransformer<T2> transformer2, ITransformer<T3> transformer3, ITransformer<T4> transformer4, ITransformer<T5> transformer5, ITransformer<T6> transformer6, ITransformer<T7> transformer7, ITransformer<TRest> transformerRest)
    {
        _helper = helper;
        _transformer1 = transformer1;
        _transformer2 = transformer2;
        _transformer3 = transformer3;
        _transformer4 = transformer4;
        _transformer5 = transformer5;
        _transformer6 = transformer6;
        _transformer7 = transformer7;
        _transformerRest = transformerRest;
    }

    protected override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);

        var t1 = _helper.ParseElement(ref enumerator, _transformer1);
        var t2 = _helper.ParseElement(ref enumerator, _transformer2, 2, "ValueTuple8");
        var t3 = _helper.ParseElement(ref enumerator, _transformer3, 3, "ValueTuple8");
        var t4 = _helper.ParseElement(ref enumerator, _transformer4, 4, "ValueTuple8");
        var t5 = _helper.ParseElement(ref enumerator, _transformer5, 5, "ValueTuple8");
        var t6 = _helper.ParseElement(ref enumerator, _transformer6, 6, "ValueTuple8");
        var t7 = _helper.ParseElement(ref enumerator, _transformer7, 7, "ValueTuple8");
        var tRest = _helper.ParseElement(ref enumerator, _transformerRest, 8, "ValueTuple8");

        _helper.ParseEnd(ref enumerator, ARITY);

        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(t1, t2, t3, t4, t5, t6, t7, tRest);
    }

    public override string Format(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> element)
    {
        var initialBuffer = ArrayPool<char>.Shared.Rent(32);
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(_transformer1, element.Item1, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer2, element.Item2, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer3, element.Item3, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer4, element.Item4, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer5, element.Item5, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer6, element.Item6, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformer7, element.Item7, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(_transformerRest, element.Rest, ref accumulator);

            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally
        {
            accumulator.Dispose();
            ArrayPool<char>.Shared.Return(initialBuffer);
        }
    }

    public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> GetEmpty() =>
        new(
            _transformer1.GetEmpty(),
            _transformer2.GetEmpty(),
            _transformer3.GetEmpty(),
            _transformer4.GetEmpty(),
            _transformer5.GetEmpty(),
            _transformer6.GetEmpty(),
            _transformer7.GetEmpty(),
            _transformerRest.GetEmpty()
        );
}
