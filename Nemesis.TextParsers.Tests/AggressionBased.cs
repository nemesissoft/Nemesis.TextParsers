using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Utils;
#if NETCOREAPP3_0
    using NotNull = System.Diagnostics.CodeAnalysis.NotNullAttribute;
#else
    using NotNull = JetBrains.Annotations.NotNullAttribute;
#endif

namespace Nemesis.TextParsers.Tests
{
    [PublicAPI]
    public interface IAggressionValuesProvider<out TValue>
    {
        /// <summary>
        /// For introspection and test purposes. This might allocate a new list
        /// </summary>
        IReadOnlyList<TValue> Values { get; }

        string ToString();
    }

    [TextConverterSyntax("Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3", '#')]
    [TextFactory(typeof(AggressionBasedFactoryChecked<>))]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IAggressionBased<out TValue>
    {
        TValue PassiveValue { get; }
        TValue NormalValue { get; }
        TValue AggressiveValue { get; }

        TValue GetValueFor(StrategyAggression aggression);
        TValue GetValueFor(byte aggression);
    }

    internal static class AggressionBasedSerializer
    {
        public static readonly SpanCollectionSerializer Instance = new SpanCollectionSerializer('#', ';', '=', '∅', '\\');
    }

    [TextConverterSyntax("Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3", '#')]
    [TextFactory(typeof(AggressionBasedFactoryChecked<>))]
    internal abstract class AggressionBasedBase<TValue> : IEquatable<IAggressionBased<TValue>>, IAggressionValuesProvider<TValue>
    {
        IReadOnlyList<TValue> IAggressionValuesProvider<TValue>.Values => GetValues().ToList();

        protected abstract LeanCollection<TValue> GetValues();

        protected static bool IsStructurallyEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);

        public bool Equals(IAggressionBased<TValue> other) =>
            other switch
            {
                null => false,
                var ab when ReferenceEquals(this, ab) => true,
                AggressionBased1<TValue> o1 => Equals1(o1),
                AggressionBased3<TValue> o3 => Equals3(o3),
                AggressionBased9<TValue> o9 => Equals9(o9),
                _ => throw new ArgumentException(
                    $@"'{nameof(other)}' argument has to be {nameof(IAggressionBased<TValue>)}", nameof(other))
            };

        protected abstract bool Equals1(in AggressionBased1<TValue> o1);

        protected abstract bool Equals3(in AggressionBased3<TValue> o3);

        protected abstract bool Equals9(in AggressionBased9<TValue> o9);

        public sealed override bool Equals(object obj) => obj != null &&
            (ReferenceEquals(this, obj) || obj is IAggressionBased<TValue> ab && Equals(ab));

        public sealed override int GetHashCode() => GetHashCodeCore();
        protected abstract int GetHashCodeCore();

