using System.Net;

using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Utils;

namespace Nemesis.TextParsers.Parsers;

public abstract class SimpleTransformer<TElement> : TransformerBase<TElement>
{
    public sealed override string ToString() => $"Transform {typeof(TElement).GetFriendlyName()}";
}

public sealed class StringTransformer : SimpleTransformer<string>
{
    protected override string ParseCore(in ReadOnlySpan<char> input) => input.ToString();

    public override string Format(string element) => element;

    public override string GetEmpty() => "";

    public static readonly ITransformer<string> Instance = new StringTransformer();

    private StringTransformer() { }
}

public sealed class BooleanTransformer : SimpleTransformer<bool>
{
#if NETSTANDARD2_0 || NETFRAMEWORK
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool EqualsOrdinalIgnoreCase(ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        if (span.Length != value.Length)
            return false;
        if (value.Length == 0)  // span.Length == value.Length == 0
            return true;
        for (int i = span.Length - 1; i >= 0; i--)
            if (char.ToUpperInvariant(span[i]) != char.ToUpperInvariant(value[i]))
                return false;

        return true;
    }

    private const string TRUE_LITERAL = "True";
    private const string FALSE_LITERAL = "False";
    private static bool TryParseBool(ReadOnlySpan<char> value, out bool result)
    {
        ReadOnlySpan<char> trueSpan = TRUE_LITERAL.AsSpan();
        if (EqualsOrdinalIgnoreCase(trueSpan, value))
        {
            result = true;
            return true;
        }

        ReadOnlySpan<char> falseSpan = FALSE_LITERAL.AsSpan();
        if (EqualsOrdinalIgnoreCase(falseSpan, value))
        {
            result = false;
            return true;
        }

        // Special case: Trim whitespace as well as null characters.
        value = TrimWhiteSpaceAndNull(value);

        if (EqualsOrdinalIgnoreCase(trueSpan, value))
        {
            result = true;
            return true;
        }

        if (EqualsOrdinalIgnoreCase(falseSpan, value))
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    private static bool ParseBool(ReadOnlySpan<char> value) =>
        TryParseBool(value, out bool result) ? result : throw new FormatException($"Boolean supports only case insensitive '{TRUE_LITERAL}' or '{FALSE_LITERAL}'");

    private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
    {
        const char NULL_CHAR = (char)0x0000;

        int start = 0;
        while (start < value.Length)
        {
            if (!char.IsWhiteSpace(value[start]) && value[start] != NULL_CHAR)
                break;
            start++;
        }

        int end = value.Length - 1;
        while (end >= start)
        {
            if (!char.IsWhiteSpace(value[end]) && value[end] != NULL_CHAR)
                break;
            end--;
        }

        return value.Slice(start, end - start + 1);
    }
#endif

    protected override bool ParseCore(in ReadOnlySpan<char> input)
    {
        try
        {
            return
#if NETSTANDARD2_0 || NETFRAMEWORK || NETFRAMEWORK
            ParseBool(input.Trim());
#else
            bool.Parse(input.Trim());
#endif
        }
        catch (FormatException e)
        {
            var seq = input.ToString();
            throw new FormatException($"{nameof(Boolean)} type does not recognize sequence {seq}", e);
        }
    }

    public override string Format(bool element) => element ? "True" : "False";

    public static readonly ITransformer<bool> Instance = new BooleanTransformer();

    private BooleanTransformer() { }
}

public sealed class CharTransformer : SimpleTransformer<char>
{
    protected override char ParseCore(in ReadOnlySpan<char> input) => input.Length switch
    {
        0 => '\0',
        1 => input[0],
        _ => throw new FormatException($"\"{input.ToString()}\" is not a valid value for char")
    };

    public override string Format(char element) => element == '\0' ? "" : char.ToString(element);


    public static readonly ITransformer<char> Instance = new CharTransformer();

    private CharTransformer() { }
}


public sealed class VersionTransformer : SimpleTransformer<Version>
{
    protected override Version ParseCore(in ReadOnlySpan<char> input) => Version.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    public override string Format(Version element) => element?.ToString();

    public static readonly ITransformer<Version> Instance = new VersionTransformer();

    private VersionTransformer() { }

