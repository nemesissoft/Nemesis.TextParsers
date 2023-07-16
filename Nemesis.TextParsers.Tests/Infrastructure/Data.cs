using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;

using JetBrains.Annotations;

using Nemesis.Essentials.Design;

namespace Nemesis.TextParsers.Tests.Infrastructure
{
    internal class LotsOfDeconstructableData : IEquatable<LotsOfDeconstructableData>
    {
        public string D1 { get; }
        public bool D2 { get; }
        public int D3 { get; }
        public uint? D4 { get; }
        public float D5 { get; }
        public double? D6 { get; }
        public FileMode D7 { get; }
        public List<string> D8 { get; }
        public IReadOnlyList<int> D9 { get; }
        public Dictionary<string, float?> D10 { get; }
        public decimal[] D11 { get; }
        public BigInteger[][] D12 { get; }
        public Complex D13 { get; }

        public LotsOfDeconstructableData(string d1, bool d2, int d3, uint? d4, float d5, double? d6, FileMode d7, List<string> d8, IReadOnlyList<int> d9, Dictionary<string, float?> d10, decimal[] d11, BigInteger[][] d12, Complex d13)
        {
            D1 = d1;
            D2 = d2;
            D3 = d3;
            D4 = d4;
            D5 = d5;
            D6 = d6;
            D7 = d7;
            D8 = d8;
            D9 = d9;
            D10 = d10;
            D11 = d11;
            D12 = d12;
            D13 = d13;
        }

        [UsedImplicitly]
        public void Deconstruct(out string d1, out bool d2, out int d3, out uint? d4, out float d5, out double? d6, out FileMode d7, out List<string> d8, out IReadOnlyList<int> d9, out Dictionary<string, float?> d10, out decimal[] d11, out BigInteger[][] d12, out Complex d13)
        {
            d1 = D1;
            d2 = D2;
            d3 = D3;
            d4 = D4;
            d5 = D5;
            d6 = D6;
            d7 = D7;
            d8 = D8;
            d9 = D9;
            d10 = D10;
            d11 = D11;
            d12 = D12;
            d13 = D13;
        }

        public static readonly LotsOfDeconstructableData EmptyInstance = new("", false, 0, null, 0.0f, null, 0, new List<string>(),
            new List<int>(), new Dictionary<string, float?>(), new decimal[0], new BigInteger[0][], new Complex(0.0, 0.0)
        );

        public bool Equals(LotsOfDeconstructableData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return D1 == other.D1 && D2 == other.D2 && D3 == other.D3 && D4 == other.D4 &&
                   D5.Equals(other.D5) && Nullable.Equals(D6, other.D6) && D7 == other.D7 &&
                   EnumerableEqualityComparer<string>.DefaultInstance.Equals(D8, other.D8) &&
                   EnumerableEqualityComparer<int>.DefaultInstance.Equals(D9, other.D9) &&
                   EnumerableEqualityComparer<KeyValuePair<string, float?>>.DefaultInstance.Equals(D10, other.D10) &&
                   EnumerableEqualityComparer<decimal>.DefaultInstance.Equals(D11, other.D11) &&
                   EnumerableEqualityComparer<BigInteger[]>.DefaultInstance.Equals(D12, other.D12) &&
                   D13.Equals(other.D13);
        }

        public override bool Equals(object obj) =>
            !(obj is null) &&
            (ReferenceEquals(this, obj) || obj is LotsOfDeconstructableData lots && Equals(lots));

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (D1 != null ? D1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ D2.GetHashCode();
                hashCode = (hashCode * 397) ^ D3;
                hashCode = (hashCode * 397) ^ D4.GetHashCode();
                hashCode = (hashCode * 397) ^ D5.GetHashCode();
                hashCode = (hashCode * 397) ^ D6.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)D7;
                hashCode = (hashCode * 397) ^ (D8 != null ? D8.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (D9 != null ? D9.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (D10 != null ? D10.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (D11 != null ? D11.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (D12 != null ? D12.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ D13.GetHashCode();
                return hashCode;
            }
        }
    }

    //this is to demonstrate support of LSP rule 
    internal abstract class EmptyConventionBase { }

    internal sealed class EmptyFactoryMethodConvention : EmptyConventionBase, IEquatable<EmptyFactoryMethodConvention>
    {
        public float Number { get; }
        public DateTime Time { get; }

        public EmptyFactoryMethodConvention(float number, DateTime time)
        {
            Number = number;
            Time = time;
        }


        //it's generally no a good idea for a property not to be deterministic. But this works well for our demo.
        //And have a look at DateTime.Now ;-)
        private static readonly DateTime _emptyDate = DateTime.Now;
        public static EmptyConventionBase Empty => new EmptyFactoryMethodConvention(3.14f, _emptyDate);

        //this is just to conform to FactoryMethod convention - will not be used 
        [UsedImplicitly]
        public static EmptyFactoryMethodConvention FromText(ReadOnlySpan<char> text) =>
            throw new NotSupportedException("Class should only be used for empty value tests");

        #region Equals
        public bool Equals(EmptyFactoryMethodConvention other) =>
                !(other is null) && (ReferenceEquals(this, other) ||
                    Number.Equals(other.Number) && Math.Abs(Time.Ticks - other.Time.Ticks) < 2 * TimeSpan.TicksPerMinute
                 );

        public override bool Equals(object obj) =>
            !(obj is null) && (ReferenceEquals(this, obj) || obj is EmptyFactoryMethodConvention ec && Equals(ec));

        public override int GetHashCode() => unchecked((Number.GetHashCode() * 397) ^ Time.GetHashCode());
        #endregion
    }
}
