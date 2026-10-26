using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class GymVarianceResolution
{
    public long Id { get; set; }
    public long CountLineId { get; set; }
    public VarianceDirection Direction { get; set; } = VarianceDirection.Missing;
    public VarianceCategory Category { get; set; } = VarianceCategory.Sale;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public DateTime VerifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public long? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
}
