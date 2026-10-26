using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class GymStockCount
{
    public long Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public long GymId { get; set; }
    public long? PreviousCountId { get; set; }
    public StockCountKind Kind { get; set; } = StockCountKind.Regular;
    public PostingStatus Status { get; set; } = PostingStatus.Draft;
    public DateTime CountedAtUtc { get; set; }
    public long CountedByUserId { get; set; }
    public string? SourceFingerprint { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public long? PostedByUserId { get; set; }
    public string? Note { get; set; }
    public byte[] Version { get; set; } = [];
}
