using System;
using JetBrains.Annotations;
using S = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>() 
            => S.Default.ToTransformer<TDeconstructable>();

        public bool CanHandle(Type type) => S.TryGetDefaultDeconstruct(type, out _, out _);

        public sbyte Priority => 120;
    }
}
