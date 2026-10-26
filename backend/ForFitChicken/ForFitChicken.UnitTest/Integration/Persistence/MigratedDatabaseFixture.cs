using ForFitChicken.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.UnitTest.Integration.Persistence;

public sealed class MigratedDatabaseFixture : IAsyncLifetime
{
    private const string Prefix = "ForFitChicken_SchemaTests_";
    private readonly string _databaseName = Prefix + Guid.NewGuid().ToString("N");
    private bool _created;
    public string ConnectionString { get; }

    public MigratedDatabaseFixture()
    {
        var connection = Environment.GetEnvironmentVariable("FORFITCHICKEN_TEST_SQL_CONNECTION")
            ?? @"Server=KDADEPTRAI\KDADEPTRAI;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=15";
        ConnectionString = new SqlConnectionStringBuilder(connection) { InitialCatalog = _databaseName }.ConnectionString;
    }

    public ForFitChickenDbContext Context() => new(new DbContextOptionsBuilder<ForFitChickenDbContext>()
        .UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("FORFITCHICKEN_RUN_SQL_TESTS") != "1") return;
        await using var context = Context();
        // Mark before migration so a partially failed migration is also cleaned up.
        _created = true;
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_created) return;
        var catalog = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (catalog != _databaseName || !catalog.StartsWith(Prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside this fixture's isolated database.");
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }
}
