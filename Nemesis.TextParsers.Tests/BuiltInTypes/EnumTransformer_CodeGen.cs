#nullable enable
using System.Globalization;
using Nemesis.Essentials.Runtime;
using Nemesis.TextParsers.Parsers;

namespace Nemesis.TextParsers.Tests.BuiltInTypes;

[TestFixture(TypeArgs = new[] { typeof(Generated.Month), typeof(byte) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.Months), typeof(short) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.DaysOfWeek), typeof(byte) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.EmptyEnum), typeof(ulong) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.EmptyEnumWithNumberParsing), typeof(uint) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.SByteEnum), typeof(sbyte) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.Int64Enum), typeof(long) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.Casing), typeof(int) })]
[TestFixture(TypeArgs = new[] { typeof(Generated.Місяць), typeof(ushort) })]
public class EnumTransformer_CodeGen<TEnum, TUnderlying>
    where TEnum : struct, Enum
    where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TUnderlying>
#endif    
{
    private static readonly NumberTransformer<TUnderlying> _numberHandler = NumberTransformerCache.Instance.GetNumberHandler<TUnderlying>();

    private static readonly ITransformer<TEnum> _sut = TextTransformer.Default.GetTransformer<TEnum>();

    private static readonly EnumMeta _meta = EnumMeta.GetEnumMeta();

    readonly record struct EnumValue(TUnderlying Number, TEnum Enum, string Text);

    readonly record struct EnumMeta(
        Type EnumType, Type TransformerType,
        bool CaseInsensitive, bool AllowParsingNumerics, bool HasFlags,
        IReadOnlyList<EnumValue> DefinedValues)
    {
        public static EnumMeta GetEnumMeta()
        {
            Type enumType = typeof(TEnum);

            var transformerAttr = enumType.GetCustomAttribute<TransformerAttribute>() ?? throw new NotSupportedException("No TransformerAttribute present");
            var codeGenOptionsAttr = enumType.GetCustomAttribute<Auto.AutoEnumTransformerAttribute>() ?? throw new NotSupportedException("No CodeGenOptionsAttribute present");
            var hasFlags = enumType.IsDefined(typeof(FlagsAttribute), false);

            return new(
               enumType,
               transformerAttr.TransformerType,
               codeGenOptionsAttr.CaseInsensitive,
               codeGenOptionsAttr.AllowParsingNumerics,
               hasFlags,
               Enum.GetValues(typeof(TEnum)).Cast<TUnderlying>()
                .Select(number => FromNumber(number))
                .ToList().AsReadOnly()
            );
        }

        private static EnumValue FromNumber(TUnderlying number) => new(number, ToEnum(number), ToEnum(number).ToString("G"));

        public IReadOnlyList<EnumValue> GetValidValues()
        {
            var seed = Environment.TickCount;
            Console.WriteLine($"Seed {seed}");
            Random rand = new(seed);

            var (zero, one, minValue, maxValue) = _numberHandler;
            var numbers = new SortedSet<TUnderlying>(DefinedValues.Select(v => v.Number));


            if (HasFlags && DefinedValues.Count > 3)
            {
                var count = DefinedValues.Count;
                for (int i = 0; i < count; i++)
                {
                    int x = rand.Next(count);
                    int y = x, z = x;

                    while (y == x)
                        y = rand.Next(count);
                    var newFlag = _numberHandler.Or(DefinedValues[x].Number, DefinedValues[y].Number);
                    numbers.Add(newFlag);

                    if (i % 3 == 0)
                    {
                        while (z == x || z == y)
                            z = rand.Next(count);
                        newFlag = _numberHandler.Or(DefinedValues[z].Number, newFlag);
                        numbers.Add(newFlag);
                    }
                }
            }

            if (AllowParsingNumerics)
            {
                var values = DefinedValues.Select(v => v.Number).ToList();

                TUnderlying min = values.Count == 0 ? zero : values.Min(),
                            max = values.Count == 0 ? zero : values.Max();

                int iMin = 10, iMax = 10;
                while (iMin-- > 0)
                    if (min.CompareTo(minValue) > 0)
                        min = _numberHandler.Sub(min, one);
                    else iMax++;

                while (iMax-- > 0)
                    if (max.CompareTo(maxValue) < 0)
                        max = _numberHandler.Add(max, one);

                for (var number = min; number.CompareTo(max) <= 0; number = _numberHandler.Add(number, one))
                    numbers.Add(number);
            }

            var result = numbers.Select(FromNumber).ToList();

            if (CaseInsensitive && result.Count > 0)
            {
                var stop = result.Count / 2 + 1;

                for (int i = 0; i < stop; i++)
                {
                    var original = result[rand.Next(result.Count)];
                    result.Add(original with { Text = RandomizeCase(rand, original.Text) });
                }
            }

            return result.AsReadOnly();
        }

        public static string RandomizeCase(Random rand, string text)
        {
            var chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (rand.NextDouble() > 0.5 && chars[i] is char c && char.IsLetter(c))
                    chars[i] = char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c);
            }

            return new string(chars);
        }

        public static TEnum ToEnum(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);
    }

    private static Func<string> GetFailedMessageBuilder(IEnumerable<string> failed) =>
        () => $"Failed cases:{Environment.NewLine}{string.Join(Environment.NewLine, failed)}";


    [Test]
    public void Sut_ShouldBe_Generated_AndNot_Standard_EnumTransformer()
    {
        var sutType = _sut.GetType();
        var generatedCodeAttr = sutType.GetCustomAttribute<System.CodeDom.Compiler.GeneratedCodeAttribute>();
        var compilerGeneratedAttr = sutType.GetCustomAttribute<CompilerGeneratedAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(sutType, Is.EqualTo(_meta.TransformerType));

            Assert.That(sutType.DerivesOrImplementsGeneric(typeof(EnumTransformer<,,>)), Is.False);
            Assert.That(sutType.Namespace, Is.EqualTo("Generated"));

            Assert.That(generatedCodeAttr, Is.Not.Null);
            Assert.That(generatedCodeAttr!.Tool, Is.EqualTo("EnumTransformerGenerator"));

            Assert.That(compilerGeneratedAttr, Is.Not.Null);
        });
    }


    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void EmptySource_ShouldReturnDefaultValue(string input)
    {
        var actual = _sut.Parse(input.AsSpan());
        var defaultValue = EnumMeta.ToEnum(_numberHandler.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(defaultValue));
            Assert.That((TUnderlying)(object)actual, Is.EqualTo(_numberHandler.Zero));
        });
    }

    [Test]
    public void Positive_Parse_CompareWithNative()
    {
        var failed = new List<string>();
        foreach (var (number, enumValue, text) in _meta.GetValidValues())
        {
            var actual = _sut.Parse(text);

            var native = (TEnum)Enum.Parse(typeof(TEnum), text, _meta.CaseInsensitive);

            var cast = (TEnum)(object)number;

            bool pass = Equals(actual, enumValue) &&
                        Equals(native, enumValue) &&
                        Equals(actual, native) &&
                        Equals(actual, cast);


            if (!pass)
                failed.Add($"✖ '{actual}' != '{native}', '{enumValue}', '{cast}', {number}, {number:D}, 0x{number:X}");
        }
        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }

    [Test]
    public void Positive_Format_CompareWithNative()
    {
        var failed = new List<string>();
        foreach (var (number, enumValue, text) in _meta.GetValidValues())
        {
            var actual = _sut.Format(enumValue);
            var native = enumValue.ToString("G");

            static bool IsEqual(string left, string right) =>
                string.Equals(left, right, _meta.CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            bool pass = string.Equals(actual, native, StringComparison.Ordinal) &&
                        IsEqual(actual, text) &&
                        IsEqual(native, text);

            if (!pass)
                failed.Add($"✖  '{actual}' != '{native}', '{text}'");
        }

        Assert.That(failed, Is.Empty, GetFailedMessageBuilder(failed));
    }

    [Test]
    public void Negative_AllowParsingNumerics_InputOutsideDomain_ShouldNotBeParsed()
    {
        if (_meta.AllowParsingNumerics)
        {
            var (_, _, minValue, maxValue) = _numberHandler;
            var cult = CultureInfo.InvariantCulture;

            var min = BigInteger.Parse(minValue.ToString(cult), cult);
            var max = BigInteger.Parse(maxValue.ToString(cult), cult);
            var faultyNumbers = new List<string>(8);
            for (int i = 1; i <= 4; i++)
            {
                faultyNumbers.Add((min - i).ToString());
                faultyNumbers.Add((max + i).ToString());
            }

            Assert.Multiple(() =>
            {
                foreach (var num in faultyNumbers)
                {
                    Assert.That(() => _sut.Parse(num),
                        Throws.TypeOf<FormatException>().And
                        .Message.Contain($"cannot be parsed from '{num.ToString(cult)}'").And
                        .Message.Contain($"or number within {typeof(TUnderlying).GetFriendlyName()} range")
                   );
                }
            });
        }
        else
        {
            Assert.That(() => _sut.Parse("69"),
                Throws.TypeOf<FormatException>().And
                .Message.Contain($"cannot be parsed from '69'").And
                .Message.Not.Contain($"or number within {typeof(TUnderlying).GetFriendlyName()} range")
            );
        }
    }

    [Test]
    public void Negative_CaseInsensitive_InputOutsideDomain_ShouldNotBeParsed()
    {
        if (_meta.CaseInsensitive)
        {
            Assert.Pass("Test designed for case sensitive parsers");
        }
        else
        {
            Assert.Multiple(() =>
            {
                var definedValuesTexts = _meta.DefinedValues.Select(v => v.Text).ToList();

                foreach (var text in definedValuesTexts)
                {
                    if (definedValuesTexts.Any(v =>
                        string.Equals(v, text, StringComparison.Ordinal) == false &&
                        string.Equals(v, text, StringComparison.OrdinalIgnoreCase)
                    ))
                        continue;

                    var flippedCase = FlipCase(text);

                    Assert.That(() => _sut.Parse(flippedCase),
                        Throws.TypeOf<FormatException>().And
                        .Message.Contain($"cannot be parsed from '{flippedCase}'"),
                        () => $"Case '{text}' => '{flippedCase}'"
                   );
                }
            });

            static string FlipCase(string text)
            {
                var chars = text.ToCharArray();

                for (int i = 0; i < chars.Length; i++)
                {
                    if (chars[i] is char c && char.IsLetter(c))
                        chars[i] = char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c);
                }

                return new string(chars);
            }
        }
    }

    [Test]
    public void Negative_Flags_NonFlagEnum_ShouldNotParseFlagInput()
    {
        if (_meta.HasFlags)
            Assert.Pass("Test designed for non flag enums");
        else
        {
            Assert.Multiple(() =>
            {
                foreach (var input in _meta.DefinedValues.Select(v => $"{v.Text},{v.Text}").Concat([","]).ToList())
                {
                    Assert.That(() => _sut.Parse(input),
                        Throws.TypeOf<FormatException>().And
                        .Message.Contain($"cannot be parsed from '{input}'")
                   );
                }
            });
        }
    }
}

