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
public class DatabaseDeleteComparison
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithUsername("username")
        .WithPassword("password")
        .WithImage("postgres:latest")
        .Build();

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

    [Benchmark]
    public void DeletePostgreSqlData()
    {
        _npgsqlConnection.Execute(Queries.DeleteTablesDataQuery);
    }

    [Benchmark]
    public void TruncatePostgreSqlData()
    {
        _npgsqlConnection.Execute(Queries.TruncateTablesQuery);
    }
}
