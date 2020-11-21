# ![Logo](http://icons.iconarchive.com/icons/iconka/cat-commerce/64/review-icon.png) Nemesis.TextParsers

***

## Synopsis
This library aims at providing means to format to (serialize) and parse from (deserialize) various common types and complex types to flat human-readable / human-editable texts. Conciseness and performance are the name of the game here and with such features in mind this library was designed. 
Generally it's not possible to heavily influence message format, but library tries to be able to serialize most use cases. Serialization can be affected to certain degree using settings. User can register own transformers or use build-in conventions and aspects. The following catalogue lists down convention, defaults and formats. They are listed in descending order of precedence down the chain of responsibility pattern that lies at heart of parsing library. Aspects are marked with **strong emphasis**.
*Compound parser* means that it employs inner value parser and adds own logic i.e. list parser will tokenize input, unescape escaped sequences and use appropriate inner parser 

When it's not marked otherwise, library follows Postel's Law (formatting is done by the book and best effort is used for parsing): 
> Be liberal in what you accept, and conservative in what you send.

## Serialization format 
General rules
1. Default value can always be enforced using '∅' character. Where it's not relevant - whitespace are trimmed upon parsing, 
2. Escaping sequences are supported – each parser/formatter might use different set of escaping sequences and it escapes/unescape only characters of its interest.  As a rule of thumb, special characters are escaped with backslash ('\\') and backslash itself is escaped with double backslash.
3. Recognized types can be arbitrarily embedded/mixed i.e. it's possible to parse/format ```SortedDictionary<char?, IList<float[][]>>``` with no hiccups
4. Serialization grammar discovery is possible using TextConverterSyntax.GetSyntaxFor method i.e.
```csharp
var actual = TextSyntaxProvider.Default.GetSyntaxFor(AggressionBased3<Dictionary<uint, System.IO.FileMode?>>);	
Assert.That(actual, Is.EqualTo(@"Hash ('#') delimited list with 1 or 3 (passive, normal, aggressive) elements i.e. 1#2#3
escape '#' with ""\#"" and '\' by doubling it ""\\""

AggressionBased3`1 elements syntax:
	KEY=VALUE pairs separated with ';' bound with nothing and nothing i.e.
	key1=value1;key2=value2;key3=value3
	(escape '=' with ""\="", ';' with ""\;"", '∅' with ""\∅"" and '\' by doubling it ""\\"")
	Key syntax:
		Whole number from 0 to 4294967295
	Value syntax:
		One of following: CreateNew, Create, Open, OpenOrCreate, Truncate, Append or null"));
```

###	Simple types 
Generally, not affected by custom settings, parsed using InvariantCulture. The following types are supported: 
1. string – no special border characters (i.e ' or ") are needed. Empty string is serialized to empty string. No inner escaping sequences are supported – but every UTF-16 character is recognized 
2. bool – case insensitive True or False literals 
3. char - single UTF-16 character
4. Numbers (byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal) are formatted in default roundtrip format with InvariantCulture
5. Several build-in types are formatted using InvariantCulture and the following format
   * TimeSpan – null
   * DateTime/ DateTimeOffset - o
   * Guid – D
   * BigInteger – R
6. Version - integers separated with dots
7. IpAddress - valid integers separated with dots
8. Complex – semicolon separated and parenthesis bound two numbers (real and imaginary part) i.e. *(3.14; 2)* which translates to *π+2ⅈ*
9. Regex - option and pattern serialized in Deconstructable fashion (see below) - separated with ';', escaped with '~' (to avoid overescaping of already frequent '\' escaping character in regex format), bounded by curly braces ('{', '}'). Options serialized using regex option format (flag combination specified without separators i.e. ``` "mi" == RegexOptions.Multiline | RegexOptions.IgnoreCase ```):
   * RegexOptions.None → '0'
   * RegexOptions.IgnoreCase → 'i'
   * RegexOptions.Multiline → 'm'
   * RegexOptions.ExplicitCapture → 'n'
   * RegexOptions.Compiled → 'c'
   * RegexOptions.Singleline → 's'
   * RegexOptions.IgnorePatternWhitespace → 'x'
   * RegexOptions.RightToLeft → 'r'
   * RegexOptions.ECMAScript → 'e'
   * RegexOptions.CultureInvariant → 'v'  


### KeyValuePair<,> (compound parser)
Key and value formatted using appropriate inner formatter, and separated with = i.e. *1=One*. 

Format can be customized using settings


### ValueTuples (compound parser)
Tuples of any arity  - colon separated and parenthesis-bound elements formatted using appropriate inner formatter i.e. *(1,ABC,10000000000)*. 

While generally possible, one might be tempted to format/parse octuples and larger tuples, it might be considered a bad practice. From octuple values must be enclosed in their own set of parenthesis to follow .net convention that that is i.e. nonuple is in fact septuple and double bound together. So tuple with 10 numbers will have to be formatted like so *(1,2,3,4,5,6,7,(8,9,10))*. User might however consider implementing own container (with own transformer or other method of transformation) type for this purpose. 

Format can be customized using settings

### Transformables aspect
User can register his own transformer. More on this topic in **Transformables** section below. 

### FactoryMethod 
(legacy) It is possible to use type's ```ToString``` method for formatting and static ```FromText(ReadOnlySpan<char> text)``` or ```FromText(string text)``` method for parsing. If given entity's code is not owned at parsing point, it's possible to provide separate FactoryMethod transformer 

### Enums 
By default, enums are parsed with case insensitive parser and numbers are allowed but format can be customized using settings

### Nullable (compound parser)
Values formatted using internal value parser, empty string is parsed as "no value"/null


### Dictionary (compound parsers)
*key1=value1;key2=value2*

Generic realizations of following types are supported: ```Dictionary<,>, IDictionary<,>, ReadOnlyDictionary<,>, IReadOnlyDictionary<,>, SortedList<,>, SortedDictionary<,>```

Moreover user can automatically parse his custom dictionary-like data structures provided that they implement ```IDictionary<,>``` while providing empty public constructor or implement ```IReadOnlyDictionary<,>``` while having public constructor that accepts ```IDictionary<,>``` realized using same generic parameters  

Format can be customized using settings.

### Collections (compound parsers)
Generally parsed as separated with '|' and optionally enclosed in brackets/braces etc. 
1. Array - single dimension and jagged arrays are supported 
2. Collections - Generic realizations of following types are supported: ```IEnumerable<>, ICollection<>, IList<>, List<>, IReadOnlyCollection<>, IReadOnlyList<>, ReadOnlyCollection<>, ISet<>, SortedSet<>, HashSet<>, LinkedList<>, Stack<>, Queue<>, ObservableCollection<>```
3. LeanCollection -  LeanCollection type is a discriminated union that conveniently stashes 1,2,3 or more types (for performance reasons) but they are formatted like normal collections 
4. Custom collection - in addition to that user can automatically parse his custom collection-like data structures provided that they implement ```ICollection<>``` while providing empty public constructor or implement ```IReadOnlyCollection<>``` while having public constructor that accepts ```IList<>``` realized using same generic parameters
5. ArraySegment<> serialized in Deconstructable fashion (see below) - separated with '@', escaped with '~', bounded by curly braces ('{', '}'). Serialized parts are (in order of occurrence): offset, count, array



Format can be customized using settings - separately for arrays and other collections.

### Deconstructables aspect
Values can be formatted automatically using deconstructor and parsed using matching constructor's metadata. More on this topic in **Deconstructables** section below. Format can be customized using settings. 


### TypeConverters (for legacy reasons) 
If all else fails, ```System.ComponentModel.TypeConverter``` is used to format/parse provided that given converter supports parsing from/to string. Due to inherent lack of ability to box ReadOnlySpan - it cannot be used for parsing using this method. As a result, performance might be slightly degraded.

## Transformables
Study how the following code presents features and possibilities of **Transformable** aspect
```csharp
//1. Transformer can be registered on concrete implementation (class/struct) but also on interfaces/base classes 
[Transformer(typeof(CustomListTransformer<>))] 
//2. transformer registration can also be open generic - generic parameter is provided from transformed types 
internal interface ICustomList<TElement> : IEnumerable<TElement>, IEquatable<ICustomList<TElement>>
{    bool IsNullContent { get; }   }

//3. concrete implementation does not need to register separate transformers, but it will only "inherit" transformers from it's base types, not interfaces 
internal class CustomList<TElement> : ICustomList<TElement>, IEquatable<ICustomList<TElement>>
{
    private readonly IReadOnlyCollection<TElement> _collection;
    public bool IsNullContent => _collection == null;
    public CustomList(IReadOnlyCollection<TElement> collection) =>_collection = collection;

    public IEnumerator<TElement> GetEnumerator() => _collection ?? Enumerable.Empty<TElement>()).GetEnumerator);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /*...*/
}

