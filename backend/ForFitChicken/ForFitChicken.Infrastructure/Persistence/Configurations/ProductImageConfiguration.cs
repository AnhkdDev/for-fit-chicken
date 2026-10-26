using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", table =>
        {
            table.HasCheckConstraint("CK_ProductImages_SortOrder", "[SortOrder] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.VariantId).HasColumnType("bigint");
        builder.Property(x => x.AssetId).HasColumnType("bigint").IsRequired();
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.AltText).HasColumnType("nvarchar(300)").HasMaxLength(300);
        builder.Property(x => x.SortOrder).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.ProductId, x.VariantId }).HasPrincipalKey(x => new { x.ProductId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.ProductId, x.SortOrder, x.Id });
    }
}
