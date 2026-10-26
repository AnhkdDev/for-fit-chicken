using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class OrderAllocation
{
    public long Id { get; set; }
    public long OrderItemId { get; set; }
    public long BalanceId { get; set; }
    public int Quantity { get; set; }
    public AllocationState State { get; set; } = AllocationState.Reserved;
    public decimal? UnitCostSnapshot { get; set; }
    public DateTime ReservedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public string? ReleaseReason { get; set; }
}
