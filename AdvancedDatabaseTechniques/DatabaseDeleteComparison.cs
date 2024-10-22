using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using Testcontainers.PostgreSql;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class DatabaseDeleteComparison
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

    private NpgsqlConnection _npgsqlConnection = default!;
    private List<Person> _people = [];

    [Params(1, 10, 100, 10_000, 100_000, 1_000_000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        _npgsqlConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _npgsqlConnection.Open();

        using var command = new NpgsqlCommand(CreateTableQuery, _npgsqlConnection);
        command.ExecuteNonQuery();

        using var reader =
            new StreamReader(
                $@"{Environment.CurrentDirectory}\..\..\..\..\..\..\..\..\DataGenerator\PeopleData\people-{N}.json");

        _people = JsonSerializer.Deserialize<List<Person>>(reader.ReadToEnd())!
            .Select((x, index) =>
            {
                x.Id = index;

                return x;
            }).ToList();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
        _npgsqlConnection.Close();
        _npgsqlConnection.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people);
    }

    [Benchmark]
    public void DeletePostgreSqlData()
    {
        _npgsqlConnection.Execute(DeleteTableDataQuery);
    }
}