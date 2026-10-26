using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? ChangesJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
