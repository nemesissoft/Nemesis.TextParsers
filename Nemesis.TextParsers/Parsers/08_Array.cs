using System;
using System.Buffers;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

[UsedImplicitly]
public sealed class ArrayTransformerCreator : ICanCreateTransformer
{
    private readonly ITransformerStore _transformerStore;
    private readonly ArraySettings _settings;
    public ArrayTransformerCreator(ITransformerStore transformerStore, ArraySettings settings)
    {
        _transformerStore = transformerStore;
        _settings = settings;
    }


    public ITransformer<TArray> CreateTransformer<TArray>()
    {
        if (TryGetElements(typeof(TArray), out var elementType) &&
            _transformerStore.IsSupportedForTransformation(elementType))
        {
            var createMethod = Method.OfExpression<
                            Func<ArrayTransformerCreator, ITransformer<int[]>>
                        >(@this => @this.CreateArrayTransformer<int>()
                        ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(elementType);

            return (ITransformer<TArray>)createMethod.Invoke(this, null);
        }
        else if (TryGetArraySegmentElements(typeof(TArray), out var arraySegmentElementType) &&
                 _transformerStore.IsSupportedForTransformation(arraySegmentElementType))
        {
            var createMethod = Method.OfExpression<
                Func<ArrayTransformerCreator, ITransformer<ArraySegment<int>>>
            >(@this => @this.CreateArraySegmentTransformer<int>()
            ).GetGenericMethodDefinition();

            createMethod = createMethod.MakeGenericMethod(arraySegmentElementType);

            return (ITransformer<TArray>)createMethod.Invoke(this, null);
        }
        else
            throw new NotSupportedException($"Type {typeof(TArray).GetFriendlyName()} is not supported by {GetType().Name}");
    }

    private ITransformer<TElement[]> CreateArrayTransformer<TElement>() =>
        new ArrayTransformer<TElement>(
            _transformerStore.GetTransformer<TElement>(),
            _settings
    );

    private ITransformer<ArraySegment<TElement>> CreateArraySegmentTransformer<TElement>() =>
        new ArraySegmentTransformer<TElement>(_transformerStore.GetTransformer<TElement[]>());



    public bool CanHandle(Type type) =>
        TryGetElements(type, out var elementType) &&
        _transformerStore.IsSupportedForTransformation(elementType)
        ||
        TryGetArraySegmentElements(type, out var arraySegmentElementType) &&
        _transformerStore.IsSupportedForTransformation(arraySegmentElementType)
    ;

    private static bool TryGetElements(Type type, out Type elementType)
    {
        if (type.IsArray &&
            type.GetArrayRank() == 1 /*do not support multi dimension arrays - jagged arrays should be preferred anyway */
        )
        {
            elementType = type.GetElementType();
            return true;
        }
        else
        {
            elementType = null;
            return false;
        }
    }

    private static bool TryGetArraySegmentElements(Type type, out Type elementType)
    {
        if (type.IsValueType && type.IsGenericType &&
            TypeMeta.TryGetGenericRealization(type, typeof(ArraySegment<>), out var arraySegmentType))
        {
            elementType = arraySegmentType.GenericTypeArguments[0];
            return true;
        }
        else
        {
            elementType = default;
            return false;
        }
    }

    public sbyte Priority => 60;

    public override string ToString() =>
        $"Create transformer for array with settings:{_settings}";
}

public sealed class ArrayTransformer<TElement> : EnumerableTransformerBase<TElement, TElement[]>
{
    public ArrayTransformer(ITransformer<TElement> elementTransformer, ArraySettings settings)
        : base(elementTransformer, settings) { }

    protected override TElement[] ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return Array.Empty<TElement>();

        var stream = ParseStream(input);

        int capacity = Settings.GetCapacity(input);

        var initialBuffer = ArrayPool<TElement>.Shared.Rent(capacity);
        var accumulator = new ValueSequenceBuilder<TElement>(initialBuffer);
        try
        {
            foreach (var part in stream)
                accumulator.Append(part.ParseWith(ElementTransformer));

            return accumulator.AsSpan().ToArray();
        }
        finally
        {
            accumulator.Dispose();
            ArrayPool<TElement>.Shared.Return(initialBuffer);
        }
    }


    public override TElement[] GetEmpty() => Array.Empty<TElement>();
}

internal class ArraySegmentTransformer
{
    internal static readonly TupleHelper Helper = new('@', '∅', '~', '{', '}');
}
public class ArraySegmentTransformer<TElement> : SimpleTransformer<ArraySegment<TElement>>
{
    private const string TYPE_NAME = "ArraySegment";

    private readonly ITransformer<TElement[]> _arrayTransformer;
    public ArraySegmentTransformer(ITransformer<TElement[]> arrayTransformer) => _arrayTransformer = arrayTransformer;


    protected override ArraySegment<TElement> ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;

        var helper = ArraySegmentTransformer.Helper;

        var enumerator = helper.ParseStart(input, 3, TYPE_NAME);

        int offset = helper.ParseElement(ref enumerator, Int32Transformer.Instance);

        helper.ParseNext(ref enumerator, 2, TYPE_NAME);
        int count = helper.ParseElement(ref enumerator, Int32Transformer.Instance);

        helper.ParseNext(ref enumerator, 3, TYPE_NAME);
        var array = helper.ParseElement(ref enumerator, _arrayTransformer);

        helper.ParseEnd(ref enumerator, 3, TYPE_NAME);

        return new ArraySegment<TElement>(array ?? Array.Empty<TElement>(), offset, count); //array cannot be null for ArraySegment
    }

    public override string Format(ArraySegment<TElement> segment)
    {
        if (segment == default || segment.Array is null) return "";

        var helper = ArraySegmentTransformer.Helper;

        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            helper.StartFormat(ref accumulator);

            helper.FormatElement(Int32Transformer.Instance, segment.Offset, ref accumulator);
            helper.AddDelimiter(ref accumulator);

            helper.FormatElement(Int32Transformer.Instance, segment.Count, ref accumulator);
            helper.AddDelimiter(ref accumulator);

            helper.FormatElement(_arrayTransformer, segment.Array, ref accumulator);

            helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    //public override ArraySegment<TElement> GetEmpty() => new(Array.Empty<TElement>());
}
