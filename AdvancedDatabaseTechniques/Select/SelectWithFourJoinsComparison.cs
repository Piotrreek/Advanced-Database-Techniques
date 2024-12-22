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
public class SelectWithFourJoinsComparison
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
        
        _db.Execute("FT.CREATE", "idx:person", "ON", "HASH", "PREFIX", "1", "person:", 
            "SCHEMA", "FirstName", "TEXT", "LastName", "TEXT", "PhoneNumber", "TEXT");
        _db.Execute("FT.CREATE", "idx:address", "ON", "HASH", "PREFIX", "1", "address:", 
            "SCHEMA", "Street", "TEXT", "City", "TEXT", "State", "TEXT", "ZipCode", "TEXT");
        _db.Execute("FT.CREATE", "idx:job", "ON", "HASH", "PREFIX", "1", "job:", 
            "SCHEMA", "JobTitle", "TEXT", "CompanyName", "TEXT", "Salary", "NUMERIC");
        _db.Execute("FT.CREATE", "idx:emergency_contact", "ON", "HASH", "PREFIX", "1", "address:", 
            "SCHEMA", "ContactName", "TEXT", "Relationship", "TEXT", "PhoneNumber", "TEXT", "EmailAddress", "TEXT");
        _db.Execute("FT.CREATE", "idx:social_media", "ON", "HASH", "PREFIX", "1", "job:",
            "SCHEMA", "Platform", "TEXT", "ProfileUrl", "TEXT");
        _db.Execute("FT.CONFIG", "SET", "MAXSEARCHRESULTS", "-1");
        
        _batchInsert = _db.CreateBatch();
        for (var i = 0; i < _people.Count; i++) 
        {
            var person = _people[i];
            
            var personKey = $"person:{i}";
            var personTask = _batchInsert.HashSetAsync(personKey, [
                new HashEntry("FirstName", person.FirstName),
                new HashEntry("LastName", person.LastName),
                new HashEntry("PhoneNumber", person.PhoneNumber),
            ]);
            _insertTasks.Add(personTask);
            
            var emergencyContactKey = $"emergency_contact:{i}";
            var emergencyContactTask = _batchInsert.HashSetAsync(emergencyContactKey, [
                new HashEntry("ContactName", person.EmergencyContact.ContactName),
                new HashEntry("Relationship", person.EmergencyContact.Relationship),
                new HashEntry("PhoneNumber", person.EmergencyContact.PhoneNumber),
                new HashEntry("EmailAddress", person.EmergencyContact.EmailAddress),
                new HashEntry("PersonId", i),
            ]);
            _insertTasks.Add(emergencyContactTask);
            
            var socialMediaKey = $"social_media:{i}";
            var socialMediaTask = _batchInsert.HashSetAsync(socialMediaKey, [
                new HashEntry("Platform", person.SocialMedia.Platform),
                new HashEntry("ProfileUrl", person.SocialMedia.ProfileUrl),
                new HashEntry("PersonId", i),
            ]);
            _insertTasks.Add(socialMediaTask);
            
            var jobKey = $"job:{i}";
            var jobTask = _batchInsert.HashSetAsync(jobKey, [
                new HashEntry("JobTitle", person.Job.JobTitle),
                new HashEntry("CompanyName", person.Job.CompanyName),
                new HashEntry("Salary", (int) person.Job.Salary),
                new HashEntry("PersonId", i),
            ]);
            _insertTasks.Add(jobTask);
            
            var addressKey = $"address:{i}";
            var addressTask = _batchInsert.HashSetAsync(addressKey, [
                new HashEntry("Street", person.Address.Street),
                new HashEntry("City", person.Address.City),
                new HashEntry("State", person.Address.State),
                new HashEntry("ZipCode", person.Address.ZipCode),
                new HashEntry("PersonId", i),
            ]);
            _insertTasks.Add(addressTask);
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
    public void PostgresSelectWithFourJoins()
    {
        _npgsqlConnection.Query(
            "SELECT * FROM person p INNER JOIN address a ON a.person_id = p.id INNER JOIN job j ON j.person_id = p.id INNER JOIN emergency_contact e ON e.person_id = p.id INNER JOIN social_media s ON s.person_id = p.id WHERE p.first_name = 'Laura'");
    }
    
    [Benchmark]
    public void RedisSelectWithFourJoins()
    {
        var result = _db.Execute("FT.SEARCH", "idx:person", "@FirstName:Laura", "LIMIT", "0", _people.Count);
        var persons = (RedisResult[])result;
        for (var i = 1; i < persons.Length; i+=2)
        {
            var key = (string)persons[i];
            var id = key.Split(":")[1];
            _db.Execute("FT.SEARCH", "idx:address", $"@PersonId:{id}");
            _db.Execute("FT.SEARCH", "idx:job", $"@PersonId:{id}");
            _db.Execute("FT.SEARCH", "idx:emergency_contact", $"@PersonId:{id}");
            _db.Execute("FT.SEARCH", "idx:social_media", $"@PersonId:{id}");
        }
    }
}
