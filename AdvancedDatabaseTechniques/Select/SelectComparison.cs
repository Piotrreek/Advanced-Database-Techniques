using AdvancedDatabaseTechniques.Postgres;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques.Select;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class SelectComparison
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
    private List<Person> _people = [];
    
    private ConnectionMultiplexer _redisConnection= default!;
    private IDatabase _db = default!;
    private IBatch _batchInsert = default!;
    private IBatch _batchSelect = default!;
    private readonly List<Task> _insertTasks = [];
    private readonly List<Task> _selectTasks = [];

    [Params(1, 10, 100, 1000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _people = DataReader.ReadPeople(N);
        
        // postgres
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
        _npgsqlConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _npgsqlConnection.Open();
        using var command = new NpgsqlCommand(Queries.CreateTablesQuery, _npgsqlConnection);
        command.ExecuteNonQuery();

        using var transaction = _npgsqlConnection.BeginTransaction();
        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people)
            .BulkInsert(_people.Select(x => x.EmergencyContact))
            .BulkInsert(_people.Select(x => x.Address))
            .BulkInsert(_people.Select(x => x.Job))
            .BulkInsert(_people.Select(x => x.SocialMedia));
        transaction.Commit();
        
        // redis
        _redisContainer.StartAsync().GetAwaiter().GetResult();
        _redisConnection = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        _db = _redisConnection.GetDatabase();
        
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
        _selectTasks.Clear();
        _batchSelect = _db.CreateBatch();
        for (var i = 0; i < _people.Count; i++)
        {
            var key = $"person:{i}";
            _selectTasks.Add(_batchSelect.HashGetAllAsync(key));
        }
    }

    [Benchmark]
    public void PostgresSelect()
    {
        _npgsqlConnection.Query("SELECT * FROM person");
    }
    
        
    [Benchmark]
    public void RedisSelect()
    {
        _batchSelect.Execute();
        Task.WaitAll(_selectTasks.ToArray());
    }
}