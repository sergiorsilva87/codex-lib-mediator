```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Core i5-4590 CPU 3.30GHz (Max: 3,530.05GHz) (Haswell), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                              | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SendAsync                           | 188.89 ns |  3.652 ns |  3.416 ns | 189.31 ns |  1.00 |    0.02 | 0.0968 |     304 B |        1.00 |
| SendAsyncWithOneBehavior            | 280.71 ns |  5.599 ns |  9.507 ns | 277.16 ns |  1.49 |    0.06 | 0.2012 |     632 B |        2.08 |
| SendAsyncWithTwoBehaviors           | 401.08 ns |  6.312 ns |  5.904 ns | 400.49 ns |  2.12 |    0.05 | 0.2699 |     848 B |        2.79 |
| SendAsyncWithFiveBehaviors          | 621.29 ns | 12.196 ns | 13.556 ns | 618.97 ns |  3.29 |    0.09 | 0.4768 |    1496 B |        4.92 |
| GetOneBehavior                      |  25.75 ns |  0.340 ns |  0.318 ns |  25.82 ns |  0.14 |    0.00 | 0.0179 |      56 B |        0.18 |
| GetOneBehaviorToArray               |  61.01 ns |  1.053 ns |  0.879 ns |  60.98 ns |  0.32 |    0.01 | 0.0280 |      88 B |        0.29 |
| BuildPipelineOneBehavior            |  40.51 ns |  0.820 ns |  0.727 ns |  40.51 ns |  0.21 |    0.01 | 0.0408 |     128 B |        0.42 |
| BuildPipelineFiveBehaviors          | 206.55 ns |  3.458 ns |  5.872 ns | 206.49 ns |  1.09 |    0.04 | 0.2446 |     768 B |        2.53 |
| ResolveInvoker                      |  19.16 ns |  0.412 ns |  0.365 ns |  19.08 ns |  0.10 |    0.00 | 0.0076 |      24 B |        0.08 |
| MakeGenericInvokerType              | 385.34 ns |  2.976 ns |  2.638 ns | 384.91 ns |  2.04 |    0.04 | 0.0505 |     160 B |        0.53 |
| GetNoBehavior                       |  14.92 ns |  0.113 ns |  0.106 ns |  14.97 ns |  0.08 |    0.00 |      - |         - |        0.00 |
| GetNoBehaviorToArray                |  30.32 ns |  0.299 ns |  0.250 ns |  30.31 ns |  0.16 |    0.00 |      - |         - |        0.00 |
| ResolveNoBehaviorHandler            |  19.77 ns |  0.468 ns |  0.415 ns |  19.87 ns |  0.10 |    0.00 | 0.0076 |      24 B |        0.08 |
| DirectHandler                       |  68.21 ns |  6.666 ns | 19.446 ns |  57.61 ns |  0.36 |    0.10 | 0.0535 |     168 B |        0.55 |
| InvokePipelineDirectly              | 111.75 ns |  2.022 ns |  1.891 ns | 112.37 ns |  0.59 |    0.01 | 0.0918 |     288 B |        0.95 |
| DirectHandlerAsObject               |  57.18 ns |  0.786 ns |  0.697 ns |  56.83 ns |  0.30 |    0.01 | 0.0612 |     192 B |        0.63 |
| DirectHandlerTask                   |  29.15 ns |  0.382 ns |  0.339 ns |  29.04 ns |  0.15 |    0.00 | 0.0306 |      96 B |        0.32 |
| ExecuteCachedOneBehaviorPipeline    |  53.29 ns |  0.713 ns |  0.632 ns |  53.35 ns |  0.28 |    0.01 | 0.0535 |     168 B |        0.55 |
| ExecuteCachedFiveBehaviorPipeline   | 147.69 ns |  3.003 ns |  7.912 ns | 144.56 ns |  0.78 |    0.04 | 0.1452 |     456 B |        1.50 |
| ExecuteReusableOneBehaviorPipeline  |  73.86 ns |  1.207 ns |  1.070 ns |  73.24 ns |  0.39 |    0.01 | 0.0867 |     272 B |        0.89 |
| ExecuteReusableFiveBehaviorPipeline | 327.97 ns | 20.678 ns | 58.660 ns | 307.05 ns |  1.74 |    0.31 | 0.3443 |    1080 B |        3.55 |
