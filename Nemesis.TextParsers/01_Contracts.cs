using System.Globalization;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers;

public interface ITransformer
{
    object ParseObject(string text);
    object ParseObject(in ReadOnlySpan<char> input);
    bool TryParseObject(in ReadOnlySpan<char> input, out object result);


    string FormatObject(object element);


    object GetNullObject();
    object GetEmptyObject();
}

public interface ISpanParser<TElement>
{
    TElement Parse(in ReadOnlySpan<char> input);
    bool TryParse(in ReadOnlySpan<char> input, out TElement result);
}

public interface IFormatter<in TElement>
{
    string Format(TElement element);
}

public interface ITransformer<TElement> : ISpanParser<TElement>, IFormatter<TElement>, ITransformer
{
    TElement Parse(string text);

    TElement GetNull();
    TElement GetEmpty();
}

public abstract class TransformerBase<TElement> : ITransformer<TElement>
{
    protected abstract TElement ParseCore(in ReadOnlySpan<char> input);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TElement Parse(in ReadOnlySpan<char> input) => input.IsEmpty ? GetEmpty() : ParseCore(input);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object ParseObject(in ReadOnlySpan<char> input) => input.IsEmpty ? GetEmpty() : ParseCore(input);




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual TElement ParseText(string text) => ParseCore(text.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TElement Parse(string text) => text switch
    {
        null => GetNull(),
        "" => GetEmpty(),
        _ => ParseText(text)
    };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object ParseObject(string text) => text switch
    {
        null => GetNull(),
        "" => GetEmpty(),
        _ => ParseText(text)
    };



    public bool TryParseObject(in ReadOnlySpan<char> input, out object result)
    {
        try
        {
            result = ParseObject(input);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public virtual bool TryParse(in ReadOnlySpan<char> input, out TElement result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }



    public abstract string Format(TElement element);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string FormatObject(object element) => Format((TElement)element);




    public virtual TElement GetEmpty() => default;
    public object GetEmptyObject() => GetEmpty();



    public virtual TElement GetNull() => default;
    public object GetNullObject() => GetNull();

    public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()}";
}

public interface ITransformerHandler
{
    bool CanHandle(Type type);
    sbyte Priority { get; }
    ITransformer<TElement> CreateTransformer<TElement>();
    string DescribeHandlerMatch();
}

public sealed class FormattableFormatter<TElement> : IFormatter<TElement> where TElement : IFormattable
{
    public string Format(TElement element) =>
        element?.ToString(null, CultureInfo.InvariantCulture);

    public override string ToString() => $"Format {typeof(TElement).GetFriendlyName()} based on ToString(null, CultureInfo.InvariantCulture)";
}

public sealed class NormalFormatter<TElement> : IFormatter<TElement>
{
    public string Format(TElement element) => element?.ToString();

    public override string ToString() => $"Format {typeof(TElement).GetFriendlyName()} based on ToString()";
}

public sealed class CompositionTransformer<TElement> : TransformerBase<TElement>
{
    private readonly ISpanParser<TElement> _parser;
    private readonly IFormatter<TElement> _formatter;
    private readonly Func<TElement> _emptyValueProvider;
    private readonly Func<TElement> _nullValueProvider;

    public CompositionTransformer([NotNull] ISpanParser<TElement> parser,
        [NotNull] IFormatter<TElement> formatter,
        Func<TElement> emptyValueProvider = null, Func<TElement> nullValueProvider = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _emptyValueProvider = emptyValueProvider;
        _nullValueProvider = nullValueProvider;
    }

    protected override TElement ParseCore(in ReadOnlySpan<char> input) => _parser.Parse(input);

    public override string Format(TElement element) => _formatter.Format(element);

    public override TElement GetEmpty() =>
        _emptyValueProvider != null ? _emptyValueProvider() : base.GetEmpty();

    public override TElement GetNull() =>
        _nullValueProvider != null ? _nullValueProvider() : base.GetNull();

    public override string ToString() => $"{_parser?.ToString() ?? ""};{_formatter?.ToString() ?? ""}";
}
