# ![Logo](http://icons.iconarchive.com/icons/iconka/cat-commerce/64/review-icon.png) Nemesis.TextParsers

***

## Synopsis
This library aims at providing means to format to (serialize) and parse from (deserialize) various common types and complex types to flat human-readable / human-editable texts. Conciseness and performance are the name of the game here and with such features in mind this library was designed. 
Generally it's not possible to heavily influence message format, but library tries to be able to serialize most use cases. Serialization can be affected to certain degree using settings. User can register own transformers or use build-in conventions and aspects. The following catalogue lists down convention, defaults and format. They are listed in descending order of precedence down the chain of responsibility pattern that lies at heart of parsing library. Aspects are marked with **strong emphasis**.
*Compound parser* means that it employs inner value parser and adds own logic i.e. list parser will tokenize input, unescape escaped sequences and use appropriate inner parser 

When it's not marked otherwise, library follows Postel's Law (formatting is done by the book and best effort is used for parsing): 
> Be liberal in what you accept, and conservative in what you send.

## Serialization format 

1.	Default value can always be enforced using '∅' character. Where it's not relevant - whitespaces are trimmed upon parsing 
2.	Escaping sequences are supported – each parser/formatter might use different set of escaping sequences and it escapes/unescapes only characters of its interest.  As a rule of thumb, special characters are escaped with backslash ('\') and backslash itself is escaped with double backslash

###	Simple types 
Generally, not affected by custom settings, parsed using InvariantCulture. The following types are supported: 
1. string – no special border characters (i.e ' or ") are needed. Empty string is serialized to empty string. No inner escaping sequences are supported – but every UTF-16 character is recognized 
2. bool – case insensitive True or False literals 
3. char - single UTF-16 character
4. Numbers (byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal) are formatted in default format for InvariantCulture
5. Several build-in types are formatted using InvariantCulture and the following format
   * TimeSpan – null
   * DateTime/ DateTimeOffset - o
   * Guid – D
   * BigInteger – R
   * Complex – semicolon separated and parenthesis bound two numbers (real and imaginary part) i.e. *(1; 10)*


### KeyValuePair<,> (compound parser)
Key and value formatted using appropriate inner formatter, and separated with = i.e. *1=One*. 

Format can be customized using settings


### ValueTuples (compound parser)
Tuples of any arity  - colon separated and parenthesis-bound elements formatted using appropriate inner formatter i.e. *(1,ABC,10000000000,)*. While generally possible, one might be tempted to format/parse octuples and larger tuples, it might be considered a bad practice. From octuple values must be enclosed in their own set of parenthesis to follow .net convention that i.e. nonuple is in fact septuple and double bound together. So tuple with 10 numbers will have to be formatted like so *(1,2,3,4,5,6,7,(8,9,10))*. User might however consider implementing own container (with own transformer or other method of transformation) type for this purpose. 

Format can be customized using settings

### **Transformables** aspect
User can register his own transformer. More on this topic in Transformables section below. 

### FactoryMethod 
(legacy) It is possible to use type's ```ToString``` method for formatting and static ```FromText(ReadOnlySpan<char> text)``` or ```FromText(string text)``` method for parsing. Is given entity's code is not owned at parsing point, it's possible to provide separate FactoryMethod transformer 

### Enums 
By default, enums are parsed with case insensitive parser and numbers are allowed but format can be customized using settings

### Nullable (compound parser)
Values formatted using internal value parser, empty string is parsed as "no value"/null


### Dictionary (compound parsers)
*key1=value1;key2=value2*. Generic realizations of following types are supported: ```Dictionary<,>, IDictionary<,>, ReadOnlyDictionary<,>, IReadOnlyDictionary<,>, SortedList<,>, SortedDictionary<,>```

Moreover user can parse his custom dictionary-like data structures provided that they implement ```IDictionary<,>``` while providing empty public constructor or implement ```IReadOnlyDictionary<,>``` while having public constructor that accepts ```IDictionary<,>``` realized using same generic parameters  

Format can be customized using settings.

### Collections (compound parsers)
Generally parsed as separated with '|' and optionally enclosed in brackets/braces etc. 
1. Array - single dimension and jagged arrays are supported 
2. Collections - Generic realizations of following types are supported: ```IEnumerable<>, ICollection<>, IList<>, List<>, IReadOnlyCollection<>, IReadOnlyList<>, ReadOnlyCollection<>, ISet<>, SortedSet<>, HashSet<>, LinkedList<>, Stack<>, Queue<>, ObservableCollection<>```
3. LeanCollection -  LeanCollection type is a discriminated union that conveniently stashes 1,2,3 or more types (for performance reasons) but they are formatted like normal collections 
4. Custom collection - in addition to that user can parse his custom collection-like data structures provided that they implement ```ICollection<>``` while providing empty public constructor or implement ```IReadOnlyCollection<>``` while having public constructor that accepts ```IList<>``` realized using same generic parameters

Format can be customized using settings - separately for arrays and other collections.

### **Deconstructables** aspect
Values can be formatted automatically using deconstructor and parsed using matching constructor's metadata. More on this topic in Deconstructables section below. Format can be customized using settings. 


### TypeConverters (for legacy reasons) 
If all else fails, ```System.ComponentModel.TypeConverter``` is used to format/parse provided that given converter supports parsing from/to string. Due to lack of ability to box ReadOnlySpan - it cannot be used for parsing using this method. As a result, performance might be slightly degraded.

## Transformables
Study how the following code presents features and possibilities of **Transformable** aspect
```csharp
//1. Transformer can be registered on concrete implementation (class/struct) but also on interfaces/base classes 
[Transformer(typeof(CustomListTransformer<>))] //2. transformer registration can also be open generic - generic parameter is provided from transformed types 
internal interface ICustomList<TElement> : IEnumerable<TElement>, IEquatable<ICustomList<TElement>>
{    bool IsNullContent { get; }   }

//3. concrete implementation does not need to register separate transformers, but it will only "inherit" transformers from it's base types, not interfaces 
internal class CustomList<TElement> : ICustomList<TElement>, IEquatable<ICustomList<TElement>>
    {
        private readonly IReadOnlyCollection<TElement> _collection;
        public bool IsNullContent => _collection == null;
        public CustomList(IReadOnlyCollection<TElement> collection) => _collection = collection;

        public IEnumerator<TElement> GetEnumerator() => (_collection ?? Enumerable.Empty<TElement>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /*...*/
    }

internal class CustomListTransformer<TElement> : TransformerBase<ICustomList<TElement>>
{
    //4. Transformable aspect support simple injection of ITransformerStore via it's constructor - user can use other transformers already registered for simple/complex types 
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
    /*2. By default ony constructor and matching Deconstruct is needed. 
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
    //3. using default settings, this will be formatted to (CityName;ZipCode)
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
    /*constructor, Deconstruct and boilerplating ommited for brevity*/
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
    //7. for deconstructables empty string parses to "empty" instance of given type but that can be overriden
    public override DataWithCustomDeconstructableTransformer GetEmpty() =>
        new DataWithCustomDeconstructableTransformer(666, true, new decimal[] { 6, 7, 8, 9 });
}

```