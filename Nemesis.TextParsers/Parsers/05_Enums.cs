#nullable enable
using System.Diagnostics.CodeAnalysis;
using Nemesis.TextParsers.Runtime;
using Nemesis.TextParsers.Settings;
using BF = System.Reflection.BindingFlags;

namespace Nemesis.TextParsers.Parsers;

public sealed class EnumTransformerHandler : ITransformerHandler
{
    private readonly EnumSettings _settings;
    public EnumTransformerHandler(EnumSettings settings) => _settings = settings;

    public ITransformer<TEnum> CreateTransformer<TEnum>()
    {
        var enumType = typeof(TEnum);

        if (!TryGetUnderlyingType(enumType, out var underlyingType) || underlyingType == null)
            throw new NotSupportedException($@"Type {enumType.GetFriendlyName()} is not supported by {GetType().Name}. 
UnderlyingType {underlyingType?.GetFriendlyName() ?? "<none>"} should be a numeric one");

        var numberHandler = NumberTransformerCache.Instance.GetNumberHandler(underlyingType) ??
            throw new NotSupportedException($"UnderlyingType {underlyingType.Name} was not found in parser cache");

        var transType = typeof(EnumTransformer<,,>).MakeGenericType(enumType, underlyingType, numberHandler.GetType());

        return (ITransformer<TEnum>)(
            Activator.CreateInstance(transType, numberHandler, _settings)
            ?? throw new Exception($"Cannot create type '{nameof(TEnum)}'")
       );
    }

    public bool CanHandle(Type type) => TryGetUnderlyingType(type, out _);

    private static bool TryGetUnderlyingType(Type type, [NotNullWhen(true)] out Type? underlyingType)
    {
        if (!type.IsEnum)
        {
            underlyingType = null;
            return false;
        }

        underlyingType = Enum.GetUnderlyingType(type);

        return Type.GetTypeCode(underlyingType) is
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;
    }

    public sbyte Priority => 30;

    public override string ToString() =>
        $"Create transformer for any Enum with settings:{_settings}";

    string ITransformerHandler.DescribeHandlerMatch() => "Enum based on system primitive number types";
}

public sealed class EnumTransformer<TEnum, TUnderlying, TNumberHandler> : TransformerBase<TEnum>
    where TEnum : struct, Enum
    where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
#if NET7_0_OR_GREATER
    , IBinaryInteger<TUnderlying>
#endif
    where TNumberHandler : NumberTransformer<TUnderlying> //PERF: making number handler a concrete specification can win additional up to 3% in speed
{
    private readonly TNumberHandler _numberHandler;
    private readonly EnumSettings _settings;

    public bool IsFlagEnum { get; } = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);

    private readonly EnumTransformerHelper.ParserDelegate<TUnderlying> _elementParser;

    public EnumTransformer(TNumberHandler numberHandler, EnumSettings settings)
    {
        _numberHandler = numberHandler ?? throw new ArgumentNullException(nameof(numberHandler));
        _settings = settings;
        _elementParser = EnumTransformerHelper.GetElementParser<TEnum, TUnderlying>(_settings);
    }

    //check performance comparison in Benchmark project - ConvertEnumBench
    internal static TEnum ToEnum(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);

    protected override TEnum ParseCore(in ReadOnlySpan<char> input)
    {
        if (input.IsWhiteSpace()) return default;

        var enumStream = input.Split(',').GetEnumerator();

        if (!enumStream.MoveNext()) throw new FormatException($"At least one element is expected to parse {typeof(TEnum).Name} enum");
        TUnderlying currentValue = ParseElement(enumStream.Current);

        while (enumStream.MoveNext())
        {
            if (!IsFlagEnum) throw new FormatException($"{typeof(TEnum).Name} enum is not marked as flag enum so only one value in comma separated list is supported for parsing");

            var element = ParseElement(enumStream.Current);

            currentValue = _numberHandler.Or(currentValue, element);
        }

        return ToEnum(currentValue);
    }

    private TUnderlying ParseElement(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace()) return default;
        input = input.Trim();

        if (_settings.AllowParsingNumerics)
        {
            bool isNumeric = input.Length > 0 && input[0] is var first &&
                             (char.IsDigit(first) || first is '-' or '+');

            return isNumeric && _numberHandler.TryParse(in input, out var number)
                ? number
                : _elementParser(input);
        }
        else
            return _elementParser(input);
    }

    public override string Format(TEnum element) => element.ToString("G");

    public override string ToString() => $"Transform {typeof(TEnum).Name} based on {typeof(TUnderlying).GetFriendlyName()} ({_settings})";
}

