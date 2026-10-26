using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Domain.Entities;

public sealed class StockLocation
{
    public long Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StockLocationKind Kind { get; set; } = StockLocationKind.Warehouse;
    public long? GymId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] Version { get; set; } = [];
}
