namespace ForFitChicken.Application.Commerce;

// Query expressions remain provider-neutral; materialization and SQL locking belong to Infrastructure.
public interface ICommerceStore
{
    IQueryable<T> Query<T>() where T : class;
    Task<T[]> ListAsync<T>(IQueryable<T> query, CancellationToken token = default);
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken token = default);
    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken token = default);
    Task<T?> FindAsync<T>(long id, CancellationToken token = default) where T : class;
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task AcquireWriteLockAsync(CancellationToken token);
}

public sealed class BusinessException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
