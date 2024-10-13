using BenchmarkDotNet.Attributes;

namespace AdvancedDatabaseTechniques;

[MemoryDiagnoser]
[RPlotExporter]
public class DatabaseComparison
{
    private object[] _data;
    
    [Params(1, 10, 100, 1000, 10_000, 100000, 1_000_000, 10_000_000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // TODO: Seed using Bogus with data for specified topic
        _data = new object[N];
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // TODO: Clean database in here, so that it is empty before each benchmark
    }
    
    // TODO: In each benchmark insert data to correct database
    
    [Benchmark]
    public void AddPostgresql()
    {
        Console.Write("adfadfsdfs");
    }

    [Benchmark]
    public void AddRedis()
    {
        Console.Write("fsngsuifngosdfno");
    }
}