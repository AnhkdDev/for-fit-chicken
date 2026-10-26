using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantities", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [NetWeightGramsSnapshot] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.VariantId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.SkuCodeSnapshot).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductNameSnapshot).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.VariantLabelSnapshot).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NetWeightGramsSnapshot).HasColumnType("int").IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("int").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,0)").IsRequired();
        builder.Property(x => x.LineTotal).HasColumnType("decimal(18,0)").HasComputedColumnSql("CONVERT(decimal(18,0), [UnitPrice] * [Quantity])", stored: true);
        builder.HasIndex(x => new { x.OrderId, x.VariantId }).IsUnique();
    }
}
