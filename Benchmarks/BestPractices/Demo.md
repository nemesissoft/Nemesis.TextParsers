[Why do we need new library](https://stackoverflow.com/questions/1047218/benchmarking-small-code-samples-in-c-can-this-implementation-be-improved)

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
1. [BenchmarkInput.cs](https://github.com/nemesissoft/Nemesis.TextParsers/blob/ef14ecc52ac0275324b70e8e89a32192d6d734a7/Benchmarks/Helpers/BenchmarkInput.cs#L5)

# Samples 
- 
- 
- [Kafka deser](https://michalbrylka.github.io/posts/kafka-protobuf-deserializer/)


# Best practices
1. Lower error == better predictability
2. Low relative error
3. Try replicating [.net improvements benchmarks](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/)
4. [Awesome links](https://github.com/adamsitnik/awesome-dot-net-performance#benchmarking)
