using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    [UsedImplicitly]
    public sealed class CustomCollectionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TCollection> CreateTransformer<TCollection>()
        {
            var collectionType = typeof(TCollection);

            var genericInterfaceType =
                collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(ICollection<>)
                    ? collectionType
                    : TypeMeta.GetConcreteInterfaceOfType(collectionType, typeof(ICollection<>))
                      ?? throw new InvalidOperationException("Type has to be or implement ICollection<>");

            var elementType = genericInterfaceType.GenericTypeArguments[0];

            var transType = typeof(InnerCustomCollectionTransformer<,>).MakeGenericType(elementType, collectionType);

            return (ITransformer<TCollection>)Activator.CreateInstance(transType);
        }

        private class InnerCustomCollectionTransformer<TElement, TCollection> : ITransformer<TCollection>, IParser<TCollection>
            where TCollection : ICollection<TElement>, new()
        {
            TCollection IParser<TCollection>.ParseText(string input) => Parse(input.AsSpan());

            public TCollection Parse(ReadOnlySpan<char> input) //input.IsEmpty ? default :
            {
                var stream = SpanCollectionSerializer.DefaultInstance.ParseStream<TElement>(input);
                var result = new TCollection();

                foreach (var element in stream)
                    result.Add(element);

                return result;
            }

            public string Format(TCollection coll) => //coll == null ? null :
                SpanCollectionSerializer.DefaultInstance.FormatCollection(coll);

            public override string ToString() => $"Transform custom {typeof(TCollection).GetFriendlyName()} with {typeof(TElement).GetFriendlyName()} elements";
        }

        public bool CanHandle(Type type) =>
            type.DerivesOrImplementsGeneric(typeof(ICollection<>)) &&
            type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;

        public sbyte Priority => 72;
    }
}
