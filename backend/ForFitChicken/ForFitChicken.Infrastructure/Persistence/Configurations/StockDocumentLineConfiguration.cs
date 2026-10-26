using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class StockDocumentLineConfiguration : IEntityTypeConfiguration<StockDocumentLine>
{
    public void Configure(EntityTypeBuilder<StockDocumentLine> builder)
    {
        builder.ToTable("StockDocumentLines", table =>
        {
            table.HasCheckConstraint("CK_StockDocumentLines_FromCondition", "[FromCondition] IS NULL OR [FromCondition] IN ('Good', 'Quarantine', 'Damaged')");
            table.HasCheckConstraint("CK_StockDocumentLines_ToCondition", "[ToCondition] IS NULL OR [ToCondition] IN ('Good', 'Quarantine', 'Damaged')");
            table.HasCheckConstraint("CK_StockDocumentLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_StockDocumentLines_Endpoints", "([FromLocationId] IS NOT NULL OR [ToLocationId] IS NOT NULL) AND (([FromLocationId] IS NULL AND [FromCondition] IS NULL) OR ([FromLocationId] IS NOT NULL AND [FromCondition] IS NOT NULL)) AND (([ToLocationId] IS NULL AND [ToCondition] IS NULL) OR ([ToLocationId] IS NOT NULL AND [ToCondition] IS NOT NULL)) AND ([FromLocationId] IS NULL OR [ToLocationId] IS NULL OR [FromLocationId] <> [ToLocationId] OR [FromCondition] <> [ToCondition])");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentId).HasColumnType("bigint").IsRequired();
        builder.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.BatchId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Quantity).HasColumnType("int").IsRequired();
        builder.Property(x => x.FromLocationId).HasColumnType("bigint");
        builder.HasOne<StockLocation>().WithMany().HasForeignKey(x => x.FromLocationId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.FromCondition).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20);
        builder.Property(x => x.ToLocationId).HasColumnType("bigint");
        builder.HasOne<StockLocation>().WithMany().HasForeignKey(x => x.ToLocationId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.ToCondition).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20);
        builder.Property(x => x.Note).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.HasIndex(x => new { x.DocumentId, x.Id });
        builder.HasIndex(x => new { x.FromLocationId, x.BatchId });
        builder.HasIndex(x => new { x.ToLocationId, x.BatchId });
    }
}
