using System;
using JetBrains.Annotations;
using Sett = Nemesis.TextParsers.Parsers.DeconstructionTransformerSettings;

namespace Nemesis.TextParsers.Parsers
{
    [UsedImplicitly]
    public sealed class DeconstructionTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TDeconstructable> CreateTransformer<TDeconstructable>() 
            => Sett.Default.ToTransformer<TDeconstructable>();

        public bool CanHandle(Type type) => Sett.TryGetDefaultDeconstruct(type, out _, out _);

        public sbyte Priority => 120;
    }
}