internal class CustomListTransformer<TElement> : TransformerBase<ICustomList<TElement>>
{
    //4. Transformable aspect supports simple injection of ITransformerStore via it's constructor - user can use other transformers already registered for simple/complex types 
    private readonly ITransformerStore _transformerStore;
    public CustomListTransformer(ITransformerStore transformerStore) => _transformerStore = transformerStore;

    protected override ICustomList<TElement> ParseCore(in ReadOnlySpan<char> text) {/*...*/}

    public override string Format(ICustomList<TElement> list) {/*...*/}

    /*5. optionally override GetEmpty() and/or GetNull() to provide custom parsers for empty/null strings respectively.
      By default null parses to default value for given type (which happens to be null for reference types)
      and empty parses to what looks like empty in given example i.e. empty array, dictionary etc.*/
}
```


## Deconstructables
Study how the following code presents features and possibilities of **Deconstructables** aspect
```csharp
//1. type can be truly immutable 💪
readonly struct Address
{
    public string City { get; }
    public int ZipCode { get; }
    /*2. By default only constructor and matching Deconstruct is needed. 
         If desired (constructor, Deconstruct) pair can be provided externally*/
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
    //3. using default settings, this will be formatted to (CityName;ZipCode), esacaped using '\' and with '∅' as null marker
}
struct Person
{
    public string Name { get; }    
    public int Age { get; }    
    /*4. Deconstructables can be embedded in other deconstructables (like in this example) 
      or be embedded in types that use any other supported method of parsing 
      (transformables, factory method, inside value tuples etc.)*/
    public Address Address { get; }
    
