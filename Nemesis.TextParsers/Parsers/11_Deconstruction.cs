using System;
using JetBrains.Annotations;
using S = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>()
        {
            var type = typeof(TDeconstructable);

            if (!S.TryGetDefaultDeconstruct(type, out var genesisPair) ||
                genesisPair.deconstruct is null ||
                genesisPair.ctor is null ||
                genesisPair.deconstruct.GetParameters().Length == 0 ||
                genesisPair.ctor.GetParameters().Length == 0
               )
                throw new NotSupportedException($"{nameof(DeconstructionTransformerCreator)} supports cases with at lease one non-nullary {S.DECONSTRUCT} method with matching constructor");

            var transType = typeof(DeconstructionTransformer<>).MakeGenericType(type);

            return (ITransformer<TDeconstructable>)Activator
                .CreateInstance(transType, genesisPair.deconstruct, genesisPair.ctor);
        }

        public bool CanHandle(Type type) => S.TryGetDefaultDeconstruct(type, out _);

        public sbyte Priority => 120;
    }
}
