using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    #region Stubs
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    internal enum EmptyEnum { }

    internal enum Enum1 { E1_1_Int }

    internal enum Enum2 { E2_1_Int, E2_2_Int }

    internal enum Enum3 { E3_1_Int, E3_2_Int, E3_3_Int }

    internal enum ByteEnum : byte { B1, B2, B3 }

    internal enum SByteEnum : sbyte { Sb1 = -10, Sb2 = 0, Sb3 = 5, Sb4 = 10 }

    internal enum Int64Enum : long { L1 = -50, L2 = 0, L3 = 1, L4 = 50 }

    internal enum UInt64Enum : ulong { Ul_1 = 0, Ul_2 = 1, Ul_3 = 50 }

    [Flags]
    internal enum Fruits : ushort
    {
        None = 0,
        Apple = 1,
        Pear = 2,
        Plum = 4,
        AppleAndPlum = Apple | Plum,
        PearAndPlum = Pear | Plum,
        All = Apple | Pear | Plum,
    }

    [Flags]
    internal enum FruitsWeirdAll : short
    {
        None = 0,
        Apple = 1,
        Pear = 2,
        Plum = 4,
        AppleAndPlum = Apple | Plum,
        PearAndPlum = Pear | Plum,
        All = -1
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnusedMember.Global 
    #endregion

    [TestFixture(TypeArgs = new[] { typeof(DaysOfWeek), typeof(byte), typeof(ByteNumber) })]
    [TestFixture(TypeArgs = new[] { typeof(EmptyEnum), typeof(int), typeof(Int32Number) })]
    [TestFixture(TypeArgs = new[] { typeof(Enum1), typeof(int), typeof(Int32Number) })]
    [TestFixture(TypeArgs = new[] { typeof(Enum2), typeof(int), typeof(Int32Number) })]
    [TestFixture(TypeArgs = new[] { typeof(Enum3), typeof(int), typeof(Int32Number) })]
    [TestFixture(TypeArgs = new[] { typeof(ByteEnum), typeof(byte), typeof(ByteNumber) })]
    [TestFixture(TypeArgs = new[] { typeof(SByteEnum), typeof(sbyte), typeof(SByteNumber) })]
    [TestFixture(TypeArgs = new[] { typeof(Int64Enum), typeof(long), typeof(Int64Number) })]
    [TestFixture(TypeArgs = new[] { typeof(UInt64Enum), typeof(ulong), typeof(UInt64Number) })]
    [TestFixture(TypeArgs = new[] { typeof(Fruits), typeof(ushort), typeof(UInt16Number) })]
    [TestFixture(TypeArgs = new[] { typeof(FruitsWeirdAll), typeof(short), typeof(Int16Number) })]
    public class EnumTransformerTests<TEnum, TUnderlying, TNumberHandler>
        where TEnum : Enum
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
        where TNumberHandler : class, INumber<TUnderlying>
    {
        private static readonly INumber<TUnderlying> _numberHandler =
            NumberHandlerCache.GetNumberHandler<TUnderlying>();

        private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sut =
            (EnumTransformer<TEnum, TUnderlying, TNumberHandler>)new EnumTransformerCreator().CreateTransformer<TEnum>();

        private static TEnum ToEnum(TUnderlying value) => EnumTransformer<TEnum, TUnderlying, TNumberHandler>.ToEnum(value);


        private static IReadOnlyList<TUnderlying> GetEnumValues()
        {
            Type enumType = typeof(TEnum);
            var values = Enum.GetValues(enumType).Cast<TUnderlying>().ToList();


            TUnderlying min = values.Count == 0 ? _numberHandler.Zero : values.Min(),
                        max = values.Count == 0 ? _numberHandler.Zero : values.Max();
            int i = 5;
            while (i-- > 0)
                if (_numberHandler.SupportsNegative && min.CompareTo(_numberHandler.MinValue) > 0)
                    min = _numberHandler.Sub(min, _numberHandler.One);

            i = 5;
            while (i-- > 0)
                if (max.CompareTo(_numberHandler.MaxValue) < 0)
                    max = _numberHandler.Add(max, _numberHandler.One);

            var result = new List<TUnderlying>();

            for (TUnderlying number = min; number.CompareTo(max) <= 0; number = _numberHandler.Add(number, _numberHandler.One))
                result.Add(number);

            return result;
        }

        private static IReadOnlyList<(TEnum Enum, string Text)> GetEnumToText() =>
            GetEnumValues().Select(v => (ToEnum(v), ToEnum(v).ToString("G"))).ToList();

        private static string ToTick(bool result) => result ? "✔" : "✖";
        private static string ToOperator(bool result) => result ? "==" : "!=";

        [TestCase(",,,")]
        [TestCase("1,2,3,4")]
        public void NonFlagEnums_NegativeTests(string input)
        {
            var isFlag = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);
            Assert.That(_sut.IsFlagEnum, Is.EqualTo(isFlag), "Flags attribute is badly retrieved");

            if (isFlag)
                Assert.DoesNotThrow(() => _sut.Parse(input.AsSpan()));
            else
                Assert.Throws<FormatException>(() => _sut.Parse(input.AsSpan()));
        }

        [TestCase(" | ")]
        [TestCase("|")]
        [TestCase("Mississippi")]
        public void BadSource_NegativeTests(string input)
        {
            if (typeof(TEnum) != typeof(EmptyEnum))
                Assert.Throws<FormatException>(() => _sut.Parse(input.AsSpan()));
            else
            {
                TEnum actual = _sut.Parse(input.AsSpan());
                Assert.That(actual, Is.EqualTo((EmptyEnum)0));
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void EmptySource_ShouldReturnDefaultValue(string input)
        {
            var actual = _sut.Parse(input.AsSpan());
            var defaultValue = ToEnum(_numberHandler.Zero);

            Assert.That(actual, Is.EqualTo(defaultValue));
            Assert.That((TUnderlying)(object)actual, Is.EqualTo(_numberHandler.Zero));
        }


        [Test]
        public void ParseNumberFlags() //7 = 1,2,4
        {
            if (!_sut.IsFlagEnum)
                Assert.Pass("Test is not supported for non-flag enums");

            var sb = new StringBuilder();
            bool allPassed = true;
            foreach (TUnderlying number in GetEnumValues())
            {
                if (number.CompareTo(_numberHandler.Zero) <= 0)
                    continue;
                sb.Length = 0;

                var num = number;
                int bitMeaning = 1;
                while (num.CompareTo(_numberHandler.Zero) > 0)
                {
                    bool isSet = _numberHandler.And(num, _numberHandler.One).Equals(_numberHandler.One); //num & 1 == 1
                    if (isSet)
                        sb.Append(bitMeaning).Append(',');
                    num = _numberHandler.ShR(num, 1); // num = num >> 1;
                    bitMeaning *= 2;
                }

                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
                var text = sb.ToString();

                var actual = _sut.Parse(text.AsSpan());
                //var native1 = (TEnum)Enum.Parse(typeof(TEnum), text, true);
                var native = (TEnum)Enum.Parse(typeof(TEnum), number.ToString(), true);
                bool pass = Equals(actual, native);
                Console.WriteLine($"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{native}', {number} ({text})");

                if (!pass)
                    allPassed = false;
            }
            Assert.IsTrue(allPassed);
        }

        [Test]
        public void ParseNumber()
        {
            bool allPassed = true;
            foreach (TUnderlying number in GetEnumValues())
            {
                var actual = _sut.Parse(number.ToString().AsSpan());

                var native = (TEnum)Enum.Parse(typeof(TEnum), number.ToString(), true);

                bool pass = Equals(actual, native);

                Console.WriteLine($"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{native}', {number}");

                if (!pass)
                    allPassed = false;
            }
            Assert.IsTrue(allPassed);
        }

        [Test]
        public void ParseText()
        {
            bool allPassed = true;

            IReadOnlyList<(TEnum Enum, string Text)> enumToText = GetEnumToText();
            
            foreach (var (enumValue, text) in enumToText)
            {
                var actual = _sut.Parse(text.AsSpan());
                var actualNative = (TEnum)Enum.Parse(typeof(TEnum), text, true);

                bool pass = Equals(actual, enumValue) &&
                            Equals(actualNative, enumValue) &&
                            Equals(actual, actualNative);

                Console.WriteLine($"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}");

                if (!pass)
                    allPassed = false;
            }


            Assert.IsTrue(allPassed);
        }

        [Test]
        public void Format()
        {
            bool allPassed = true;

            foreach (var (enumValue, text) in GetEnumToText())
            {
                var actual = _sut.Format(enumValue);
                var actualNative = enumValue.ToString("G");

                bool pass = Equals(actual, text) &&
                            Equals(actualNative, text) &&
                            Equals(actual, actualNative);

                Console.WriteLine($"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{text}'");


                if (!pass)
                    allPassed = false;
            }

            Assert.IsTrue(allPassed);
        }
    }

    [TestFixture]
    public class EnumParsingViaGeneratedCodeTests
    {
        private static string ToTick(bool result) => result ? "✔" : "✖";
        private static string ToOperator(bool result) => result ? "==" : "!=";

        [Test]
        public void ParseViaCSharpCode()
        {
            bool allPassed = true;

            for (int i = 0; i < 135; i++)
            {
                var enumValue = (DaysOfWeek)i;
                var text = enumValue.ToString("G");

                var actual = ParseDaysOfWeek(text.AsSpan());
                var actualNative = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);

                bool pass = Equals(actual, enumValue) &&
                            Equals(actualNative, enumValue) &&
                            Equals(actual, actualNative);

                Console.WriteLine($"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}");

                if (!pass)
                    allPassed = false;
            }


            Assert.IsTrue(allPassed);

        }

        private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;

            var enumStream = input.Split(',').GetEnumerator();

            if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {typeof(DaysOfWeek).Name} enum");
            byte currentValue = ParseDaysOfWeekElement(enumStream.Current);

            while (enumStream.MoveNext())
            {
                var element = ParseDaysOfWeekElement(enumStream.Current);

                currentValue |= element;
            }

            return (DaysOfWeek)currentValue;

        }

        private static byte ParseDaysOfWeekElement(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;
            input = input.Trim();

            return IsNumeric(input) && byte.TryParse(
#if NETFRAMEWORK
                input.ToString()
#else
                input
#endif
                , out byte number) ? number : ParseDaysOfWeekByLabelOr(input);
        }

        private static bool IsNumeric(ReadOnlySpan<char> input)
        {
            char firstChar;
            return input.Length > 0 && (char.IsDigit(firstChar = input[0]) || firstChar == '-' || firstChar == '+');
        }

        /*private static byte ParseDaysOfWeekByLabelStd(ReadOnlySpan<char> text)
        {
            if (text.Length == 4 && char.ToUpper(text[0]) == 'N' && char.ToUpper(text[1]) == 'O' &&
                char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'E'
            )
                return 0;
            else if (text.Length == 6 && char.ToUpper(text[0]) == 'M' && char.ToUpper(text[1]) == 'O' &&
                char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' &&
                char.ToUpper(text[5]) == 'Y'
            )
                return 1;
            else if (text.Length == 7 && char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'U' &&
                char.ToUpper(text[2]) == 'E' && char.ToUpper(text[3]) == 'S' && char.ToUpper(text[4]) == 'D' &&
                char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y'
            )
                return 2;
            else if (text.Length == 9 && char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' &&
                char.ToUpper(text[2]) == 'D' && char.ToUpper(text[3]) == 'N' && char.ToUpper(text[4]) == 'E' &&
                char.ToUpper(text[5]) == 'S' && char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'A' &&
                char.ToUpper(text[8]) == 'Y'
            )
                return 4;
            else if (text.Length == 8 && char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'H' &&
                char.ToUpper(text[2]) == 'U' && char.ToUpper(text[3]) == 'R' &&
                char.ToUpper(text[4]) == 'S' && char.ToUpper(text[5]) == 'D' &&
                char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y'
            )
                return 8;
            else if (text.Length == 6 && char.ToUpper(text[0]) == 'F' && char.ToUpper(text[1]) == 'R' &&
                char.ToUpper(text[2]) == 'I' && char.ToUpper(text[3]) == 'D' &&
                char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y'
            )
                return 16;
            else if (text.Length == 8 && char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'A' &&
                char.ToUpper(text[2]) == 'T' && char.ToUpper(text[3]) == 'U' &&
                char.ToUpper(text[4]) == 'R' && char.ToUpper(text[5]) == 'D' &&
                char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y'
            )
                return 32;
            else if (text.Length == 6 && char.ToUpper(text[0]) == 'S' &&
                char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'N' &&
                char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' &&
                char.ToUpper(text[5]) == 'Y'
            )
                return 64;
            else if (text.Length == 8 && char.ToUpper(text[0]) == 'W' &&
                char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'D' &&
                char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y' &&
                char.ToUpper(text[7]) == 'S'
            )
                return 31;
            else if (text.Length == 8 && char.ToUpper(text[0]) == 'W' &&
                char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'E' &&
                char.ToUpper(text[5]) == 'N' && char.ToUpper(text[6]) == 'D' &&
                char.ToUpper(text[7]) == 'S'
            )
                return 96;
            else if (text.Length == 3 && char.ToUpper(text[0]) == 'A' &&
                     char.ToUpper(text[1]) == 'L' && char.ToUpper(text[2]) == 'L'
            )
                return 127;
            else
                throw new FormatException("Enum of type 'DaysOfWeek' cannot be parsed. Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");
            //return 0;
        }

        private static byte ParseDaysOfWeekByLabelSwitch(ReadOnlySpan<char> text)
        {
            switch (text.Length)
            {
                case 4:
                    if (char.ToUpper(text[0]) == 'N' && char.ToUpper(text[1]) == 'O' && char.ToUpper(text[2]) == 'N' && char.ToUpper(text[3]) == 'E')
                        return 0;
                    else
                        break;
                case 6:
                    if (char.ToUpper(text[0]) == 'M' && char.ToUpper(text[1]) == 'O' && char.ToUpper(text[2]) == 'N' &&
                        char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                        return 1;
                    else if (char.ToUpper(text[0]) == 'F' && char.ToUpper(text[1]) == 'R' && char.ToUpper(text[2]) == 'I' &&
                             char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                        return 16;
                    else if (char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'N' &&
                             char.ToUpper(text[3]) == 'D' && char.ToUpper(text[4]) == 'A' && char.ToUpper(text[5]) == 'Y')
                        return 64;
                    else
                        break;
                case 7:
                    if (char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'U' && char.ToUpper(text[2]) == 'E' &&
                        char.ToUpper(text[3]) == 'S' && char.ToUpper(text[4]) == 'D' && char.ToUpper(text[5]) == 'A' && char.ToUpper(text[6]) == 'Y')
                        return 2;
                    else
                        break;
                case 8:
                    if (char.ToUpper(text[0]) == 'T' && char.ToUpper(text[1]) == 'H' && char.ToUpper(text[2]) == 'U' &&
                        char.ToUpper(text[3]) == 'R' && char.ToUpper(text[4]) == 'S' && char.ToUpper(text[5]) == 'D' &&
                        char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y')
                        return 8;
                    else if (char.ToUpper(text[0]) == 'S' && char.ToUpper(text[1]) == 'A' && char.ToUpper(text[2]) == 'T' &&
                             char.ToUpper(text[3]) == 'U' && char.ToUpper(text[4]) == 'R' && char.ToUpper(text[5]) == 'D' &&
                             char.ToUpper(text[6]) == 'A' && char.ToUpper(text[7]) == 'Y')
                        return 32;

                    else if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                             char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'D' && char.ToUpper(text[5]) == 'A' &&
                             char.ToUpper(text[6]) == 'Y' && char.ToUpper(text[7]) == 'S')
                        return 31;
                    else if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'E' &&
                             char.ToUpper(text[3]) == 'K' && char.ToUpper(text[4]) == 'E' && char.ToUpper(text[5]) == 'N' &&
                             char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'S')
                        return 96;
                    else
                        break;
                case 9:
                    if (char.ToUpper(text[0]) == 'W' && char.ToUpper(text[1]) == 'E' && char.ToUpper(text[2]) == 'D' &&
                        char.ToUpper(text[3]) == 'N' && char.ToUpper(text[4]) == 'E' && char.ToUpper(text[5]) == 'S' &&
                        char.ToUpper(text[6]) == 'D' && char.ToUpper(text[7]) == 'A' && char.ToUpper(text[8]) == 'Y')
                        return 4;
                    else
                        break;
                default:
                    if (char.ToUpper(text[0]) == 'A' && char.ToUpper(text[1]) == 'L' && char.ToUpper(text[2]) == 'L')
                        return 127;
                    else
                        break;
            }
            throw new FormatException("Enum of type 'DaysOfWeek' cannot be parsed. Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");

        }*/

        private static byte ParseDaysOfWeekByLabelOr(ReadOnlySpan<char> input)
        {
            if (input.Length == 4 && (input[3] == 'E' || input[3] == 'e') && (input[2] == 'N' || input[2] == 'n') &&
                (input[1] == 'O' || input[1] == 'o') && (input[0] == 'N' || input[0] == 'n')
            )
                return 0;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') && (input[4] == 'A' || input[4] == 'a') &&
                (input[3] == 'D' || input[3] == 'd') && (input[2] == 'N' || input[2] == 'n') &&
                (input[1] == 'O' || input[1] == 'o') && (input[0] == 'M' || input[0] == 'm')
            )
                return 1;
            else if (
                input.Length == 7 && (input[6] == 'Y' || input[6] == 'y') && (input[5] == 'A' || input[5] == 'a') &&
                (input[4] == 'D' || input[4] == 'd') && (input[3] == 'S' || input[3] == 's') &&
                (input[2] == 'E' || input[2] == 'e') && (input[1] == 'U' || input[1] == 'u') &&
                (input[0] == 'T' || input[0] == 't')
            )
                return 2;
            else if (
                input.Length == 9 && (input[8] == 'Y' || input[8] == 'y') &&
                (input[7] == 'A' || input[7] == 'a') && (input[6] == 'D' || input[6] == 'd') &&
                (input[5] == 'S' || input[5] == 's') && (input[4] == 'E' || input[4] == 'e') &&
                (input[3] == 'N' || input[3] == 'n') && (input[2] == 'D' || input[2] == 'd') &&
                (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
            )
                return 4;
            else if (
                input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                (input[4] == 'S' || input[4] == 's') && (input[3] == 'R' || input[3] == 'r') &&
                (input[2] == 'U' || input[2] == 'u') && (input[1] == 'H' || input[1] == 'h') &&
                (input[0] == 'T' || input[0] == 't')
            )
                return 8;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                (input[2] == 'I' || input[2] == 'i') && (input[1] == 'R' || input[1] == 'r') &&
                (input[0] == 'F' || input[0] == 'f')
            )
                return 16;
            else if (
                input.Length == 8 && (input[7] == 'Y' || input[7] == 'y') &&
                (input[6] == 'A' || input[6] == 'a') && (input[5] == 'D' || input[5] == 'd') &&
                (input[4] == 'R' || input[4] == 'r') && (input[3] == 'U' || input[3] == 'u') &&
                (input[2] == 'T' || input[2] == 't') && (input[1] == 'A' || input[1] == 'a') &&
                (input[0] == 'S' || input[0] == 's')
            )
                return 32;
            else if (
                input.Length == 6 && (input[5] == 'Y' || input[5] == 'y') &&
                (input[4] == 'A' || input[4] == 'a') && (input[3] == 'D' || input[3] == 'd') &&
                (input[2] == 'N' || input[2] == 'n') && (input[1] == 'U' || input[1] == 'u') &&
                (input[0] == 'S' || input[0] == 's')
            )
                return 64;
            else if (
                input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                (input[6] == 'Y' || input[6] == 'y') &&
                (input[5] == 'A' || input[5] == 'a') &&
                (input[4] == 'D' || input[4] == 'd') &&
                (input[3] == 'K' || input[3] == 'k') &&
                (input[2] == 'E' || input[2] == 'e') &&
                (input[1] == 'E' || input[1] == 'e') && (input[0] == 'W' || input[0] == 'w')
            )
                return 31;
            else if (
                input.Length == 8 && (input[7] == 'S' || input[7] == 's') &&
                (input[6] == 'D' || input[6] == 'd') &&
                (input[5] == 'N' || input[5] == 'n') &&
                (input[4] == 'E' || input[4] == 'e') &&
                (input[3] == 'K' || input[3] == 'k') &&
                (input[2] == 'E' || input[2] == 'e') &&
                (input[1] == 'E' || input[1] == 'e') &&
                (input[0] == 'W' || input[0] == 'w')
            )
                return 96;
            else if (
                input.Length == 3 && (input[2] == 'L' || input[2] == 'l') &&
                (input[1] == 'L' || input[1] == 'l') &&
                (input[0] == 'A' || input[0] == 'a')
            )
                return 127;
            else
                throw new FormatException(
                    "Enum of type 'DaysOfWeek' cannot be parsed.Valid values are: None or Monday or Tuesday or Wednesday or Thursday or Friday or Saturday or Sunday or Weekdays or Weekends or All or number within Byte range.");
        }
    }
}

/* \.Call Nemesis\.TextParsers\.EnumTransformerHelper\.CharEq\(\s*input,\s*(\d+),\s*('\w'),\s*('\w')\)
   =>
   \(input[\1]==\2 || input[\1]==\3 \)
 
\.Return\s*exit\s*\{\s*\.Constant<System\.Byte>\((\d+)\)\s*\}
 =>
 return \1;
 */
