using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class StockDocumentLine
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public long BatchId { get; set; }
    public int Quantity { get; set; }
    public long? FromLocationId { get; set; }
    public StockCondition? FromCondition { get; set; }
    public long? ToLocationId { get; set; }
    public StockCondition? ToCondition { get; set; }
    public string? Note { get; set; }
}
