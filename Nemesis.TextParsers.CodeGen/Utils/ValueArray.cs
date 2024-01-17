#nullable enable
using System.Collections;
using System.Globalization;

namespace Nemesis.TextParsers.CodeGen.Utils;

internal static class ValueArray
{
    public static ValueArray<T> Create<T>(ReadOnlySpan<T> items) => items.Length == 0 ? new([]) : new(items.ToArray());
}

[CollectionBuilder(typeof(ValueArray), nameof(ValueArray.Create))]
public readonly struct ValueArray<T>(T[]? array, IEqualityComparer<T>? equalityComparer = null)
    : IEquatable<ValueArray<T>>, IEnumerable<T>, IFormattable
{
    private readonly T[]? _array = array;
    private readonly IEqualityComparer<T> _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;


    /// <sinheritdoc/>
    public bool Equals(ValueArray<T> other) => AsSpan().SequenceEqual(other.AsSpan(), _equalityComparer);

    /// <sinheritdoc/>
    public override bool Equals(object? obj) => obj is ValueArray<T> array && Equals(array);

    /// <sinheritdoc/>
    public override int GetHashCode()
    {
        if (_array is null) return 0;

        var hash = 0;

        for (int i = 0; i < _array.Length; i++)
        {
            var element = _array[i];
            hash = unchecked(
                (hash * 397)
                     ^
                (element is null ? 0 : _equalityComparer.GetHashCode(element))
            );
        }

        return hash;
    }

    public ReadOnlySpan<T> AsSpan() => _array.AsSpan();

    /// <sinheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(_array ?? [])).GetEnumerator();

    /// <sinheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)(_array ?? [])).GetEnumerator();

    public int Count => _array?.Length ?? 0;

    public T? this[int i] => _array is null ? default : _array[i];

    public static bool operator ==(ValueArray<T> left, ValueArray<T> right) => left.Equals(right);

    public static bool operator !=(ValueArray<T> left, ValueArray<T> right) => !left.Equals(right);


    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var len = Count;
        if (_array is null) return "∅";
        else if (len == 0) return "[]";
        else if (len < 5)
        {
            return $"[{string.Join(", ", this.Select(e => FormatValue(e, formatProvider)))}]";
        }
        else if (len < 25)
        {
            var sb = new StringBuilder(100);
            sb.Append('[');

            for (int i = 0; i < len - 1; i++)
                sb.Append(FormatValue(_array[i], formatProvider)).Append(", ");

            sb.Append(FormatValue(_array[len - 1], formatProvider)).Append(']');
            return sb.ToString();
        }
        else
        {
            var sb = new StringBuilder(256);
            sb.Append('[');

            for (int i = 0; i < 10; i++)
            {
                sb.Append(FormatValue(_array[i], formatProvider));
                sb.Append(", ");
            }
            sb.Append("..., ");


            for (int i = len - 10; i < len; i++)
            {
                sb.Append(FormatValue(_array[i], formatProvider));
                if (i < len - 1)
                    sb.Append(", ");
            }

            sb.Append(']');
            return sb.ToString();
        }
    }


    public override string ToString() => ToString(null, CultureInfo.InvariantCulture);

    private static string? FormatValue(object? value, IFormatProvider? formatProvider) =>
        value switch
        {
            null => "∅",
            bool b => b ? "true" : "false",
            string s => $"\"{s}\"",
            char c => $"\'{c}\'",
            DateTime dt => dt.ToString("o", formatProvider),
            IFormattable @if => @if.ToString(null, formatProvider),
            IEnumerable ie => "[" + string.Join(", ", ie.Cast<object>().Select(e => FormatValue(e, formatProvider))) + "]",
            _ => value.ToString()
        };
}

#if NETSTANDARD

file static class Extensions
{
    public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null)
    {
        if (span.Length != other.Length) return false;

        comparer ??= EqualityComparer<T>.Default;
        for (int i = 0; i < span.Length; i++)
        {
            if (!comparer.Equals(span[i], other[i]))
                return false;
        }

        return true;
    }
}

#endif