internal static class EnumTransformerHelper
{
    internal delegate TUnderlying ParserDelegate<out TUnderlying>(ReadOnlySpan<char> input);

    internal static ParserDelegate<TUnderlying> GetElementParser<TEnum, TUnderlying>(EnumSettings settings)
    {
        var enumValues = typeof(TEnum).GetFields(BF.Public | BF.Static)
            .Select(enumField => (
                enumField.Name,
                Value: (TUnderlying)(enumField.GetValue(null) ?? throw new MissingMemberException(nameof(TEnum), enumField.Name))
        )).ToList();

        var inputParam = Expression.Parameter(typeof(ReadOnlySpan<char>), "input");
        //this can potentially be subject to property in EnumSettings
        if (enumValues.Count == 0)
            return Expression.Lambda<ParserDelegate<TUnderlying>>(Expression.Default(typeof(TUnderlying)), inputParam).Compile();

        var formatException = Expression.Throw(Expression.Constant(new FormatException(
              $"Enum of type '{typeof(TEnum).Name}' cannot be parsed. " +
              $"Valid values are: {string.Join(" or ", enumValues.Select(ev => ev.Name))}" +
              (settings.AllowParsingNumerics ? $" or number within {typeof(TUnderlying).Name} range. " : ". ") +
              (settings.CaseInsensitive ? "Ignore case option on." : "Case sensitive option on.")
            )));

        var conditionToValue = new List<(Expression Condition, TUnderlying value)>();

        var caseInsensitiveMethod = GetCharEqMethod(nameof(CharEqCaseInsensitive));
        var caseSensitiveMethod = GetCharEqMethod(nameof(CharEqCaseSensitive));

        foreach (var (name, value) in enumValues)
        {
            var lengthCheck = Expression.Equal(
                Expression.Property(inputParam, nameof(ReadOnlySpan<char>.Length)),
                Expression.Constant(name.Length)
                );
            var conditionElements = new List<Expression> { lengthCheck };

            for (int i = name.Length - 1; i >= 0; i--)
            {
                char expectedChar = name[i],
                    upper = char.ToUpper(expectedChar),
                    lower = char.ToLower(expectedChar)
                    ;
                var index = Expression.Constant(i);

                var charCheck = settings.CaseInsensitive && (lower != upper)
                    ? Expression.Call(caseInsensitiveMethod,
                        inputParam, index,
                        Expression.Constant(upper),
                        Expression.Constant(lower)
                    )
                    : Expression.Call(caseSensitiveMethod,
                        inputParam, index,
                        Expression.Constant(expectedChar)
                    );

                conditionElements.Add(charCheck);
            }

            var condition = AndAlsoJoin(conditionElements);

            conditionToValue.Add((condition, value));
        }
        LabelTarget returnTarget = Expression.Label(typeof(TUnderlying), "exit");
        var iff = IfThenElseJoin(conditionToValue, formatException, returnTarget);

        var body = Expression.Block(
            iff,
            Expression.Label(returnTarget, Expression.Default(typeof(TUnderlying)))
            );

        var lambda = Expression.Lambda<ParserDelegate<TUnderlying>>(body, inputParam);
        return lambda.Compile();
    }

    private static MethodInfo GetCharEqMethod(string charEqMethodName) =>
        typeof(EnumTransformerHelper).GetMethod(charEqMethodName, BF.Public | BF.NonPublic | BF.Static)
        ?? throw new MissingMethodException(nameof(EnumTransformerHelper), charEqMethodName);

    private static Expression AndAlsoJoin(IReadOnlyCollection<Expression> expressionList) =>
        expressionList?.Count > 0
        ? expressionList.Aggregate<Expression, Expression>(null!, (curr, elem) => curr is null ? elem : Expression.AndAlso(curr, elem))
        : Expression.Constant(false);

    private static Expression IfThenElseJoin<TResult>(IReadOnlyList<(Expression Condition, TResult value)> expressionList, Expression lastElse, LabelTarget exitTarget)
    {
        if (expressionList is { Count: > 0 })
        {
            Expression @else = lastElse;

            for (int i = expressionList.Count - 1; i >= 0; i--)
            {
                var (condition, value) = expressionList[i];

                var then = Expression.Return(exitTarget, Expression.Constant(value));

                @else = Expression.IfThenElse(condition, then, @else);
            }

            return @else;
        }
        else
            return lastElse;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CharEqCaseInsensitive(ReadOnlySpan<char> input, int index, char expectedUpperChar, char expectedLowerChar)
        => input[index] == expectedUpperChar || input[index] == expectedLowerChar;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CharEqCaseSensitive(ReadOnlySpan<char> input, int index, char expectedExactChar)
        => input[index] == expectedExactChar;
}
