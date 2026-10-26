using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class NotificationJob
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public NotificationEventType EventType { get; set; } = NotificationEventType.OrderRequested;
    public NotificationChannel Channel { get; set; } = NotificationChannel.Telegram;
    public long TargetChatId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public string? LastError { get; set; }
    public long? TelegramMessageId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
