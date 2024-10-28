using System.Text.Json;
using BenchmarkDotNet.Attributes;
using DataGenerator;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace AdvancedDatabaseTechniques.Redis;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class DatabaseSelectComparisonRedis
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis/redis-stack:latest")
        .Build();
    
    private List<Person> _people = [];
    private ConnectionMultiplexer _redisConnection= default!;
    private IDatabase _db = default!;
    private IBatch _batchInsert = default!;
    private IBatch _batchSelect = default!;
    private IBatch _batchSelectField = default!;
    private readonly List<Task> _insertTasks = [];
    private readonly List<Task> _selectTasks = [];
    private readonly List<Task> _selectFieldTasks = [];

    [Params(1, 10, 100, 10_000, 100_000, 1_000_000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _redisContainer.StartAsync().GetAwaiter().GetResult();
        _redisConnection = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        
        _db = _redisConnection.GetDatabase();

        using var reader =
            new StreamReader(
                $@"{Environment.CurrentDirectory}/../../../../../../../../DataGenerator/PeopleData/people-{N}.json");

        _people = JsonSerializer.Deserialize<List<Person>>(reader.ReadToEnd())!
            .Select((x, index) =>
            {
                x.Id = index;
                return x;
            }).ToList();
        
        _batchInsert = _db.CreateBatch();
        for (var i = 0; i < _people.Count; i++)
        {
            var key = $"person:{i}";
            var firstName = _people[i].FirstName;
            var lastName = _people[i].LastName;
            var phoneNumber = _people[i].PhoneNumber;
            var task = _batchInsert.HashSetAsync(key, [
                new HashEntry("FirstName", firstName),
                new HashEntry("LastName", lastName),
                new HashEntry("PhoneNumber", phoneNumber),
            ]);
            _insertTasks.Add(task);
        }
        _batchInsert.Execute();
        Task.WaitAll(_insertTasks.ToArray());
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        _selectTasks.Clear();
        _selectFieldTasks.Clear();
        _batchSelect = _db.CreateBatch();
        _batchSelectField = _db.CreateBatch();
        for (var i = 0; i < _people.Count; i++)
        {
            var key = $"person:{i}";
            _selectTasks.Add(_batchSelect.HashGetAllAsync(key));
            _selectTasks.Add(_batchSelect.HashGetAsync(key, "FirstName"));
        }
    }
    

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _redisContainer.StopAsync().GetAwaiter().GetResult();
        _redisContainer.DisposeAsync().GetAwaiter().GetResult();
        _redisConnection.Close();
        _redisConnection.Dispose();
    }
    
    [Benchmark]
    public void SelectRedisData()
    {
        _batchSelect.Execute();
        Task.WaitAll(_selectTasks.ToArray());
    }
    
    [Benchmark]
    public void SelectOneFieldRedisData()
    {
        _batchSelectField.Execute();
        Task.WaitAll(_selectFieldTasks.ToArray());
    }
}