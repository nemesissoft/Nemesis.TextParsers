using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

//dotnet run -c Release --framework net472 -- --runtimes net472 net6.0

var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .AddValidator(ExecutionValidator.FailOnError)
    .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage))
    .AddDiagnoser(MemoryDiagnoser.Default)
    .HideColumns("Median", "RatioSD", "Alloc Ratio") //"Error", "StdDev",
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core60))
    .AddJob(Job.Default.WithRuntime(CoreRuntime.Core80))
    ;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);