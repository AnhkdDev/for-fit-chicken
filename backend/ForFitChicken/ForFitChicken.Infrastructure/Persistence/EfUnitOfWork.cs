using System.Transactions;
using ForFitChicken.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForFitChicken.Infrastructure.Persistence;

public sealed class EfUnitOfWork(
    ForFitChickenDbContext dbContext,
    ILogger<EfUnitOfWork> logger) : IUnitOfWork
{
    private bool _transactionRunning;
    private bool _faulted;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureUsable();

        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _faulted = true;
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureUsable();
        cancellationToken.ThrowIfCancellationRequested();

        if (_transactionRunning || dbContext.Database.CurrentTransaction is not null
            || Transaction.Current is not null)
        {
            throw new InvalidOperationException("Nested or ambient transactions are not supported.");
        }

        if (dbContext.Database.CreateExecutionStrategy().RetriesOnFailure)
        {
            throw new InvalidOperationException(
                "This unit of work does not replay operations. Retry the whole use case with a fresh scope and idempotency key instead.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        _transactionRunning = true;

        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            _faulted = true;

            try
            {
                // A cancelled request must not prevent rollback of earlier saves.
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogWarning(rollbackException,
                    "Rollback failed. The transaction outcome must be verified before retrying the use case.");
            }

            dbContext.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            _transactionRunning = false;
        }
    }

    private void EnsureUsable()
    {
        if (_faulted)
        {
            throw new InvalidOperationException(
                "This unit of work failed. Discard the scope and re-read data in a fresh scope before retrying.");
        }
    }
}
