using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class IdempotencyRequest
{
    public long Id { get; set; }
    public string ScopeHash { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string RequestKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public int ResponseStatus { get; set; }
    public string? SafeResponseJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
