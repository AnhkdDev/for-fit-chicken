using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class SiteContentConfiguration : IEntityTypeConfiguration<SiteContent>
{
    public void Configure(EntityTypeBuilder<SiteContent> builder)
    {
        builder.ToTable("SiteContent", table =>
        {
            table.HasCheckConstraint("CK_SiteContent_Section", "[SectionKey] IN ('HomeHero', 'Story', 'Usage', 'Faq', 'Contact', 'SiteSettings', 'HomeProducts')");
            table.HasCheckConstraint("CK_SiteContent_Json", "[SchemaVersion] > 0 AND ISJSON([DraftJson]) = 1 AND ([PublishedJson] IS NULL OR ISJSON([PublishedJson]) = 1)");
            table.HasCheckConstraint("CK_SiteContent_Publication", "([PublishedJson] IS NULL AND [PublishedAtUtc] IS NULL AND [PublishedByUserId] IS NULL) OR ([PublishedJson] IS NOT NULL AND [PublishedAtUtc] IS NOT NULL AND [PublishedByUserId] IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SectionKey).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SchemaVersion).HasColumnType("int").IsRequired();
        builder.Property(x => x.DraftJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.PublishedJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PublishedAtUtc).HasColumnType("datetime2(3)");
        builder.Property(x => x.PublishedByUserId).HasColumnType("bigint");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.SectionKey).IsUnique();
    }
}
