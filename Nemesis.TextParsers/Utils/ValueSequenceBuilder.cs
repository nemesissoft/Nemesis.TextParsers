using System.Buffers;

namespace Nemesis.TextParsers.Utils;

//TODO rework + add Append(ROS<>) + add ValueStringBuilder 
public ref struct ValueSequenceBuilder<T>(Span<T> initialSpan)
{
    private Span<T> _current = initialSpan;

    private T[] _arrayFromPool = null;

    public int Length { get; private set; } = 0;

    public ref T this[int index] => ref _current[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        int pos = Length;
        if (pos >= _current.Length)
            Grow();
        _current[pos] = item;
        Length = pos + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Pop() => _current[--Length];

    /// <summary>
    /// Return values written so far to underlying memory 
    /// </summary>
    public readonly ReadOnlySpan<T> AsSpan() => _current[..Length];

    public readonly ReadOnlySpan<T> AsSpanFromTo(int start, int length) => _current.Slice(start, length);

    public readonly ReadOnlySpan<T> AsSpanTo(int length) => _current[..length];

    public readonly ReadOnlySpan<T> AsSpanFrom(int start) => _current.Slice(start, Length - start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_arrayFromPool != null)
        {
            ArrayPool<T>.Shared.Return(_arrayFromPool);
            _arrayFromPool = null;
        }
        this = default;
    }

    public void Shrink(byte by = 1)
    {
        if (Length > 0)
            Length -= by;
    }

    /// <summary>
    /// Converts this instance to string. If underlying type <paramref name="{T}"/> is <see cref="System.Char"/> then it returns text written so far
    /// </summary>
    public readonly override string ToString() => AsSpan().ToString();

    private void Grow()
    {
        var array = ArrayPool<T>.Shared.Rent(Math.Max(_current.Length * 2, 16));
        _current.CopyTo(array);
        var prevFromPool = _arrayFromPool;
        _current = _arrayFromPool = array;
        if (prevFromPool != null)
            ArrayPool<T>.Shared.Return(prevFromPool);
    }
}

/*
public ref struct ValueStringBuilder
{
    private char[] _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public int Length
    {
        get => _pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _chars.Length);
            _pos = value;
        }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _chars.Length)
            Grow(capacity - _chars.Length);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate = false)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }
        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index]
    {
        get
        {
            Debug.Assert(index < _pos);
            return ref _chars[index];
        }
    }

    public override string ToString()
    {
        var s = new string(_chars.Slice(0, _pos));
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }
        return _chars.Slice(0, _pos);
    }

    public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _pos);
    public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars.Slice(0, _pos).TryCopyTo(destination))
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }
        else
        {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
        _chars.Slice(index, count).Fill(value);
        _pos += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        int pos = _pos;
        if (pos < _chars.Length)
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string s)
    {
        int pos = _pos;
        if (s.Length == 1 && pos < _chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
            AppendSlow(s);
    }

    private void AppendSlow(string s)
    {
        int pos = _pos;
        if (pos > _chars.Length - s.Length)
            Grow(s.Length);

        s.AsSpan().CopyTo(_chars.Slice(pos));
        _pos += s.Length;
    }

    public void Append(char c, int count)
    {
        if (_pos > _chars.Length - count)
            Grow(count);

        Span<char> dst = _chars.Slice(_pos, count);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }
        _pos += count;
    }

    public unsafe void Append(char* value, int length)
    {
        int pos = _pos;
        if (pos > _chars.Length - length)
            Grow(length);

        Span<char> dst = _chars.Slice(_pos, length);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = *value++;
        }
        _pos += length;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        int pos = _pos;
        if (pos > _chars.Length - value.Length)
            Grow(value.Length);

        value.CopyTo(_chars.Slice(_pos));
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        int origPos = _pos;
        if (origPos > _chars.Length - length)
            Grow(length);

        _pos = origPos + length;
        return _chars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int requiredAdditionalCapacity)
    {
        Debug.Assert(requiredAdditionalCapacity > 0);

        char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_pos + requiredAdditionalCapacity, _chars.Length * 2));

        _chars.CopyTo(poolArray);

        char[] toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        char[] toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
            ArrayPool<char>.Shared.Return(toReturn);
    }
}*/
