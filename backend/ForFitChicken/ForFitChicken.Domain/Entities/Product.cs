using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Ingredients { get; set; }
    public string? NutritionText { get; set; }
    public string? UsageInstructions { get; set; }
    public string? StorageInstructions { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime? FirstPublishedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
