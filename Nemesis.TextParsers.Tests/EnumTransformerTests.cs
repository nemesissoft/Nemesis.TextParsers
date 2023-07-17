using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests;

#region Stubs
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
internal enum EmptyEnum { }

internal enum Enum1 { E1_1_Int }

internal enum Enum2 { E2_1_Int, E2_2_Int }

internal enum Enum3 { E3_1_Int, E3_2_Int, E3_3_Int }

internal enum ByteEnum : byte { B1 = 0, B2 = 1, B3 = 2 }

internal enum SByteEnum : sbyte { Sb1 = -10, Sb2 = 0, Sb3 = 5, Sb4 = 10 }

internal enum Int64Enum : long { L1 = -50, L2 = 0, L3 = 1, L4 = 50 }

internal enum UInt64Enum : ulong { Ul_1 = 0, Ul_2 = 1, Ul_3 = 50 }

internal enum Casing { A, a, B, b, C, c }

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

[TestFixture(TypeArgs = new[] { typeof(DaysOfWeek), typeof(byte), typeof(ByteTransformer) })]
[TestFixture(TypeArgs = new[] { typeof(EmptyEnum), typeof(int), typeof(Int32Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(Enum1), typeof(int), typeof(Int32Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(Enum2), typeof(int), typeof(Int32Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(Enum3), typeof(int), typeof(Int32Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(ByteEnum), typeof(byte), typeof(ByteTransformer) })]
[TestFixture(TypeArgs = new[] { typeof(SByteEnum), typeof(sbyte), typeof(SByteTransformer) })]
[TestFixture(TypeArgs = new[] { typeof(Int64Enum), typeof(long), typeof(Int64Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(UInt64Enum), typeof(ulong), typeof(UInt64Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(Fruits), typeof(ushort), typeof(UInt16Transformer) })]
[TestFixture(TypeArgs = new[] { typeof(FruitsWeirdAll), typeof(short), typeof(Int16Transformer) })]
public class EnumTransformerTests<TEnum, TUnderlying, TNumberHandler>
    where TEnum : Enum
    where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
    where TNumberHandler : NumberTransformer<TUnderlying>
{
    private static readonly TNumberHandler _numberHandler =
        (TNumberHandler)NumberTransformerCache.GetNumberHandler<TUnderlying>();

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sut =
        new(_numberHandler, EnumSettings.Default);

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sutCaseSensitive =
        new(_numberHandler, EnumSettings.Default.With(s => s.CaseInsensitive, false));

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sutOnlyEnumNames =
        new(_numberHandler, EnumSettings.Default.With(s => s.AllowParsingNumerics, false));


    private static TEnum ToEnum(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);

    private static readonly IReadOnlyList<(TUnderlying Number, TEnum Enum, string Text)> _testValues = GetTestValues();
    private static IReadOnlyList<(TUnderlying Number, TEnum Enum, string Text)> GetTestValues()
    {
        Type enumType = typeof(TEnum);
        var values = Enum.GetValues(enumType).Cast<TUnderlying>().ToList();


        TUnderlying min = values.Count == 0 ? _numberHandler.Zero : values.Min(),
                    max = values.Count == 0 ? _numberHandler.Zero : values.Max();

        int iMin = 10, iMax = 10;
        while (iMin-- > 0)
            if (min.CompareTo(_numberHandler.MinValue) > 0)
                min = _numberHandler.Sub(min, _numberHandler.One);
            else iMax++;

        while (iMax-- > 0)
            if (max.CompareTo(_numberHandler.MaxValue) < 0)
                max = _numberHandler.Add(max, _numberHandler.One);

        var result = new List<(TUnderlying, TEnum, string)>();

        for (var number = min; number.CompareTo(max) <= 0; number = _numberHandler.Add(number, _numberHandler.One))
            result.Add(
                (number, ToEnum(number), ToEnum(number).ToString("G"))
                );

        return result.AsReadOnly();
    }

    private static readonly IReadOnlyList<(TUnderlying Number, TEnum Enum, string Text)> _definedValues =
        Enum.GetValues(typeof(TEnum)).Cast<TUnderlying>()
            .Select(number => (number, ToEnum(number), ToEnum(number).ToString("G")))
            .ToList().AsReadOnly();

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
            var zero = 0;
            var result = Unsafe.As<int, TEnum>(ref zero);
            Assert.That(actual, Is.EqualTo(result));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void EmptySource_ShouldReturnDefaultValue(string input)
    {
        var actual = _sut.Parse(input.AsSpan());
        var defaultValue = ToEnum(_numberHandler.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(defaultValue));
            Assert.That((TUnderlying)(object)actual, Is.EqualTo(_numberHandler.Zero));
        });
    }

    [Test]
    public void ParseNumberFlags() //7 = 1,2,4
    {
        if (!_sut.IsFlagEnum)
            Assert.Pass("Test is not supported for non-flag enums");

        var sb = new StringBuilder();
        var failed = new List<string>();
        foreach (var (number, _, _) in _testValues)
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

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{native}', {number} ({text})";

            if (!pass)
                failed.Add(message);
        }
        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }

    private static Func<string> GetFailedMessageBuilder(IEnumerable<string> failed) =>
        () => $"Failed cases:{Environment.NewLine}{string.Join(Environment.NewLine, failed)}";

    [Test]
    public void ParseNumber()
    {
        var failed = new List<string>();
        foreach (var (number, _, _) in _testValues)
        {
            var actual = _sut.Parse(number.ToString().AsSpan());

            var native = (TEnum)Enum.Parse(typeof(TEnum), number.ToString(), true);

            // ReSharper disable once PossibleInvalidCastException
            var cast = (TEnum)(object)number;

            bool pass = Equals(actual, native) && Equals(actual, cast);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{native}', {number}";

            if (!pass)
                failed.Add(message);
        }
        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }

    [Test]
    public void ParseText()
    {
        var failed = new List<string>();

        foreach (var (_, enumValue, text) in _testValues)
        {
            var actual = _sut.Parse(text.AsSpan());
            var actualNative = (TEnum)Enum.Parse(typeof(TEnum), text, true);

            bool pass = Equals(actual, enumValue) &&
                        Equals(actualNative, enumValue) &&
                        Equals(actual, actualNative);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}";

            if (!pass)
                failed.Add(message);
        }

        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }

    [Test]
    public void Format()
    {
        var failed = new List<string>();

        foreach (var (_, enumValue, text) in _testValues)
        {
            var actual = _sut.Format(enumValue);
            var actualNative = enumValue.ToString("G");

            bool pass = Equals(actual, text) &&
                        Equals(actualNative, text) &&
                        Equals(actual, actualNative);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{text}'";

            if (!pass)
                failed.Add(message);
        }

        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }




    [Test]
    public void DoNotAllowParsingNumerics_Exploratory()
    {
        if (_definedValues.Count == 0)
            Assert.Pass("Test is not designed for empty enums");

        foreach (var (_, enumValue, text) in _testValues)
        {
            Assert.DoesNotThrow(() => _sut.Parse(text));

            bool isNumber = _numberHandler.TryParse(text.AsSpan(), out _);
            if (isNumber)
            {
                var assertMessage = $"Parsing '{text}'";

                var ex = Assert.Throws<FormatException>(() => _sutOnlyEnumNames.Parse(text), assertMessage);

                Assert.That(ex.Message, Does.Contain("cannot be parsed. Valid values are:"), assertMessage);
                Assert.That(ex.Message, Does.Not.Contain("or number within"), assertMessage);
            }
            else
            {
                var actual = _sutOnlyEnumNames.Parse(text);
                Assert.That(actual, Is.EqualTo(enumValue));
            }
        }
    }

    [Test]
    public void CaseSensitive_PositiveExploratory()
    {
        foreach (var (value, _, name) in _definedValues)
        {
            var init = _sut.Parse(name);
            Assert.That((TUnderlying)(object)init, Is.EqualTo(value));

            var actual = _sutCaseSensitive.Parse(name.AsSpan());
            var actualNative = (TEnum)Enum.Parse(typeof(TEnum), name, false);
            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(actualNative));
                Assert.That((TUnderlying)(object)actual, Is.EqualTo(value));
            });
        }
    }

    [Test]
    public void CaseSensitive_NegativeExploratory()
    {
        var rand = new Random();
        string ShuffleText(in string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            int i = 0;
            string newText;
            do
            {
                var chars = text.ToUpperInvariant().ToCharArray();
                var index = rand.Next(chars.Length);
                chars[index] = char.ToLower(chars[index]);

                newText = new string(chars);
                if (i++ > 200)
                    Assert.Fail($"Cannot construct different casing for '{text}'");
            } while (newText == text);

            return newText;
        }

        var shuffledNames = _definedValues
            .Select(v => ShuffleText(v.Text)).ToList();

        foreach (var name in shuffledNames)
        {
            Assert.DoesNotThrow(() => _sut.Parse(name));

            var assertMessage = $"Parsing '{name}'";

            var ex = Assert.Throws<FormatException>(() => _sutCaseSensitive.Parse(name), assertMessage);

            Assert.That(ex.Message, Does.Contain("cannot be parsed. Valid values are:"), assertMessage);
            Assert.That(ex.Message, Does.Contain("Case sensitive option on"), assertMessage);
        }
    }

    [Test]
    public static void CaseSensitive_Weird()
    {
        var sut = new EnumTransformer<Casing, int, Int32Transformer>((Int32Transformer)Int32Transformer.Instance, EnumSettings.Default.With(s => s.CaseInsensitive, false));

        var enumValues = typeof(Casing).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(enumField => (enumField.Name, Value: (Casing)(int)enumField.GetValue(null))).ToList();

        foreach (var (name, value) in enumValues)
        {
            var actual = sut.Parse(name.AsSpan());
            var actualNative = (Casing)Enum.Parse(typeof(Casing), name, false);

            Assert.That(actual, Is.EqualTo(actualNative));
            Assert.That(actual, Is.EqualTo(value));
        }
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
        var failed = new List<string>();

        for (int i = 0; i < 135; i++)
        {
            var enumValue = (DaysOfWeek)i;
            var text = enumValue.ToString("G");

            var actual = ParseDaysOfWeek(text.AsSpan());
            var actualNative = (DaysOfWeek)Enum.Parse(typeof(DaysOfWeek), text, true);

            bool pass = Equals(actual, enumValue) &&
                        Equals(actualNative, enumValue) &&
                        Equals(actual, actualNative);

            string message = $"{ToTick(pass)}  '{actual}' {ToOperator(pass)} '{actualNative}', '{enumValue}', {enumValue:D}, 0x{enumValue:X}";

            if (!pass)
                failed.Add(message);
        }

        Assert.That(failed, Is.Empty, () =>
            $"Failed cases:{Environment.NewLine}{string.Join(Environment.NewLine, failed)}"
        );
    }

    private static DaysOfWeek ParseDaysOfWeek(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;

        var enumStream = input.Split(',').GetEnumerator();

        if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {nameof(DaysOfWeek)} enum");
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

[TestFixture]
public class RegexOptionsTransformerTests
{
    [Test]
    public void ParseTests()
    {
        var sut = RegexOptionsTransformer.Instance;

        for (int i = 0; i < 1024; i++)
        {
            var option = (RegexOptions)i;

            var text = sut.Format(option);

            var parsed1 = sut.Parse(text);
            var parsed2 = sut.Parse(text);
            Assert.Multiple(() =>
            {
                Assert.That(parsed1, Is.EqualTo(option), $"Case {i}");
                Assert.That(parsed2, Is.EqualTo(option), $"Case {i}");
            });
        }
    }
}