using AdvancedDatabaseTechniques.Postgres;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques.Insert;

public class BulkInsertComparison
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
    private IBatch _batchDelete = default!;
    private List<Task> _insertTasks = [];
    private List<Task> _deleteTasks = [];
    
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
        // redis
        _batchInsert = _db.CreateBatch();
        _batchDelete = _db.CreateBatch();
        for (var i = 1; i < _people.Count + 1; i++)
        {
            var person = _people[i - 1];
            
            var personKey = $"person:{i}";
            var personTask = _batchInsert.HashSetAsync(personKey, [
                new HashEntry("FirstName", person.FirstName),
                new HashEntry("LastName", person.LastName),
                new HashEntry("PhoneNumber", person.PhoneNumber),
            ]);
            _insertTasks.Add(personTask);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(personKey));
            
            var emergencyContactKey = $"emergency_contact:{i}";
            var emergencyContactTask = _batchInsert.HashSetAsync(emergencyContactKey, [
                new HashEntry("ContactName", person.EmergencyContact.ContactName),
                new HashEntry("Relationship", person.EmergencyContact.Relationship),
                new HashEntry("PhoneNumber", person.EmergencyContact.PhoneNumber),
                new HashEntry("EmailAddress", person.EmergencyContact.EmailAddress),
            ]);
            _insertTasks.Add(emergencyContactTask);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(emergencyContactKey));
            
            var socialMediaKey = $"social_media:{i}";
            var socialMediaTask = _batchInsert.HashSetAsync(socialMediaKey, [
                new HashEntry("Platform", person.SocialMedia.Platform),
                new HashEntry("ProfileUrl", person.SocialMedia.ProfileUrl),
            ]);
            _insertTasks.Add(socialMediaTask);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(socialMediaKey));
            
            var jobKey = $"job:{i}";
            var jobTask = _batchInsert.HashSetAsync(jobKey, [
                new HashEntry("JobTitle", person.Job.JobTitle),
                new HashEntry("CompanyName", person.Job.CompanyName),
                new HashEntry("Salary", (int) person.Job.Salary),
            ]);
            _insertTasks.Add(jobTask);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(jobKey));
            
            var addressKey = $"address:{i}";
            var addressTask = _batchInsert.HashSetAsync(addressKey, [
                new HashEntry("Street", person.Address.Street),
                new HashEntry("City", person.Address.City),
                new HashEntry("State", person.Address.State),
                new HashEntry("ZipCode", person.Address.ZipCode),
            ]);
            _insertTasks.Add(addressTask);
            _deleteTasks.Add(_batchDelete.KeyDeleteAsync(addressKey));
        }
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
    }

    [Benchmark]
    public void PostgresqlBulkInsertData()
    {
        using var transaction = _npgsqlConnection.BeginTransaction();

        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people)
            .BulkInsert(_people.Select(x => x.EmergencyContact))
            .BulkInsert(_people.Select(x => x.Address))
            .BulkInsert(_people.Select(x => x.Job))
            .BulkInsert(_people.Select(x => x.SocialMedia));

        transaction.Commit();
    }
    
    [Benchmark]
    public void RedisInsertData()
    {
        _batchInsert.Execute();
        Task.WaitAll(_insertTasks.ToArray());
    }
}