using System;
using System.Collections.Generic;
using System.Text;

namespace Nemesis.TextParsers.Settings
{

    //TODO add defaults static instance 
    //TODO deconstructable settings ?
    //TODO cache transformers with different settings ? (AB<T> vs collection)
    //TODO propose mechanism for generating default instances por class/struct 


    public interface IEscapable<out T>
    {
        T With(char nullElementMarker, char escapingSequenceStart);
    }

    public interface IBorderable<out T>
    {
        T With(char? start, char? end);
    }

    public readonly struct TupleSettings : IEscapable<TupleSettings>, IBorderable<TupleSettings>, IEquatable<TupleSettings>
    {
        public char TupleDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? TupleStart { get; }
        public char? TupleEnd { get; }

        public TupleSettings(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char? tupleStart, char? tupleEnd)
        {
            TupleDelimiter = tupleDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            TupleStart = tupleStart;
            TupleEnd = tupleEnd;
        }

        public TupleSettings With(char nullElementMarker, char escapingSequenceStart) => 
            new TupleSettings(TupleDelimiter, nullElementMarker, escapingSequenceStart, TupleStart, TupleEnd);

        public TupleSettings With(char? start, char? end) => 
            new TupleSettings(TupleDelimiter, NullElementMarker, EscapingSequenceStart, start, end);

        public override string ToString() =>
            $"{TupleStart}Item1{TupleDelimiter}Item2{TupleDelimiter}…{TupleDelimiter}ItemN{TupleEnd} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";

        public bool Equals(TupleSettings other) => 
            TupleDelimiter == other.TupleDelimiter && NullElementMarker == other.NullElementMarker &&
            EscapingSequenceStart == other.EscapingSequenceStart && TupleStart == other.TupleStart &&
            TupleEnd == other.TupleEnd;

        public override bool Equals(object obj) => obj is TupleSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = TupleDelimiter.GetHashCode();
                hashCode = (hashCode * 397) ^ NullElementMarker.GetHashCode();
                hashCode = (hashCode * 397) ^ EscapingSequenceStart.GetHashCode();
                hashCode = (hashCode * 397) ^ TupleStart.GetHashCode();
                hashCode = (hashCode * 397) ^ TupleEnd.GetHashCode();
                return hashCode;
            }
        }
    }

    public sealed class FactoryMethodSettings : IEquatable<FactoryMethodSettings>
    {
        public string FactoryMethodName { get; }
        public string EmptyPropertyName { get; }
        public string NullPropertyName { get; }

        public FactoryMethodSettings(string factoryMethodName = "FromText", string emptyPropertyName = "Empty", string nullPropertyName = "Null")
        {
            FactoryMethodName = factoryMethodName;
            EmptyPropertyName = emptyPropertyName;
            NullPropertyName = nullPropertyName;
        }

        public override string ToString() => 
            $"Parsed by {FactoryMethodName} Empty: {EmptyPropertyName} Null: {NullPropertyName}";

        public bool Equals(FactoryMethodSettings other) =>
            !(other is null) && (ReferenceEquals(this, other) || FactoryMethodName == other.FactoryMethodName &&
                EmptyPropertyName == other.EmptyPropertyName &&
                NullPropertyName == other.NullPropertyName);

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is FactoryMethodSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (FactoryMethodName != null ? FactoryMethodName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (EmptyPropertyName != null ? EmptyPropertyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NullPropertyName != null ? NullPropertyName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public readonly struct EnumSettings : IEquatable<EnumSettings>
    {
        public bool CaseSensitive { get; }
        public bool AllowParsingNumerics { get; }

        public EnumSettings(bool caseSensitive, bool allowParsingNumerics)
        {
            CaseSensitive = caseSensitive;
            AllowParsingNumerics = allowParsingNumerics;
        }

        public override string ToString() => $"{nameof(CaseSensitive)}: {CaseSensitive}, {nameof(AllowParsingNumerics)}: {AllowParsingNumerics}";

        public bool Equals(EnumSettings other) => CaseSensitive == other.CaseSensitive && AllowParsingNumerics == other.AllowParsingNumerics;

        public override bool Equals(object obj) => obj is EnumSettings other && Equals(other);

        public override int GetHashCode() => unchecked((CaseSensitive.GetHashCode() * 397) ^ AllowParsingNumerics.GetHashCode());
    }

    public readonly struct CollectionSettings : IEscapable<CollectionSettings>, IBorderable<CollectionSettings>, IEquatable<CollectionSettings>
    {
        public char ListDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }

        public CollectionSettings(char listDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        {
            ListDelimiter = listDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
        }

        public CollectionSettings With(char nullElementMarker, char escapingSequenceStart) => 
            new CollectionSettings(ListDelimiter, nullElementMarker, escapingSequenceStart, Start, End);

        public CollectionSettings With(char? start, char? end) => 
            new CollectionSettings(ListDelimiter, NullElementMarker, EscapingSequenceStart, start, end);


        public override string ToString() => $"{Start}Item1{ListDelimiter}Item2{ListDelimiter}…{ListDelimiter}ItemN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";


        public bool Equals(CollectionSettings other) => 
            ListDelimiter == other.ListDelimiter && NullElementMarker == other.NullElementMarker && 
            EscapingSequenceStart == other.EscapingSequenceStart && Start == other.Start && End == other.End;

        public override bool Equals(object obj) => obj is CollectionSettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ListDelimiter.GetHashCode();
                hashCode = (hashCode * 397) ^ NullElementMarker.GetHashCode();
                hashCode = (hashCode * 397) ^ EscapingSequenceStart.GetHashCode();
                hashCode = (hashCode * 397) ^ Start.GetHashCode();
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                return hashCode;
            }
        }
    }

    public readonly struct DictionarySettings : IEscapable<DictionarySettings>, IBorderable<DictionarySettings>, IEquatable<DictionarySettings>
    {
        public char DictionaryPairsDelimiter { get; }
        public char DictionaryKeyValueDelimiter { get; }
        public char NullElementMarker { get; }
        public char EscapingSequenceStart { get; }
        public char? Start { get; }
        public char? End { get; }

        public DictionarySettings(char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, char escapingSequenceStart, char? start, char? end)
        {
            DictionaryPairsDelimiter = dictionaryPairsDelimiter;
            DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
            NullElementMarker = nullElementMarker;
            EscapingSequenceStart = escapingSequenceStart;
            Start = start;
            End = end;
        }

        public DictionarySettings With(char nullElementMarker, char escapingSequenceStart) => 
            new DictionarySettings(DictionaryPairsDelimiter, DictionaryKeyValueDelimiter, nullElementMarker,
                escapingSequenceStart, Start, End);

        public DictionarySettings With(char? start, char? end) => 
            new DictionarySettings(DictionaryPairsDelimiter, DictionaryKeyValueDelimiter, NullElementMarker,
                EscapingSequenceStart, start, end);


        public override string ToString() => 
            $"{Start}Key1{DictionaryKeyValueDelimiter}Value1{DictionaryPairsDelimiter}…{DictionaryPairsDelimiter}KeyN{DictionaryKeyValueDelimiter}ValueN{End} escaped by '{EscapingSequenceStart}', null marked by '{NullElementMarker}'";


        public bool Equals(DictionarySettings other) => 
            DictionaryPairsDelimiter == other.DictionaryPairsDelimiter && 
            DictionaryKeyValueDelimiter == other.DictionaryKeyValueDelimiter &&
            NullElementMarker == other.NullElementMarker && EscapingSequenceStart == other.EscapingSequenceStart &&
            Start == other.Start && End == other.End;

        public override bool Equals(object obj) => obj is DictionarySettings other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = DictionaryPairsDelimiter.GetHashCode();
                hashCode = (hashCode * 397) ^ DictionaryKeyValueDelimiter.GetHashCode();
                hashCode = (hashCode * 397) ^ NullElementMarker.GetHashCode();
                hashCode = (hashCode * 397) ^ EscapingSequenceStart.GetHashCode();
                hashCode = (hashCode * 397) ^ Start.GetHashCode();
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                return hashCode;
            }
        }
    }

}
