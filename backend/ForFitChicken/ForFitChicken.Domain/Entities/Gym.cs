using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class Gym
{
    public long Id { get; set; }
    public string GymCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PublicAddress { get; set; } = string.Empty;
    public string? PublicPhone { get; set; }
    public string? PublicDescription { get; set; }
    public string? InternalContactName { get; set; }
    public string? InternalPhone { get; set; }
    public string? InternalNotes { get; set; }
    public long? CoverAssetId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
