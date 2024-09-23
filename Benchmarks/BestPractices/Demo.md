[Start](https://benchmarkdotnet.org/) 
[Getting Started Guide](https://benchmarkdotnet.org/articles/guides/getting-started.html) 
[Main Features List](https://benchmarkdotnet.org/#main-features)



# Running 
[Config](https://benchmarkdotnet.org/articles/configs/configs.html)

```csharp
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
```


# Results 
1. [Exporters](https://benchmarkdotnet.org/articles/configs/exporters.html)
2. [Charting](https://chartbenchmark.net/)
3. [Plot for blogs](https://michalbrylka.github.io/posts/generic-math-matrix/#performance)


# Utils
1. [BenchmarkInput.cs](https://github.com/nemesissoft/Nemesis.TextParsers/blob/f55f176088193de995ebb59f92420ae419d0dd36/Benchmarks/Helpers/BenchmarkInput.cs#L5)
2. 
