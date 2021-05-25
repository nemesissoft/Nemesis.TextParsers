using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

// ReSharper disable MemberCanBePrivate.Local

// ReSharper disable CommentTypo

namespace Benchmarks
{
    [MemoryDiagnoser]
    [HardwareCounters(HardwareCounter.LlcMisses, HardwareCounter.BranchMispredictions)]

    public class StructureOfArraysBench
    {
        class EmployeeClass
        {
            public byte NumberOfHours { get; }
            public float HourlyPay { get; }
            public float FancyMultiplicator { get; }
            public bool ApplyFactor { get; }
            public float Salary { get; private set; }

            public EmployeeClass(byte numberOfHours, float hourlyPay, float fancyMultiplicator, bool applyFactor)
            {
                NumberOfHours = numberOfHours;
                HourlyPay = hourlyPay;
                FancyMultiplicator = fancyMultiplicator;
                ApplyFactor = applyFactor;
            }

            public void CalculateSalary() => Salary = NumberOfHours * HourlyPay * (ApplyFactor ? FancyMultiplicator : 1.0f);
        }

        struct EmployeeStruct
        {
            public byte NumberOfHours { get; }
            public float HourlyPay { get; }
            public float FancyMultiplicator { get; }
            public bool ApplyFactor { get; }
            public float Salary { get; private set; }

            public EmployeeStruct(byte numberOfHours, float hourlyPay, float fancyMultiplicator, bool applyFactor)
            {
                NumberOfHours = numberOfHours;
                HourlyPay = hourlyPay;
                FancyMultiplicator = fancyMultiplicator;
                ApplyFactor = applyFactor;
                Salary = 0.0f;
            }

            public void CalculateSalary() => Salary = NumberOfHours * HourlyPay * (ApplyFactor ? FancyMultiplicator : 1.0f);
        }

        class Employees
        {
            public int NumberOfEmployees { get; set; }
            public byte[] NumberOfHours { get; }
            public float[] HourlyPay { get; }
            public float[] FancyMultiplicator { get; }
            public bool[] ApplyFactor { get; }
            public float[] Salary { get; }

            public Employees(int numberOfEmployees, byte[] numberOfHours, float[] hourlyPay, float[] fancyMultiplicator, bool[] applyFactor)
            {
                NumberOfEmployees = numberOfEmployees;
                NumberOfHours = numberOfHours;
                HourlyPay = hourlyPay;
                FancyMultiplicator = fancyMultiplicator;
                ApplyFactor = applyFactor;
                Salary = new float[numberOfEmployees];
            }

            public void CalculateSalaries()
            {
                for (int i = 0; i < NumberOfEmployees; i++)
                    Salary[i] = NumberOfHours[i] * HourlyPay[i] * (ApplyFactor[i] ? FancyMultiplicator[i] : 1.0f);
            }
        }

        const int N = 10;

        private static readonly EmployeeClass[] _arrayOfClasses = Enumerable.Range(0, N).Select(
            i => new EmployeeClass((byte)(i % 7 + 160), 50.0f + (i % 8 + 1) * 10, 1.0f + (i % 9) / 10.0f, i < N / 2)
        ).ToArray();

        private static readonly EmployeeStruct[] _arrayOfStructs = Enumerable.Range(0, N).Select(
            i => new EmployeeStruct((byte)(i % 7 + 160), 50.0f + (i % 8 + 1) * 10, 1.0f + (i % 9) / 10.0f, i < N / 2)
        ).ToArray();

        private static readonly Employees _structureOfArrays = new(N,
            Enumerable.Range(0, N).Select(i => (byte)(i % 7 + 160)).ToArray(),
            Enumerable.Range(0, N).Select(i => 50.0f + (i % 8 + 1) * 10).ToArray(),
            Enumerable.Range(0, N).Select(i => 1.0f + (i % 9) / 10.0f).ToArray(),
            Enumerable.Range(0, N).Select(i => i < N / 2).ToArray()
        );


        [Benchmark(Baseline = true)]
        public float ArrayOfClasses()
        {
            foreach (var e in _arrayOfClasses)
                e.CalculateSalary();

            return _arrayOfClasses[0].Salary;
        }

        [Benchmark]
        public float ArrayOfStructs()
        {
            foreach (var e in _arrayOfStructs)
                e.CalculateSalary();

            return _arrayOfStructs[0].Salary;
        }

        [Benchmark]
        public float StructOfArray()
        {
            _structureOfArrays.CalculateSalaries();

            return _structureOfArrays.Salary[0];
        }


    }
}
