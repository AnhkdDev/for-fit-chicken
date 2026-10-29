using ForFitChicken.Domain.Entities;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ForFitChicken.Infrastructure.Persistence;

public class ForFitChickenDbContext(DbContextOptions<ForFitChickenDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, long>(options)
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaStorageState> MediaStorageStates => Set<MediaStorageState>();
    public DbSet<MediaObjectReservation> MediaObjectReservations => Set<MediaObjectReservation>();
    public DbSet<MediaUsageCounter> MediaUsageCounters => Set<MediaUsageCounter>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<WarehouseBalance> WarehouseBalances => Set<WarehouseBalance>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAllocation> OrderAllocations => Set<OrderAllocation>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<CodReceipt> CodReceipts => Set<CodReceipt>();
    public DbSet<StockDocument> StockDocuments => Set<StockDocument>();
    public DbSet<StockDocumentLine> StockDocumentLines => Set<StockDocumentLine>();
    public DbSet<GymStockCount> GymStockCounts => Set<GymStockCount>();
    public DbSet<GymStockCountLine> GymStockCountLines => Set<GymStockCountLine>();
    public DbSet<GymVarianceResolution> GymVarianceResolutions => Set<GymVarianceResolution>();
    public DbSet<SiteContent> SiteContent => Set<SiteContent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();
    public DbSet<NotificationJob> NotificationJobs => Set<NotificationJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForFitChickenDbContext).Assembly);
        var utc = new ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetProperties()))
        {
            if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)) property.SetValueConverter(utc);
        }
        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
        });
        modelBuilder.Entity<ApplicationRole>().HasData(
            new ApplicationRole { Id = 1, Name = "Owner", NormalizedName = "OWNER", ConcurrencyStamp = "role-owner-v1" },
            new ApplicationRole { Id = 2, Name = "Operator", NormalizedName = "OPERATOR", ConcurrencyStamp = "role-operator-v1" });

        // History and internal accounts must not disappear through cascading deletes.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}
