using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class AnyTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>()
        {
            var typeConverter = TypeDescriptor.GetConverter(typeof(TElement));

            return typeConverter is ITransformer<TElement> transformer ?
                transformer :
                (
                    typeConverter.CanConvertFrom(typeof(string)) && typeConverter.CanConvertTo(typeof(string)) ?
                    new ConverterTransformer<TElement>(typeConverter) :
                    throw new NotSupportedException($"{typeof(TElement).Name} is not supported for parsing. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string")
                );
        }

        private sealed class ConverterTransformer<TElement> : TransformerBase<TElement>
        {
            private readonly TypeConverter _typeConverter;
            public ConverterTransformer(TypeConverter typeConverter) => _typeConverter = typeConverter;


            public override TElement Parse(ReadOnlySpan<char> input) =>
                (TElement)_typeConverter.ConvertFromInvariantString(input.ToString());

            public override string Format(TElement element) =>
                _typeConverter.ConvertToInvariantString(element);

            public override string ToString() => $"Transform {typeof(TElement).Name} using {nameof(TypeConverter)}";
        }

        public bool CanHandle(Type type) => true;

        public sbyte Priority => 127;
    }
}
