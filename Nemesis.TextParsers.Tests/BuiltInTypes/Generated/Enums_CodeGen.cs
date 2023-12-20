using Nemesis.TextParsers.Parsers;

namespace Generated;

[Transformer(typeof(MonthTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true)]
enum Month : byte
{
    None = 0,
    January = 1, February = 2, March = 3, April = 4, May = 5, June = 6,
    July = 7, August = 8, September = 9, October = 10, November = 11, December = 12
}

[Transformer(typeof(MonthsTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false)]
[Flags]
enum Months : short
{
    None = 0,
    January
        = 0b0000_0000_0001,
    February
        = 0b0000_0000_0010,
    March
        = 0b0000_0000_0100,
    April
        = 0b0000_0000_1000,
    May
        = 0b0000_0001_0000,
    June
        = 0b0000_0010_0000,
    July
        = 0b0000_0100_0000,
    August
        = 0b0000_1000_0000,
    September
        = 0b0001_0000_0000,
    October
        = 0b0010_0000_0000,
    November
        = 0b0100_0000_0000,
    December
        = 0b1000_0000_0000,

    Summer = July | August | September,
    All = January | February | March | April |
          May | June | July | August |
          September | October | November | December
}

[Transformer(typeof(DaysOfWeekTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true)]
[Flags]
enum DaysOfWeek : byte
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

[Transformer(typeof(EmptyEnumTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false)]
internal enum EmptyEnum : ulong { }


[Transformer(typeof(EmptyEnumWithNumberParsingTransformer))]
[Auto.AutoEnumTransformer(AllowParsingNumerics = true)]
internal enum EmptyEnumWithNumberParsing : uint { }


[Transformer(typeof(SByteEnumTransformer))]
[Auto.AutoEnumTransformer(AllowParsingNumerics = false)]
enum SByteEnum : sbyte { Sb1 = -10, Sb2 = 0, Sb3 = 5, Sb4 = 10 }


[Transformer(typeof(Int64EnumTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = false, AllowParsingNumerics = false)]
enum Int64Enum : long { L1 = -50, L2 = 0, L3 = 1, L4 = 50 }


[Transformer(typeof(CasingTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = false)]
enum Casing { A, a, B, b, C, c, Good, Case1, case1 }

[Transformer(typeof(МісяцьTransformer))]
[Auto.AutoEnumTransformer(CaseInsensitive = true, AllowParsingNumerics = true)]
enum Місяць : ushort
{
    Жодного = 0,
    січень = 1, лютий = 2, березень = 3, квітень = 4, травень = 5, червень = 6,
    липень = 7, серпень = 8, вересень = 9, жовтень = 10, листопад = 11, грудень = 12
}