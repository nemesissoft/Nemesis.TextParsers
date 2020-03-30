using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using JetBrains.Annotations;

namespace Nemesis.TextParsers.Tests.Infrastructure
{
    internal class LotsOfDeconstructableData
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

        public static readonly LotsOfDeconstructableData EmptyInstance = new LotsOfDeconstructableData("", false, 0, null, 0.0f, null, 0, new List<string>(),
            new List<int>(), new Dictionary<string, float?>(), new decimal[0], new BigInteger[0][], new Complex(0.0, 0.0)
        );
    }

    //this is to demonstrate support of LSP rule 
    internal abstract class EmptyConventionBase { }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
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
        public static EmptyConventionBase Empty => new EmptyFactoryMethodConvention(3.14f, DateTime.Now);

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