    public Person(string name, int age, Address address) { Name = name;  Age = age;  Address = address; }    
    public void Deconstruct(out string name, out int age, out Address address) { name = Name;  age = Age;  address = Address; }
}

//5. optionally an on-the-fly transformed can be constructed using the following construct
DeconstructionTransformerBuilder
   .GetDefault(TextTransformer.Default)
   .WithBorders('{', '}')
   .WithDelimiter('_')
   .WithNullElementMarker('␀')
   .ToTransformer<Person>();

//6. Transformable and deconstructable aspects can be combined i.e. by deriving from CustomDeconstructionTransformer
[Transformer(typeof(DeconstructableTransformer))]
readonly struct DataWithCustomDeconstructableTransformer  
{
    public float Number { get; }
    public bool IsEnabled { get; }
    public decimal[] Prices { get; }
    /*constructor, Deconstruct and boilerplating omitted for brevity*/
}

class DeconstructableTransformer : CustomDeconstructionTransformer<DataWithCustomDeconstructableTransformer>
{
    public DeconstructableTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

    protected override DeconstructionTransformerBuilder BuildSettings(DeconstructionTransformerBuilder prototype) =>
        prototype
            .WithBorders('{', '}')
            .WithDelimiter('_')
            .WithNullElementMarker('␀')
            .WithDeconstructableEmpty();//default value but just to be clear 
    //7. for deconstructables empty string parses to "empty" instance of given type but that can be overridden
    public override DataWithCustomDeconstructableTransformer GetEmpty() =>
        new DataWithCustomDeconstructableTransformer(666, true, new decimal[] { 6, 7, 8, 9 });
}

