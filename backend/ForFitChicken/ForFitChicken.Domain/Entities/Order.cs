using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class Order
{
    public long Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderSource Source { get; set; } = OrderSource.Website;
    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }
    public string? InternalNote { get; set; }
    public decimal ItemsSubtotal { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public DateTime? HandedOverAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public long? ReplacesOrderId { get; set; }
    public byte[] Version { get; set; } = [];
}
