using System.Collections.ObjectModel;
using Nemesis.TextParsers.Parsers;
using Nemesis.TextParsers.Settings;
using Nemesis.TextParsers.Tests.Utils;

namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture(TypeArgs = [typeof(DaysOfWeek), typeof(byte), typeof(ByteTransformer)])]
[TestFixture(TypeArgs = [typeof(EmptyEnum), typeof(int), typeof(Int32Transformer)])]
[TestFixture(TypeArgs = [typeof(Enum1), typeof(int), typeof(Int32Transformer)])]
[TestFixture(TypeArgs = [typeof(Enum2), typeof(int), typeof(Int32Transformer)])]
[TestFixture(TypeArgs = [typeof(Enum3), typeof(int), typeof(Int32Transformer)])]
[TestFixture(TypeArgs = [typeof(ByteEnum), typeof(byte), typeof(ByteTransformer)])]
[TestFixture(TypeArgs = [typeof(SByteEnum), typeof(sbyte), typeof(SByteTransformer)])]
[TestFixture(TypeArgs = [typeof(Int64Enum), typeof(long), typeof(Int64Transformer)])]
[TestFixture(TypeArgs = [typeof(UInt64Enum), typeof(ulong), typeof(UInt64Transformer)])]
[TestFixture(TypeArgs = [typeof(Fruits), typeof(ushort), typeof(UInt16Transformer)])]
[TestFixture(TypeArgs = [typeof(FruitsWeirdAll), typeof(short), typeof(Int16Transformer)])]
public class EnumTransformerTests<TEnum, TUnderlying, TNumberHandler>
    where TEnum : struct, Enum
    where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TUnderlying>
#endif
    where TNumberHandler : NumberTransformer<TUnderlying>
{
    private static readonly TNumberHandler _numberHandler =
        (TNumberHandler) NumberTransformerCache.Instance.GetNumberHandler<TUnderlying>();

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sut =
        new(_numberHandler, EnumSettings.Default);

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sutCaseSensitive =
        new(_numberHandler, EnumSettings.Default with {CaseInsensitive = false});

    private static readonly EnumTransformer<TEnum, TUnderlying, TNumberHandler> _sutOnlyEnumNames =
        new(_numberHandler, EnumSettings.Default with {AllowParsingNumerics = false});


    private static TEnum ToEnum(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);

    private static readonly IReadOnlyList<(TUnderlying Number, TEnum Enum, string Text)> _testValues = GetTestValues();

    private static ReadOnlyCollection<(TUnderlying Number, TEnum Enum, string Text)> GetTestValues()
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
            Assert.DoesNotThrow(() => _sut.Parse(input));
        else
            Assert.Throws<FormatException>(() => _sut.Parse(input));
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
            Assert.That((TUnderlying) (object) actual, Is.EqualTo(_numberHandler.Zero));
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
            var native = (TEnum) Enum.Parse(typeof(TEnum), number.ToString() ?? "", true);
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

            var native = (TEnum) Enum.Parse(typeof(TEnum), number.ToString() ?? "", true);

            var cast = (TEnum) (object) number;

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
            var actualNative = (TEnum) Enum.Parse(typeof(TEnum), text, true);

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
                Assert.That(() => _sutOnlyEnumNames.Parse(text),
                    Throws.TypeOf<FormatException>().And
                        .Message.Contains("cannot be parsed. Valid values are:").And
                        .Message.Not.Contains("or number within"),
                    () => $"Parsing '{text}'"
                );
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
            Assert.That((TUnderlying) (object) init, Is.EqualTo(value));

            var actual = _sutCaseSensitive.Parse(name.AsSpan());
            var actualNative = (TEnum) Enum.Parse(typeof(TEnum), name, false);
            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(actualNative));
                Assert.That((TUnderlying) (object) actual, Is.EqualTo(value));
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

            Assert.That(() => _sutCaseSensitive.Parse(name),
                Throws.TypeOf<FormatException>().And
                    .Message.Contains("cannot be parsed. Valid values are:").And
                    .Message.Contains("Case sensitive option on"),
                () => $"Parsing '{name}'"
            );
        }
    }

    [Test]
    public static void CaseSensitive_Weird()
    {
        var sut = new EnumTransformer<Casing, int, Int32Transformer>(
            (Int32Transformer) Int32Transformer.Instance,
            EnumSettings.Default with {CaseInsensitive = false});

        var enumValues = typeof(Casing).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(enumField => (enumField.Name, Value: (Casing) (int) enumField.GetValue(null))).ToList();

        foreach (var (name, value) in enumValues)
        {
            var actual = sut.Parse(name.AsSpan());
            var actualNative = (Casing) Enum.Parse(typeof(Casing), name, false);

            Assert.That(actual, Is.EqualTo(actualNative));
            Assert.That(actual, Is.EqualTo(value));
        }
    }
}