using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Infrastructure;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ForFitChicken.UnitTest.Integration.Persistence;

[Trait("Category", "SqlServerIntegration")]
public sealed class EfUnitOfWorkTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    private static EfUnitOfWork UnitOfWork(ForFitChickenDbContext context) =>
        new(context, NullLogger<EfUnitOfWork>.Instance);

    [SqlServerFact]
    public async Task SuccessfulOperation_CommitsIntermediateAndFinalTrackedWrites()
    {
        var first = new TransactionProbe();
        var second = new TransactionProbe();
        await using var context = fixture.Context();
        var unit = UnitOfWork(context);

        var result = await unit.ExecuteInTransactionAsync(async cancellationToken =>
        {
            context.Probes.Add(first);
            await unit.SaveChangesAsync(cancellationToken);
            context.Probes.Add(second);
            return 42;
        });

        await using var verification = fixture.Context();
        Assert.Equal(42, result);
        Assert.Equal(2, await verification.Probes.CountAsync(probe => probe.Id == first.Id || probe.Id == second.Id));
        Assert.Null(context.Database.CurrentTransaction);
    }

    [SqlServerFact]
    public async Task ExceptionAfterSave_RollsBackEarlierSqlWrites_AndInvalidatesScope()
    {
        var probe = new TransactionProbe();
        await using var context = fixture.Context();
        var unit = UnitOfWork(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unit.ExecuteInTransactionAsync<int>(async token =>
        {
            context.Probes.Add(probe);
            await unit.SaveChangesAsync(token);
            throw new InvalidOperationException("Simulated later step failure.");
        }));

        await using var verification = fixture.Context();
        Assert.False(await verification.Probes.AnyAsync(row => row.Id == probe.Id));
        Assert.Empty(context.ChangeTracker.Entries());
        await Assert.ThrowsAsync<InvalidOperationException>(() => unit.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task DatabaseConstraintFailure_RollsBackEarlierSave()
    {
        var first = new TransactionProbe();
        var second = new TransactionProbe { Label = first.Label };
        await using var context = fixture.Context();
        var unit = UnitOfWork(context);

        await Assert.ThrowsAsync<DbUpdateException>(() => unit.ExecuteInTransactionAsync(async token =>
        {
            context.Probes.Add(first);
            await unit.SaveChangesAsync(token);
            context.Probes.Add(second); // SQL unique index fails on final SaveChanges.
            return true;
        }));

        await using var verification = fixture.Context();
        Assert.False(await verification.Probes.AnyAsync(probe => probe.Label == first.Label));
    }

    [SqlServerFact]
    public async Task CancellationAfterSave_StillRollsBackUsingAnUncancelledToken()
    {
        var probe = new TransactionProbe();
        using var cancellation = new CancellationTokenSource();
        await using var context = fixture.Context();
        var unit = UnitOfWork(context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unit.ExecuteInTransactionAsync(async token =>
        {
            context.Probes.Add(probe);
            await unit.SaveChangesAsync(token);
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return true;
        }, cancellation.Token));

        await using var verification = fixture.Context();
        Assert.False(await verification.Probes.AnyAsync(row => row.Id == probe.Id));
    }

    [SqlServerFact]
    public async Task NestedUnitOfWork_DoesNotCommitInnerWork()
    {
        var probe = new TransactionProbe();
        await using var context = fixture.Context();
        var unit = UnitOfWork(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unit.ExecuteInTransactionAsync(async token =>
        {
            context.Probes.Add(probe);
            await unit.SaveChangesAsync(token);
            return await unit.ExecuteInTransactionAsync(_ => Task.FromResult(true), token);
        }));

        await using var verification = fixture.Context();
        Assert.False(await verification.Probes.AnyAsync(row => row.Id == probe.Id));
    }

    [SqlServerFact]
    public async Task DependencyInjection_UsesTheSameScopedContextAndTransaction()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var context = scope.ServiceProvider.GetRequiredService<ForFitChickenDbContext>();

        Assert.Same(unit, scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        await unit.ExecuteInTransactionAsync(_ =>
        {
            Assert.NotNull(context.Database.CurrentTransaction);
            return Task.FromResult(true);
        });

        await using var secondScope = provider.CreateAsyncScope();
        Assert.NotSame(unit, secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.NotSame(context, secondScope.ServiceProvider.GetRequiredService<ForFitChickenDbContext>());
    }
}
