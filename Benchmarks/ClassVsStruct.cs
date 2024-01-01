namespace Benchmarks;

/*|             Method |      Mean | Ratio | Allocated |
  |------------------- |----------:|------:|----------:|
  |              Class |  69.36 us |  0.96 |      64 B |
  | ClassFromInterface |  78.04 us |  1.08 |      64 B |
  |             Struct |  70.45 us |  0.97 |      41 B |
  |     StructReadonly |  72.31 us |  1.00 |      41 B |
  |       StructCreate | 198.49 us |  2.74 |      26 B |
  */
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class ClassVsStruct
{
    class ClassContainer
    {
        readonly HelperClass _field = new(',', '∅', '\\', '(', ')', "ABCDEFG");

        public int Count() => _field.Count();
    }

    class ClassFromInterfaceContainer
    {
        readonly ICounter _field = new HelperClassFromInterface(',', '∅', '\\', '(', ')', "ABCDEFG");

        public int Count() => _field.Count();
    }

    class StructContainer
    {
        readonly HelperStruct _field = new(',', '∅', '\\', '(', ')', "ABCDEFG");

        public int Count() => _field.Count();
    }

    class StructReadonlyContainer
    {
        readonly HelperStructReadonly _field = new(',', '∅', '\\', '(', ')', "ABCDEFG");

        public int Count() => _field.Count();
    }

    class StructCreateContainer
    {
        public int Count() => new HelperStruct(',', '∅', '\\', '(', ')', "ABCDEFG").Count();
    }

    private const int ITERATIONS = 10_000;

    [Benchmark]
    public int Class()
    {
        var container = new ClassContainer();

        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
            count += container.Count();

        return count;
    }

    [Benchmark]
    public int ClassFromInterface()
    {
        var container = new ClassFromInterfaceContainer();

        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
            count += container.Count();

        return count;
    }

    [Benchmark]
    public int Struct()
    {
        var container = new StructContainer();

        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
            count += container.Count();

        return count;
    }

    [Benchmark(Baseline = true)]
    public int StructReadonly()
    {
        var container = new StructReadonlyContainer();

        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
            count += container.Count();

        return count;
    }

    [Benchmark]
    public int StructCreate()
    {
        var container = new StructCreateContainer();

        int count = 0;
        for (int i = 0; i < ITERATIONS; i++)
            count += container.Count();

        return count;
    }


    sealed class HelperClass
    {
#pragma warning disable IDE0032 // Use auto property
        private readonly char _tupleDelimiter;
        private readonly char _nullElementMarker;
        private readonly char _escapingSequenceStart;
        private readonly char _tupleStart;
        private readonly char _tupleEnd;
        private readonly string _text;
#pragma warning restore IDE0032 // Use auto property

        public HelperClass(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd, string text)
        {
            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
            _text = text;
        }

        public char TupleDelimiter => _tupleDelimiter;

        public char NullElementMarker => _nullElementMarker;

        public char EscapingSequenceStart => _escapingSequenceStart;

        public char TupleStart => _tupleStart;

        public char TupleEnd => _tupleEnd;

        public string Text => _text;

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

            return i + _text.Length;
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
        private readonly string _text;

        public HelperClassFromInterface(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd, string text)
        {
            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
            _text = text;
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

            return i + _text.Length;
        }
    }

#pragma warning disable IDE0250 // Make struct 'readonly' - not-read only for tests
    struct HelperStruct
#pragma warning restore IDE0250 // Make struct 'readonly'
    {
#pragma warning disable IDE0032 // Use auto property
        private readonly char _tupleDelimiter;
        private readonly char _nullElementMarker;
        private readonly char _escapingSequenceStart;
        private readonly char _tupleStart;
        private readonly char _tupleEnd;
        private readonly string _text;
#pragma warning restore IDE0032 // Use auto property

        public char TupleDelimiter => _tupleDelimiter;
        public char NullElementMarker => _nullElementMarker;
        public char EscapingSequenceStart => _escapingSequenceStart;
        public char TupleStart => _tupleStart;
        public char TupleEnd => _tupleEnd;
        public string Text => _text;

        public HelperStruct(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd, string text)
        {
            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
            _text = text;
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

            return i + _text.Length;
        }
    }

    readonly struct HelperStructReadonly
    {
        private readonly char _tupleDelimiter;
        private readonly char _nullElementMarker;
        private readonly char _escapingSequenceStart;
        private readonly char _tupleStart;
        private readonly char _tupleEnd;
        private readonly string _text;

        public HelperStructReadonly(char tupleDelimiter, char nullElementMarker, char escapingSequenceStart, char tupleStart, char tupleEnd, string text)
        {
            _tupleDelimiter = tupleDelimiter;
            _nullElementMarker = nullElementMarker;
            _escapingSequenceStart = escapingSequenceStart;
            _tupleStart = tupleStart;
            _tupleEnd = tupleEnd;
            _text = text;
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

            return i + _text.Length;
        }
    }
}
