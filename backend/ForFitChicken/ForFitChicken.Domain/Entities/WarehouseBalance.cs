using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class WarehouseBalance
{
    public long Id { get; set; }
    public long LocationId { get; set; }
    public long BatchId { get; set; }
    public StockCondition Condition { get; set; } = StockCondition.Good;
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
