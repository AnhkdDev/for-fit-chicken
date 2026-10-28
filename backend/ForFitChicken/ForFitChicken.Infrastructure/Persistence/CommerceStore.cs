using ForFitChicken.Application.Commerce;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.Infrastructure.Persistence;

public sealed class CommerceStore(ForFitChickenDbContext db) : ICommerceStore
{
    public IQueryable<T> Query<T>() where T : class => db.Set<T>();
    public Task<T[]> ListAsync<T>(IQueryable<T> query, CancellationToken token = default) => query.ToArrayAsync(token);
    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken token = default) => query.CountAsync(token);
    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken token = default) => query.AnyAsync(token);
    public async Task<T?> FindAsync<T>(long id, CancellationToken token = default) where T : class => await db.Set<T>().FindAsync([id], token);
    public void Add<T>(T entity) where T : class => db.Add(entity);
    public void Remove<T>(T entity) where T : class => db.Remove(entity);
    public Task AcquireWriteLockAsync(CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Business write lock requires an active transaction.");
        return db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'ForFitChicken.BusinessWrite.v1',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51001, 'Business write lock unavailable. Retry with a fresh request scope.', 1;
            """, token);
    }
}
