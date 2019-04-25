using System;
using System.Globalization;

namespace Nemesis.TextParsers
{
    public interface ISpanParser<out TTo> { TTo Parse(ReadOnlySpan<char> input); }
    public interface IFormatter<in TElement> { string Format(TElement element); }
    public interface ITransformer<TElement> : ISpanParser<TElement>, IFormatter<TElement> { }

    public interface ICanTransform
    {
        bool CanHandle(Type type);
    }
    public interface ICanTransformType : ICanTransform
    {
        Type Type { get; }
    }
    public interface ICanCreateTransformer : ICanTransform
    {
        sbyte Priority { get; }
        ITransformer<TElement> CreateTransformer<TElement>();
    }
    
    public sealed class FormattableFormatter<TElement> : IFormatter<TElement> where TElement : IFormattable
    {
        public string Format(TElement element) =>
            element?.ToString(null, CultureInfo.InvariantCulture);
    }

    public sealed class NormalFormatter<TElement> : IFormatter<TElement>
    {
        public string Format(TElement element) => element?.ToString();
    }

    public sealed class CompositionTransformer<TElement> : ITransformer<TElement>
    {
        private readonly ISpanParser<TElement> _parser;
        private readonly IFormatter<TElement> _formatter;

        public CompositionTransformer(ISpanParser<TElement> parser, IFormatter<TElement> formatter)
        {
            _parser = parser;
            _formatter = formatter;
        }

        public TElement Parse(ReadOnlySpan<char> input) => _parser.Parse(input);

        public string Format(TElement element) => _formatter.Format(element);

        public override string ToString() => _parser?.ToString() ?? "";
    }
}
