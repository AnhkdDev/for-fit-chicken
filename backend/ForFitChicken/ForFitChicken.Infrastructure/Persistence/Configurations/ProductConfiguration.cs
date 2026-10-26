using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_SortOrder", "[SortOrder] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasColumnType("varchar(200)").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortDescription).HasColumnType("nvarchar(600)").HasMaxLength(600);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Ingredients).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NutritionText).HasColumnType("nvarchar(max)");
        builder.Property(x => x.UsageInstructions).HasColumnType("nvarchar(max)");
        builder.Property(x => x.StorageInstructions).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsPublished).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasColumnType("bit").IsRequired().HasDefaultValue(true).HasSentinel(true);
        builder.Property(x => x.IsFeatured).HasColumnType("bit").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.SortOrder).HasColumnType("int").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.FirstPublishedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
