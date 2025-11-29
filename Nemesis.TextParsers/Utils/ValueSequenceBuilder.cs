using System.Buffers;

namespace Nemesis.TextParsers.Utils;

//TODO rework + add Append(ROS<>) + add ValueStringBuilder 
public ref struct ValueSequenceBuilder<T>(Span<T> initialSpan) : IDisposable
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