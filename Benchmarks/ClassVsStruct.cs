using BenchmarkDotNet.Attributes;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ClassVsStruct
    {
        class ClassContainer
        {
            readonly HelperClass _field = new HelperClass(',', '∅', '\\', '(', ')');

            public int Count() => _field.Count();
        }
        
        class ClassFromInterfaceContainer
        {
            readonly ICounter _field = new HelperClassFromInterface(',', '∅', '\\', '(', ')');

            public int Count() => _field.Count();
        }

        class StructContainer
        {
            readonly HelperStruct _field = new HelperStruct(',', '∅', '\\', '(', ')');

            public int Count() => _field.Count();
        }
       
        class StructReadonlyContainer
        {
            readonly HelperStructReadonly _field = new HelperStructReadonly(',', '∅', '\\', '(', ')');

            public int Count() => _field.Count();
        }

        [Benchmark]
        public int Class()
        {
            var container = new ClassContainer();

            int count = 0;
            for (int i = 0; i < 1000; i++)
                count += container.Count();

            return count;
        }

        [Benchmark]
        public int ClassFromInterface()
        {
            var container = new ClassFromInterfaceContainer();

            int count = 0;
            for (int i = 0; i < 1000; i++)
                count += container.Count();

            return count;
        }

        [Benchmark]
        public int Struct()
        {
            var container = new StructContainer();

            int count = 0;
            for (int i = 0; i < 1000; i++)
                count += container.Count();

            return count;
        }

        [Benchmark(Baseline = true)]
        public int StructReadonly()
        {
            var container = new StructReadonlyContainer();

            int count = 0;
            for (int i = 0; i < 1000; i++)
                count += container.Count();

            return count;
        }




        sealed class HelperClass
        {
            private readonly char _tupleDelimiter;
            private readonly char _nullElementMarker;
            private readonly char _escapingSequenceStart;
            private readonly char _tupleStart;
            private readonly char _tupleEnd;

            public HelperClass(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd)
            {
                _tupleDelimiter = tupleDelimiter;
                _nullElementMarker = nullElementMarker;
                _escapingSequenceStart = escapingSequenceStart;
                _tupleStart = tupleStart;
                _tupleEnd = tupleEnd;
            }

            public int Count()
            {
                int i = 0;
                i += _tupleDelimiter != _nullElementMarker ? 1 : 0;
                i += _tupleDelimiter != _escapingSequenceStart ? 1 : 0;
                i += _tupleDelimiter != _tupleStart ? 1 : 0;
                i += _tupleDelimiter != _tupleEnd ? 1 : 0;

                i += _nullElementMarker != _escapingSequenceStart ? 1 : 0;
                i += _nullElementMarker != _tupleStart ? 1 : 0;
                i += _nullElementMarker != _tupleEnd ? 1 : 0;

                i += _escapingSequenceStart != _tupleStart ? 1 : 0;
                i += _escapingSequenceStart != _tupleEnd ? 1 : 0;

                i += _tupleStart != _tupleEnd ? 1 : 0;

                return i;
            }
        }

        interface ICounter
        {
            int Count();
        }
        sealed class HelperClassFromInterface : ICounter
        {
            private readonly char _tupleDelimiter;
            private readonly char _nullElementMarker;
            private readonly char _escapingSequenceStart;
            private readonly char _tupleStart;
            private readonly char _tupleEnd;

            public HelperClassFromInterface(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd)
            {
                _tupleDelimiter = tupleDelimiter;
                _nullElementMarker = nullElementMarker;
                _escapingSequenceStart = escapingSequenceStart;
                _tupleStart = tupleStart;
                _tupleEnd = tupleEnd;
            }

            public int Count()
            {
                int i = 0;
                i += _tupleDelimiter != _nullElementMarker ? 1 : 0;
                i += _tupleDelimiter != _escapingSequenceStart ? 1 : 0;
                i += _tupleDelimiter != _tupleStart ? 1 : 0;
                i += _tupleDelimiter != _tupleEnd ? 1 : 0;

                i += _nullElementMarker != _escapingSequenceStart ? 1 : 0;
                i += _nullElementMarker != _tupleStart ? 1 : 0;
                i += _nullElementMarker != _tupleEnd ? 1 : 0;

                i += _escapingSequenceStart != _tupleStart ? 1 : 0;
                i += _escapingSequenceStart != _tupleEnd ? 1 : 0;

                i += _tupleStart != _tupleEnd ? 1 : 0;

                return i;
            }
        }

        struct HelperStruct
        {
            private readonly char _tupleDelimiter;
            private readonly char _nullElementMarker;
            private readonly char _escapingSequenceStart;
            private readonly char _tupleStart;
            private readonly char _tupleEnd;

            public HelperStruct(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd)
            {
                _tupleDelimiter = tupleDelimiter;
                _nullElementMarker = nullElementMarker;
                _escapingSequenceStart = escapingSequenceStart;
                _tupleStart = tupleStart;
                _tupleEnd = tupleEnd;
            }

            public int Count()
            {
                int i = 0;
                i += _tupleDelimiter != _nullElementMarker ? 1 : 0;
                i += _tupleDelimiter != _escapingSequenceStart ? 1 : 0;
                i += _tupleDelimiter != _tupleStart ? 1 : 0;
                i += _tupleDelimiter != _tupleEnd ? 1 : 0;

                i += _nullElementMarker != _escapingSequenceStart ? 1 : 0;
                i += _nullElementMarker != _tupleStart ? 1 : 0;
                i += _nullElementMarker != _tupleEnd ? 1 : 0;

                i += _escapingSequenceStart != _tupleStart ? 1 : 0;
                i += _escapingSequenceStart != _tupleEnd ? 1 : 0;

                i += _tupleStart != _tupleEnd ? 1 : 0;

                return i;
            }
        }

        readonly struct HelperStructReadonly
        {
            private readonly char _tupleDelimiter;
            private readonly char _nullElementMarker;
            private readonly char _escapingSequenceStart;
            private readonly char _tupleStart;
            private readonly char _tupleEnd;

            public HelperStructReadonly(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd)
            {
                _tupleDelimiter = tupleDelimiter;
                _nullElementMarker = nullElementMarker;
                _escapingSequenceStart = escapingSequenceStart;
                _tupleStart = tupleStart;
                _tupleEnd = tupleEnd;
            }

            public int Count()
            {
                int i = 0;
                i += _tupleDelimiter != _nullElementMarker ? 1 : 0;
                i += _tupleDelimiter != _escapingSequenceStart ? 1 : 0;
                i += _tupleDelimiter != _tupleStart ? 1 : 0;
                i += _tupleDelimiter != _tupleEnd ? 1 : 0;

                i += _nullElementMarker != _escapingSequenceStart ? 1 : 0;
                i += _nullElementMarker != _tupleStart ? 1 : 0;
                i += _nullElementMarker != _tupleEnd ? 1 : 0;

                i += _escapingSequenceStart != _tupleStart ? 1 : 0;
                i += _escapingSequenceStart != _tupleEnd ? 1 : 0;

                i += _tupleStart != _tupleEnd ? 1 : 0;

                return i;
            }
        }
    }
}
