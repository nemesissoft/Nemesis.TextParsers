using BenchmarkDotNet.Running;

//dotnet run -c Release --framework net472 -- --runtimes net472 net6.0

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
