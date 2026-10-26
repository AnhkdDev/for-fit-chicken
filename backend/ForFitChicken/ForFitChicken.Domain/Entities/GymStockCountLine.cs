using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class GymStockCountLine
{
    public long Id { get; set; }
    public long CountId { get; set; }
    public long BatchId { get; set; }
    public int? ExpectedGood { get; set; }
    public int? ExpectedQuarantine { get; set; }
    public int? ExpectedDamaged { get; set; }
    public int? ActualGood { get; set; }
    public int? ActualQuarantine { get; set; }
    public int? ActualDamaged { get; set; }
    public string? Note { get; set; }
    public byte[] Version { get; set; } = [];
}
