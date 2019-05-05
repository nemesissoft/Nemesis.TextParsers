# Nemesis.TextParsers
When stucked with a task of parsing various items form strings we ofter opt for TypeConverter (https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.typeconverter) ?

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

## Parser requirements vs implemented features
0. has to be as concise as possible - both JSON or XML exist but thay are not ready to be created from hand by human support
1. has to work in various architecture supporting .net Core and .net Standard
2. has to support basic system types (C#-like type names):
   * string
   * bool
   * byte/sbyte
   * short/ushort
   * int/uint 
   * long/ulong
   * float/double
   * decimal
   * BigInteger
   * TimeSpan
   * DateTime/DateTimeOffset
   * Guid
3. has to support pattern based parsing/formatting via ToString/FromText methods placed inside type or static/instance factory 
4. has to support compound types:
   * KeyValuePair<,>
   * Enums (with number underlying types)
   * Nullables
   * Dictionaries (including SortedDictionary/SortedList)
   * Arrays (including jagged arrays)
   * Standard collections and collection contracts (List vs IList vs IEnumerable)
   * User defined collections 
   * everything mentioned above but combined i.e. **SortedDictionary&lt;char, IList&lt;float[][]&gt;&gt;**
5. has to be able to fallback to TypeConverter if no parsing/formatting strategy was found 
6. has to be **fast** to parse while allocating as little memory as possible upon parsing 
7. has to provide basic building blocks for parser's callers to be able to create their own transformers/factories 
    * LeanCollection type has to be provided
    * string.Split equivalent has to be provided to accept faster representaion of string - ReadOnlySpan&lt;char&gt;. Both standard/escaping sequence supporting versions have to be implemented 
8. basic LINQ support 
```csharp
var avg = SpanCollectionSerializer.DefaultInstance.ParseStream<double>("1|2|3".AsSpan()).Average();
```
9. basic support for GUI editors for compound types like collections/dictionaries
10. lean/frugal implementation of StringBuilder - ValueSequenceBuilder
```csharp
Span<char> initialBuffer = stackalloc char[32];
var accumulator = new ValueSequenceBuilder<char>initialBuffer);
using (var enumerator = coll.GetEnumerator())
    while (enumerator.MoveNext())
        FormatElement(formatter, enumerator.Current, ref ccumulator);
var text = accumulator.AsSpanTo(accumulator.Length > 0 ? ccumulator.Length - 1 : 0).ToString();
accumulator.Dispose();
```