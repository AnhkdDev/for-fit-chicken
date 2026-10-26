using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class ProductBatch
{
    public long Id { get; set; }
    public long VariantId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateOnly ManufacturedOn { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public decimal? UnitCost { get; set; }
    public bool IsSaleBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAtUtc { get; set; }
    public long? BlockedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
