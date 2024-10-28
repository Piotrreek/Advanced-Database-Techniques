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
public class DatabaseUpdateComparisonRedis
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis/redis-stack:latest")
        .Build();
    
    private List<Person> _people = [];
    private ConnectionMultiplexer _redisConection= default!;
    private IDatabase _db = default!;
    private IBatch _batchInsert = default!;
    private IBatch _batchUpdate = default!;
    private IBatch _batchDelete = default!;
    private readonly List<Task> _insertTasks = [];
    private readonly List<Task> _updateTasks = [];
    private readonly List<Task> _deleteTasks = [];

    [Params(1, 10, 100, 10_000, 100_000, 1_000_000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _redisContainer.StartAsync().GetAwaiter().GetResult();
        _redisConection = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        
        _db = _redisConection.GetDatabase();

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
        _batchUpdate = _db.CreateBatch();
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
            _updateTasks.Add(_batchUpdate.HashSetAsync(key, "FirstName", "Jacek"));
        }
        _batchInsert.Execute();
        Task.WaitAll(_insertTasks.ToArray());
    }
    
    [IterationSetup]
    public void IterationSetup()
    {
        _batchInsert = _db.CreateBatch();
        _batchDelete = _db.CreateBatch();
        _batchUpdate = _db.CreateBatch();
        for (var i = 1; i < _people.Count + 1; i++)
        {
            var key = $"person:{i}";
            var firstName = _people[i - 1].FirstName;
            var lastName = _people[i - 1].LastName;
            var phoneNumber = _people[i - 1].PhoneNumber;
            var task = _db.HashSetAsync(key, [
                new HashEntry("FirstName", firstName),
                new HashEntry("LastName", lastName),
                new HashEntry("PhoneNumber", phoneNumber),
            ]);
            _insertTasks.Add(task);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(key));
            _updateTasks.Add(_batchUpdate.HashSetAsync(key, "FirstName", "Jacek"));
        }
        Task.WaitAll(_insertTasks.ToArray());
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _batchDelete.Execute();
        Task.WaitAll(_deleteTasks.ToArray());
        
        _deleteTasks.Clear();
        _insertTasks.Clear();
        _updateTasks.Clear();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _redisContainer.StopAsync().GetAwaiter().GetResult();
        _redisContainer.DisposeAsync().GetAwaiter().GetResult();
        _redisConection.Close();
        _redisConection.Dispose();
    }
    
    [Benchmark]
    public void SelectRedisData()
    {
        _batchUpdate.Execute();
        Task.WaitAll(_updateTasks.ToArray());
    }
}