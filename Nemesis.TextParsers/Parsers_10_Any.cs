using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class AnyTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>()
        {
            var typeConverter = TypeDescriptor.GetConverter(typeof(TElement));

            return typeConverter is ITransformer<TElement> transformer ?
                transformer :
                (
                    typeConverter.GetType() != typeof(TypeConverter) &&
                    typeConverter.CanConvertFrom(typeof(string)) && typeConverter.CanConvertTo(typeof(string)) 
                        ? new ConverterTransformer<TElement>(typeConverter) 
                        : throw new NotSupportedException($"{typeof(TElement).GetFriendlyName()} is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself")
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

            public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()} using {nameof(TypeConverter)}";
        }

        public bool CanHandle(Type type) => true;

        public sbyte Priority => 127;
    }
}
