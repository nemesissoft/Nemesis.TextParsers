using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IAggressionBased<out TValue>
    {
        TValue PassiveValue { get; }
        TValue NormalValue { get; }
        TValue AggressiveValue { get; }

        TValue GetValueFor(byte aggression);
    }

    public class AggressionBasedClass<TValue> : IAggressionBased<TValue>
    {
        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public TValue GetValueFor(byte aggression) => NormalValue;

        public AggressionBasedClass(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }
    }

    public readonly struct AggressionBasedStruct<TValue> : IAggressionBased<TValue>
    {
        public TValue PassiveValue { get; }
        public TValue NormalValue { get; }
        public TValue AggressiveValue { get; }

        public TValue GetValueFor(byte aggression) => NormalValue;

        public AggressionBasedStruct(TValue passiveValue, TValue normalValue, TValue aggressiveValue)
        {
            PassiveValue = passiveValue;
            NormalValue = normalValue;
            AggressiveValue = aggressiveValue;
        }
    }

    [MemoryDiagnoser]
    public class AggressionBasedBench
    {
        [Benchmark]
        public int StructAllocTest()
        {
            int result = 0;

            for (int i = 0; i < 100; i++)
            {
                IAggressionBased<int> ab = new AggressionBasedStruct<int>(1, 2, 3);
                result += ab.NormalValue * ab.GetValueFor(10);
            }

            return result;
        }

        [Benchmark]
        public int ClassAllocTest()
        {
            int result = 0;

            for (int i = 0; i < 100; i++)
            {
                IAggressionBased<int> ab = new AggressionBasedClass<int>(1, 2, 3);
                result += ab.NormalValue * ab.GetValueFor(10);
            }

            return result;
        }
    }
}