/*8. TextTransformer.Default provides default values for (among others) DeconstructableSettings. They can be globally overriden inside referenced SettingsStore but user may opt to override them only for given type. For more info see point 5 and 6 of this listing, but there is also a convenient declarative approach: */
[DeconstructableSettings(',', '∅', '\\', '{', '}')] //9. user may want to specify all characters or leave some out as defaults - taken from DeconstructableSettingsAttribute constructor defaults, not from SettingsStore 
internal readonly struct Child
{
    public byte Age { get; }
    public float Weight { get; }

    public Child(byte age, float weight) { Age = age; Weight = weight; }

    public void Deconstruct(out byte age, out float weight) { age = Age; weight = Weight; }

    public override string ToString() => $"{nameof(Age)}: {Age}, {nameof(Weight)}: {Weight}";
}
```

## Settings
User can use default parsing/formatting settings or opt-in with own settings instances or overridden ones. Settings need to extend ```ISettings``` marker interface. Entry point is ```SettingsStore``` class that can be constructed easily using ```SettingsStoreBuilder```:

```csharp 
var customStore = SettingsStoreBuilder.GetDefault()
    .AddOrUpdate(ownSettings)
    .Build();
```

Settings class instances can be instantiated using normal constructors or, especially if changing only couple of settings from default ones is desired - user can choose to use .With pattern (example for DictionarySettings):
```csharp 
var borderedDictionary = DictionarySettings.Default
    .With(s => s.Start, '{')
    .With(s => s.End, '}')
    .With(s => s.DictionaryKeyValueDelimiter, ',')
    .With(s => s.NullElementMarker, '␀') //'␀' is special Unicode character. Difficult to insert from normal keyboard, but unlikely to be part of normal message - so no need to use escaping sequences in most cases 
    ;
```

"With" extension method is analogous to With-pattern known from functional languages:
[Copy and Update Record Expressions](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/copy-and-update-record-expressions)

Here however a With is merely an extension method that works due to convention - new instance is created using types largest (number of parameter-wise) constructor. A property that user wishes to change will be used from method's second parameter, while all remaining parameters will be taken from already existing instance. Immutability of settings is not hindered this way. Property and constructor parameter names need to be equal in name using case insensitive comparison. See an example:
```csharp 
var tupleSettings = new ValueTupleSettings(',', '∅', '\\', '(', ')');
var newSettings = tupleSettings.With(s => s.Delimiter, '_');

Assert.That(tupleSettings.Delimiter, Is.EqualTo(',')); //not modified, Delimiter property is get-only by the way 
Assert.That(newSettings.Delimiter, Is.EqualTo('_'));
```

## C# 9.0 Records
With introduction of [Records](https://devblogs.microsoft.com/dotnet/welcome-to-c-9-0/#records) in C# 9.0 one may wonder how they might be serialized to flat text formats. As a matter of fact, Records are merely a syntax sugar for C# - they stand for a class with automatically implemented structural equality(along with IEquatable and operators), positional deconstruction and printing/formatting. Hence, as such they are supported in NTP out-of-the-box - via Deconstructable pattern. Caution is advised when employing this pattern for derived positional records:
```csharp 
record Vertebrate(string Name)
{
    public Vertebrate() : this("") { }
}

//Deconstruct will be generated for reduced property set 
record ReptileWithoutName(Habitat Habitat) : Vertebrate { }

//repeat Name property to become part of contract
record ReptileWithName(string Name, Habitat Habitat) : Vertebrate(Name) { }

//use automatic transformer
TextTransformer.Default.GetTransformer<ReptileWithName>().Parse("(Comodo Dragon;Terrestrial)")
```

Alternatively you might implement semi-automatic transformation via Transformable pattern (here depicted in tandem with Deconstructable transformation):
```csharp 
[Transformer(typeof(PersonTransformer))]
record Person(string FirstName, string FamilyName, int Age) { }

class PersonTransformer : CustomDeconstructionTransformer<Person>
{
    public PersonTransformer([NotNull] ITransformerStore transformerStore) : base(transformerStore) { }

    protected override DeconstructionTransformerBuilder BuildSettings(DeconstructionTransformerBuilder prototype) =>
        prototype
            .WithoutBorders()
            .WithDelimiter('-')
            .WithNullElementMarker('␀')
            .WithDeconstructableEmpty();
}
```

Finally, you can implement own _`Nemesis.TextParsers.ITransformer<TElement>`_ or (if you really have no other option) _`System.ComponentModel.TypeConverter`_ class


## TBA
 - [ ] ILookup<,>
 - [ ] IGrouping<,>
 - [ ] ReadOnlyObservableCollection<>