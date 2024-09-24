[Why do we need new library](https://stackoverflow.com/questions/1047218/benchmarking-small-code-samples-in-c-can-this-implementation-be-improved)

[Start](https://benchmarkdotnet.org/) 

[Perfolizer](https://github.com/AndreyAkinshin/perfolizer)

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
4. [Hardware counters](https://github.com/nemesissoft/Nemesis.TextParsers/blob/main/Benchmarks/Collections/CollectionParserBench.cs#L37)
5. [Hardware counters 2](https://benchmarkdotnet.org/articles/samples/IntroHardwareCounters.html)


# Utils
1. [BenchmarkInput.cs](https://github.com/nemesissoft/Nemesis.TextParsers/blob/ef14ecc52ac0275324b70e8e89a32192d6d734a7/Benchmarks/Helpers/BenchmarkInput.cs#L5)

# Samples 
- [StringConcat](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/StringConcatBench.cs#L53)
- [Generator](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/BestPractices/GeneratorBench.cs#L16)
- [Nullable](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/BestPractices/Nullable.cs#L12)
- [Boxing](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/BestPractices/Boxing.cs#L12)
- [BoxingUnsafe](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/BestPractices/BoxingUnsafe.cs#L12)
- [BoundsCheck](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/BestPractices/BoundsCheck.cs#L20)
- [EnumParser](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/EnumBenchmarks/EnumParserBench_CodeGen.cs#L30)
- [Linq](https://github.com/nemesissoft/Nemesis.TextParsers/blob/main/Benchmarks/LinqBench/Linq_WhereAndFirst_Vs_First.cs)
- [MultiKeyDictionary](https://github.com/nemesissoft/Nemesis.TextParsers/blob/b7772ce4fe66381d301ad46213f67c077d35ae89/Benchmarks/Collections/MultiKeyDictionaryBench.cs#L15)
- [Kafka deser](https://michalbrylka.github.io/posts/kafka-protobuf-deserializer/)

# CodeGen
[Compiler explorer](https://godbolt.org/)
```csharp
internal class CodeGen
{
    static bool IsEven(int i) => (i % 2) == 0;
}  
```

```assembly
; mono:
; Program:IsEven(System.Int32):System.Boolean:
       sub      rsp, 8
       mov      [rsp], r15
       mov      r15, rdi
       mov      rcx, r15
       shr      ecx, 1Fh
       mov      rax, r15
       add      eax, ecx
       and      rax, 1
       sub      eax, ecx
       test     eax, eax
       sete     al
       movzx    rax, al
       mov      r15, [rsp]
       add      rsp, 8
       ret

; 6.0
; Program:IsEven(int):bool:
       test     dil, 1
       sete     al
       movzx    rax, al
       ret  

; 8.0
; Program:IsEven(int):bool (FullOpts):
G_M20272_IG01:  ;; offset=0x0000
G_M20272_IG02:  ;; offset=0x0000
       mov      eax, edi
       not      eax
       and      eax, 1
G_M20272_IG03:  ;; offset=0x0007
       ret  
```


# Best practices
1. Lower error == better predictability
2. Low relative error
3. Try replicating [.net improvements benchmarks](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/)
4. [Awesome links](https://github.com/adamsitnik/awesome-dot-net-performance#benchmarking)
