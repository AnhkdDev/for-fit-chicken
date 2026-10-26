using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class CodReceipt
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public long RecordedByUserId { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public long? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
}