    public override Version GetEmpty() => new(0, 0, 0, 0);
}

public sealed class IpAddressTransformer : SimpleTransformer<IPAddress>
{
    protected override IPAddress ParseCore(in ReadOnlySpan<char> input) => IPAddress.Parse(
#if NETSTANDARD2_0 || NETFRAMEWORK
            input.ToString()
#else
        input
#endif
        );

    public override string Format(IPAddress element) => element?.ToString();

    public static readonly ITransformer<IPAddress> Instance = new IpAddressTransformer();

    private IpAddressTransformer() { }

    public override IPAddress GetEmpty() => new(0);
}

public sealed class RegexTransformer : SimpleTransformer<Regex>
{
    private static readonly TupleHelper _helper = new(';', '∅', '~', '{', '}');

    protected override Regex ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, 2, nameof(Regex));

        var options = _helper.ParseElement(ref enumerator, RegexOptionsTransformer.Instance);
        var pattern = _helper.ParseElement(ref enumerator, StringTransformer.Instance, 2, nameof(Regex));

        _helper.ParseEnd(ref enumerator, 2, nameof(Regex));

        return new(pattern, options);
    }

    public override string Format(Regex re)
    {
        if (re == null) return null;

        Span<char> initialBuffer = stackalloc char[16];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(RegexOptionsTransformer.Instance, re.Options, ref accumulator);
            _helper.AddDelimiter(ref accumulator);

            _helper.FormatElement(StringTransformer.Instance, re.ToString(), ref accumulator);

            _helper.EndFormat(ref accumulator);

            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public static readonly ITransformer<Regex> Instance = new RegexTransformer();

    private RegexTransformer() { }

    public override Regex GetEmpty() => new("", RegexOptions.None);
}

public sealed class RegexOptionsTransformer : SimpleTransformer<RegexOptions>
{
    private static readonly RegexOptions[] _optionValues = [
        RegexOptions.IgnoreCase,
        RegexOptions.Multiline,
        RegexOptions.ExplicitCapture,
        RegexOptions.Compiled,
        RegexOptions.Singleline,
        RegexOptions.IgnorePatternWhitespace,
        RegexOptions.RightToLeft,
        (RegexOptions)128,
        RegexOptions.ECMAScript,
        RegexOptions.CultureInvariant,
#if NET7_0_OR_GREATER
        RegexOptions.NonBacktracking
#endif
    ];

    public static readonly ITransformer<RegexOptions> Instance = new RegexOptionsTransformer();
    private RegexOptionsTransformer() { }

    protected override RegexOptions ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsEmpty) return default;

        var result = RegexOptions.None;

        for (int i = input.Length - 1; i >= 0; i--)
            result |= ParseSingle(input[i]);

        return result;

        static RegexOptions ParseSingle(char element) => element switch
        {
            '0' => RegexOptions.None,
            'i' => RegexOptions.IgnoreCase,
            'm' => RegexOptions.Multiline,
            'n' => RegexOptions.ExplicitCapture,
            'c' => RegexOptions.Compiled,
            's' => RegexOptions.Singleline,
            'x' => RegexOptions.IgnorePatternWhitespace,
            'r' => RegexOptions.RightToLeft,

            'd' => (RegexOptions)128,

            'e' => RegexOptions.ECMAScript,
            'v' => RegexOptions.CultureInvariant,
#if NET7_0_OR_GREATER
            'b' => RegexOptions.NonBacktracking,
#endif
            _ => throw new NotSupportedException($"'{element}' is not supported for parsing RegexOptions")
        };
    }

    public override string Format(RegexOptions element)
    {
        if (element == RegexOptions.None) return "0";

        Span<char> initialBuffer = stackalloc char[8];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

        try
        {
            foreach (var option in _optionValues)
                if ((element & option) > 0)
                    accumulator.Append(FormatSingle(option));

            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }

        static char FormatSingle(RegexOptions option) => option switch
        {
            RegexOptions.None => '0',
            RegexOptions.IgnoreCase => 'i',
            RegexOptions.Multiline => 'm',
            RegexOptions.ExplicitCapture => 'n',
            RegexOptions.Compiled => 'c',
            RegexOptions.Singleline => 's',
            RegexOptions.IgnorePatternWhitespace => 'x',
            RegexOptions.RightToLeft => 'r',

            (RegexOptions)128 => 'd',

            RegexOptions.ECMAScript => 'e',
            RegexOptions.CultureInvariant => 'v',

#if NET7_0_OR_GREATER
            RegexOptions.NonBacktracking => 'b',
#endif
            _ => throw new NotSupportedException($"'{option}' is not supported for formatting RegexOptions")
        };
    }

    /*[Flags]
    public enum RegexOptions
    {
        None                    = 0x0000, // '0'
        IgnoreCase              = 0x0001, // 'i'
        Multiline               = 0x0002, // 'm'
        ExplicitCapture         = 0x0004, // 'n'
        Compiled                = 0x0008, // 'c'
        Singleline              = 0x0010, // 's'
        IgnorePatternWhitespace = 0x0020, // 'x'
        RightToLeft             = 0x0040, // 'r'

        ECMAScript              = 0x0100, // 'e'
        CultureInvariant        = 0x0200, // 'v'
        NonBacktracking         = 0x0400, // 'b'
    }*/
}

