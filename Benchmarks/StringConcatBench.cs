using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Nemesis.TextParsers.Utils;
using System.Buffers;
using System.Runtime.CompilerServices;

// ReSharper disable CommentTypo

namespace Benchmarks
{/*
|                    Method | Size |          Mean | Ratio | Allocated |
|-------------------------- |----- |--------------:|------:|----------:|
|              StringConcat |    4 |      72.47 ns |  1.78 |     192 B |
|          StringBuilderNew |    4 |      40.77 ns |  1.00 |     232 B |
|         StringBuilderPool |    4 |      22.98 ns |  0.56 |      32 B |
|    StringBuilderPoolLarge |    4 |      23.13 ns |  0.57 |      32 B |
|      ValueSequenceBuilder |    4 |      31.25 ns |  0.77 |      32 B |
| ValueSequenceBuilderUsing |    4 |      30.37 ns |  0.74 |      32 B |
|        ValueStringBuilder |    4 |      30.93 ns |  0.76 |      32 B |
|                           |      |               |       |           |
|              StringConcat |   16 |     373.94 ns |  5.21 |    1032 B |
|          StringBuilderNew |   16 |      71.79 ns |  1.00 |     256 B |
|         StringBuilderPool |   16 |      55.69 ns |  0.78 |      56 B |
|    StringBuilderPoolLarge |   16 |      55.72 ns |  0.78 |      56 B |
|      ValueSequenceBuilder |   16 |      53.24 ns |  0.74 |      56 B |
| ValueSequenceBuilderUsing |   16 |      52.97 ns |  0.74 |      56 B |
|        ValueStringBuilder |   16 |      56.20 ns |  0.78 |      56 B |
|                           |      |               |       |           |
|              StringConcat |   64 |   1,786.20 ns |  8.59 |    7272 B |
|          StringBuilderNew |   64 |     208.42 ns |  1.00 |     352 B |
|         StringBuilderPool |   64 |     194.60 ns |  0.93 |     152 B |
|    StringBuilderPoolLarge |   64 |     190.14 ns |  0.91 |     152 B |
|      ValueSequenceBuilder |   64 |     156.32 ns |  0.75 |     152 B |
| ValueSequenceBuilderUsing |   64 |     155.36 ns |  0.75 |     152 B |
|        ValueStringBuilder |   64 |     156.37 ns |  0.75 |     152 B |
|                           |      |               |       |           |
|              StringConcat |  256 |  11,079.68 ns | 13.43 |   78312 B |
|          StringBuilderNew |  256 |     825.54 ns |  1.00 |    1264 B |
|         StringBuilderPool |  256 |     716.29 ns |  0.87 |     536 B |
|    StringBuilderPoolLarge |  256 |     720.22 ns |  0.87 |     536 B |
|      ValueSequenceBuilder |  256 |     528.65 ns |  0.64 |     536 B |
| ValueSequenceBuilderUsing |  256 |     527.02 ns |  0.64 |     536 B |
|        ValueStringBuilder |  256 |     545.14 ns |  0.66 |     536 B |
|                           |      |               |       |           |
|              StringConcat | 1024 | 121,527.94 ns | 38.14 | 1099752 B |
|          StringBuilderNew | 1024 |   3,195.15 ns |  1.00 |    4480 B |
|         StringBuilderPool | 1024 |   2,984.43 ns |  0.93 |    2072 B |
|    StringBuilderPoolLarge | 1024 |   3,016.04 ns |  0.94 |    2072 B |
|      ValueSequenceBuilder | 1024 |   2,107.25 ns |  0.66 |    2072 B |
| ValueSequenceBuilderUsing | 1024 |   2,135.26 ns |  0.67 |    2072 B |
|        ValueStringBuilder | 1024 |   2,134.57 ns |  0.67 |    2072 B |
*/
    [MemoryDiagnoser]
    public class StringConcatBench
    {
        [Params(4, 16, 64, 256, 1024)]
        public int Size { get; set; }

