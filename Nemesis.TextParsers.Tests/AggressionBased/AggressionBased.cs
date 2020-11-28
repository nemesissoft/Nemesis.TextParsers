using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
#if !NET
using NotNull = JetBrains.Annotations.NotNullAttribute;
#else
using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#endif

// ReSharper disable once CheckNamespace
namespace Nemesis.TextParsers.Tests
{
    //TODO add documentation 
    [PublicAPI]
    public interface IAggressionBased
    {
        int Arity { get; }
    }

    [Transformer(typeof(AggressionBasedTransformer<>))]
    [PublicAPI]
    public interface IAggressionBased<TValue> : IAggressionBased
    {
        TValue PassiveValue { get; }
        TValue NormalValue { get; }
        TValue AggressiveValue { get; }

        TValue GetValueFor(StrategyAggression aggression);
        TValue GetValueFor(byte aggression);

        LeanCollection<TValue> GetValues();
    }

    [Transformer(typeof(AggressionBasedTransformer<>))]
    internal abstract class AggressionBasedBase<TValue> : IEquatable<IAggressionBased<TValue>>
    {
        public abstract LeanCollection<TValue> GetValues();

        protected static bool IsStructurallyEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);

        public bool Equals(IAggressionBased<TValue> other) =>
            other switch
            {
                null => false,
                var ab when ReferenceEquals(this, ab) => true,
                AggressionBased1<TValue> o1 => Equals1(o1),
                AggressionBased3<TValue> o3 => Equals3(o3),
                AggressionBased9<TValue> o9 => Equals9(o9),
                _ => throw new ArgumentException($@"'{nameof(other)}' argument has to be {nameof(IAggressionBased<TValue>)}", nameof(other)),
            };

        protected abstract bool Equals1(in AggressionBased1<TValue> o1);

        protected abstract bool Equals3(in AggressionBased3<TValue> o3);

        protected abstract bool Equals9(in AggressionBased9<TValue> o9);

        public sealed override bool Equals(object obj) => obj != null &&
            (ReferenceEquals(this, obj) || obj is IAggressionBased<TValue> ab && Equals(ab));

        public sealed override int GetHashCode() => GetHashCodeCore();
        protected abstract int GetHashCodeCore();

