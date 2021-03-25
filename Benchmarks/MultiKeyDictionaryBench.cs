using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class MultiKeyDictionaryBench
    {
        private const int COUNT = 500;

        private static readonly Dictionary<string, int> _string = GetStringDict();
        private static readonly Dictionary<Tuple<string, string, string>, int> _tuple = GetTupleDict();
        private static readonly Dictionary<(string, string, string), int> _valueTuple = GetValueTupleDict();

        private static Dictionary<string, int> GetStringDict()
        {
            var dict = new Dictionary<string, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10) + "Token" + (i * 100);
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<Tuple<string, string, string>, int> GetTupleDict()
        {
            var dict = new Dictionary<Tuple<string, string, string>, int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = new Tuple<string, string, string>(i.ToString(), (i * 10).ToString(), (i * 100).ToString());
                dict[key] = i;
            }

            return dict;
        }

        private static Dictionary<(string, string, string), int> GetValueTupleDict()
        {
            var dict = new Dictionary<(string, string, string), int>(COUNT);
            for (int i = 0; i < COUNT; i++)
            {
                var key = (i.ToString(), (i * 10).ToString(), (i * 100).ToString());
                dict[key] = i;
            }

            return dict;
        }

        [BenchmarkCategory("Build"), Benchmark(Baseline = true)]
        public int MakeStringDictionary() => GetStringDict().Count;

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeTupleDictionary() => GetTupleDict().Count;
        

        [BenchmarkCategory("Build"), Benchmark]
        public int MakeValueTupleDictionary() => GetValueTupleDict().Count;


        [BenchmarkCategory("Retrieval"), Benchmark(Baseline = true)]
        public int FromStringDictionary()
        {
            var dict = _string;
            int value = 0;

            for (int i = 0; i < COUNT; i++)
            {
                var key = "Client=" + i + "Username" + (i * 10) + "Token" + (i * 100);
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
                var key = new Tuple<string, string, string>(i.ToString(), (i * 10).ToString(), (i * 100).ToString());
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
                var key = (i.ToString(), (i * 10).ToString(), (i * 100).ToString());
                value = dict[key];
            }

            return value;
        }
    }
}
