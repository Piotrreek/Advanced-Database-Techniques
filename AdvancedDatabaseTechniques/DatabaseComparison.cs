using BenchmarkDotNet.Attributes;
using Bogus;
using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class DatabaseComparison
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithUsername("username")
        .WithPassword("password")
        .WithImage("postgres:latest")
        .Build();

    private const string CreateTableQuery = """
                                            
                                                            CREATE TABLE IF NOT EXISTS person (
                                                                id SERIAL PRIMARY KEY,
                                                                first_name VARCHAR(50),
                                                                last_name VARCHAR(50),
                                                                phone_number VARCHAR(50)
                                                            )
                                            """;

    private const string DeleteTableDataQuery = "DELETE FROM person";

    private const string InsertTableDataQuery =
        "INSERT INTO employees (Id, FirstName, LastName, PhoneNumber) VALUES (@Id, @FirstName, @LastName, @PhoneNumber)";


    private NpgsqlConnection _npgsqlConnection = default!;
    private readonly List<Person> _people = [];

    [Params(1, 10, 100, 10_000, 100_000, 1_000_000, 10_000_000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        _npgsqlConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _npgsqlConnection.Open();

        using var command = new NpgsqlCommand(CreateTableQuery, _npgsqlConnection);
        command.ExecuteNonQuery();

        var peopleGenerator = new Faker<Person>()
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName())
            .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber());

        for (var i = 0; i < N; i++)
        {
            var person = peopleGenerator.Generate();
            person.Id = i;
            _people.Add(person);
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var command = new NpgsqlCommand(DeleteTableDataQuery, _npgsqlConnection);
        command.ExecuteNonQuery();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
        _npgsqlConnection.Close();
        _npgsqlConnection.Dispose();
    }

    // Comment this out for large amount of data
    [Benchmark]
    public void AddPostgresqlData()
    {
        _npgsqlConnection.Execute(InsertTableDataQuery, _people);
    }

    [Benchmark]
    public void AddPostgreSqlDataBulkInsert()
    {
        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people);
    }

    // [Benchmark]
    // public void AddRedis()
    // {
    //     Console.Write("fsngsuifngosdfno");
    // }
}