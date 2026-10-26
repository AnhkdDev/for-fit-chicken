using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class GymStockCountLineConfiguration : IEntityTypeConfiguration<GymStockCountLine>
{
    public void Configure(EntityTypeBuilder<GymStockCountLine> builder)
    {
        builder.ToTable("GymStockCountLines", table =>
        {
            table.HasCheckConstraint("CK_GymStockCountLines_Actual", "([ActualGood] IS NULL OR [ActualGood] >= 0) AND ([ActualQuarantine] IS NULL OR [ActualQuarantine] >= 0) AND ([ActualDamaged] IS NULL OR [ActualDamaged] >= 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CountId).HasColumnType("bigint").IsRequired();
        builder.HasOne<GymStockCount>().WithMany().HasForeignKey(x => x.CountId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.BatchId).HasColumnType("bigint").IsRequired();
        builder.HasOne<ProductBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.ExpectedGood).HasColumnType("int");
        builder.Property(x => x.ExpectedQuarantine).HasColumnType("int");
        builder.Property(x => x.ExpectedDamaged).HasColumnType("int");
        builder.Property(x => x.ActualGood).HasColumnType("int");
        builder.Property(x => x.ActualQuarantine).HasColumnType("int");
        builder.Property(x => x.ActualDamaged).HasColumnType("int");
        builder.Property(x => x.Note).HasColumnType("nvarchar(500)").HasMaxLength(500);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.CountId, x.BatchId }).IsUnique();
    }
}
