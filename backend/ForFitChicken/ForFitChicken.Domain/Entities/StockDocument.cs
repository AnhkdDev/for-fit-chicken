using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class StockDocument
{
    public long Id { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public Guid OperationKey { get; set; }
    public StockDocumentType DocumentType { get; set; } = StockDocumentType.ProductionReceipt;
    public PostingStatus Status { get; set; } = PostingStatus.Draft;
    public long? OrderId { get; set; }
    public long? CorrectsDocumentId { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public long? PostedByUserId { get; set; }
    public string? Reason { get; set; }
    public byte[] Version { get; set; } = [];
}
