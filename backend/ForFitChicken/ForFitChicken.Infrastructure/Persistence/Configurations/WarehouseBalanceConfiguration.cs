using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class WarehouseBalanceConfiguration : IEntityTypeConfiguration<WarehouseBalance>
{
    public void Configure(EntityTypeBuilder<WarehouseBalance> builder)
    {
        builder.ToTable("WarehouseBalances", table =>
        {
            table.HasCheckConstraint("CK_WarehouseBalances_Condition", "[Condition] IN ('Good', 'Quarantine', 'Damaged')");
            table.HasCheckConstraint("CK_WarehouseBalances_Quantities", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand] AND ([Condition] = 'Good' OR [Reserved] = 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationId).HasColumnType("bigint").IsRequired();
        builder.HasOne<StockLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.BatchId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Condition).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.OnHand).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Reserved).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.LocationId, x.BatchId, x.Condition }).IsUnique();
    }
}
