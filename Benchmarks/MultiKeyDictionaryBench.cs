using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class MultiKeyDictionaryBench
    {
        private const int COUNT = 500;

        private static readonly Dictionary<string, int> _string = GetStringDict();
        private static readonly Dictionary<Tuple<int, int>, int> _tuple = GetTupleDict();
        private static readonly Dictionary<(int, int), int> _valueTuple = GetValueTupleDict();

        private static Dictionary<string, int> GetStringDict()
        {
            var dict = new Dictionary<string, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10);
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<Tuple<int, int>, int> GetTupleDict()
        {
            var dict = new Dictionary<Tuple<int, int>, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = new Tuple<int, int>(i, i * 10);
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<(int, int), int> GetValueTupleDict()
        {
            var dict = new Dictionary<(int, int), int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = (i, i * 10);
                dict[key] = i;
            }

            return dict;
        }

        [BenchmarkCategory("Build"), Benchmark(Baseline = true)]
        public int MakeStringDictionary()
        {
            var dict = GetStringDict();
            return dict.Count;
        }

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeTupleDictionary()
        {
            var dict = GetTupleDict();
            return dict.Count;
        }

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeValueTupleDictionary()
        {
            var dict = GetValueTupleDict();
            return dict.Count;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromStringDictionary()
        {
            var dict = _string;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10);
                value = dict[key];
            }

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromTupleDictionary()
        {
            var dict = _tuple;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = new Tuple<int, int>(i, i * 10);
                value = dict[key];
            }

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromValueTupleDictionary()
        {
            var dict = _valueTuple;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = (i, i * 10);
                value = dict[key];
            }

            return value;
        }
    }
}
