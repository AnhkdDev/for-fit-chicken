using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class CodReceiptConfiguration : IEntityTypeConfiguration<CodReceipt>
{
    public void Configure(EntityTypeBuilder<CodReceipt> builder)
    {
        builder.ToTable("CodReceipts", table =>
        {
            table.HasCheckConstraint("CK_CodReceipts_Amount", "[Amount] > 0");
            table.HasCheckConstraint("CK_CodReceipts_Void", "([VoidedAtUtc] IS NULL AND [VoidedByUserId] IS NULL AND [VoidReason] IS NULL) OR ([VoidedAtUtc] IS NOT NULL AND [VoidedByUserId] IS NOT NULL AND [VoidReason] IS NOT NULL AND LEN(LTRIM(RTRIM([VoidReason]))) > 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)").IsRequired();
        builder.Property(x => x.ReceivedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.RecordedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.RecordedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Reference).HasColumnType("nvarchar(200)").HasMaxLength(200);
        builder.Property(x => x.Note).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.Property(x => x.VoidedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.VoidedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.VoidReason).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.HasIndex(x => x.OrderId).IsUnique().HasFilter("[VoidedAtUtc] IS NULL");
    }
}
