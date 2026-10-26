using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class OrderAllocationConfiguration : IEntityTypeConfiguration<OrderAllocation>
{
    public void Configure(EntityTypeBuilder<OrderAllocation> builder)
    {
        builder.ToTable("OrderAllocations", table =>
        {
            table.HasCheckConstraint("CK_OrderAllocations_State", "[State] IN ('Reserved', 'Issued', 'Released')");
            table.HasCheckConstraint("CK_OrderAllocations_QuantityCost", "[Quantity] > 0 AND ([UnitCostSnapshot] IS NULL OR [UnitCostSnapshot] >= 0)");
            table.HasCheckConstraint("CK_OrderAllocations_StateTimes", "([State] = 'Reserved' AND [IssuedAtUtc] IS NULL AND [ReleasedAtUtc] IS NULL) OR ([State] = 'Issued' AND [IssuedAtUtc] IS NOT NULL AND [ReleasedAtUtc] IS NULL) OR ([State] = 'Released' AND [IssuedAtUtc] IS NULL AND [ReleasedAtUtc] IS NOT NULL AND [ReleaseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleaseReason]))) > 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderItemId).HasColumnType("bigint").IsRequired();
        builder.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.BalanceId).HasColumnType("bigint").IsRequired();
        builder.HasOne<WarehouseBalance>().WithMany().HasForeignKey(x => x.BalanceId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Quantity).HasColumnType("int").IsRequired();
        builder.Property(x => x.State).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.UnitCostSnapshot).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ReservedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.IssuedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ReleasedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ReleaseReason).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.HasIndex(x => new { x.OrderItemId, x.State });
        builder.HasIndex(x => new { x.BalanceId, x.State });
        builder.HasIndex(x => new { x.OrderItemId, x.BalanceId }).IsUnique().HasFilter("[State] = 'Reserved'");
    }
}
