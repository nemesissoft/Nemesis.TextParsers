using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    public sealed class EnumTransformerCreator : ICanCreateTransformer
    {
        public ITransformer<TEnum> CreateTransformer<TEnum>()
        {
            Type enumType = typeof(TEnum), underlyingType = Enum.GetUnderlyingType(enumType);

            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:

                case TypeCode.Int16:
                case TypeCode.UInt16:

                case TypeCode.Int32:
                case TypeCode.UInt32:

                case TypeCode.Int64:
                case TypeCode.UInt64:
                    break;
                default:
                    throw new NotSupportedException($"UnderlyingType {underlyingType.Name} is not supported for enum parsing");
            }

            var numberHandler = NumberHandlerCache.GetNumberHandler(underlyingType) ??
                throw new NotSupportedException($"UnderlyingType {underlyingType.Name} was not found in parser cache");

            var transType = typeof(EnumTransformer<,,>).MakeGenericType(enumType, underlyingType, numberHandler.GetType());

            return (ITransformer<TEnum>)Activator.CreateInstance(transType, numberHandler);
        }

        public bool CanHandle(Type type) => type.IsEnum;

        public sbyte Priority => 30;
    }

    public sealed class EnumTransformer<TEnum, TUnderlying, TNumberHandler> : ITransformer<TEnum>
        where TEnum : Enum
        where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable
        where TNumberHandler : class, INumber<TUnderlying> //PERF: making number handler a concrete specification can win additional up to 3% in speed
    {
        private readonly TNumberHandler _numberHandler;

        public bool IsFlagEnum { get; } = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);
        
        private readonly EnumTransformerHelper.ParserDelegate<TUnderlying> _elementParser = EnumTransformerHelper.GetElementParser<TEnum, TUnderlying>();

        public EnumTransformer([NotNull]TNumberHandler numberHandler) =>
                _numberHandler = numberHandler ?? throw new ArgumentNullException(nameof(numberHandler));

        //check performance comparison in Benchmark project - ToEnumBench
        internal TEnum ToEnum(TUnderlying value) => Unsafe.As<TUnderlying, TEnum>(ref value);
        
        //TODO check Echo for EnumBehaviour and other nonstandard formatting/parsing
        public TEnum Parse(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty || input.IsWhiteSpace()) return default;

            //TODO "" || null => default(Enum) ? + additional questions from mail
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

            char first;
            bool isNumeric = input.Length > 0 && (char.IsDigit(first = input[0]) || first == '-' || first == '+');

            return isNumeric && _numberHandler.TryParse(in input, out var number) ?
                number :
                _elementParser(input);
        }

        public string Format(TEnum element) => element.ToString("G");

        public override string ToString() => $"Transform {typeof(TEnum).Name}";
    }

    internal static class EnumTransformerHelper
    {
        internal static Func<TUnderlying, TEnum> GetNumberConverter<TEnum, TUnderlying>()
        {
            var input = Expression.Parameter(typeof(TUnderlying), "input");

            var λ = Expression.Lambda<Func<TUnderlying, TEnum>>(
                Expression.Convert(input, typeof(TEnum)),
                input);
            return λ.Compile();

            // ReSharper disable once CommentTypo
            /* //DynamicMethod is not present in .net Standard 2.0
            var method = new DynamicMethod("Convert", typeof(TEnum), new[] { typeof(TUnderlying) }, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            return (Func<TUnderlying, TEnum>)method.CreateDelegate(typeof(Func<TUnderlying, TEnum>));*/
        }

        internal delegate TUnderlying ParserDelegate<out TUnderlying>(ReadOnlySpan<char> input);

        internal static ParserDelegate<TUnderlying> GetElementParser<TEnum, TUnderlying>()
        {
            var enumValues = typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(enumField => (enumField.Name, Value: (TUnderlying)enumField.GetValue(null))).ToList();

            var inputParam = Expression.Parameter(typeof(ReadOnlySpan<char>), "input");
            if (enumValues.Count == 0)
                return Expression.Lambda<ParserDelegate<TUnderlying>>(Expression.Default(typeof(TUnderlying)), inputParam).Compile();

            var formatException = Expression.Throw(Expression.Constant(new FormatException(
                $"Enum of type '{typeof(TEnum).Name}' cannot be parsed. " +
                $"Valid values are: {string.Join(" or ", enumValues.Select(ev => ev.Name))} or number within {typeof(TUnderlying).Name} range."
                )));

            var conditionToValue = new List<(Expression Condition, TUnderlying value)>();

            var charEqMethod = typeof(EnumTransformerHelper).GetMethod(nameof(CharEq), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var (name, value) in enumValues)
            {
                var lengthCheck = Expression.Equal(
                    Expression.Property(inputParam, nameof(ReadOnlySpan<char>.Length)),
                    Expression.Constant(name.Length)
                    );
                var conditionElements = new List<Expression> { lengthCheck };

                for (int i = name.Length - 1; i >= 0; i--)
                {
                    //char expectedCharacter = ;

                    var charCheck = Expression.Call(charEqMethod,
                        inputParam,
                        Expression.Constant(i),
                        Expression.Constant(char.ToUpper(name[i])),
                        Expression.Constant(char.ToLower(name[i]))
                        );

                    conditionElements.Add(charCheck);
                }

                var condition = ExpressionUtils.AndAlsoJoin(conditionElements);

                conditionToValue.Add((condition, value));
            }
            LabelTarget returnTarget = Expression.Label(typeof(TUnderlying), "exit");
            var iff = ExpressionUtils.IfThenElseJoin(conditionToValue, formatException, returnTarget);

            var body = Expression.Block(
                iff,
                Expression.Label(returnTarget, Expression.Default(typeof(TUnderlying)))
                );

            var λ = Expression.Lambda<ParserDelegate<TUnderlying>>(body, inputParam);
            return λ.Compile();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CharEq(ReadOnlySpan<char> input, int index, char expectedUpperChar, char expectedLowerChar) =>
            input[index] == expectedUpperChar || input[index] == expectedLowerChar;
        //char.ToUpper(input[index]).Equals(expectedUpperChar);
    }
}
