using System;
using System.Runtime.CompilerServices;

namespace EnumConverter
{
    public class Class1
    {
        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern TEnum ToEnumIl<TEnum, TUnderlying>(TUnderlying number)
            where TEnum : Enum
            where TUnderlying : struct, IComparable, IComparable<TUnderlying>, IConvertible, IEquatable<TUnderlying>, IFormattable;


        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern DayOfWeek ToEnumIlConcrete(int number);
    }
}
