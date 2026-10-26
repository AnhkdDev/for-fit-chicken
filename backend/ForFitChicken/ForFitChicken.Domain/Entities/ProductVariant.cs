using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? NetWeightGrams { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
