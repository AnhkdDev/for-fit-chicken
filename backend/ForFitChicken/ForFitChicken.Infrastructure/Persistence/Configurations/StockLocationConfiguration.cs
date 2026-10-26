using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.ToTable("StockLocations", table =>
        {
            table.HasCheckConstraint("CK_StockLocations_Kind", "[Kind] IN ('Warehouse', 'Gym')");
            table.HasCheckConstraint("CK_StockLocations_GymKind", "([Kind] = 'Gym' AND [GymId] IS NOT NULL) OR ([Kind] = 'Warehouse' AND [GymId] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasColumnType("varchar(40)").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
        builder.Property(x => x.GymId).HasColumnType("bigint");
        builder.HasOne<Gym>().WithMany().HasForeignKey(x => x.GymId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.LocationCode).IsUnique();
        builder.HasIndex(x => x.GymId).IsUnique().HasFilter("[GymId] IS NOT NULL");
    }
}
