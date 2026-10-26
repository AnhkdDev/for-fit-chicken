using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class SiteContent
{
    public long Id { get; set; }
    public string SectionKey { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string DraftJson { get; set; } = string.Empty;
    public string? PublishedJson { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public long? PublishedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public long UpdatedByUserId { get; set; }
    public byte[] Version { get; set; } = [];
}
