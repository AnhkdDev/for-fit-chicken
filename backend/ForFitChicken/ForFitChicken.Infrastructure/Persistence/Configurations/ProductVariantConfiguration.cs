using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants", table =>
        {
            table.HasCheckConstraint("CK_ProductVariants_SortOrder", "[SortOrder] >= 0");
            table.HasCheckConstraint("CK_ProductVariants_PriceWeight", "[SellingPrice] > 0 AND ([NetWeightGrams] IS NULL OR [NetWeightGrams] > 0) AND ([IsActive] = 0 OR [NetWeightGrams] IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.SkuCode).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Label).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NetWeightGrams).HasColumnType("int");
        builder.Property(x => x.SellingPrice).HasColumnType("decimal(18,0)").IsRequired();
        builder.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(x => x.SortOrder).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasAlternateKey(x => new { x.ProductId, x.Id });
        builder.HasIndex(x => x.SkuCode).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.Label }).IsUnique();
    }
}
