using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class MediaAsset
{
    public long Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ByteSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? DefaultAlt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
}
