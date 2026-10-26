using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.Infrastructure.Persistence;

public sealed class ForFitChickenDbContext(DbContextOptions<ForFitChickenDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForFitChickenDbContext).Assembly);
    }
}
