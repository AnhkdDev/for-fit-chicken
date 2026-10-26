using ForFitChicken.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.UnitTest.Integration.Persistence;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("FORFITCHICKEN_RUN_SQL_TESTS") != "1")
        {
            Skip = "Set FORFITCHICKEN_RUN_SQL_TESTS=1 to run isolated SQL Server/LocalDB integration tests.";
        }
    }
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DatabasePrefix = "ForFitChicken_UowTests_";
    private readonly string _databaseName = DatabasePrefix + Guid.NewGuid().ToString("N");
    private bool _created;

    public string ConnectionString { get; }

    public SqlServerFixture()
    {
        var baseConnection = Environment.GetEnvironmentVariable("FORFITCHICKEN_TEST_SQL_CONNECTION")
            ?? @"Server=KDADEPTRAI\KDADEPTRAI;Integrated Security=true;Encrypt=true;TrustServerCertificate=true;Connect Timeout=15";
        var builder = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = _databaseName };
        ConnectionString = builder.ConnectionString;
    }

    public ProbeDbContext Context() => new(new DbContextOptionsBuilder<ForFitChickenDbContext>()
        .UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("FORFITCHICKEN_RUN_SQL_TESTS") != "1")
        {
            return;
        }

        await using var context = Context();
        await context.Database.EnsureCreatedAsync();
        _created = true;
    }

    public async Task DisposeAsync()
    {
        if (!_created)
        {
            return;
        }

        var catalog = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (catalog != _databaseName || !catalog.StartsWith(DatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing cleanup outside this fixture's isolated test database.");
        }

        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }
}

public sealed class ProbeDbContext(DbContextOptions<ForFitChickenDbContext> options)
    : ForFitChickenDbContext(options)
{
    public DbSet<TransactionProbe> Probes => Set<TransactionProbe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TransactionProbe>(entity =>
        {
            entity.ToTable("UnitOfWorkProbe");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Id).ValueGeneratedNever();
            entity.Property(probe => probe.Label).HasMaxLength(100).IsRequired();
            entity.HasIndex(probe => probe.Label).IsUnique();
        });
    }
}

public sealed class TransactionProbe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = Guid.NewGuid().ToString("N");
}
