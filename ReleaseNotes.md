# Release 2.8.2 - Integrate with Github Actions 
Published [2023-12-19 10:10:43 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/2.8.2) by [MichalBrylka](https://github.com/MichalBrylka)

- Add Github Actions for build. Remove integration with AppVeyor
- Add architecture tests to check if settings can be properly handled
- Add debuging option for code gen

## Improve TupleHelper
User is no longer required to call for TupleHelper.ParseNext method. It's code was incorporated into ParseElement method. It's now enough to call:
```csharp
var _helper = new TupleHelper(',', '∅', '\\', '(', ')');

var input = "(3.14, Ala ma kota)";
var enumerator = _helper.ParseStart(input, 2, "DoubleAndString");

var key = _helper.ParseElement(ref enumerator, TextTransformer.Default.GetTransformer<double>());
var value = _helper.ParseElement(ref enumerator, TextTransformer.Default.GetTransformer<string>(), 2, "DoubleAndString");

_helper.ParseEnd(ref enumerator, 2, "DoubleAndString");
```
All generated calls were updated accordingly 

**Full Changelog**: https://github.com/nemesissoft/Nemesis.TextParsers/compare/v2.7.2...2.8.2


# Release v2.7.2 - Add support for ReadOnlyObservableCollection 
Published [2023-07-16 10:12:51 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.7.2) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.7.1 - Optimize build targets 
Published [2023-07-14 13:23:24 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.7.1) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.7.0 - Upgrade to .NET 7 
Published [2023-07-14 09:51:30 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.7.0) by [MichalBrylka](https://github.com/MichalBrylka)

Migrate to NET 7.0
Remove support for net 5.0 (out of support)
Add [central package management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/)
Add [Generic Math](https://learn.microsoft.com/en-us/dotnet/standard/generics/math) version of LightLinq
Pack readme inside nuget package


# Release v2.6.3 - Modernize code base 
Published [2022-05-30 09:50:55 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.6.3) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.6.2 - Add code gen package 
Published [2021-03-01 21:26:14 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.6.2) by [MichalBrylka](https://github.com/MichalBrylka)

Add code generation package that automatically generates necessary transformers
## C# 9.0 Code generation
With introduction of new code-gen engine, you can opt to have your transformer generated automatically without any imperative code.
```csharp 
//1. use specially provided (via code-gen) Auto.AutoDeconstructable attribute
[Auto.AutoDeconstructable]
//2. provide deconstructable aspect options or leave this attribute out - default options will be engaged 
[DeconstructableSettings('_', '∅', '%', '〈', '〉')]
readonly partial /*3. partial modifier is VERY important - you need this cause generated code is placed in different file*/ struct StructPoint3d
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    //4. specify constructor and matching deconstructor 
    public StructPoint3d(double x, double y, double z) { X = x; Y = y; Z = z; }

    public void Deconstruct(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
}

//5. sit back, relax and enjoy - code-gen will do the job for you :-)
``` 

This in turn might generate the following (parts of code ommited for brevity)
```csharp 
using /* ... */;
[Transformer(typeof(StructPoint3dTransformer))]
readonly partial struct StructPoint3d { }

sealed class StructPoint3dTransformer : TransformerBase<StructPoint3d>
{
    private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
    /* specify remaining transformers... */
    private const int ARITY = 3;
    private readonly TupleHelper _helper = new TupleHelper('_', '∅', '%', '〈', '〉');

    protected override StructPoint3d ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);
        var t1 = _helper.ParseElement(ref enumerator, _transformer_x);        
        /* parse Y and Z... */
        _helper.ParseEnd(ref enumerator, ARITY);
        return new StructPoint3d(t1, t2, t3);
    }

    public override string Format(StructPoint3d element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
             _helper.StartFormat(ref accumulator);
             var (x, y, z) = element;
            _helper.FormatElement(_transformer_x, x, ref accumulator);
            /* format Y and Z... */
            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }
}
``` 
### Code gen diagnositcs
Various diagnositcs exist to guide end user in creation of proper types that can be consumed by automatic generation. They might for example:
1. check if types decorated with Auto* attributes are declared partial (prerequisite for additive code generation)
2. validate settings passed via declarative syntax
3. validate internal structure of type (i.e. check if constructor has matching Deconstruct method)
4. check if external dependencies are included 


# Release v2.6.1 - Add code gen package 
Published [2021-02-25 14:24:49 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.6.1) by [MichalBrylka](https://github.com/MichalBrylka)

Add code generation package that automatically generates necessary transformers
## C# 9.0 Code generation
With introduction of new code-gen engine, you can opt to have your transformer generated automatically without any imperative code.
```csharp 
//1. use specially provided (via code-gen) Auto.AutoDeconstructable attribute
[Auto.AutoDeconstructable]
//2. provide deconstructable aspect options or leave this attribute out - default options will be engaged 
[DeconstructableSettings('_', '∅', '%', '〈', '〉')]
readonly partial /*3. partial modifier is VERY important - you need this cause generated code is placed in different file*/ struct StructPoint3d
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    //4. specify constructor and matching deconstructor 
    public StructPoint3d(double x, double y, double z) { X = x; Y = y; Z = z; }

    public void Deconstruct(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
}

//5. sit back, relax and enjoy - code-gen will do the job for you :-)
``` 

This in turn might generate the following (parts of code ommited for brevity)
```csharp 
using /* ... */;
[Transformer(typeof(StructPoint3dTransformer))]
readonly partial struct StructPoint3d { }

sealed class StructPoint3dTransformer : TransformerBase<StructPoint3d>
{
    private readonly ITransformer<double> _transformer_x = TextTransformer.Default.GetTransformer<double>();
    /* specify remaining transformers... */
    private const int ARITY = 3;
    private readonly TupleHelper _helper = new TupleHelper('_', '∅', '%', '〈', '〉');

    protected override StructPoint3d ParseCore(in ReadOnlySpan<char> input)
    {
        var enumerator = _helper.ParseStart(input, ARITY);
        var t1 = _helper.ParseElement(ref enumerator, _transformer_x);        
        /* parse Y and Z... */
        _helper.ParseEnd(ref enumerator, ARITY);
        return new StructPoint3d(t1, t2, t3);
    }

    public override string Format(StructPoint3d element)
    {
        Span<char> initialBuffer = stackalloc char[32];
        var accumulator = new ValueSequenceBuilder<char>(initialBuffer);
        try
        {
             _helper.StartFormat(ref accumulator);
             var (x, y, z) = element;
            _helper.FormatElement(_transformer_x, x, ref accumulator);
            /* format Y and Z... */
            _helper.EndFormat(ref accumulator);
            return accumulator.AsSpan().ToString();
        }
        finally { accumulator.Dispose(); }
    }
}
``` 
### Code gen diagnositcs
Various diagnositcs exist to guide end user in creation of proper types that can be consumed by automatic generation. They might for example:
1. check if types decorated with Auto* attributes are declared partial (prerequisite for additive code generation)
2. validate settings passed via declarative syntax
3. validate internal structure of type (i.e. check if constructor has matching Deconstruct method)
4. check if external dependencies are included 


# Release v2.6 - Add code gen package 
Published [2021-02-25 13:49:14 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.6) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.5 - Rework settings to records 
Published [2020-12-31 14:34:17 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.5) by [MichalBrylka](https://github.com/MichalBrylka)

- Rework settings to records with native support for __with__ operator. 
- Code gen: Add full debugging support. 


# Release v2.4 - Code gen capabilities 
Published [2020-11-30 22:27:47 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.4) by [MichalBrylka](https://github.com/MichalBrylka)

- General .NET 5 standards cleanup
- Add code gen capabilities 
- Fix unit tests 
- Fix LeanCollection operators 
- Add support for System.Half type
- Add settings for automatic Deconstructable pattern
- Improve Deconstructable generation



# Release v2.3 - Upgrade to net5.0 
Published [2020-11-16 16:15:23 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.3) by [MichalBrylka](https://github.com/MichalBrylka)

- Upgrade to net5.0
- Make NumberTransformerCache helper public
- Add diagnostic clues for transformer creators
- Demonstrate using records with automatic/custom serialization


# Release v2.2.1 - TextSyntaxProvider rename 
Published [2020-05-15 09:08:54 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.2.1) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.2.0 - Format grammar discovery feature based on TransformationStore and settings  
Published [2020-05-14 22:33:11 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.2.0) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.1.2 - Introduce transforming of ArraySegment<> 
Published [2020-05-12 22:59:53 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.1.2) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.1.1 - Add TryParse to transformers. Reuse number transformers to double duty as enum parsers  
Published [2020-05-01 20:28:29 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.1.1) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.1.0 - Add support for transforming Version, IPAddress, Regex 
Published [2020-04-28 22:05:35 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.1.0) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.0.4 - Add support for parsing Stack<T> 
Published [2020-04-26 22:40:59 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.0.4) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.0.2 - Fix emptiness parsing for compound parsers. Add unit tests 
Published [2020-04-21 14:39:07 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.0.2) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v2.0.1 - Support for settings and basic Dependency Injection capabilities 
Published [2020-04-17 08:13:05 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.0.1) by [MichalBrylka](https://github.com/MichalBrylka)

0. New features
- Deconstructable aspect
- Transformable aspect 
for more information on both see: https://github.com/nemesissoft/Nemesis.TextParsers/blob/master/Specification.md


1. Introduce settings for handling:
- dictionaries
- lists/collections
- arrays
- value tuples
- key-value pairs
- deconstructables

2. Transformer creators may obtain ITransformerStore instance via simple constructor injection

3. Breaking changes:
- SpanCollectionSerializer was removed - opt for using ITransformerStore.GetTransformer. Use any common collection/array/dictionary class or interface as generic parameter i.e. IList<something>, IEnumerable<something>, Dictionary<key, value>, SomeType[], OtherType[][]
- ParsingPairSequence/ParsingSequence non generic ref structs were introduced in place of old ParsedSequence/ParsedPairSequence
- several methods from SpanParserHelper were removed/moved to more appropriate places 


# Release v2.0.0-alpha - Support for settings and basic Dependency Injection capabilities  
Published [2020-04-15 15:44:46 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v2.0.0-alpha) by [MichalBrylka](https://github.com/MichalBrylka)

Introduce settings for handling:
- dictionaries
- lists/collections
- arrays 
- value tuples 
- key-value pairs 
- deconstructables 

Transformer creators may obtain ITransformerStore instance via simple constructor injection 


# Release v1.5.1 - Improve unit tests experience  
Published [2020-03-29 22:08:26 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.5.1) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.5.0 - Deconstructables + simple parsers singletons + emptiness  
Published [2020-03-28 22:20:16 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.5.0) by [MichalBrylka](https://github.com/MichalBrylka)

1. Simple type parsers are exposed via singletons (see i.e. DoubleParser.Instance)
2. New type of parsing strategy - Deconstructables. It is enough for type to have constructor and matching Deconstruct - these members will be used to parse/format object automatically i.e. this structure 
```csharp
readonly struct Address
{
    public string City { get; }
    public int ZipCode { get; }

    public Address(string city, int zipCode)
    {
        City = city;
        ZipCode = zipCode;
    }

    public void Deconstruct(out string city, out int zipCode)
    {
        city = City;
        zipCode = ZipCode;
    }
}
```
will be automatically formatted to "(City;ZipCode)" format and parsed from same format


3. New concept - emptiness vs null parsing. Now transformers can decide what it meas to them when "" string is parsed as opposed to _null_ string

4. TupleHelper - user can now create complex parser that parse tuple-like structures (records etc.) using common logic 

5. Transformables - types can register own transformers using TransformerAttribute. Such custom transformers can benefit from simple dependency injections (currently only TransformerStore object) via constructor 

DEPRECATED:
One of next minor releases will be the last one where we support FromText(string) convention. It was only a bridge for consumers bound to targets < .NET Standard 2.1. Now we encourage upgrade to FromText(ReadOnlySpan<char>) - especially since all simple type parsers are exposed via singletons (see i.e. DoubleParser.Instance)


# Release v1.4.1 - Add support for deconstructable parsing  
Published [2020-03-23 22:37:32 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.4.1) by [MichalBrylka](https://github.com/MichalBrylka)

1. Improve text syntax provider - take from text factory
2. add support for automatic parsing of deconstructable objects i.e.
```csharp
readonly struct Address
{
    public string City { get; }
    public int ZipCode { get; }

    public Address(string city, int zipCode)
    {
        City = city;
        ZipCode = zipCode;
    }

    [UsedImplicitly]
    public void Deconstruct(out string city, out int zipCode)
    {
        city = City;
        zipCode = ZipCode;
    }
}
```


# Release v1.3.2 - Introduce TextTypeConverter 
Published [2020-03-19 13:24:08 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.3.2) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.3.0 - Reorganize namespaces 
Published [2020-03-16 09:51:39 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.3.0) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.2.0 - Move parsers to new namespace 
Published [2020-03-15 22:49:14 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.2.0) by [MichalBrylka](https://github.com/MichalBrylka)

Make parsers public and move them to Nemesis.TextParsers.Parsers namespace


# Release v1.1.3 - New parsers, improved exploratory tests 
Published [2020-03-14 23:10:48 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.1.3) by [MichalBrylka](https://github.com/MichalBrylka)

1. Improve exploratory texts
   * add nullable tests
   * support for testing jagged arrays
   * generic exploratory tests
2. Add Complex numbers parser 
3. Improve parsing nulls
4. Improve Factory method parsing description


# Release v1.1.2 - Improve performance by adding "in" param modifier for Parse 
Published [2020-02-27 21:36:57 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.1.2) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.1.1 - Extend support for parsing any convertible types + tests 
Published [2020-02-26 13:43:23 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.1.1) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.1.0 - CharParser + throw for generic purpose TypeConverter 
Published [2020-02-26 00:37:23 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.1.0) by [MichalBrylka](https://github.com/MichalBrylka)

Add Char type parser
Change behaviour - throw for generic purpose TypeConverter


# Release v1.0.6 - Common logic for setting new build version 
Published [2020-02-25 23:00:52 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.0.6) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.0.4 - Add support for retrieving release info from github 
Published [2020-02-25 22:36:36 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.0.4) by [MichalBrylka](https://github.com/MichalBrylka)

Release notes are taken from github metadata 


# Release v1.0.3 - Change AggBased compacting to opt-in behaviour 
Published [2020-02-18 14:07:59 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.0.3) by [MichalBrylka](https://github.com/MichalBrylka)




# Release v1.0.2 - Adding net461 and net47 targets 
Published [2019-11-08 12:58:03 GMT](https://github.com/nemesissoft/Nemesis.TextParsers/releases/tag/v1.0.2) by [Leszek-Kowalski](https://github.com/Leszek-Kowalski)


