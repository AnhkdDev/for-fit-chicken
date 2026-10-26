using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class OrderEvent
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public OrderStatus? FromStatus { get; set; } = OrderStatus.PendingConfirmation;
    public OrderStatus? ToStatus { get; set; } = OrderStatus.PendingConfirmation;
    public string? Note { get; set; }
    public string? ChangedFieldsJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? ActorUserId { get; set; }
}
