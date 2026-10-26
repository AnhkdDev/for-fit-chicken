using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", table =>
        {
            table.HasCheckConstraint("CK_AuditLogs_Json", "[ChangesJson] IS NULL OR ISJSON([ChangesJson]) = 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Action).HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityType).HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnType("varchar(100)").HasMaxLength(100);
        builder.Property(x => x.ChangesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.Id });
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
