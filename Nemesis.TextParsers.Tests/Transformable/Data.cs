using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Tests.Transformable;

[Transformer(typeof(ParsleyAndLeekFactorsTransformer))]
internal readonly struct ParsleyAndLeekFactors : IEquatable<ParsleyAndLeekFactors>
{
    public float Parsley { get; }
    public float[] LeekFactors { get; }

    public ParsleyAndLeekFactors(float parsley, float[] leekFactors)
    {
        Parsley = parsley;
        LeekFactors = leekFactors;
    }

    public override string ToString() => FormattableString.Invariant(
        $"{Parsley:G9};{(LeekFactors == null ? "∅" : string.Join(",", LeekFactors.Select(of => of.ToString("G9", CultureInfo.InvariantCulture))))}"
    );


    public bool Equals(ParsleyAndLeekFactors other) =>
        Parsley.Equals(other.Parsley) &&
        EnumerableEqualityComparer<float>.DefaultInstance.Equals(LeekFactors, other.LeekFactors);

    public override bool Equals(object obj) => obj is ParsleyAndLeekFactors other && Equals(other);

    public override int GetHashCode() =>
        unchecked((Parsley.GetHashCode() * 397) ^ (LeekFactors?.GetHashCode() ?? 0));
}

[UsedImplicitly]
internal sealed class ParsleyAndLeekFactorsTransformer : TransformerBase<ParsleyAndLeekFactors>
{
    protected override ParsleyAndLeekFactors ParseCore(in ReadOnlySpan<char> text)
    {
        var stream = text.Split(';').GetEnumerator();
        var floatParser = SingleTransformer.Instance;

        if (!stream.MoveNext())
            throw new FormatException($"At least one element is expected to parse {nameof(ParsleyAndLeekFactors)}");
        float parsley = floatParser.Parse(stream.Current);

        if (!stream.MoveNext())
            throw new FormatException($"Second element is expected to parse {nameof(ParsleyAndLeekFactors)}");

        var current = stream.Current;

        ParsleyAndLeekFactors Parse(ReadOnlySpan<char> span)
        {
            byte leekCount = 0;
            Span<float> leekFactors = stackalloc float[16];
            var leekStream = span.Split(',', true).GetEnumerator();
            while (leekStream.MoveNext())
                leekFactors[leekCount++] = floatParser.Parse(leekStream.Current);

            return new ParsleyAndLeekFactors(parsley, leekFactors.Slice(0, leekCount).ToArray());
        }

        return current.Length switch
        {
            0 => new ParsleyAndLeekFactors(parsley, new float[0]),
            1 when current[0] == '∅' => new ParsleyAndLeekFactors(parsley, null),
            _ => Parse(current)
        };
    }

    public override string Format(ParsleyAndLeekFactors element) => element.ToString();

    public override ParsleyAndLeekFactors GetEmpty() =>
        new(10, new[] { 20.0f, 30.0f });

    public override ParsleyAndLeekFactors GetNull() =>
        new(0, new[] { 0f, 0f });
}



//CustomList illustrates various aspects
//1. being able to transform interfaces/base classes 
[Transformer(typeof(CustomListTransformer<>))]
internal interface ICustomList<TElement> : IEnumerable<TElement>, IEquatable<ICustomList<TElement>>
{
    bool IsNullContent { get; }
}

//2. concrete implementation does not need to register transformer, but it will only "inherit" transformers from it's base types, not interfaces 
internal class CustomList<TElement> : ICustomList<TElement>, IEquatable<ICustomList<TElement>>
{
    private readonly IReadOnlyCollection<TElement> _collection;
    public bool IsNullContent => _collection == null;

    public CustomList(IReadOnlyCollection<TElement> collection) => _collection = collection;

    public IEnumerator<TElement> GetEnumerator() => (_collection ?? Enumerable.Empty<TElement>()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(ICustomList<TElement> other) =>
        EnumerableEqualityComparer<TElement>.DefaultInstance.Equals(this, other);

    public override bool Equals(object obj) =>
        obj is not null && (ReferenceEquals(this, obj) || obj is CustomList<TElement> list && Equals(list));

    public override int GetHashCode() => _collection?.GetHashCode() ?? 0;
}

internal class CustomListTransformer<TElement> : TransformerBase<ICustomList<TElement>>
{
    //3. Transformable aspect support simple injection of ITransformerStore via it's constructor 
    private readonly ITransformerStore _transformerStore;
    public CustomListTransformer(ITransformerStore transformerStore) => _transformerStore = transformerStore;


    protected override ICustomList<TElement> ParseCore(in ReadOnlySpan<char> text)
    {
        //4. since we are parsing interface, implementers need to provide way to choose concrete implementation
        //i.e. by saving type name if front of serialized message 
        //Here we take one type to simplify implementation 
        if (text.IsEmpty)
            return new CustomList<TElement>(Array.Empty<TElement>());

        var stream = text.Split(';').GetEnumerator();
        var parser = _transformerStore.GetTransformer<TElement>();

        var initialBuffer = ArrayPool<TElement>.Shared.Rent(16);
        try
        {
            using var accumulator = new ValueSequenceBuilder<TElement>(initialBuffer);

            while (stream.MoveNext())
                accumulator.Append(parser.Parse(stream.Current));

            return new CustomList<TElement>(accumulator.AsSpan().ToArray());
        }
        finally
        {
            ArrayPool<TElement>.Shared.Return(initialBuffer);
        }
    }

    public override string Format(ICustomList<TElement> list) => list.IsNullContent ? null : string.Join("; ", list);


    public override ICustomList<TElement> GetEmpty() => new CustomList<TElement>(Array.Empty<TElement>());

    public override ICustomList<TElement> GetNull() => new CustomList<TElement>(null);
}
