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
public class SelectWithBetweenComparison
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
        
        _db.Execute("FT.CREATE", "idx:job", "ON", "HASH", "PREFIX", "1", "job:", 
            "SCHEMA", "JobTitle", "TEXT", "CompanyName", "TEXT", "Salary", "NUMERIC");
        _db.Execute("FT.CONFIG", "SET", "MAXSEARCHRESULTS", "-1");
        
        _batchInsert = _db.CreateBatch();
        for (var i = 0; i < _people.Count; i++) 
        {
            var person = _people[i];
            var jobKey = $"job:{i}";
            var jobTask = _batchInsert.HashSetAsync(jobKey, [
                new HashEntry("JobTitle", person.Job.JobTitle),
                new HashEntry("CompanyName", person.Job.CompanyName),
                new HashEntry("Salary", (int) person.Job.Salary),
            ]);
            _insertTasks.Add(jobTask);
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

    [Benchmark]
    public void PostgresSelectWithBetween()
    {
        _npgsqlConnection.Query(
            "SELECT * FROM job WHERE salary BETWEEN 100 AND 300");
    }
    
    [Benchmark]
    public void RedisSelectWithBetween()
    {
        _db.Execute("FT.SEARCH", "idx:job", "@salary:[100 300]", "LIMIT", "0", _people.Count);
    }
}
