using BenchmarkDotNet.Running;

namespace Benchmarks;

//dotnet run -c Release --framework net472 -- --runtimes net472 net6.0
internal class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
