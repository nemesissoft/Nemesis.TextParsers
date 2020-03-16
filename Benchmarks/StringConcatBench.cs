using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Nemesis.TextParsers.Utils;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class StringConcatBench
    {
        private const int ITERATIONS = 500;

        [Benchmark]
        public string StringConcat()
        {
            string s = "";
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                s += c.ToString();
            return s;
        }

        [Benchmark(Baseline = true)]
        public string StringBuilderNew()
        {
            var sb = new StringBuilder();
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCache = new StringBuilder();
        [Benchmark]
        public string StringBuilderPool()
        {
            var sb = _builderCache;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        private readonly StringBuilder _builderCacheLarge = new StringBuilder(1000);
        [Benchmark]
        public string StringBuilderPoolLarge()
        {
            var sb = _builderCacheLarge;
            sb.Length = 0;
            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                sb.Append(c);
            return sb.ToString();
        }

        [Benchmark]
        public string ValueStringBuilder()
        {
            Span<char> initialBuffer = stackalloc char[1000];
            var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                accumulator.Append(c);

            var text = accumulator.AsSpan().ToString();
            accumulator.Dispose();
            return text;
        }

        [Benchmark]
        public string ValueStringBuilderUsing()
        {
            Span<char> initialBuffer = stackalloc char[1000];
            using var accumulator = new ValueSequenceBuilder<char>(initialBuffer);

            for (char c = 'A'; c < 'A' + ITERATIONS; c++)
                accumulator.Append(c);

            return accumulator.AsSpan().ToString();
        }
    }
}
