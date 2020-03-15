using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Nemesis.TextParsers.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    internal sealed class AnyTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TElement> CreateTransformer<TElement>() =>
            TypeDescriptor.GetConverter(typeof(TElement)) switch
            {
                ITransformer<TElement> t => t,

                var t1 when t1.GetType() == typeof(TypeConverter) => throw new NotSupportedException(
                    $"{typeof(TElement).GetFriendlyName()} is not supported for text transformation. Type converter should be a subclass of TypeConverter but must not be TypeConverter itself"
                ),

                var t2 when t2.CanConvertFrom(typeof(string)) && t2.CanConvertTo(typeof(string)) =>
                    new ConverterTransformer<TElement>(t2),

                _ => throw new NotSupportedException(
                    $"{typeof(TElement).GetFriendlyName()} is not supported for text transformation. Create appropriate chain of responsibility pattern element or provide a TypeConverter that can parse from/to string"
                )
            };

        private sealed class ConverterTransformer<TElement> : TransformerBase<TElement>
        {
            private readonly TypeConverter _typeConverter;
            public ConverterTransformer(TypeConverter typeConverter) => _typeConverter = typeConverter;


            public override TElement Parse(in ReadOnlySpan<char> input) =>
                (TElement)_typeConverter.ConvertFromInvariantString(input.ToString());

            public override string Format(TElement element) =>
                element is null
                    ? null
                    : _typeConverter.ConvertToInvariantString(element);

            public override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()} using {_typeConverter?.GetType().GetFriendlyName()}";
        }

        public bool CanHandle(Type type) => true;

        public sbyte Priority => 127;
    }
}
