using AdvancedDatabaseTechniques.Postgres;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques.Update;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class UpdateComparison
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithUsername("username")
        .WithPassword("password")
        .WithImage("postgres:latest")
        .Build();
    
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis/redis-stack:latest")
        .Build();

    private NpgsqlConnection _npgsqlConnection = default!;
    
    private ConnectionMultiplexer _redisConnection= default!;
    private IDatabase _db = default!;
    private IBatch _batchInsert = default!;
    private IBatch _batchUpdate = default!;
    private IBatch _batchDelete = default!;
    private readonly List<Task> _insertTasks = [];
    private readonly List<Task> _updateTasks = [];
    private readonly List<Task> _deleteTasks = [];
    
    private List<Person> _people = [];

    [Params(1, 10, 100, 1000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // postgres
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
        _npgsqlConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _npgsqlConnection.Open();
        using var command = new NpgsqlCommand(Queries.CreateTablesQuery, _npgsqlConnection);
        command.ExecuteNonQuery();
        
        // redis
        _redisContainer.StartAsync().GetAwaiter().GetResult();
        _redisConnection = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        _db = _redisConnection.GetDatabase();

        _people = DataReader.ReadPeople(N);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // postgres
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
        _npgsqlConnection.Close();
        _npgsqlConnection.Dispose();
        
        // redis
        _redisContainer.StopAsync().GetAwaiter().GetResult();
        _redisContainer.DisposeAsync().GetAwaiter().GetResult();
        _redisConnection.Close();
        _redisConnection.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // postgres
        using var transaction = _npgsqlConnection.BeginTransaction();
        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people)
            .BulkInsert(_people.Select(x => x.EmergencyContact))
            .BulkInsert(_people.Select(x => x.Address))
            .BulkInsert(_people.Select(x => x.Job))
            .BulkInsert(_people.Select(x => x.SocialMedia));
        transaction.Commit();
        
        // redis
        _batchInsert = _db.CreateBatch();
        _batchDelete = _db.CreateBatch();
        _batchUpdate = _db.CreateBatch();
        for (var i = 1; i < _people.Count + 1; i++)
        {
            var key = $"person:{i}";
            var firstName = _people[i - 1].FirstName;
            var lastName = _people[i - 1].LastName;
            var phoneNumber = _people[i - 1].PhoneNumber;
            var task = _batchInsert.HashSetAsync(key, [
                new HashEntry("FirstName", firstName),
                new HashEntry("LastName", lastName),
                new HashEntry("PhoneNumber", phoneNumber),
            ]);
            _insertTasks.Add(task);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(key));
            _updateTasks.Add(_batchUpdate.HashSetAsync(key, "FirstName", "Jacek"));
        }
        _batchInsert.Execute();
        Task.WaitAll(_insertTasks.ToArray());
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // postgres
        _npgsqlConnection.Execute(Queries.TruncateTablesQuery);
        
        // redis
        _batchDelete.Execute();
        Task.WaitAll(_deleteTasks.ToArray());
        _deleteTasks.Clear();
        _insertTasks.Clear();
        _updateTasks.Clear();
    }

    [Benchmark]
    public void PostgresqlUpdateData()
    {
        _npgsqlConnection.Execute("UPDATE person SET first_name = 'Jacek'");
    }
    
        
    [Benchmark]
    public void RedisUpdateData()
    {
        _batchUpdate.Execute();
        Task.WaitAll(_updateTasks.ToArray());
    }
}
