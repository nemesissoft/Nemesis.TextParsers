using JetBrains.Annotations;
using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Nemesis.Essentials.Runtime;
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
    /// <summary>
    /// Helper interface for <see cref="IAggressionBased{T}"/>. 
    /// </summary>
    [PublicAPI]
    public interface IAggressionBased
    {
        int Arity { get; }
    }

    /// <summary>
    /// Encompasses multiple values dependent on aggression parameter
    /// </summary>
    /// <typeparam name="TValue">Type of container elements</typeparam>
    [Transformer(typeof(AggressionBasedTransformer<>))]
    [PublicAPI]
    public interface IAggressionBased<out TValue> : IAggressionBased
    {
        TValue PassiveValue { get; }
        TValue NormalValue { get; }
        TValue AggressiveValue { get; }

        TValue GetValueFor(byte aggression);
    }

    internal static class AggressionBasedExtensions
    {
        /// <summary>
        /// Convert <see cref="IAggressionBased{T}"/> to <see cref="List{T}"/>. This obviously allocates. For tests ONLY
        /// </summary>
        internal static IReadOnlyList<TValue> ToList<TValue>(this IAggressionBased<TValue> ab) =>
            ab switch
            {
                null => Array.Empty<TValue>(),
                AggressionBased1<TValue> ab1 => new[] { ab1.One },
                AggressionBased3<TValue> ab3 => new[] { ab3.PassiveValue, ab3.NormalValue, ab3.AggressiveValue },
                AggressionBased5<TValue> ab5 => new[] { ab5.VeryPassive, ab5.PassiveValue, ab5.NormalValue, ab5.AggressiveValue, ab5.VeryAggressive },
                AggressionBased9<TValue> ab9 => ab9.Values,
                _ => throw new NotSupportedException($"Type {ab.GetType().GetFriendlyName()} is not supported by {nameof(AggressionBasedExtensions)}.{nameof(ToList)}"),
            };
    }

    [Transformer(typeof(AggressionBasedTransformer<>))]
    internal abstract class AggressionBasedBase<TValue> : IEquatable<IAggressionBased<TValue>>
    {
        protected static bool IsStructurallyEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);

        public bool Equals(IAggressionBased<TValue> other) =>
            other switch
            {
                null => false,
                var ab when ReferenceEquals(this, ab) => true,
                AggressionBased1<TValue> o1 => Equals1(o1),
                AggressionBased3<TValue> o3 => Equals3(o3),
                AggressionBased5<TValue> o5 => Equals5(o5),
                AggressionBased9<TValue> o9 => Equals9(o9),
                _ => throw new ArgumentException($@"'{nameof(other)}' argument has to be {nameof(IAggressionBased<TValue>)}", nameof(other)),
            };

        protected abstract bool Equals1(in AggressionBased1<TValue> o1);

        protected abstract bool Equals3(in AggressionBased3<TValue> o3);

        protected abstract bool Equals5(in AggressionBased5<TValue> o3);

        protected abstract bool Equals9(in AggressionBased9<TValue> o9);

        public sealed override bool Equals(object obj) => obj != null &&
            (ReferenceEquals(this, obj) || obj is IAggressionBased<TValue> ab && Equals(ab));

        public sealed override int GetHashCode() => GetHashCodeCore();
        protected abstract int GetHashCodeCore();

        /// <summary>
        /// Text representation. For debugging purposes only. Does not support escaping characters. For formatting use <see cref="AggressionBasedTransformer{TValue}.Format"/>
        /// </summary>
        public sealed override string ToString() => string.Join(" # ",
            ((IAggressionBased<TValue>)this).ToList().Select(element => FormatValue(element))
        );

        private static string FormatValue(object value) =>
              value switch
              {
                  null => "∅",
                  bool b => b ? "true" : "false",
                  string s => $"\"{s}\"",
                  char c => $"\'{c}\'",
                  DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
                  IFormattable @if => @if.ToString(null, CultureInfo.InvariantCulture),
                  IEnumerable ie when !(ie.GetType() is { IsGenericType: true } t && t.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                      => "[" + string.Join(", ", ie.Cast<object>().Select(FormatValue)) + "]",
                  _ => value.ToString()
              };
    }

    internal sealed class AggressionBased1<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 1;
        public TValue One { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue PassiveValue => One;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue NormalValue => One;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TValue AggressiveValue => One;

        public AggressionBased1(TValue one) => One = one;

        public TValue GetValueFor(byte aggression) => One;

        protected override bool Equals1(in AggressionBased1<TValue> o1) =>
            IsStructurallyEqual(One, o1.One);

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(One, o3.PassiveValue) &&
            IsStructurallyEqual(One, o3.NormalValue) &&
            IsStructurallyEqual(One, o3.AggressiveValue);

        protected override bool Equals5(in AggressionBased5<TValue> o5) =>
           IsStructurallyEqual(One, o5.VeryPassive) &&
           IsStructurallyEqual(One, o5.PassiveValue) &&
           IsStructurallyEqual(One, o5.NormalValue) &&
           IsStructurallyEqual(One, o5.AggressiveValue) &&
           IsStructurallyEqual(One, o5.VeryAggressive);

        protected override bool Equals9(in AggressionBased9<TValue> o9) => o9.Equals(this);

        protected override int GetHashCodeCore() => One?.GetHashCode() ?? 0;
    }

    internal sealed class AggressionBased3<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 3;

        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public AggressionBased3(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }

        /*public TValue GetValueFor(byte aggression) =>
            aggression switch
            {
                1 or 2 or 3 => PassiveValue,
                0 or 4 or 5 or 6 => NormalValue,
                7 or 8 or 9 => AggressiveValue,
                _ => throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9"),
            };*/
        public TValue GetValueFor(byte aggression)
        {
            switch (aggression)
            {
                case 1: case 2: case 3: return PassiveValue;

                case 0: case 4: case 5: case 6: return NormalValue;

                case 7: case 8: case 9: return AggressiveValue;

                default: throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9");
            }
        }

        protected override bool Equals1(in AggressionBased1<TValue> o1) =>
            IsStructurallyEqual(PassiveValue, o1.One) &&
            IsStructurallyEqual(NormalValue, o1.One) &&
            IsStructurallyEqual(AggressiveValue, o1.One);

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(PassiveValue, o3.PassiveValue) &&
            IsStructurallyEqual(NormalValue, o3.NormalValue) &&
            IsStructurallyEqual(AggressiveValue, o3.AggressiveValue);

        protected override bool Equals5(in AggressionBased5<TValue> o5) =>
            IsStructurallyEqual(PassiveValue, o5.VeryPassive) &&
            IsStructurallyEqual(PassiveValue, o5.PassiveValue) &&
            IsStructurallyEqual(NormalValue, o5.NormalValue) &&
            IsStructurallyEqual(AggressiveValue, o5.AggressiveValue) &&
            IsStructurallyEqual(AggressiveValue, o5.VeryAggressive)
            ;

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

    internal sealed class AggressionBased5<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 5;

        public TValue VeryPassive { get; }
        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }
        public TValue VeryAggressive { get; }

        public AggressionBased5(TValue veryPassive, TValue passiveValue, TValue normalValue, TValue aggressiveValue, TValue veryAggressive)
        {
            VeryPassive = veryPassive;
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
            VeryAggressive = veryAggressive;
        }

        public TValue GetValueFor(byte aggression)
        {
            switch (aggression)
            {
                case 1: return VeryPassive;
                case 2: case 3: return PassiveValue;

                case 0: case 4: case 5: case 6: return NormalValue;

                case 7: case 8: return AggressiveValue;
                case 9: return VeryAggressive;

                default: throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9");
            }
        }

        protected override bool Equals1(in AggressionBased1<TValue> o1) =>
            IsStructurallyEqual(VeryPassive, o1.One) &&
            IsStructurallyEqual(PassiveValue, o1.One) &&
            IsStructurallyEqual(NormalValue, o1.One) &&
            IsStructurallyEqual(AggressiveValue, o1.One) &&
            IsStructurallyEqual(VeryAggressive, o1.One);

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(VeryPassive, o3.PassiveValue) &&
            IsStructurallyEqual(PassiveValue, o3.PassiveValue) &&
            IsStructurallyEqual(NormalValue, o3.NormalValue) &&
            IsStructurallyEqual(AggressiveValue, o3.AggressiveValue) &&
            IsStructurallyEqual(VeryAggressive, o3.AggressiveValue);

        protected override bool Equals5(in AggressionBased5<TValue> o5) =>
            IsStructurallyEqual(VeryPassive, o5.VeryPassive) &&
            IsStructurallyEqual(PassiveValue, o5.PassiveValue) &&
            IsStructurallyEqual(NormalValue, o5.NormalValue) &&
            IsStructurallyEqual(AggressiveValue, o5.AggressiveValue) &&
            IsStructurallyEqual(VeryAggressive, o5.VeryAggressive);


        protected override bool Equals9(in AggressionBased9<TValue> o9) =>
            IsStructurallyEqual(VeryPassive, o9.GetValueFor(1)) &&
            IsStructurallyEqual(PassiveValue, o9.GetValueFor(2)) && IsStructurallyEqual(PassiveValue, o9.GetValueFor(3)) &&

            IsStructurallyEqual(NormalValue, o9.GetValueFor(4)) && IsStructurallyEqual(NormalValue, o9.GetValueFor(5)) && IsStructurallyEqual(NormalValue, o9.GetValueFor(6)) &&

            IsStructurallyEqual(AggressiveValue, o9.GetValueFor(7)) && IsStructurallyEqual(AggressiveValue, o9.GetValueFor(8)) && 
            
            IsStructurallyEqual(VeryAggressive, o9.GetValueFor(9));

        protected override int GetHashCodeCore()
        {
            unchecked
            {
                int hashCode = PassiveValue?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (NormalValue?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (AggressiveValue?.GetHashCode() ?? 0);

                hashCode = (hashCode * 397) ^ (VeryPassive?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (VeryAggressive?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
    internal sealed class AggressionBased9<TValue> : AggressionBasedBase<TValue>, IAggressionBased<TValue>
    {
        public int Arity => 9;

        internal readonly TValue[] Values;

        public TValue PassiveValue => GetValueFor(2);
        public TValue NormalValue => GetValueFor(5);
        public TValue AggressiveValue => GetValueFor(8);

        // ReSharper disable once RedundantVerbatimPrefix
        public AggressionBased9([@NotNull] TValue[] values) => Values = values ?? throw new ArgumentNullException(nameof(values));


        /*public TValue GetValueFor(byte aggression)
        {
            if (_values == null || _values.Length != 9) throw new InvalidOperationException("Internal state of values is compromised");

            aggression = aggression switch
            {
                > 9 => throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9"),
                0 => 5,
                _ => aggression
            };

            return _values[aggression - 1];
        }*/

        public TValue GetValueFor(byte aggression)
        {
            if (aggression > 9)
                throw new ArgumentOutOfRangeException(nameof(aggression), $@"{nameof(aggression)} should be value from 0 to 9");

            if (Values == null || Values.Length != 9) throw new InvalidOperationException("Internal state of values is compromised");

            if (aggression == 0) aggression = 5;

            return Values[aggression - 1];
        }

        protected override bool Equals1(in AggressionBased1<TValue> o1)
        {
            var thisOne = o1.One;
            return Values?.All(v => IsStructurallyEqual(v, thisOne)) == true;
        }

        protected override bool Equals3(in AggressionBased3<TValue> o3) =>
            IsStructurallyEqual(o3.PassiveValue, GetValueFor(1)) && IsStructurallyEqual(o3.PassiveValue, GetValueFor(2)) && IsStructurallyEqual(o3.PassiveValue, GetValueFor(3)) &&
            IsStructurallyEqual(o3.NormalValue, GetValueFor(4)) && IsStructurallyEqual(o3.NormalValue, GetValueFor(5)) && IsStructurallyEqual(o3.NormalValue, GetValueFor(6)) &&
            IsStructurallyEqual(o3.AggressiveValue, GetValueFor(7)) && IsStructurallyEqual(o3.AggressiveValue, GetValueFor(8)) && IsStructurallyEqual(o3.AggressiveValue, GetValueFor(9))
        ;
        
        protected override bool Equals5(in AggressionBased5<TValue> o5) =>
            IsStructurallyEqual(o5.VeryPassive, GetValueFor(1)) &&
            IsStructurallyEqual(o5.PassiveValue, GetValueFor(2)) && IsStructurallyEqual(o5.PassiveValue, GetValueFor(3)) &&

            IsStructurallyEqual(o5.NormalValue, GetValueFor(4)) && IsStructurallyEqual(o5.NormalValue, GetValueFor(5)) && IsStructurallyEqual(o5.NormalValue, GetValueFor(6)) &&

            IsStructurallyEqual(o5.AggressiveValue, GetValueFor(7)) && IsStructurallyEqual(o5.AggressiveValue, GetValueFor(8)) && 
            IsStructurallyEqual(o5.VeryAggressive, GetValueFor(9))
        ;

        protected override bool Equals9(in AggressionBased9<TValue> o9)
        {
            var o9Values = o9.Values;

            if (ReferenceEquals(Values, o9Values)) return true;

            using var enumerator = ((IReadOnlyList<TValue>)Values).GetEnumerator();
            using var enumerator2 = ((IReadOnlyList<TValue>)o9Values).GetEnumerator();
            while (enumerator.MoveNext())
                if (!enumerator2.MoveNext() || !IsStructurallyEqual(enumerator.Current, enumerator2.Current))
                    return false;

            return !enumerator2.MoveNext();
        }

        protected override int GetHashCodeCore() => Values?.GetHashCode() ?? 0;
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
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);


            try
            {
                switch (ab)
                {
                    case AggressionBased1<TValue> ab1:
                        FormatElement(ref accumulator, ab1.One);
                        break;
                        
                    case AggressionBased3<TValue> ab3:
                        FormatElement(ref accumulator, ab3.PassiveValue);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab3.NormalValue);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab3.AggressiveValue);
                        break;

                    case AggressionBased5<TValue> ab5:
                        FormatElement(ref accumulator, ab5.VeryPassive);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab5.PassiveValue);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab5.NormalValue);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab5.AggressiveValue);
                        accumulator.Append(LIST_DELIMITER);
                        FormatElement(ref accumulator, ab5.VeryAggressive);
                        break;

                    case AggressionBased9<TValue> ab9:
                        foreach (var value in ab9.Values)
                        {
                            FormatElement(ref accumulator, value);
                            accumulator.Append(LIST_DELIMITER);
                        }
                        accumulator.Shrink();
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Type {ab.GetType().GetFriendlyName()} is not supported by {nameof(AggressionBasedTransformer<object>)}.{nameof(Format)}");

                }

                return accumulator.ToString();
            }
            finally { accumulator.Dispose(); }
        }

        private void FormatElement(ref ValueSequenceBuilder<char> accumulator, TValue element)
        {
            string elementText = _elementTransformer.Format(element);
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
            var v1 = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext()) return AggressionBasedFactory<TValue>.FromOneValue(v1);
            var v2 = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext()) throw GetException(2);
            var v3 = enumerator.Current.ParseWith(_elementTransformer);

            if (!enumerator.MoveNext())
                return AggressionBasedFactory<TValue>.FromPassiveNormalAggressiveChecked(v1, v2, v3);

            
            var v4 = enumerator.Current.ParseWith(_elementTransformer);
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current.ParseWith(_elementTransformer);
            if (!enumerator.MoveNext())
                return AggressionBasedFactory<TValue>.FromFiveValuesChecked(v1, v2, v3, v4, v5);


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
                v4, v5, v6.ParseWith(_elementTransformer),
                v7.ParseWith(_elementTransformer), v8.ParseWith(_elementTransformer), v9.ParseWith(_elementTransformer)
            });

            static Exception GetException(int numberOfElements) => new ArgumentException(
                // ReSharper disable once UseNameofExpression
                $@"Sequence should contain either 0, 1, 3, 5 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
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

        public static IAggressionBased<TValue> FromFiveValuesChecked(TValue veryPassive, TValue passiveValue, TValue normalValue, TValue aggressiveValue, TValue veryAggressive)
            => new AggressionBased5<TValue>(veryPassive, passiveValue, normalValue, aggressiveValue, veryAggressive);

        
        internal static IAggressionBased<TValue> FromValues(IEnumerable<TValue> values)
        {
            if (values == null) return Default();

            using var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext()) return Default();
            var v1 = enumerator.Current;

            if (!enumerator.MoveNext()) return FromOneValue(v1);
            var v2 = enumerator.Current;

            if (!enumerator.MoveNext()) throw GetException(2);
            var v3 = enumerator.Current;

            if (!enumerator.MoveNext())
                return FromPassiveNormalAggressiveChecked(v1, v2, v3);
                        

            var v4 = enumerator.Current;
            if (!enumerator.MoveNext()) throw GetException(4);
            var v5 = enumerator.Current;
            if (!enumerator.MoveNext())
                return FromFiveValuesChecked(v1, v2, v3, v4, v5);

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
                $@"Sequence should contain either 0, 1, 3, 5 or 9 elements, but contained {(numberOfElements > 9 ? "more than 9" : numberOfElements.ToString())} elements", nameof(values));
        }
               
                
        private static bool IsEqual(TValue left, TValue right) => StructuralEquality.Equals(left, right);
    }    
}
