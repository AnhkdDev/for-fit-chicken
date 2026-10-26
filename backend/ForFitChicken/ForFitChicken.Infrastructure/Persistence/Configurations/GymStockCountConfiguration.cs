using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class GymStockCountConfiguration : IEntityTypeConfiguration<GymStockCount>
{
    public void Configure(EntityTypeBuilder<GymStockCount> builder)
    {
        builder.ToTable("GymStockCounts", table =>
        {
            table.HasCheckConstraint("CK_GymStockCounts_Kind", "[Kind] IN ('Regular', 'OpeningBaseline')");
            table.HasCheckConstraint("CK_GymStockCounts_Status", "[Status] IN ('Draft', 'Posted', 'Cancelled')");
            table.HasCheckConstraint("CK_GymStockCounts_Posting", "([Status] = 'Posted' AND [PostedAtUtc] IS NOT NULL AND [PostedByUserId] IS NOT NULL) OR ([Status] <> 'Posted' AND [PostedAtUtc] IS NULL AND [PostedByUserId] IS NULL)");
            table.HasCheckConstraint("CK_GymStockCounts_NotSelfPrevious", "[PreviousCountId] IS NULL OR [PreviousCountId] <> [Id]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountNumber).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GymId).HasColumnType("bigint").IsRequired();
        builder.HasOne<Gym>().WithMany().HasForeignKey(x => x.GymId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.PreviousCountId).HasColumnType("bigint");
        builder.Property(x => x.Kind).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CountedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CountedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CountedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.SourceFingerprint).HasColumnType("varchar(64)").HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.PostedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.PostedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Note).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasAlternateKey(x => new { x.GymId, x.Id });
        builder.HasOne<GymStockCount>().WithMany().HasForeignKey(x => new { x.GymId, x.PreviousCountId }).HasPrincipalKey(x => new { x.GymId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.CountNumber).IsUnique();
        builder.HasIndex(x => new { x.GymId, x.CountedAtUtc }).IsUnique().HasFilter("[Status] = 'Posted'");
        builder.HasIndex(x => new { x.GymId, x.Status, x.CountedAtUtc });
        builder.HasIndex(x => x.PreviousCountId).IsUnique().HasFilter("[Status] = 'Posted' AND [PreviousCountId] IS NOT NULL");
        builder.HasIndex(x => x.GymId).IsUnique().HasFilter("[Status] = 'Posted' AND [PreviousCountId] IS NULL");
    }
}
