using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class GymConfiguration : IEntityTypeConfiguration<Gym>
{
    public void Configure(EntityTypeBuilder<Gym> builder)
    {
        builder.ToTable("Gyms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.GymCode).HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PublicAddress).HasColumnType("nvarchar(600)").HasMaxLength(600).IsRequired();
        builder.Property(x => x.PublicPhone).HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.PublicDescription).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.InternalContactName).HasColumnType("nvarchar(150)").HasMaxLength(150);
        builder.Property(x => x.InternalPhone).HasColumnType("varchar(30)").HasMaxLength(30);
        builder.Property(x => x.InternalNotes).HasColumnType("nvarchar(1000)").HasMaxLength(1000);
        builder.Property(x => x.CoverAssetId).HasColumnType("bigint");
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.CoverAssetId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.IsPublished).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.GymCode).IsUnique();
    }
}
