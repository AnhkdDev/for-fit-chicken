using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Source", "[Source] IN ('Website', 'Facebook', 'Zalo', 'Phone', 'Other')");
            table.HasCheckConstraint("CK_Orders_Status", "[Status] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("CK_Orders_PaymentMethod", "[PaymentMethod] IN ('COD')");
            table.HasCheckConstraint("CK_Orders_Subtotal", "[ItemsSubtotal] >= 0");
            table.HasCheckConstraint("CK_Orders_NotSelfReplacement", "[ReplacesOrderId] IS NULL OR [ReplacesOrderId] <> [Id]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasColumnType("varchar(10)").HasMaxLength(10).IsRequired().HasDefaultValue(PaymentMethod.COD).HasSentinel(PaymentMethod.COD);
        builder.Property(x => x.RecipientName).HasColumnType("nvarchar(150)").HasMaxLength(150).IsRequired();
        builder.Property(x => x.RecipientPhone).HasColumnType("varchar(30)").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RecipientAddress).HasColumnType("nvarchar(600)").HasMaxLength(600).IsRequired();
        builder.Property(x => x.CustomerNote).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.InternalNote).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.ItemsSubtotal).HasColumnType("decimal(18,0)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.ConfirmedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfirmedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.HandedOverAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.CancelledAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.CancellationReason).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.Property(x => x.ReplacesOrderId).HasColumnType("bigint");
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.ReplacesOrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
