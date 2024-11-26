using BenchmarkDotNet.Attributes;
using Dapper;
using DataGenerator;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AdvancedDatabaseTechniques.Postgres;

[MemoryDiagnoser]
[RPlotExporter]
[MaxIterationCount(16)]
[InvocationCount(1)]
public class DatabaseInsertComparison
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithUsername("username")
        .WithPassword("password")
        .WithImage("postgres:latest")
        .Build();

    private NpgsqlConnection _npgsqlConnection = default!;
    private List<Person> _people = [];

    [Params(1, 10, 100)] public int N;

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

    [IterationCleanup]
    public void IterationCleanup()
    {
        _npgsqlConnection.Execute(Queries.TruncateTablesQuery);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _postgreSqlContainer.StopAsync().GetAwaiter().GetResult();
        _postgreSqlContainer.DisposeAsync().GetAwaiter().GetResult();
        _npgsqlConnection.Close();
        _npgsqlConnection.Dispose();
    }

    [Benchmark]
    public void AddPostgresqlData()
    {
        using var transaction = _npgsqlConnection.BeginTransaction();

        _npgsqlConnection.Execute(Queries.InsertPersonDataQuery, _people);
        _npgsqlConnection.Execute(Queries.InsertEmergencyContactDataQuery, _people.Select(x => x.EmergencyContact));
        _npgsqlConnection.Execute(Queries.InsertSocialMediaDataQuery, _people.Select(x => x.SocialMedia));
        _npgsqlConnection.Execute(Queries.InsertJobDataQuery, _people.Select(x => x.Job));
        _npgsqlConnection.Execute(Queries.InsertAddressDataQuery, _people.Select(x => x.Address));

        transaction.Commit();
    }
}