        public sealed override string ToString() => AggressionBasedSerializer.Instance.FormatCollection(GetValues());
    }

    internal sealed class AggressionBased1<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public TValue One { get; }

        protected override LeanCollection<TValue> GetValues() => new LeanCollection<TValue>(One);

        public TValue PassiveValue => One;
        public TValue NormalValue => One;
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
        protected override LeanCollection<TValue> GetValues() => new LeanCollection<TValue>(PassiveValue, NormalValue, AggressiveValue);

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
                1 => PassiveValue,
                2 => PassiveValue,
                3 => PassiveValue,
                0 => NormalValue,
                4 => NormalValue,
                5 => NormalValue,
                6 => NormalValue,
                7 => AggressiveValue,
                8 => AggressiveValue,
                9 => AggressiveValue,
                _ => throw new ArgumentOutOfRangeException($@"{nameof(aggression)} should be value from 0 to 9", nameof(aggression))
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
                var hashCode = PassiveValue?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (NormalValue?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (AggressiveValue?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    internal sealed class AggressionBased9<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        private readonly TValue[] _values;

        protected override LeanCollection<TValue> GetValues() => LeanCollectionFactory.FromArrayChecked(_values);

        public TValue PassiveValue => GetValueFor(StrategyAggression.Passive);
        public TValue NormalValue => GetValueFor(StrategyAggression.Normal);
        public TValue AggressiveValue => GetValueFor(StrategyAggression.Aggressive);

        // ReSharper disable once RedundantVerbatimPrefix
        public AggressionBased9([@NotNull] TValue[] values) => _values = values ?? throw new ArgumentNullException(nameof(values));

        public TValue GetValueFor(StrategyAggression aggression) => GetValueFor((byte)aggression);

        public TValue GetValueFor(byte aggression)
        {
            if (aggression > 9)
                throw new ArgumentOutOfRangeException($@"{nameof(aggression)} should be value from 0 to 9", nameof(aggression));

            if (aggression == 0) aggression = 5;

            if (_values == null || _values.Length != 9) throw new InvalidOperationException("Internal state of values is compromised");
            else
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

            using var enumerator = ((IReadOnlyList<TValue>) _values).GetEnumerator();
            using var enumerator2 = ((IReadOnlyList<TValue>) o9Values).GetEnumerator();
            while (enumerator.MoveNext())
                if (!enumerator2.MoveNext() || !IsStructurallyEqual(enumerator.Current, enumerator2.Current))
                    return false;

            return !enumerator2.MoveNext();
        }

        protected override int GetHashCodeCore() => _values?.GetHashCode() ?? 0;
    }

    [PublicAPI]
    public static class AggressionBasedFactoryChecked<TValue>
    {
        public static IAggressionBased<TValue> FromText(string text) => string.IsNullOrEmpty(text) ?
            AggressionBasedFactory<TValue>.FromOneValue(typeof(TValue) == typeof(string) ? (TValue)(object)text : default) :
            FromValues(AggressionBasedSerializer.Instance.ParseStream<TValue>(text.AsSpan(), out _))
        ;

        public static IAggressionBased<TValue> FromText(ReadOnlySpan<char> text) => text.IsEmpty ?
            AggressionBasedFactory<TValue>.FromOneValue(typeof(TValue) == typeof(string) ? (TValue)(object)"" : default) :
            FromValues(AggressionBasedSerializer.Instance.ParseStream<TValue>(text, out _))
        ;

        public static IAggressionBased<TValue> FromValues(IEnumerable<TValue> values)
        {
            if (values == null) return AggressionBasedFactory<TValue>.Default();

            using var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.Default();
            var pass = enumerator.Current;

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.FromOneValue(pass);
            var norm = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current;

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

            return new AggressionBased9<TValue>(new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9 });

            Exception GetException(int numberOfElements) => new ArgumentException(
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
        }

        public static IAggressionBased<TValue> FromValues(ParsedSequence<TValue> values)
        {
            var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.Default();
            var pass = enumerator.Current;

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.FromOneValue(pass);
            var norm = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var aggr = enumerator.Current;

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

            return new AggressionBased9<TValue>(new[] { v1, v2, v3, v4, v5, v6, v7, v8, v9 });

            Exception GetException(int numberOfElements) => new ArgumentException(
                // ReSharper disable once UseNameofExpression
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", "values");
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

        public static IAggressionBased<TValue> FromValuesCompact(IEnumerable<TValue> values)
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

        public static IAggressionBased<TValue> FromValuesCompact(ParsedSequence<TValue> values)
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

            Exception GetException(int numberOfElements) => new ArgumentException(
                // ReSharper disable once UseNameofExpression
                $@"Sequence should contain either 0, 1, 3 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", "values");
        }
        
        public static IAggressionBased<TValue> FromTextCompact(string text) => string.IsNullOrEmpty(text) ?
            FromOneValue(typeof(TValue) == typeof(string) ? (TValue)(object)text : default) :
            FromValuesCompact(AggressionBasedSerializer.Instance.ParseStream<TValue>(text.AsSpan(), out _))
        ;

        public static IAggressionBased<TValue> FromTextCompact(ReadOnlySpan<char> text) => text.IsEmpty ?
            FromOneValue(typeof(TValue) == typeof(string) ? (TValue)(object)"" : default) :
            FromValuesCompact(AggressionBasedSerializer.Instance.ParseStream<TValue>(text, out _))
        ;

        private static bool IsEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
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
