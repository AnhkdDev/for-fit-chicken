using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class OrderEventConfiguration : IEntityTypeConfiguration<OrderEvent>
{
    public void Configure(EntityTypeBuilder<OrderEvent> builder)
    {
        builder.ToTable("OrderEvents", table =>
        {
            table.HasCheckConstraint("CK_OrderEvents_FromStatus", "[FromStatus] IS NULL OR [FromStatus] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("CK_OrderEvents_ToStatus", "[ToStatus] IS NULL OR [ToStatus] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
            table.HasCheckConstraint("CK_OrderEvents_Json", "[ChangedFieldsJson] IS NULL OR ISJSON([ChangedFieldsJson]) = 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.EventType).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion<string>().HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.ToStatus).HasConversion<string>().HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.Note).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.ChangedFieldsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.OrderId, x.Id });
    }
}
