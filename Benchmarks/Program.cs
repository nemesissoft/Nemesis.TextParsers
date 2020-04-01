using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;

// ReSharper disable CommentTypo

namespace Benchmarks
{
    //dotnet run -c Release --framework net472 -- --runtimes net472 netcoreapp2.2
    internal class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            var d = new Deconstructables { N = 10 };
            d.GlobalSetup();
            d.Standard();
            d.Dedicated();
            d.Convention();
            d.Deconstructable();

#endif
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

}
