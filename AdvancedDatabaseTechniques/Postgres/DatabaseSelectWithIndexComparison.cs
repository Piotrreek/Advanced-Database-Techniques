using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using Testcontainers.PostgreSql;
using Z.Dapper.Plus;

namespace AdvancedDatabaseTechniques.Postgres;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class DatabaseSelectWithIndexComparison
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithUsername("username")
        .WithPassword("password")
        .WithImage("postgres:latest")
        .Build();

    private const string CreateIndexQuery = "CREATE INDEX  idx_person_first_name ON person(first_name)";

    private NpgsqlConnection _npgsqlConnection = default!;
    private List<Person> _people = [];

    [Params(1, 10, 100, 1000)] public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _postgreSqlContainer.StartAsync().GetAwaiter().GetResult();

        _npgsqlConnection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        _npgsqlConnection.Open();

        using var command = new NpgsqlCommand(Queries.CreateTablesQuery, _npgsqlConnection);
        command.ExecuteNonQuery();

        using var createIndexCommand = new NpgsqlCommand(CreateIndexQuery, _npgsqlConnection);
        createIndexCommand.ExecuteNonQuery();

        _people = DataReader.ReadPeople(N);
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
        using var transaction = _npgsqlConnection.BeginTransaction();

        _npgsqlConnection.UseBulkOptions(x => x.InsertKeepIdentity = true)
            .BulkInsert(_people)
            .BulkInsert(_people.Select(x => x.EmergencyContact))
            .BulkInsert(_people.Select(x => x.Address))
            .BulkInsert(_people.Select(x => x.Job))
            .BulkInsert(_people.Select(x => x.SocialMedia));

        transaction.Commit();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var command = new NpgsqlCommand(Queries.TruncateTablesQuery, _npgsqlConnection);
        command.ExecuteNonQuery();
    }

    [Benchmark]
    public void SelectWherePostgreSqlData()
    {
        _npgsqlConnection.Execute(
            "SELECT * FROM person p INNER JOIN address a ON a.person_id = p.id WHERE p.first_name = 'Laura'");
    }

    [Benchmark]
    public void SelectWherePostgreSqlDataWithJoin()
    {
        _npgsqlConnection.Execute("SELECT * FROM person p WHERE p.first_name = 'Laura'");
    }

    [Benchmark]
    public void SelectWherePostgreSqlDataWithTwoJoins()
    {
        _npgsqlConnection.Execute(
            "SELECT * FROM person p INNER JOIN address a ON a.person_id = p.id INNER JOIN job j ON j.person_id = p.id WHERE p.first_name = 'Laura'");
    }

    [Benchmark]
    public void SelectWherePostgreSqlDataWithThreeJoins()
    {
        _npgsqlConnection.Execute(
            "SELECT * FROM person p INNER JOIN address a ON a.person_id = p.id INNER JOIN job j ON j.person_id = p.id INNER JOIN emergency_contact e ON e.person_id = p.id WHERE p.first_name = 'Laura'");
    }

    [Benchmark]
    public void SelectWherePostgreSqlDataWithFourJoins()
    {
        _npgsqlConnection.Execute(
            "SELECT * FROM person p INNER JOIN address a ON a.person_id = p.id INNER JOIN job j ON j.person_id = p.id INNER JOIN emergency_contact e ON e.person_id = p.id INNER JOIN social_media s ON s.person_id = p.id WHERE p.first_name = 'Laura'");
    }
}
