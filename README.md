# ![Logo](http://icons.iconarchive.com/icons/iconka/cat-commerce/64/review-icon.png) Nemesis.TextParsers

[![Build status - master](https://img.shields.io/appveyor/ci/Nemesis/nemesis-textparsers?style=flat-square)](https://ci.appveyor.com/project/Nemesis/nemesis-textparsers/branch/master)
[![Tests](https://img.shields.io/appveyor/tests/Nemesis/nemesis-textparsers?compact_message&style=flat-square)](https://ci.appveyor.com/project/Nemesis/nemesis-textparsers/build/tests)
[![Last commit](https://img.shields.io/github/last-commit/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers)
[![Last release](https://img.shields.io/github/release-date/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://ci.appveyor.com/project/Nemesis/nemesis-textparsers/build/artifacts)

[![Code size](https://img.shields.io/github/languages/code-size/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers)
[![Issues](https://img.shields.io/github/issues/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/issues)
![Commit activity](https://img.shields.io/github/commit-activity/y/nemesissoft/Nemesis.TextParsers.svg?style=flat-square)
[![GitHub stars](https://img.shields.io/github/stars/nemesissoft/Nemesis.TextParsers?style=flat-square)](https://github.com/nemesissoft/Nemesis.TextParsers/stargazers)


[
 ![NuGet version](https://img.shields.io/nuget/v/Nemesis.TextParsers.svg?style=flat-square)
 ![Downloads](https://img.shields.io/nuget/dt/Nemesis.TextParsers.svg?style=flat-square)
](https://www.nuget.org/packages/Nemesis.TextParsers/)
***

## Benefits and Features
TL;DR - are you looking for performant, non allocating serializer from structural object to flat, human editable string? Look no further. [Benchmarks](Benchmarks/ParserBench.cs) shows potential gains from using Nemesis.TextParsers


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

### Other popular choices

When stucked with a task of parsing various items form strings we ofter opt for TypeConverter (https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter) ? We tend to create methods like:
```csharp
public static T FromString<T>(string text) =>
    (T)TypeDescriptor.GetConverter(typeof(T))
        .ConvertFromInvariantString(text);
```

or even create similar constructs to be in line with object oriented design:

```csharp
public abstract class TextTypeConverter : TypeConverter
{
    public sealed override bool CanConvertFrom(ITypeDescriptorContextcontext, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, ourceType);

    public sealed override bool CanConvertTo(ITypeDescriptorContext ontext, Type destinationType) =>
        destinationType == typeof(string) || base.CanConvertTocontext, destinationType);
}

public abstract class BaseTextConverter<TValue> : TextTypeConverter
{
    public sealed override object ConvertFrom(ITypeDescriptorContext ontext, CultureInfo culture, object value) =>
        value is string text ? ParseString(text) : default;

    public abstract TValue ParseString(string text);
    

    public sealed override object ConvertTo(ITypeDescriptorContext ontext, CultureInfo culture, object value, Type estinationType) =>
        destinationType == typeof(string) ?
            FormatToString((TValue)value) :
            base.ConvertTo(context, culture, value, destinationType);

    public abstract string FormatToString(TValue value);
}
```

What is wrong with that? Well, nothing... except of performance. 

TypeConverter was designed 15+ years ago when processing power tended to double every now and then and (in my opinion) it was more suited for creating GUI-like editors where performance usually is not an issue. 
But imagine a service application like exchange trading suite that has to perform multiple operations per second and in such cases processor has more important thing to do than parsing strings. 

### Features
0. as concise as possible - both JSON or XML exist but thay are not ready to be created from hand by human support
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
6. parsing is **fast** to while allocating as little memory as possible upon parsing. The follwing benchmark illustrates this speed via parsing 1000 element array 

|                     Method |        Mean | Ratio |    Gen 0 |  Gen 1 | Allocated | Remarks |
|--------------------------- |-------------|-------|----------|--------|-----------|-----------|
|              RegEx parsing | 4,528.99 us | 44.98 | 492.1875 |      - | 2089896 B | Regular expression with escaping support |
|  StringSplitTest_KnownType |    93.41 us |  0.92 |   9.5215 | 0.1221 |   40032 B | string.Split(..).Select(text=>int.Parse(text)) |
|StringSplitTest_DynamicType |   474.73 us |  4.69 |  24.4141 |      - |  104032 B | string.Split + TypeDescriptor.GetConverter |
|      SpanSplitTest_NoAlloc |   101.00 us |  1.00 |        - |      - |         - | "1\|2\|3".AsSpan().Tokenize() |
|        SpanSplitTest_Alloc |   101.38 us |  1.00 |   0.8545 |      - |    4024 B | "1\|2\|3".AsSpan().Tokenize();   var array = new int[1000];|

7. provides basic building blocks for parser's callers to be able to create their own transformers/factories 
    * LeanCollection that can store 1,2,3 or more elements 
    * [SpanSplit](Nemesis.TextParsers/SpanSplit.cs) - string.Split equivalent is provided to accept faster representaion of string - ReadOnlySpan&lt;char&gt;. Supports both standard and custom escaping sequences
    * access to every implemented parser/formatter
8. basic LINQ support 
```csharp
var avg = SpanCollectionSerializer.DefaultInstance.ParseStream<double>("1|2|3".AsSpan()).Average();
```
9. basic support for GUI editors for compound types like collections/dictionaries: [CollectionMeta](Nemesis.TextParsers/CollectionMeta.cs), [DictionaryMeta](Nemesis.TextParsers/DictionaryMeta.cs)
10. lean/frugal implementation of StringBuilder - ValueSequenceBuilder
```csharp
Span<char> initialBuffer = stackalloc char[32];
using var accumulator = new ValueSequenceBuilder<char>initialBuffer);
using (var enumerator = coll.GetEnumerator())
    while (enumerator.MoveNext())
        FormatElement(formatter, enumerator.Current, ref ccumulator);
return accumulator.AsSpanTo(accumulator.Length > 0 ? ccumulator.Length - 1 : 0).ToString();
```


## Todo / road map
1. context based transformer creation with settings for:
	* DictionaryBehaviour
    * Enum casing+other customizations
    * empty string meaing (empty, default, null?))
2. custom TextParser factory/customizations
3. ability to format to buffer
4. become DI friendly adding support for cross cutting concerns i.e. logging
5. support for pattern based Dispose
6. improve Tokenizer performance