using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long? VariantId { get; set; }
    public long AssetId { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
}
