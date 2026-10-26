using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("ProductBatches", table =>
        {
            table.HasCheckConstraint("CK_ProductBatches_Cost", "[UnitCost] IS NULL OR [UnitCost] >= 0");
            table.HasCheckConstraint("CK_ProductBatches_Blocked", "[IsSaleBlocked] = 0 OR ([BlockReason] IS NOT NULL AND LEN(LTRIM(RTRIM([BlockReason]))) > 0 AND [BlockedAtUtc] IS NOT NULL AND [BlockedByUserId] IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VariantId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.BatchCode).HasColumnType("varchar(60)").HasMaxLength(60).IsRequired();
        builder.Property(x => x.ManufacturedOn).HasColumnType("date").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(x => x.IsSaleBlocked).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.BlockReason).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.Property(x => x.BlockedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.BlockedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.BlockedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Notes).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.BatchCode).IsUnique();
        builder.HasIndex(x => new { x.VariantId, x.ExpiresAtUtc, x.Id });
    }
}
