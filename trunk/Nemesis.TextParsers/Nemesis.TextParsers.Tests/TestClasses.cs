﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Nemesis.Essentials.Design;

namespace Nemesis.TextParsers.Tests
{
    [Flags]
    public enum DaysOfWeek : byte
    {
        None = 0,
        Monday
            = 0b0000_0001,
        Tuesday
            = 0b0000_0010,
        Wednesday
            = 0b0000_0100,
        Thursday
            = 0b0000_1000,
        Friday
            = 0b0001_0000,
        Saturday
            = 0b0010_0000,
        Sunday
            = 0b0100_0000,

        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        Weekends = Saturday | Sunday,
        All = Weekdays | Weekends
    }

    [TypeConverter(typeof(PointConverter))]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    internal struct Point : IEquatable<Point>
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y) => (X, Y) = (x, y);

        public bool Equals(Point other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => !(obj is null) && obj is Point other && Equals(other);

        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public override string ToString() => $"{X};{Y}";

        public static Point FromText(string text) =>
            text.Split(';') is string[] arr ?
                new Point(int.Parse(arr[0]), int.Parse(arr[1])) : default;
    }

    internal sealed class PointConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
            value is string text ? Point.FromText(text) : default;

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
            destinationType == typeof(string) ?
                value?.ToString() :
                base.ConvertTo(context, culture, value, destinationType);
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    internal struct Rect : IEquatable<Rect>
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Equals(Rect other) => X == other.X && Y == other.Y && Height == other.Height && Width == other.Width;

        public override bool Equals(object obj) => !(obj is null) && obj is Rect other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ Width;
                return hashCode;
            }
        }

        public static bool operator ==(Rect left, Rect right) => left.Equals(right);

        public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

        public override string ToString() => $"{X};{Y};{Width};{Height}";

        [UsedImplicitly]
        public static Rect FromText(string text) =>
            text.Split(';') is string[] arr ?
                new Rect(int.Parse(arr[0]), int.Parse(arr[1]),
                    int.Parse(arr[2]), int.Parse(arr[3])) : default;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    internal struct ThreeLetters : IEquatable<ThreeLetters>
    {
        public char C1 { get; }
        public char C2 { get; }
        public char C3 { get; }

        public ThreeLetters(char c1, char c2, char c3)
        {
            C1 = c1;
            C2 = c2;
            C3 = c3;
        }

        public bool Equals(ThreeLetters other) => C1 == other.C1 && C2 == other.C2 && C3 == other.C3;

        public override bool Equals(object obj) => !(obj is null) && obj is ThreeLetters other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = C1.GetHashCode();
                hashCode = (hashCode * 397) ^ C2.GetHashCode();
                hashCode = (hashCode * 397) ^ C3.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ThreeLetters left, ThreeLetters right) => left.Equals(right);

        public static bool operator !=(ThreeLetters left, ThreeLetters right) => !left.Equals(right);

        public override string ToString() => $"{C1}{C2}{C3}";

        [UsedImplicitly]
        public static ThreeLetters FromText(ReadOnlySpan<char> text) =>
            text.Length == 3 ?
                new ThreeLetters(text[0], text[1], text[2]) : default;
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    internal struct ThreeElements<TElement> : IEquatable<ThreeElements<TElement>>
        where TElement : IEquatable<TElement>
    {
        public TElement E1 { get; }
        public TElement E2 { get; }
        public TElement E3 { get; }

        public ThreeElements(TElement e1, TElement e2, TElement e3)
        {
            E1 = e1;
            E2 = e2;
            E3 = e3;
        }

        public bool Equals(ThreeElements<TElement> other) =>
            E1.Equals(other.E1) && E2.Equals(other.E2) && E3.Equals(other.E3);

        public override bool Equals(object obj) =>
            !(obj is null) && obj is ThreeElements<TElement> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TElement>.Default.GetHashCode(E1);
                hashCode = (hashCode * 397) ^ EqualityComparer<TElement>.Default.GetHashCode(E2);
                hashCode = (hashCode * 397) ^ EqualityComparer<TElement>.Default.GetHashCode(E3);
                return hashCode;
            }
        }

        public static bool operator ==(ThreeElements<TElement> left, ThreeElements<TElement> right) => left.Equals(right);

        public static bool operator !=(ThreeElements<TElement> left, ThreeElements<TElement> right) => !left.Equals(right);

        public override string ToString() => FormattableString.Invariant($"{E1},{E2},{E3}");

        [UsedImplicitly]
        public static ThreeElements<TElement> FromText(ReadOnlySpan<char> text)
        {
            var tokens = text.Tokenize(',', '\\', true);
            var parsed = tokens.Parse<TElement>('\\', '∅', ',');

            var enumerator = parsed.GetEnumerator();
            {
                if (!enumerator.MoveNext()) throw GetException(0);
                var first = enumerator.Current;

                if (!enumerator.MoveNext()) throw GetException(1);
                var second = enumerator.Current;

                if (!enumerator.MoveNext()) throw GetException(2);
                var third = enumerator.Current;

                //end of sequence
                if (enumerator.MoveNext()) throw GetException(4);

                return new ThreeElements<TElement>(first, second, third);
            }

            Exception GetException(int numberOfElements) => new ArgumentException(
                $@"Sequence should contain either 3, but contained {(numberOfElements > 3 ? "more than 3" : numberOfElements.ToString())} elements");
        }

        [UsedImplicitly]
        public static ThreeElements<TElement> FromText(string text) => throw new NotSupportedException("This should never be used");
    }


    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [TextFactory(typeof(PairTextFactory<>))]
    internal struct PairWithFactory<TElement> : IEquatable<PairWithFactory<TElement>>
        where TElement : IEquatable<TElement>
    {
        public TElement Left { get; }
        public TElement Right { get; }

        public PairWithFactory(TElement left, TElement right)
        {
            Left = left;
            Right = right;
        }

        public bool Equals(PairWithFactory<TElement> other) => Left.Equals(other.Left) && Right.Equals(other.Right);

        public override bool Equals(object obj) =>
            !(obj is null) && obj is PairWithFactory<TElement> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<TElement>.Default.GetHashCode(Left);
                hashCode = (hashCode * 397) ^ EqualityComparer<TElement>.Default.GetHashCode(Right);
                return hashCode;
            }
        }

        public override string ToString() => FormattableString.Invariant($"{Left},{Right}");
    }

    internal static class PairTextFactory<TElement> where TElement : IEquatable<TElement>
    {
        [UsedImplicitly]
        public static PairWithFactory<TElement> FromText(ReadOnlySpan<char> text)
        {
            var tokens = text.Tokenize(',', '\\', true);
            var parsed = tokens.Parse<TElement>('\\', '∅', ',');

            var enumerator = parsed.GetEnumerator();
            {
                if (!enumerator.MoveNext()) throw GetException(0);
                var left = enumerator.Current;

                if (!enumerator.MoveNext()) throw GetException(1);
                var right = enumerator.Current;

                //end of sequence
                if (enumerator.MoveNext()) throw GetException(3);

                return new PairWithFactory<TElement>(left, right);
            }

            Exception GetException(int numberOfElements) => new ArgumentException(
                $@"Sequence should contain either 2, but contained {(numberOfElements > 2 ? "more than 2" : numberOfElements.ToString())} elements");
        }
    }

    internal enum Color { Red = 1, Blue = 2, Green = 3 }

    [Flags]
    internal enum Colors { None = 0, Red = 1, Blue = 2, Green = 4, RedAndBlue = Red | Blue }


    [TypeConverter(typeof(OptionConverter))]
    internal struct Option
    {
        public OptionEnum Value { get; }

        public Option(OptionEnum value) => Value = value;

        public override string ToString() => Value.ToString();
    }

    internal enum OptionEnum : byte
    {
        None,
        Option1,
        Option2,
        Option3
    }

    internal sealed class OptionConverter : BaseTextConverter<Option>, ITransformer<Option>
    {
        public override Option ParseString(string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "option1":
                case "o1":
                    return new Option(OptionEnum.Option1);
                case "option2":
                case "o2":
                    return new Option(OptionEnum.Option2);
                case "option3":
                case "o3":
                    return new Option(OptionEnum.Option3);
                //case "none":
                default:
                    return new Option(OptionEnum.None);
            }
        }

        public override string FormatToString(Option value) => value.ToString();

        public Option Parse(ReadOnlySpan<char> input)
        {
            input = input.Trim();

            if (input.Length == 2 &&
                (input[0] == 'o' || input[0] == 'O') &&
                char.IsDigit(input[1]) &&
                int.Parse(input[1].ToString()) is int i1 &&
                i1 >= 1 && i1 <= 3
            )
                return new Option((OptionEnum)i1);
            else if (input.Length == 7 &&
                   (input[0] == 'o' || input[0] == 'O') &&
                   (input[1] == 'p' || input[1] == 'P') &&
                   (input[2] == 't' || input[2] == 'T') &&
                   (input[3] == 'i' || input[3] == 'I') &&
                   (input[4] == 'o' || input[4] == 'O') &&
                   (input[5] == 'n' || input[5] == 'N') &&

                   int.TryParse(
#if NET472 || NET48
                input.Slice(6, 1).ToString()
#else
                input.Slice(6, 1)
#endif
                , out int i2) &&
                   i2 >= 1 && i2 <= 3
            )
                return new Option((OptionEnum)i2);
            else
                return int.TryParse(
#if NET472|| NET48
                input.ToString()
#else
                input
#endif
                    , out int i3) ? new Option((OptionEnum) i3) : new Option(OptionEnum.None);
        }

        public string Format(Option element) => element.ToString();
    }
}
