using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class StockDocumentConfiguration : IEntityTypeConfiguration<StockDocument>
{
    public void Configure(EntityTypeBuilder<StockDocument> builder)
    {
        builder.ToTable("StockDocuments", table =>
        {
            table.HasCheckConstraint("CK_StockDocuments_DocumentType", "[DocumentType] IN ('ProductionReceipt', 'GymDelivery', 'GymRecall', 'Reclassify', 'WriteOff', 'Adjustment', 'OrderIssue')");
            table.HasCheckConstraint("CK_StockDocuments_Status", "[Status] IN ('Draft', 'Posted', 'Cancelled')");
            table.HasCheckConstraint("CK_StockDocuments_OrderReference", "([DocumentType] = 'OrderIssue' AND [OrderId] IS NOT NULL) OR ([DocumentType] <> 'OrderIssue' AND [OrderId] IS NULL)");
            table.HasCheckConstraint("CK_StockDocuments_NotSelfCorrection", "[CorrectsDocumentId] IS NULL OR [CorrectsDocumentId] <> [Id]");
            table.HasCheckConstraint("CK_StockDocuments_Posting", "([Status] = 'Posted' AND [PostedAtUtc] IS NOT NULL AND [PostedByUserId] IS NOT NULL) OR ([Status] <> 'Posted' AND [PostedAtUtc] IS NULL AND [PostedByUserId] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNo).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.OperationKey).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<string>().HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.OrderId).HasColumnType("bigint");
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.CorrectsDocumentId).HasColumnType("bigint");
        builder.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.CorrectsDocumentId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.EffectiveAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.PostedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Reason).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.DocumentNo).IsUnique();
        builder.HasIndex(x => x.OperationKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.EffectiveAtUtc });
        builder.HasIndex(x => x.OrderId).IsUnique().HasFilter("[DocumentType] = 'OrderIssue' AND [Status] = 'Posted' AND [OrderId] IS NOT NULL");
    }
}
