using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class GymVarianceResolutionConfiguration : IEntityTypeConfiguration<GymVarianceResolution>
{
    public void Configure(EntityTypeBuilder<GymVarianceResolution> builder)
    {
        builder.ToTable("GymVarianceResolutions", table =>
        {
            table.HasCheckConstraint("CK_GymVarianceResolutions_Direction", "[Direction] IN ('Missing', 'Surplus')");
            table.HasCheckConstraint("CK_GymVarianceResolutions_Category", "[Category] IN ('Sale', 'Loss', 'Other')");
            table.HasCheckConstraint("CK_GymVarianceResolutions_Void", "([VoidedAtUtc] IS NULL AND [VoidedByUserId] IS NULL AND [VoidReason] IS NULL) OR ([VoidedAtUtc] IS NOT NULL AND [VoidedByUserId] IS NOT NULL AND [VoidReason] IS NOT NULL AND LEN(LTRIM(RTRIM([VoidReason]))) > 0)");
            table.HasCheckConstraint("CK_GymVarianceResolutions_Resolution", "[Quantity] > 0 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND ([Category] <> 'Sale' OR [Direction] = 'Missing')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountLineId).HasColumnType("bigint").IsRequired();
        builder.HasOne<GymStockCountLine>().WithMany().HasForeignKey(x => x.CountLineId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Direction).HasConversion<string>().HasColumnType("varchar(10)").HasMaxLength(10).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Quantity).HasColumnType("int").IsRequired();
        builder.Property(x => x.Reason).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.EvidenceReference).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.VoidedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.VoidedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.VoidReason).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.HasIndex(x => new { x.CountLineId, x.VoidedAtUtc });
    }
}
