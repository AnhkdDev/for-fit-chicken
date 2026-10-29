using ForFitChicken.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;
public sealed class MediaGovernanceConfiguration : IEntityTypeConfiguration<MediaStorageState>, IEntityTypeConfiguration<MediaObjectReservation>, IEntityTypeConfiguration<MediaUsageCounter>
{
    public void Configure(EntityTypeBuilder<MediaStorageState> b)
    {
        b.ToTable("MediaStorageStates", t => t.HasCheckConstraint("CK_MediaStorageStates_Bytes", "[ChargedBytes] >= 0 AND [Id] = 1"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.ScopeHash).HasMaxLength(64).IsRequired();
    }
    public void Configure(EntityTypeBuilder<MediaObjectReservation> b)
    {
        b.ToTable("MediaObjectReservations", t => {
            t.HasCheckConstraint("CK_MediaObjectReservations_Bytes", "[ByteSize] >= 0");
            t.HasCheckConstraint("CK_MediaObjectReservations_State", "[State] IN ('Reserved','Unknown','Stored','Deleting','Deleted')");
        });
        b.HasKey(x => x.Id); b.Property(x => x.StorageKey).HasMaxLength(450).IsRequired(); b.HasIndex(x => x.StorageKey).IsUnique();
        b.Property(x => x.State).HasMaxLength(20).IsRequired(); b.HasIndex(x => new { x.State, x.CreatedAtUtc });
    }
    public void Configure(EntityTypeBuilder<MediaUsageCounter> b)
    {
        b.ToTable("MediaUsageCounters", t => t.HasCheckConstraint("CK_MediaUsageCounters_NonNegative", "[Count] >= 0 AND [Bytes] >= 0"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).HasMaxLength(100);
    }
}
