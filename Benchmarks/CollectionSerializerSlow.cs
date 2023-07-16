using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Benchmarks
{
    public class CollectionSerializerSlow
    {
        public const char DEFAULT_LIST_DELIMITER = '|';
        public const char DEFAULT_DICTIONARY_PAIRS_DELIMITER = ';';
        public const char DEFAULT_DICTIONARY_KEY_VALUE_DELIMITER = '=';
        public const char DEFAULT_NULL_ELEMENT_MARKER = '∅';

        public const char ALTERNATIVE_LIST_DELIMITER = '|';

        //for performance reasons, all delimiters and escaped characters are single chars
        //this makes a parsing grammar to conform LL1 rules and is very beneficial to overall parsing performance 
        public char ListDelimiter { get; }
        public char DictionaryPairsDelimiter { get; }
        public char DictionaryKeyValueDelimiter { get; }
        public char NullElementMarker { get; }

        private readonly char[] _validListDelimiters;
        private readonly char[] _validDictDelimiters;

        private readonly Escaper _escaper;
        private readonly IStringConverter _converter;

        #region Patterns

        private const RegexOptions REGEX_OPTIONS = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;
        private readonly Regex _listSplitter;
        private readonly Regex _dictPairsSplitter;
        private readonly Regex _dictKeyValueSplitter;
        private readonly Regex _illegalListEscapeSequencesDetector;
        private readonly Regex _illegalDictEscapeSequencesDetector;

        private static readonly Regex _invalidRegexCharsReplacer = new(@"
(?<EscapeableChar>
\.   |
\^   |
\$   |
\*   |
\+   |
\-   |
\?   |
\(   |
\)   |
\[   |
\]   |
\{   |
\}   |
\\   |
\|
)
", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static string EscapeControlCharacters(string pattern)
            => _invalidRegexCharsReplacer.Replace(pattern, @"\${EscapeableChar}");

        #endregion

        internal CollectionSerializerSlow(char listDelimiter, char dictionaryPairsDelimiter, char dictionaryKeyValueDelimiter, char nullElementMarker, IStringConverter converter)
        {
            char escapingSequenceStart = '\\';//FIX: this can be potentially a ctor param

            var specialCharacters = new[] { listDelimiter, dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker, escapingSequenceStart };
            if (specialCharacters.Length != specialCharacters.Distinct().Count())
                throw new ArgumentException($"All special characters have to be distinct. Actual special characters:{string.Join(", ", specialCharacters.Select(c => $"'{c}'"))}");

            _escaper = new Escaper(escapingSequenceStart, nullElementMarker);

            ListDelimiter = listDelimiter;
            DictionaryPairsDelimiter = dictionaryPairsDelimiter;
            DictionaryKeyValueDelimiter = dictionaryKeyValueDelimiter;
            NullElementMarker = nullElementMarker;
            _converter = converter;


            const string SPLITTER_PATTERN = @"(?<!(^|$|[^\\])(\\\\)*?\\)";

            _listSplitter = new Regex(SPLITTER_PATTERN + $@"\{listDelimiter}", REGEX_OPTIONS);
            _dictPairsSplitter = new Regex(SPLITTER_PATTERN + $@"\{dictionaryPairsDelimiter}", REGEX_OPTIONS);
            _dictKeyValueSplitter = new Regex(SPLITTER_PATTERN + $@"\{dictionaryKeyValueDelimiter}", REGEX_OPTIONS);


            _validListDelimiters = new[] { escapingSequenceStart, listDelimiter, nullElementMarker };
            _illegalListEscapeSequencesDetector = new Regex($@"
(?<= 
    (^|$|[^\\])
    (\\\\)*?
    \\  
)   ( $ | [^{EscapeControlCharacters(new string(_validListDelimiters))}] )
", REGEX_OPTIONS);


            _validDictDelimiters = new[] { escapingSequenceStart, dictionaryPairsDelimiter, dictionaryKeyValueDelimiter, nullElementMarker };
            _illegalDictEscapeSequencesDetector = new Regex($@"
(?<= 
    (^|$|[^\\])
    (\\\\)*?
    \\  
)   ( $ | [^{EscapeControlCharacters(new string(_validDictDelimiters))}] )
", REGEX_OPTIONS);
        }

        public static readonly CollectionSerializerSlow DefaultInstance = new(
            DEFAULT_LIST_DELIMITER, DEFAULT_DICTIONARY_PAIRS_DELIMITER, DEFAULT_DICTIONARY_KEY_VALUE_DELIMITER, DEFAULT_NULL_ELEMENT_MARKER,
            TextValueConverter.Instance);

        public static readonly CollectionSerializerSlow AlternativeInstance = new(
            ALTERNATIVE_LIST_DELIMITER, DEFAULT_DICTIONARY_PAIRS_DELIMITER, DEFAULT_DICTIONARY_KEY_VALUE_DELIMITER, DEFAULT_NULL_ELEMENT_MARKER,
            TextValueConverter.Instance);


        public IEnumerable<string> ParseCollectionOfStrings(string text)
        {
            if (text == null) return null;
            if (text == string.Empty) return Array.Empty<string>();

            ThrowOnInvalidText(text, _illegalListEscapeSequencesDetector, _validListDelimiters);

            return
                from s in _listSplitter.Split(text)
                let element = _escaper.UnescapeCharacter(s, ListDelimiter)
                let elementOrNull = element == NullElementMarker.ToString() ? null : element
                let unescapedNullMarker = _escaper.UnescapeNullMarker(elementOrNull)
                select _escaper.UnescapeEscapeSequenceStart(unescapedNullMarker);
        }

        public IEnumerable<T> ParseCollection<T>(string text) => ParseCollectionOfStrings(text).Select(s => _converter.ConvertFromString<T>(s));

        public string FormatCollection(IEnumerable<string> coll)
        {
            if (coll == null) return null;
            if (!coll.Any()) return "";

            var normalizedColl =
                from element in coll
                let escapedEscapeSequenceStart = _escaper.EscapeEscapeSequenceStart(element)
                let escapedNullMarker = _escaper.EscapeNullMarker(escapedEscapeSequenceStart)
                let elementOrNull = escapedNullMarker ?? NullElementMarker.ToString()
                select _escaper.EscapeCharacter(elementOrNull, ListDelimiter);

            return string.Join(ListDelimiter.ToString(), normalizedColl);
        }

        public string FormatCollection<T>(IEnumerable<T> coll)
            => FormatCollection(coll.Select(elem => _converter.ConvertToString(elem)));

        public IDictionary<string, string> ParseStringToStringDictionary(string text)
        {
            if (text == null) return null;
            if (text == string.Empty) return new Dictionary<string, string>(0);

            ThrowOnInvalidText(text, _illegalDictEscapeSequencesDetector, _validDictDelimiters);

            var keyValuePairs = _dictPairsSplitter.Split(text).Select(
                s => _escaper.UnescapeCharacter(s, DictionaryPairsDelimiter)
                )
                ;

            var dict = new Dictionary<string, string>(8);
            foreach (string kvp in keyValuePairs)
            {
                var potentialKeyAndValue =
                    from keyValue in _dictKeyValueSplitter.Split(kvp)
                    let keyOrValue = _escaper.UnescapeCharacter(keyValue, DictionaryKeyValueDelimiter)
                    let keyOrValueOrNull = keyOrValue == NullElementMarker.ToString() ? null : keyOrValue
                    let unescapedNullMarker = _escaper.UnescapeNullMarker(keyOrValueOrNull)
                    select _escaper.UnescapeEscapeSequenceStart(unescapedNullMarker);

                using var enumerator = potentialKeyAndValue.GetEnumerator();

                if (!enumerator.MoveNext()) throw GetArgumentException();
                var key = enumerator.Current;

                if (!enumerator.MoveNext()) throw GetArgumentException();
                var value = enumerator.Current;

                if (enumerator.MoveNext()) throw GetArgumentException();

                if (key == null) throw GetArgumentException();

                dict.Add(key, value);
            }
            return dict;

            Exception GetArgumentException() => new ArgumentException($@"Key{DictionaryKeyValueDelimiter}value pair expects token collection to be of length 2", nameof(text));
        }

        public IDictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string text)
            => ParseStringToStringDictionary(text).ToDictionary
               (
                    stringPair => _converter.ConvertFromString<TKey>(stringPair.Key),
                    stringPair => _converter.ConvertFromString<TValue>(stringPair.Value)
               );

        public string FormatDictionary(IDictionary<string, string> dict)
        {
            if (dict == null) return null;
            if (dict.Count == 0) return "";

            var keyValuePairsAsTexts = dict.Select(kvp => new[] { kvp.Key, kvp.Value });

            var normalizedPairs =
                from kvp in keyValuePairsAsTexts
                let escapedEscapeSequenceStart = kvp.Select(elem => _escaper.EscapeEscapeSequenceStart(elem))
                let escapedNullMarker = escapedEscapeSequenceStart.Select(elem => _escaper.EscapeNullMarker(elem))
                let elementsOrNulls = escapedNullMarker.Select(elem => elem ?? NullElementMarker.ToString())
                let escapedDictionaryKeyValueDelimiter = elementsOrNulls.Select(elem => _escaper.EscapeCharacter(elem, DictionaryKeyValueDelimiter)).ToArray()
                let normalizedKvp = $"{escapedDictionaryKeyValueDelimiter[0]}{DictionaryKeyValueDelimiter}{escapedDictionaryKeyValueDelimiter[1]}"
                select _escaper.EscapeCharacter(normalizedKvp, DictionaryPairsDelimiter);

            return string.Join(DictionaryPairsDelimiter.ToString(), normalizedPairs);
        }

        public string FormatDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
            => FormatDictionary(dict.ToDictionary(
                    compoundPair => _converter.ConvertToString(compoundPair.Key),
                    compoundPair => _converter.ConvertToString(compoundPair.Value)
               ));

        internal void ThrowOnInvalidText(string text, Regex illegalEscapeSequencesDetector, char[] validDelimiters)
        {
            MatchCollection mc = illegalEscapeSequencesDetector.Matches(text);
            if (mc.Count > 0)
            {
                var captures = mc.Cast<Match>().SelectMany(m => m.Captures.Cast<Capture>()).ToList();
                var capturesText = captures.Select(cap => $"\"{cap.Value}\" found at {cap.Index}");
                var detectedErrors = captures.Aggregate(
                    Enumerable.Repeat(' ', text.Length + 2).ToArray(),
                    (ch, capture) => { ch[capture.Index] = '^'; return ch; },
                    ch => new string(ch)
                    );

                throw new ArgumentException($@"The following escape characters are not valid:{string.Join(", ", capturesText)}.
Only [{string.Join(", ", validDelimiters.Select(d => $"'{d}'"))}] are supported as escaping sequence characters.
{text}
{detectedErrors}
");
            }
        }

        public override string ToString()
            => $@"Collection: Item1{ListDelimiter}Item2{Environment.NewLine}Dictionary: key1{DictionaryKeyValueDelimiter}value1{DictionaryPairsDelimiter}key2{DictionaryKeyValueDelimiter}value2{Environment.NewLine}Escaping: {_escaper}";

        private class Escaper
        {
            private readonly char _escapingSequenceStart;
            private readonly string _escapingSequenceStartEscaped;
            private readonly string _escapingSequenceStartUnescaped;


            private readonly char _nullMarker;
            private readonly string _nullMarkerEscaped;
            private readonly string _nullMarkerUnescaped;

            public Escaper(char escapingSequenceStart, char nullMarker)
            {
                _escapingSequenceStart = escapingSequenceStart;
                _escapingSequenceStartEscaped = $"{escapingSequenceStart}{escapingSequenceStart}";
                _escapingSequenceStartUnescaped = $"{escapingSequenceStart}";

                _nullMarker = nullMarker;
                _nullMarkerEscaped = $"{escapingSequenceStart}{nullMarker}";
                _nullMarkerUnescaped = $"{nullMarker}";
            }

            //Escape start, null, given character

            public string UnescapeEscapeSequenceStart(string text)
                => text?.Replace(_escapingSequenceStartEscaped, _escapingSequenceStartUnescaped);

            public string UnescapeNullMarker(string text)
                => text?.Replace(_nullMarkerEscaped, _nullMarkerUnescaped);

            public string UnescapeCharacter(string text, char character)
                => text?.Replace($"{_escapingSequenceStart}{character}", $"{character}");

            public string EscapeEscapeSequenceStart(string text)
                => text?.Replace(_escapingSequenceStartUnescaped, _escapingSequenceStartEscaped);

            public string EscapeNullMarker(string text)
                => text?.Replace(_nullMarkerUnescaped, _nullMarkerEscaped);

            public string EscapeCharacter(string text, char character)
                => text?.Replace($"{character}", $"{_escapingSequenceStart}{character}");


            public override string ToString() => $"Escape with '{_escapingSequenceStart}', Null: '{_nullMarker}'";
        }
    }

    public interface IStringConverter
    {
        string ConvertToString<T>(T value);
        T ConvertFromString<T>(string text);
    }

    /*public class IdentityConverter : IStringConverter
    {
        public string ConvertToString<T>(T value) => value?.ToString();

        public T ConvertFromString<T>(string text) => (T)(object)text;
    }*/

    public class TextValueConverter : IStringConverter
    {
        private TextValueConverter() { }
        public static readonly TextValueConverter Instance = new();


        public T ConvertFromString<T>(string text) => (T)FromString(typeof(T), text);

        public object FromString(Type destinationType, string text) => destinationType == typeof(string)
            ? text ?? GetEmptyString()
            : GetParseMethod(destinationType).Invoke(this, new object[] { text });

        private static MethodInfo GetParseMethod(Type destinationType)
        {
            if (destinationType.IsArray)
                return GetMethod(nameof(ParseArray)).MakeGenericMethod(destinationType.GetElementType());

            else if (ImplementsGenericInterface(destinationType, typeof(IList<>)) && !destinationType.IsArray)
                return GetMethod(nameof(ParseList)).MakeGenericMethod(destinationType.GetGenericArguments()[0]);

            else if (ImplementsGenericInterface(destinationType, typeof(IDictionary<,>)))
                return GetMethod(nameof(ParseDictionary))
                    .MakeGenericMethod(destinationType.GetGenericArguments()[0], destinationType.GetGenericArguments()[1]);

            else return GetMethod(nameof(ParseSimple)).MakeGenericMethod(destinationType);
        }

        private TElement[] ParseArray<TElement>(string text) => string.IsNullOrEmpty(text)
            ? GetEmptyArray<TElement>()
            : CollectionSerializerSlow.DefaultInstance.ParseCollection<TElement>(text).ToArray();

        private List<TElement> ParseList<TElement>(string text) => string.IsNullOrEmpty(text)
            ? GetEmptyList<TElement>()
            : CollectionSerializerSlow.DefaultInstance.ParseCollection<TElement>(text).ToList();

        private Dictionary<TKey, TValue> ParseDictionary<TKey, TValue>(string text) => string.IsNullOrEmpty(text)
            ? GetEmptyDictionary<TKey, TValue>()
            : (Dictionary<TKey, TValue>)CollectionSerializerSlow.DefaultInstance.ParseDictionary<TKey, TValue>(text);

        private T ParseSimple<T>(string text) => string.IsNullOrEmpty(text) ? GetEmptySimple<T>() :
            (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(text);


        protected virtual string GetEmptyString() => null;
        protected virtual TElement[] GetEmptyArray<TElement>() => null;
        protected virtual List<TElement> GetEmptyList<TElement>() => null;
        protected virtual Dictionary<TKey, TValue> GetEmptyDictionary<TKey, TValue>() => null;
        protected virtual T GetEmptySimple<T>() => default;







        public string ConvertToString<T>(T value) => ToString(typeof(T), value);

        public string ToString(Type sourceType, object obj) => sourceType == typeof(string)
            ? (string)obj ?? GetEmptyFormattedText()
            : (string)GetFormatMethod(sourceType).Invoke(this, new[] { obj });

        private static MethodInfo GetFormatMethod(Type sourceType)
        {
            if (sourceType.IsArray)
                return GetMethod(nameof(FormatArray)).MakeGenericMethod(sourceType.GetElementType());

            else if (ImplementsGenericInterface(sourceType, typeof(IList<>)) && !sourceType.IsArray)
                return GetMethod(nameof(FormatList)).MakeGenericMethod(sourceType.GetGenericArguments()[0]);

            else if (ImplementsGenericInterface(sourceType, typeof(IDictionary<,>)))
                return GetMethod(nameof(FormatDictionary))
                    .MakeGenericMethod(sourceType.GetGenericArguments()[0], sourceType.GetGenericArguments()[1]);

            else return GetMethod(nameof(FormatSimple)).MakeGenericMethod(sourceType);
        }

        private string FormatArray<TElement>(TElement[] array) =>
            array == null ? GetEmptyFormattedText() : CollectionSerializerSlow.DefaultInstance.FormatCollection(array);

        private string FormatList<TElement>(IList<TElement> list) =>
            list == null ? GetEmptyFormattedText() : CollectionSerializerSlow.DefaultInstance.FormatCollection(list);

        private string FormatDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict) =>
            dict == null ? GetEmptyFormattedText() : CollectionSerializerSlow.DefaultInstance.FormatDictionary(dict);


        private string FormatSimple<T>(T obj) => obj == null ? GetEmptyFormattedText() :
            TypeDescriptor.GetConverter(typeof(T)).ConvertToInvariantString(obj);


        protected virtual string GetEmptyFormattedText() => null;


        private static MethodInfo GetMethod(string name) =>
            typeof(TextValueConverter).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) ??
            throw new MissingMethodException($"Method {typeof(TextValueConverter).Name}.{name} does not exist");

        private static bool ImplementsGenericInterface(Type type, Type generic) =>
            type == generic ||
            type.IsGenericType && type.GetGenericTypeDefinition() == generic ||
            type.GetInterfaces().Any(t => t.IsGenericType && ImplementsGenericInterface(t, generic));
    }
}
