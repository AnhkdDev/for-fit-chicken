namespace ForFitChicken.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs database-only work in one transaction, saves remaining tracked changes,
    /// and commits. Intermediate SaveChanges calls are allowed. No automatic retries.
    /// On failure, discard this scope; use a fresh scope to re-read and retry.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
