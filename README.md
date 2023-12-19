# ![Logo](https://raw.githubusercontent.com/nemesissoft/Nemesis.TextParsers/main/images/icon.png) Nemesis.TextParsers

[![Build status - main](https://img.shields.io/github/actions/workflow/status/nemesissoft/Nemesis.TextParsers/ci.yml?style=flat-square&label=build&logo=github)](https://github.com/nemesissoft/Nemesis.TextParsers/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/github/actions/workflow/status/nemesissoft/Nemesis.TextParsers/test-report.yml?style=flat-square&label=tests&logo=github)](https://github.com/nemesissoft/Nemesis.TextParsers/actions/workflows/test-report.yml)
[![Last commit](https://img.shields.io/github/last-commit/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/commits/main/)
[![Last release](https://img.shields.io/github/release-date/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/releases/)

[![Code size](https://img.shields.io/github/languages/code-size/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers)
[![Issues](https://img.shields.io/github/issues/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/issues)
![Commit activity](https://img.shields.io/github/commit-activity/y/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)
[![GitHub stars](https://img.shields.io/github/stars/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/stargazers)


[![Nuget](https://img.shields.io/nuget/v/Nemesis.TextParsers.svg?style=flat-square&logo=nuget&label=Nemesis.TextParsers&color=pink)](https://www.nuget.org/packages/Nemesis.TextParsers/)
![Downloads](https://img.shields.io/nuget/dt/Nemesis.TextParsers.svg?style=flat-square&color=pink)

[![Nuget](https://img.shields.io/nuget/v/Nemesis.TextParsers.CodeGen.svg?style=flat-square&logo=nuget&label=Nemesis.TextParsers.CodeGen&color=purple)](https://www.nuget.org/packages/Nemesis.TextParsers.CodeGen/)
![Downloads](https://img.shields.io/nuget/dt/Nemesis.TextParsers.CodeGen.svg?style=flat-square&color=purple)


![License](https://img.shields.io/github/license/nemesissoft/Nemesis.TextParsers)
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fnemesissoft%2FNemesis.TextParsers.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fnemesissoft%2FNemesis.TextParsers?ref=badge_shield)

***

## Benefits and Features
TL;DR - are you looking for performant, non allocating serializer from structural object to flat, human editable string? Look no further. 
[Benchmarks](https://github.com/nemesissoft/Nemesis.TextParsers/blob/04605830652bc9ebd76594516932765bcfc4fb6c/Benchmarks/ParserBench.cs) shows potential gains from using Nemesis.TextParsers


|        Method | Count |        Mean | Ratio | Allocated |
|-------------- |------ |------------:|------:|----------:|
|      TextJson |    10 |   121.02 us |  1.00 |   35200 B |
| TextJsonBytes |    10 |   120.79 us |  1.00 |   30400 B |
|   TextJsonNet |    10 |   137.28 us |  1.13 |  288000 B |
|   TextParsers |    10 |    49.02 us |  0.41 |    6400 B |
|               |       |             |       |           |
|      TextJson |   100 |   846.06 us |  1.00 |  195200 B |
| TextJsonBytes |   100 |   845.84 us |  1.00 |  163200 B |
|   TextJsonNet |   100 |   943.71 us |  1.12 |  636800 B |
|   TextParsers |   100 |   463.33 us |  0.55 |   42400 B |
|               |       |             |       |           |
|      TextJson |  1000 | 8,142.13 us |  1.00 | 1639200 B |
| TextJsonBytes |  1000 | 8,155.41 us |  1.00 | 1247200 B |
|   TextJsonNet |  1000 | 8,708.12 us |  1.07 | 3880800 B |
|   TextParsers |  1000 | 4,384.00 us |  0.54 |  402400 B |

More comprehensive examples are [here](https://github.com/nemesissoft/Nemesis.TextParsers/blob/main/Specification.md)

### Other popular choices

When stucked with a task of parsing various items form strings we often opt for [TypeConverter](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter).
We tend to create methods like:
```csharp
public static T FromString<T>(string text) =>
    (T)TypeDescriptor.GetConverter(typeof(T))
        .ConvertFromInvariantString(text);
```

or even create similar constructs to be in line with object oriented design:

```csharp
public abstract class TextTypeConverter : TypeConverter
{
    public sealed override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public sealed override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
}

public abstract class BaseTextConverter<TValue> : TextTypeConverter
{
    public sealed override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        value is string text ? ParseString(text) : default;

    public abstract TValue ParseString(string text);
    

    public sealed override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
        destinationType == typeof(string) ?
            FormatToString((TValue)value) :
            base.ConvertTo(context, culture, value, destinationType);

    public abstract string FormatToString(TValue value);
}
```

What is wrong with that? Well, nothing... except of performance and possibly - support for generics. 

TypeConverter was designed around 2002 when processing power tended to double every now and then and (in my opinion) it was more suited for creating GUI-like editors where performance usually is not an issue. 
But imagine a service application like exchange trading suite that has to perform multiple operations per second and in such cases processor has more important thing to do than parsing strings. 

### Features
0. as concise as possible - both JSON or XML exist but they are not ready to be created from hand by human support
1. works in various architectures supporting .Net Core and .Net Standard and is culture independent 
2. support for basic system types (C#-like type names):
   * string
   * bool
   * byte/sbyte, short/ushort, int/uint, long/ulong
   * float/double
   * decimal
   * BigInteger
   * TimeSpan, DateTime/DateTimeOffset
   * Guid, Uri
3. supports pattern based parsing/formatting via ToString/FromText methods placed inside type or static/instance factory 
4. supports compound types:
   * KeyValuePair<,> and ValueTuple of any arity 
   * Enums (with number underlying types)
   * Nullables
   * Dictionaries (built-in i.e. SortedDictionary/SortedList and custom ones)
   * Arrays (including jagged arrays)
   * Standard collections and collection contracts (List vs IList vs IEnumerable) 
   * User defined collections 
   * everything mentioned above but combined with inner elements properly escaped in final string i.e. **SortedDictionary&lt;char?, IList&lt;float[][]&gt;&gt;**
5. ability to fallback to TypeConverter if no parsing/formatting strategy was found 
6. parsing is **fast** to while allocating as little memory as possible upon parsing. The following benchmark illustrates this speed via parsing 1000 element array 

|                     Method |        Mean | Ratio |    Gen 0 |  Gen 1 | Allocated | Remarks |
|--------------------------- |-------------|-------|----------|--------|-----------|-----------|
|              RegEx parsing | 4,528.99 us | 44.98 | 492.1875 |      - | 2089896 B | Regular expression with escaping support |
|  StringSplitTest_KnownType |    93.41 us |  0.92 |   9.5215 | 0.1221 |   40032 B | string.Split(..).Select(text=>int.Parse(text)) |
|StringSplitTest_DynamicType |   474.73 us |  4.69 |  24.4141 |      - |  104032 B | string.Split + TypeDescriptor.GetConverter |
|      SpanSplitTest_NoAlloc |   101.00 us |  1.00 |        - |      - |         - | "1\|2\|3".AsSpan().Tokenize() |
|        SpanSplitTest_Alloc |   101.38 us |  1.00 |   0.8545 |      - |    4024 B | "1\|2\|3".AsSpan().Tokenize();   var array = new int[1000];|

7. provides basic building blocks for parser's callers to be able to create their own transformers/factories 
    * LeanCollection that can store 1,2,3 or more elements 
    * [SpanSplit](https://github.com/nemesissoft/Nemesis.TextParsers/blob/04605830652bc9ebd76594516932765bcfc4fb6c/Nemesis.TextParsers/SpanSplit.cs) - string.Split equivalent is provided to accept faster representation of string - ReadOnlySpan&lt;char&gt;. Supports both standard and custom escaping sequences
    * access to every implemented parser/formatter
8. basic LINQ support 
```csharp
var avg = "1|2|3".AsSpan()
    .Tokenize('|', '\\', true)
    .Parse('\\', '∅', '|')
    .Average(DoubleTransformer.Instance);
```
9. basic support for GUI editors for compound types like collections/dictionaries: [CollectionMeta](https://github.com/nemesissoft/Nemesis.TextParsers/blob/04605830652bc9ebd76594516932765bcfc4fb6c/Nemesis.TextParsers/Utils/CollectionMeta.cs), [DictionaryMeta](https://github.com/nemesissoft/Nemesis.TextParsers/blob/04605830652bc9ebd76594516932765bcfc4fb6c/Nemesis.TextParsers/Utils/DictionaryMeta.cs)
10. lean/frugal implementation of StringBuilder - ValueSequenceBuilder
```csharp
Span<char> initialBuffer = stackalloc char[32];
using var accumulator = new ValueSequenceBuilder<char>initialBuffer);
using (var enumerator = coll.GetEnumerator())
    while (enumerator.MoveNext())
        FormatElement(formatter, enumerator.Current, ref accumulator);
return accumulator.AsSpanTo(accumulator.Length > 0 ? accumulator.Length - 1 : 0).ToString();
```
11. use C# 9.0 code-gen to provide several transformers (currently automatic generation of deconstructable pattern, more to follow in future)


## Funding
Open source software is free to use but creating and maintaining is a laborious effort. Should you wish to support us in our noble endeavour, please consider the following donation methods:
[![Donate using Liberapay](https://raw.githubusercontent.com/nemesissoft/Nemesis.TextParsers/main/images/donate.svg)](https://liberapay.com/Michal.Brylka/donate) ![Liberapay receiving](https://img.shields.io/liberapay/receives/Michal.Brylka?color=blue&style=flat-square)


## Todo / road map
- [ ] ability to format to buffer i.e. TryFormat pattern
- [ ] support for ILookup<,>, IGrouping<,>
- [ ] support for native parsing/formatting of F# types (map, collections, records...)


## Links
- [Documentation](https://github.com/nemesissoft/Nemesis.TextParsers/blob/main/Specification.md)
- [NuGet Package](https://www.nuget.org/packages/Nemesis.TextParsers/)
- [Release Notes](https://github.com/nemesissoft/Nemesis.TextParsers/releases)

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fnemesissoft%2FNemesis.TextParsers.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fnemesissoft%2FNemesis.TextParsers?ref=badge_large)