        /// <summary>
        /// Text representation. For debugging purposes only
        /// </summary>
        public sealed override string ToString() => string.Join(" # ", GetValues().ToList());
    }

    internal sealed class AggressionBased1<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 1;
        public TValue One { get; }

        public override LeanCollection<TValue> GetValues() => new(One);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue PassiveValue => One;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue NormalValue => One;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue AggressiveValue => One;

        public AggressionBased1(TValue one) => One = one;

        public TValue GetValueFor(StrategyAggression aggression) => One;

        public TValue GetValueFor(byte aggression) => One;

        protected override bool Equals1(in AggressionBased1<TValue> o1) =>
            IsStructurallyEqual(One, o1.One);

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(One, o3.PassiveValue) &&
            IsStructurallyEqual(One, o3.NormalValue) &&
            IsStructurallyEqual(One, o3.AggressiveValue);

        protected override bool Equals9(in AggressionBased9<TValue> o9) => o9.Equals(this);

        protected override int GetHashCodeCore() => One?.GetHashCode() ?? 0;
    }

    internal sealed class AggressionBased3<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 3;

        public override LeanCollection<TValue> GetValues() => new(PassiveValue, NormalValue, AggressiveValue);

        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public AggressionBased3(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }

        public TValue GetValueFor(StrategyAggression aggression) => GetValueFor((byte)aggression);

        public TValue GetValueFor(byte aggression) =>
            aggression switch
            {
                1 or 2 or 3 => PassiveValue,
                0 or 4 or 5 or 6 => NormalValue,
                7 or 8 or 9 => AggressiveValue,
                _ => throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9"),
            };

        protected override bool Equals1(in AggressionBased1<TValue> o1) =>
            IsStructurallyEqual(PassiveValue, o1.One) &&
            IsStructurallyEqual(NormalValue, o1.One) &&
            IsStructurallyEqual(AggressiveValue, o1.One);

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(PassiveValue, o3.PassiveValue) &&
            IsStructurallyEqual(NormalValue, o3.NormalValue) &&
            IsStructurallyEqual(AggressiveValue, o3.AggressiveValue);

        protected override bool Equals9(in AggressionBased9<TValue> o9) =>
            IsStructurallyEqual(PassiveValue, o9.GetValueFor(1)) && IsStructurallyEqual(PassiveValue, o9.GetValueFor(2)) && IsStructurallyEqual(PassiveValue, o9.GetValueFor(3)) &&
            IsStructurallyEqual(NormalValue, o9.GetValueFor(4)) && IsStructurallyEqual(NormalValue, o9.GetValueFor(5)) && IsStructurallyEqual(NormalValue, o9.GetValueFor(6)) &&
            IsStructurallyEqual(AggressiveValue, o9.GetValueFor(7)) && IsStructurallyEqual(AggressiveValue, o9.GetValueFor(8)) && IsStructurallyEqual(AggressiveValue, o9.GetValueFor(9));

        protected override int GetHashCodeCore()
        {
            unchecked
            {
                int hashCode = PassiveValue?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (NormalValue?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (AggressiveValue?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    internal sealed class AggressionBased9<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 9;

        private readonly TValue[] _values;

        public override LeanCollection<TValue> GetValues() => LeanCollectionFactory.FromArrayChecked(_values);

        public TValue PassiveValue => GetValueFor(StrategyAggression.Passive);
        public TValue NormalValue => GetValueFor(StrategyAggression.Normal);
        public TValue AggressiveValue => GetValueFor(StrategyAggression.Aggressive);

        // ReSharper disable once RedundantVerbatimPrefix
        public AggressionBased9([@NotNull] TValue[] values) => _values = values ?? throw new ArgumentNullException(nameof(values));

        public TValue GetValueFor(StrategyAggression aggression) => GetValueFor((byte)aggression);

        public TValue GetValueFor(byte aggression)
        {
            if (_values == null || _values.Length != 9) throw new InvalidOperationException("Internal state of values is compromised");

            aggression = aggression switch
            {
                > 9 => throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9"),
                0 => 5,
                _ => aggression
            };

            return _values[aggression - 1];
        }

        protected override bool Equals1(in AggressionBased1<TValue> o1)
        {
            var thisOne = o1.One;
            return _values?.All(v => IsStructurallyEqual(v, thisOne)) == true;
        }

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(o3.PassiveValue, GetValueFor(1)) && IsStructurallyEqual(o3.PassiveValue, GetValueFor(2)) && IsStructurallyEqual(o3.PassiveValue, GetValueFor(3)) &&
            IsStructurallyEqual(o3.NormalValue, GetValueFor(4)) && IsStructurallyEqual(o3.NormalValue, GetValueFor(5)) && IsStructurallyEqual(o3.NormalValue, GetValueFor(6)) &&
            IsStructurallyEqual(o3.AggressiveValue, GetValueFor(7)) && IsStructurallyEqual(o3.AggressiveValue, GetValueFor(8)) && IsStructurallyEqual(o3.AggressiveValue, GetValueFor(9))
        ;

        protected override bool Equals9(in AggressionBased9<TValue> o9)
        {
            var o9Values = o9._values;

            if (ReferenceEquals(_values, o9Values)) return true;

            using var enumerator = ((IReadOnlyList<TValue>)_values).GetEnumerator();
            using var enumerator2 = ((IReadOnlyList<TValue>)o9Values).GetEnumerator();
            while (enumerator.MoveNext())
                if (!enumerator2.MoveNext() || !IsStructurallyEqual(enumerator.Current, enumerator2.Current))
                    return false;

            return !enumerator2.MoveNext();
        }

        protected override int GetHashCodeCore() => _values?.GetHashCode() ?? 0;
    }


    [TextConverterSyntax("Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3", '#')]
    public sealed class AggressionBasedTransformer<TValue> : TransformerBase<IAggressionBased<TValue>>
    {
        private const char LIST_DELIMITER = '#';
        private const char NULL_ELEMENT_MARKER = '∅';
        private const char ESCAPING_SEQUENCE_START = '\\';

        private readonly ITransformerStore _transformerStore;
        private readonly ITransformer<TValue> _elementTransformer;
        public AggressionBasedTransformer(ITransformerStore transformerStore)
        {
            _transformerStore = transformerStore;
            _elementTransformer = _transformerStore.GetTransformer<TValue>();
        }

        protected override IAggressionBased<TValue> ParseCore(in ReadOnlySpan<char> input)
        {
            var tokens = input.Tokenize(LIST_DELIMITER, ESCAPING_SEQUENCE_START, true);
            var parsed = tokens.PreParse(ESCAPING_SEQUENCE_START, NULL_ELEMENT_MARKER, LIST_DELIMITER);

            return FromValues(parsed);
        }

        public override string Format(IAggressionBased<TValue> ab)
        {
            if (ab == null) return null;

            Span<char> initialBuffer = stackalloc char[32];
            using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            var enumerator = ab.GetValues().GetEnumerator();
            while (enumerator.MoveNext())
            {
                string elementText = _elementTransformer.Format(enumerator.Current);
                if (elementText == null)
                    accumulator.Append(NULL_ELEMENT_MARKER);
                else
                {
                    foreach (char c in elementText)
                    {
                        if (c == ESCAPING_SEQUENCE_START || c == NULL_ELEMENT_MARKER || c == LIST_DELIMITER)
                            accumulator.Append(ESCAPING_SEQUENCE_START);
                        accumulator.Append(c);
                    }
                }
                accumulator.Append(LIST_DELIMITER);
            }
            accumulator.Shrink();

            return accumulator.ToString();
        }



        //this should not be cached. Nobody wants to expose globally available i.e. empty collection just for people to be able to add elements to it ;-)
        public override IAggressionBased<TValue> GetEmpty() =>
            AggressionBasedFactory<TValue>.FromOneValue(_transformerStore.GetTransformer<TValue>().GetEmpty());

        //this can be safely cached as long as it wraps null/immutable objects - AB<T> is immutable itself
        public override IAggressionBased<TValue> GetNull() =>
            AggressionBasedFactory<TValue>.FromOneValue(default);

        private IAggressionBased<TValue> FromValues(ParsingSequence values)
        {
            var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.Default();
            var pass = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.FromOneValue(pass);
            var norm = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext())
                return AggressionBasedFactory<TValue>.FromPassiveNormalAggressiveChecked(pass, norm, aggr);

            TValue v1 = pass, v2 = norm, v3 = aggr;

            var v4 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(5);
            var v6 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(6);
            var v7 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(7);
            var v8 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(8);
            var v9 = enumerator.Current;

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(10);//10 means more than 9 == do not check for more

            return new AggressionBased9<TValue>(new[]
            {
                v1, v2, v3,
                v4.ParseWith(_elementTransformer), v5.ParseWith(_elementTransformer), v6.ParseWith(_elementTransformer),
                v7.ParseWith(_elementTransformer), v8.ParseWith(_elementTransformer), v9.ParseWith(_elementTransformer)
            });

            static Exception GetException(int numberOfElements) => new ArgumentException(
                // ReSharper disable once UseNameofExpression
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
        }
    }

    [PublicAPI]
    public static class AggressionBasedFactory<TValue>
    {
        public static IAggressionBased<TValue> Default() => new AggressionBased1<TValue>(default);
        public static IAggressionBased<TValue> FromOneValue(TValue value) => new AggressionBased1<TValue>(value);

        public static IAggressionBased<TValue> FromPassiveNormalAggressive(TValue passive, TValue normal, TValue aggressive)
            => IsEqual(passive, normal) && IsEqual(passive, aggressive)
            ? FromOneValue(passive)
            : new AggressionBased3<TValue>(passive, normal, aggressive);

        [UsedImplicitly]
        public static IAggressionBased<TValue> FromPassiveNormalAggressiveChecked(TValue passive, TValue normal, TValue aggressive)
            => new AggressionBased3<TValue>(passive, normal, aggressive);

        internal static IAggressionBased<TValue> FromValues(IEnumerable<TValue> values)
        {
            if (values == null) return Default();

            using var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return Default();
            var pass = enumerator.Current;

            if (!enumerator.MoveNext()) return FromOneValue(pass);
            var norm = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current;

            if (!enumerator.MoveNext())
                return FromPassiveNormalAggressiveChecked(pass, norm, aggr);

            TValue v1 = pass, v2 = norm, v3 = aggr;

            var v4 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(5);
            var v6 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(6);
            var v7 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(7);
            var v8 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(8);
            var v9 = enumerator.Current;

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(10);//10 means more than 9 == do not check for more

            return new AggressionBased9<TValue>(new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9 });

            Exception GetException(int numberOfElements) => new ArgumentException(
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
        }

        internal static IAggressionBased<TValue> FromValuesCompact(IEnumerable<TValue> values)
        {
            if (values == null) return Default();

            using var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return Default();
            var pass = enumerator.Current;

            if (!enumerator.MoveNext()) return FromOneValue(pass);
            var norm = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current;

            if (!enumerator.MoveNext())
                return FromPassiveNormalAggressive(pass, norm, aggr);

            TValue v1 = pass, v2 = norm, v3 = aggr;

            var v4 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(5);
            var v6 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(6);
            var v7 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(7);
            var v8 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(8);
            var v9 = enumerator.Current;

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(10);//10 means more than 9 == do not check for more


            if (IsEqual(v1, v2) && IsEqual(v1, v3) &&
                IsEqual(v4, v5) && IsEqual(v4, v6) &&
                IsEqual(v7, v8) && IsEqual(v7, v9)
            )
                return FromPassiveNormalAggressive(v1, v4, v7);
            else
                return new AggressionBased9<TValue>(new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9 });

            Exception GetException(int numberOfElements) => new ArgumentException(
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
        }

        /*internal static IAggressionBased<TValue> FromValuesCompact(ParsingSequence<TValue> values)
        {
            var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return Default();
            var pass = enumerator.Current;

            if (!enumerator.MoveNext()) return FromOneValue(pass);
            var norm = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current;

            if (!enumerator.MoveNext())
                return FromPassiveNormalAggressive(pass, norm, aggr);

            TValue v1 = pass, v2 = norm, v3 = aggr;

            var v4 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(5);
            var v6 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(6);
            var v7 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(7);
            var v8 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(8);
            var v9 = enumerator.Current;

            //end of sequence
            if (enumerator.MoveNext()) throw GetException(10);//10 means more than 9 == do not check for more


            if (IsEqual(v1, v2) && IsEqual(v1, v3) &&
                IsEqual(v4, v5) && IsEqual(v4, v6) &&
                IsEqual(v7, v8) && IsEqual(v7, v9)
               )
                return FromPassiveNormalAggressive(v1, v4, v7);
            else
                return new AggressionBased9<TValue>(new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9 });

            static Exception GetException(int numberOfElements) => new ArgumentException(
                // ReSharper disable once UseNameofExpression
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", "values");
        }*/

        private static bool IsEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);
    }

    [PublicAPI]
    public enum StrategyAggression : byte
    {
        [Description("Default")]
        Default = 0,

        [Description("VeryPassive")]
        VeryPassive = 1,
        [Description("Passive")]
        Passive = 2,
        [Obsolete("Not used", true)]
        [Description("Passive3")]
        Passive3 = 3,


        [Obsolete("Not used", true)]
        [Description("Normal4")]
        Normal4 = 4,
        [Description("Normal")]
        Normal = 5,
        [Obsolete("Not used", true)]
        [Description("Normal6")]
        Normal6 = 6,


        [Obsolete("Not used", true)]
        [Description("Aggressive7")]
        Aggressive7 = 7,
        [Description("Aggressive")]
        Aggressive = 8,
        [Description("VeryAggressive")]
        VeryAggressive = 9
    }
}
