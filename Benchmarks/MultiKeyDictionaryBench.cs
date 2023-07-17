using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
// ReSharper disable UseDeconstruction
// ReSharper disable SuggestVarOrType_SimpleTypes

// ReSharper disable CommentTypo

namespace Benchmarks
{
    /*
    |                   Method |     Mean |    Error |   StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
    |------------------------- |---------:|---------:|---------:|------:|--------:|--------:|-------:|------:|----------:|
    |     MakeStringDictionary | 75.03 us | 0.817 us | 0.764 us |  1.00 |    0.00 | 20.7520 | 4.1504 |     - |  130912 B |
    |      MakeTupleDictionary | 67.30 us | 0.944 us | 0.883 us |  0.90 |    0.02 |  7.8125 | 1.0986 |     - |   49464 B |
    | MakeValueTupleDictionary | 43.43 us | 0.316 us | 0.295 us |  0.58 |    0.01 |  5.3101 | 0.4883 |     - |   33640 B |
    |                          |          |          |          |       |         |         |        |       |           |
    |     FromStringDictionary | 75.06 us | 0.657 us | 0.614 us |  1.00 |    0.00 | 17.3340 |      - |     - |  109480 B |
    |      FromTupleDictionary | 69.90 us | 0.691 us | 0.646 us |  0.93 |    0.01 |  4.3945 |      - |     - |   28032 B |
    | FromValueTupleDictionary | 57.07 us | 0.460 us | 0.407 us |  0.76 |    0.01 |       - |      - |     - |      32 B |
     */
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class MultiKeyDictionaryBench
    {
        private const int COUNT = 700;
        private static readonly IEnumerable<(string Client, string Username, string Token)> _keys = Enumerable.Range(1, COUNT)
            .Select(i => (
                i % 07 == 0 ? "ALL" : i.ToString(),
                i % 09 == 0 ? "ALL" : (i * 10).ToString(),
                i % 11 == 0 ? "ALL" : (i * 100).ToString()
            )).ToArray();

        private static readonly Dictionary<string, int> _string = GetStringDict();
        private static readonly Dictionary<Tuple<string, string, string>, int> _tuple = GetTupleDict();
        private static readonly Dictionary<(string, string, string), int> _valueTuple = GetValueTupleDict();

        private static Dictionary<string, int> GetStringDict()
        {
            var dict = new Dictionary<string, int>(COUNT);
            int i = 0;
            foreach (var (client, username, token) in _keys)
                dict["Client=" + client + " Username=" + username + " Token=" + token] = i++;

            return dict;
        }

        private static Dictionary<Tuple<string, string, string>, int> GetTupleDict()
        {
            var dict = new Dictionary<Tuple<string, string, string>, int>(COUNT);
            int i = 0;
            foreach (var (Client, Username, Token) in _keys)
                dict[new Tuple<string, string, string>(Client, Username, Token)] = i++;

            return dict;
        }

        private static Dictionary<(string, string, string), int> GetValueTupleDict()
        {
            var dict = new Dictionary<(string, string, string), int>(COUNT);
            int i = 0;
            foreach (var (Client, Username, Token) in _keys)
                dict[(Client, Username, Token)] = i++;

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

            foreach (var (Client, Username, Token) in _keys)
                value = dict["Client=" + Client + " Username=" + Username + " Token=" + Token];

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromTupleDictionary()
        {
            var dict = _tuple;
            int value = 0;

            foreach (var (Client, Username, Token) in _keys)
                value = dict[new Tuple<string, string, string>(Client, Username, Token)];

            return value;
        }

        [BenchmarkCategory("Retrieval"), Benchmark]
        public int FromValueTupleDictionary()
        {
            var dict = _valueTuple;
            int value = 0;

            foreach (var (Client, Username, Token) in _keys)
                value = dict[(Client, Username, Token)];

            return value;
        }
    }
}
