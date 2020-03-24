using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    public interface ITransformer
    {
        object ParseObject(string text);
        [UsedImplicitly]
        object ParseObject(in ReadOnlySpan<char> input);


        string FormatObject(object element);


        object GetNullObject();
        object GetEmptyObject();
    }

    public interface ISpanParser<out TElement>
    {
        TElement Parse(in ReadOnlySpan<char> input);
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
        public abstract TElement Parse(in ReadOnlySpan<char> input);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        TElement ITransformer<TElement>.Parse(string text) => text == null ? default : Parse(text.AsSpan());

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ParseObject(string text) => text == null ? default : Parse(text.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ParseObject(in ReadOnlySpan<char> input) => Parse(input);
        /*=>
                    text switch
                    {
                        null => default,
                        "" => GetEmpty(),
                        _ => Parse(text.AsSpan())
                    };*/



        public abstract string Format(TElement element);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string FormatObject(object element) => Format((TElement)element);

        


        public virtual TElement GetEmpty() => default;
        public object GetEmptyObject() => GetEmpty();



        public virtual TElement GetNull() => default;
        public object GetNullObject() => GetNull();
    }

    public interface ICanCreateTransformer
    {
        bool CanHandle(Type type);
        sbyte Priority { get; }
        ITransformer<TElement> CreateTransformer<TElement>();
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

        public CompositionTransformer([NotNull] ISpanParser<TElement> parser,
            [NotNull] IFormatter<TElement> formatter,
            Func<TElement> emptyValueProvider = null)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _emptyValueProvider = emptyValueProvider;
        }

        public override TElement Parse(in ReadOnlySpan<char> input) => _parser.Parse(input);

        public override string Format(TElement element) => _formatter.Format(element);

        public override TElement GetEmpty() =>
            _emptyValueProvider != null ? _emptyValueProvider() : base.GetEmpty();


        public override string ToString() => $"{_parser?.ToString() ?? ""};{_formatter?.ToString() ?? ""}";
    }
}
