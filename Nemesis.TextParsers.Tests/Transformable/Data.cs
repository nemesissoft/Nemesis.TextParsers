using System;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests.Transformable
{
    [Transformer(typeof(ParsleyAndLeekFactorsTransformer))]
    internal struct ParsleyAndLeekFactors : IEquatable<ParsleyAndLeekFactors>
    {
        public float Parsley { get; }
        public float[] LeekFactors { get; }

        public ParsleyAndLeekFactors(float parsley, float[] leekFactors)
        {
            Parsley = parsley;
            LeekFactors = leekFactors;
        }

        public override string ToString() => FormattableString.Invariant(
            $"{Parsley:G9};{(LeekFactors == null ? "∅" : string.Join(",", LeekFactors.Select(of => of.ToString("G9", CultureInfo.InvariantCulture))))}"
        );


        public bool Equals(ParsleyAndLeekFactors other) =>
            Parsley.Equals(other.Parsley) &&
            EnumerableEqualityComparer<float>.DefaultInstance.Equals(LeekFactors, other.LeekFactors);

        public override bool Equals(object obj) => obj is ParsleyAndLeekFactors other && Equals(other);

        public override int GetHashCode() =>
            unchecked((Parsley.GetHashCode() * 397) ^ (LeekFactors?.GetHashCode() ?? 0));
    }

    [UsedImplicitly]
    internal sealed class ParsleyAndLeekFactorsTransformer : TransformerBase<ParsleyAndLeekFactors>
    {
        protected override ParsleyAndLeekFactors ParseCore(in ReadOnlySpan<char> text)
        {
            var stream = text.Split(';').GetEnumerator();
            var floatParser = SingleParser.Instance;

            if (!stream.MoveNext())
                throw new FormatException($"At least one element is expected to parse {nameof(ParsleyAndLeekFactors)}");
            float parsley = floatParser.Parse(stream.Current);

            if (!stream.MoveNext())
                throw new FormatException($"Second element is expected to parse {nameof(ParsleyAndLeekFactors)}");

            var current = stream.Current;

            ParsleyAndLeekFactors Parse(ReadOnlySpan<char> span)
            {
                byte leekCount = 0;
                Span<float> leekFactors = stackalloc float[16];
                var leekStream = span.Split(',', true).GetEnumerator();
                while (leekStream.MoveNext())
                    leekFactors[leekCount++] = floatParser.Parse(leekStream.Current);

                return new ParsleyAndLeekFactors(parsley, leekFactors.Slice(0, leekCount).ToArray());
            }

            return current.Length switch
            {
                0 => new ParsleyAndLeekFactors(parsley, new float[0]),
                1 when current[0] == '∅' => new ParsleyAndLeekFactors(parsley, null),
                _ => Parse(current)
            };
        }

        public override string Format(ParsleyAndLeekFactors element) => element.ToString();

        public override ParsleyAndLeekFactors GetEmpty() =>
            new ParsleyAndLeekFactors(10, new[] { 20.0f, 30.0f });

        public override ParsleyAndLeekFactors GetNull() =>
            new ParsleyAndLeekFactors(0, new[] { 0f, 0f });
    }
}
