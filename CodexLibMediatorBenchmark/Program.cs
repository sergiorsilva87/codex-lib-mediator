using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class MediatorBenchmark
{
    [Benchmark]
    public int SimpleOperation()
    {
        return 123;
    }
}

internal static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<MediatorBenchmark>();
    }
}