        [Benchmark]
        public string StringConcat()
        {
            string s = "";
            for (char c = 'A'; c < 'A' + Size; c++)
                s += c.ToString();
            return s;
        }

        [Benchmark(Baseline = true)]
        public string StringBuilderNew()
        {
            var sb = new StringBuilder(64);
            for (char c = 'A'; c < 'A' + Size; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCache = new StringBuilder(64);
        [Benchmark]
        public string StringBuilderPool()
        {
            var sb = _builderCache;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + Size; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCacheLarge = new StringBuilder(1030);
        [Benchmark]
        public string StringBuilderPoolLarge()
        {
            var sb = _builderCacheLarge;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + Size; c++)
                sb.Append(c);
            return sb.ToString();
        }

        [Benchmark]
        public string ValueSequenceBuilder()
        {
            Span<char> initialBuffer = stackalloc char[Math.Min(512, Size)];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + Size; c++)
                accumulator.Append(c);

            var text = accumulator.AsSpan().ToString();
            accumulator.Dispose();
            return text;
        }

        [Benchmark]
        public string ValueSequenceBuilderUsing()
        {
            Span<char> initialBuffer = stackalloc char[Math.Min(512, Size)];
            using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + Size; c++)
                accumulator.Append(c);

            return accumulator.AsSpan().ToString();
        }

        [Benchmark]
        public string ValueSequenceBuilderAllocHalf()
        {
            Span<char> initialBuffer = stackalloc char[Math.Min(512, Size) >> 1];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + Size; c++)
                accumulator.Append(c);

            var text = accumulator.AsSpan().ToString();
            accumulator.Dispose();
            return text;
        }

        [Benchmark]
        public string ValueStringBuilder()
        {
            Span<char> initialBuffer = stackalloc char[Math.Min(512, Size)];
            var accumulator = new ValueStringBuilder(initialBuffer);

            for (char c = 'A'; c < 'A' + Size; c++)
                accumulator.Append(c);

            var text = accumulator.AsSpan().ToString();
            accumulator.Dispose();
            return text;
        }
    }

    public ref struct ValueStringBuilder
    {
        private Span<char> _current;

        private char[] _arrayFromPool;

        public int Length { get; private set; }

        public ref char this[int index] => ref _current[index];

        public ValueStringBuilder(Span<char> initialSpan)
        {
            _current = initialSpan;
            _arrayFromPool = null;
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char item)
        {
            int pos = Length;
            if (pos >= _current.Length)
                Grow();
            _current[pos] = item;
            Length = pos + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char Pop() => _current[--Length];

        /// <summary>
        /// Return values written so far to underlying memory 
        /// </summary>
        public readonly ReadOnlySpan<char> AsSpan() => _current.Slice(0, Length);

        public readonly ReadOnlySpan<char> AsSpanFromTo(int start, int length) => _current.Slice(start, length);

        public readonly ReadOnlySpan<char> AsSpanTo(int length) => _current.Slice(0, length);

        public readonly ReadOnlySpan<char> AsSpanFrom(int start) => _current.Slice(start, Length - start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                ArrayPool<char>.Shared.Return(_arrayFromPool);
                _arrayFromPool = null;
            }
            this = default;
        }

        public void Shrink(uint by = 1)
        {
            if (Length > 0)
                Length -= (int)by;
        }

        /// <summary>
        /// Converts this instance to string. If underlying type <paramref name="{T}"/> is <see cref="System.Char"/> then it returns text written so far
        /// </summary>
        public override string ToString() => AsSpan().ToString();

        private void Grow()
        {
            var array = ArrayPool<char>.Shared.Rent(Math.Max(_current.Length * 2, 16));
            _current.CopyTo(array);
            char[] prevFromPool = _arrayFromPool;
            _current = _arrayFromPool = array;
            if (prevFromPool != null)
                ArrayPool<char>.Shared.Return(prevFromPool);
        }
    }
}