public sealed class ComplexTransformer : SimpleTransformer<Complex>
{
    private static readonly TupleHelper _helper = new(';', '∅', '\\', '(', ')');

    protected override Complex ParseCore(in ReadOnlySpan<char> input)
    {
        const string TYPE_NAME = "Complex number";

        var doubleParser = DoubleTransformer.Instance;

        var enumerator = _helper.ParseStart(input, 2, TYPE_NAME);

        double real = _helper.ParseElement(ref enumerator, doubleParser);
        double imaginary = _helper.ParseElement(ref enumerator, doubleParser, 2, TYPE_NAME);

        _helper.ParseEnd(ref enumerator, 2, TYPE_NAME);

        return new Complex(real, imaginary);
    }

    public override string Format(Complex c)
    {
        var doubleParser = DoubleTransformer.Instance;
        Span<char> initialBuffer = stackalloc char[16];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
            _helper.StartFormat(ref accumulator);

            _helper.FormatElement(doubleParser, c.Real, ref accumulator);
            _helper.AddDelimiter(ref accumulator);
            accumulator.Append(' ');//this is pure cosmetics 

            _helper.FormatElement(doubleParser, c.Imaginary, ref accumulator);

            _helper.EndFormat(ref accumulator);

            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }

    public static readonly ITransformer<Complex> Instance = new ComplexTransformer();

    private ComplexTransformer() { }
}

public sealed class IndexTransformer : SimpleTransformer<Index>
{
    protected override Index ParseCore(in ReadOnlySpan<char> input)
    {
        var span = input.Trim();

        return span.Length switch
        {
            0 => GetEmpty(),
            > 1 when span[0] == '^' && Int32Transformer.Instance.TryParse(span[1..], out int valueFromEnd)
                => new Index(valueFromEnd, fromEnd: true),
            > 0 when Int32Transformer.Instance.TryParse(span, out int value)
                => new Index(value, fromEnd: false),
            _ => throw new FormatException($"Invalid Index string representation: '{input.ToString()}'"),
        };
    }

    public override string Format(Index element) =>
        element.IsFromEnd ? $"^{(uint) element.Value}" : ((uint) element.Value).ToString();

    public override Index GetEmpty() => Index.Start;

    public static readonly ITransformer<Index> Instance = new IndexTransformer();

    private IndexTransformer() { }
}

public sealed class RangeTransformer : SimpleTransformer<Range>
{
    protected override Range ParseCore(in ReadOnlySpan<char> input)
    {
        int separatorIndex = input.IndexOf('.');

        if (separatorIndex == -1 || separatorIndex + 1 >= input.Length || input[separatorIndex + 1] != '.')
            throw new FormatException(
                $"Invalid Range string representation: '{input.ToString()}'. Expected '..' separator.");

        var start = input[..separatorIndex] is var startSpan && !startSpan.IsWhiteSpace()
            ? IndexTransformer.Instance.Parse(startSpan)
            : Index.Start;

        var end = input[(separatorIndex + 2)..] is var endSpan && !endSpan.IsWhiteSpace()
            ? IndexTransformer.Instance.Parse(endSpan)
            : Index.End;

        return new Range(start, end);
    }

    public override string Format(Range element) => $"{element.Start}..{element.End}";

    public override Range GetEmpty() => Range.All;

    public static readonly ITransformer<Range> Instance = new RangeTransformer();
    private RangeTransformer() { }
}
