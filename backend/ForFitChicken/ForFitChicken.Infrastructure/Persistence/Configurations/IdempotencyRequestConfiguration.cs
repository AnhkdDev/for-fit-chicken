using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForFitChicken.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("IdempotencyRequests", table =>
        {
            table.HasCheckConstraint("CK_IdempotencyRequests_Response", "[ResponseStatus] BETWEEN 100 AND 599 AND [ExpiresAtUtc] > [CreatedAtUtc] AND LEN([ScopeHash]) = 64 AND LEN([RequestHash]) = 64 AND ([SafeResponseJson] IS NULL OR ISJSON([SafeResponseJson]) = 1)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScopeHash).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Operation).HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RequestKey).HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnType("varchar(64)").HasMaxLength(64).IsRequired();
        builder.Property(x => x.OrderId).HasColumnType("bigint");
        builder.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.ResponseStatus).HasColumnType("int").IsRequired();
        builder.Property(x => x.SafeResponseJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.HasIndex(x => new { x.ScopeHash, x.Operation, x.RequestKey }).IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
