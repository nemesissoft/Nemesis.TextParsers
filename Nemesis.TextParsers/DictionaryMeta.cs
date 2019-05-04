using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using Nemesis.Essentials.Runtime;

namespace Nemesis.TextParsers
{
    /// <summary>
    /// Aids in providing metadata for GUI applications 
    /// </summary>
    public struct DictionaryMeta : IEquatable<DictionaryMeta>
    {
        public DictionaryKind Kind { get; }
        public Type KeyType { get; }
        public Type ValueType { get; }

        public bool IsValid => Kind != DictionaryKind.Unknown && KeyType != null && ValueType != null;

        public DictionaryMeta(DictionaryKind kind, Type keyType, Type valueType)
        {
            if (!Enum.IsDefined(typeof(DictionaryKind), kind))
                throw new InvalidEnumArgumentException(nameof(kind), (int)kind, typeof(DictionaryKind));
            Kind = kind;
            KeyType = keyType;
            ValueType = valueType;
        }

        public void Deconstruct(out DictionaryKind kind, out Type keyType, out Type valueType)
        {
            kind = Kind;
            keyType = KeyType;
            valueType = ValueType;
        }

        public override string ToString() => $"{Kind}<{KeyType?.FullName}, {ValueType?.FullName}>";

        #region Equals
        public bool Equals(DictionaryMeta other) => Kind == other.Kind && KeyType == other.KeyType && ValueType == other.ValueType;

        public override bool Equals(object obj) => !(obj is null) && obj is DictionaryMeta other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ (KeyType != null ? KeyType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ValueType != null ? ValueType.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(DictionaryMeta left, DictionaryMeta right) => left.Equals(right);

        public static bool operator !=(DictionaryMeta left, DictionaryMeta right) => !left.Equals(right);
        #endregion

        public object CreateDictionary(IList<(object Key, object Value)> sourceElements)
        {
            if (!IsValid) return null;

            var createDictMethod = typeof(DictionaryMeta).GetMethod(nameof(CreateDictionaryGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) ??
                                   throw new MissingMethodException(nameof(DictionaryMeta), nameof(CreateDictionaryGeneric));
            createDictMethod = createDictMethod.MakeGenericMethod(KeyType, ValueType);

            return createDictMethod.Invoke(this, new object[] { sourceElements });
        }

        public IDictionary<TKey, TValue> CreateDictionaryGeneric<TKey, TValue>(IList<(object Key, object Value)> sourceElements)
        {
            if (!IsValid) return null;
            int capacity = sourceElements.Count;

            IDictionary<TKey, TValue> result =
                Kind == DictionaryKind.SortedDictionary ? new SortedDictionary<TKey, TValue>() :
                    (
                        Kind == DictionaryKind.SortedList
                            ? new SortedList<TKey, TValue>(capacity)
                            : (IDictionary<TKey, TValue>)new Dictionary<TKey, TValue>(capacity)
                    );

            for (int i = 0; i < capacity; i++)
            {
                var (oKey, oValue) = sourceElements[i];
                TKey key = SpanParserHelper.ConvertElement<TKey>(oKey);
                TValue value = SpanParserHelper.ConvertElement<TValue>(oValue);

                if (key == null) key = GetQuiteUniqueNotNullKey();

                result[key] = value;
            }

            return Kind == DictionaryKind.ReadOnlyDictionary 
                ? new ReadOnlyDictionary<TKey, TValue>(result) 
                : result;

            TKey GetQuiteUniqueNotNullKey()
            {
                try
                {
                    var nextKey = capacity + 1;

                    var typeOfKey = typeof(TKey);
                    if (typeOfKey == typeof(string))
                        return (TKey)(object)$"Key {nextKey}";
                    else if (TypeMeta.IsNumeric(typeOfKey))
                        return (TKey)Convert.ChangeType(nextKey % 128, typeOfKey);
                    else if (typeOfKey.IsValueType && Nullable.GetUnderlyingType(typeOfKey) is Type underlyingType)
                        return (TKey)FormatterServices.GetUninitializedObject(underlyingType);
                    else
                        return (TKey)FormatterServices.GetUninitializedObject(typeOfKey);
                }
                catch (Exception) { return default; }
            }
        }
    }

    internal static class DictionaryKindHelper
    {
        public static DictionaryMeta GetDictionaryMeta(Type dictType)
        {
            if (IsTypeSupported(dictType) &&
                GetDictionaryKind(dictType) is DictionaryKind kind &&
                kind != DictionaryKind.Unknown
               )
            {
                var (keyType, valueType) = GetPairType(dictType);
                return new DictionaryMeta(kind, keyType, valueType);
            }

            return default;
        }

        private static DictionaryKind GetDictionaryKind(Type dictType)
        {
            if (dictType.IsGenericType && dictType.GetGenericTypeDefinition() is Type definition)
            {
                if (definition == typeof(Dictionary<,>) ||
                    definition == typeof(IDictionary<,>))
                    return DictionaryKind.Dictionary;

                else if (definition == typeof(ReadOnlyDictionary<,>) ||
                         definition == typeof(IReadOnlyDictionary<,>))
                    return DictionaryKind.ReadOnlyDictionary;

                else if (definition == typeof(SortedList<,>))
                    return DictionaryKind.SortedList;
                else if (definition == typeof(SortedDictionary<,>))
                    return DictionaryKind.SortedDictionary;
            }

            return DictionaryKind.Unknown;
        }

        private static (Type keyType, Type valueType) GetPairType(Type dictType)
        {
            var genType =
                dictType.IsGenericType && dictType.GetGenericTypeDefinition() is Type definition &&
                (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                    ? dictType
                    : TypeMeta.GetConcreteInterfaceOfType(dictType, typeof(IDictionary<,>))
                      ?? throw new InvalidOperationException("Type has to be or implement IDictionary<,> or IReadOnlyDictionary<,>");

            return (genType.GenericTypeArguments[0], genType.GenericTypeArguments[1]);
        }

        private static readonly HashSet<Type> _supportedDictionaryTypes = new HashSet<Type>()
        {
            typeof(Dictionary<,>),
            typeof(IDictionary<,>),

            typeof(ReadOnlyDictionary<,>),
            typeof(IReadOnlyDictionary<,>),

            typeof(SortedList<,>),
            typeof(SortedDictionary<,>),
        };

        public static bool IsTypeSupported(Type dictType) =>
            dictType != null &&
            dictType.IsGenericType &&
            dictType.GetGenericTypeDefinition() is Type definition &&
            _supportedDictionaryTypes.Contains(definition);
    }

    public enum DictionaryKind : byte
    {
        Unknown,
        Dictionary,
        SortedDictionary,
        SortedList,
        ReadOnlyDictionary
    }
}
