using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class NotificationJobConfiguration : IEntityTypeConfiguration<NotificationJob>
{
    public void Configure(EntityTypeBuilder<NotificationJob> builder)
    {
        builder.ToTable("NotificationJobs", table =>
        {
            table.HasCheckConstraint("CK_NotificationJobs_EventType", "[EventType] IN ('OrderRequested')");
            table.HasCheckConstraint("CK_NotificationJobs_Channel", "[Channel] IN ('Telegram')");
            table.HasCheckConstraint("CK_NotificationJobs_Status", "[Status] IN ('Pending', 'Processing', 'Sent', 'Failed')");
            table.HasCheckConstraint("CK_NotificationJobs_Payload", "ISJSON([PayloadJson]) = 1 AND [AttemptCount] >= 0");
            table.HasCheckConstraint("CK_NotificationJobs_Lease", "([Status] = 'Processing' AND [LeaseToken] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR ([Status] <> 'Processing' AND [LeaseToken] IS NULL AND [LeaseUntilUtc] IS NULL)");
            table.HasCheckConstraint("CK_NotificationJobs_Sent", "([Status] = 'Sent' AND [SentAtUtc] IS NOT NULL) OR ([Status] <> 'Sent' AND [SentAtUtc] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.EventType).HasConversion<string>().HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.TargetChatId).HasColumnType("bigint").IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.AttemptCount).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.NextAttemptAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LeaseToken).HasColumnType("uniqueidentifier");
        builder.Property(x => x.LeaseUntilUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.LastError).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.TelegramMessageId).HasColumnType("bigint");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.SentAtUtc).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new { x.OrderId, x.EventType, x.Channel, x.TargetChatId }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
    }
}
