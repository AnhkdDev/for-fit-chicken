using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long VariantId { get; set; }
    public string SkuCodeSnapshot { get; set; } = string.Empty;
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string VariantLabelSnapshot { get; set; } = string.Empty;
    public int NetWeightGramsSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; private set; }
}
