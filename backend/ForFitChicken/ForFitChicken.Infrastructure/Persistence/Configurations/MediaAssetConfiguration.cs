using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets", table =>
        {
            table.HasCheckConstraint("CK_MediaAssets_Size", "[ByteSize] > 0 AND ([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).HasColumnType("nvarchar(450)").HasMaxLength(450).IsRequired();
        builder.Property(x => x.ContentType).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ByteSize).HasColumnType("bigint").IsRequired();
        builder.Property(x => x.Width).HasColumnType("int");
        builder.Property(x => x.Height).HasColumnType("int");
        builder.Property(x => x.DefaultAlt).HasColumnType("nvarchar(300)").HasMaxLength(300);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.StorageKey).IsUnique();
    }
}
