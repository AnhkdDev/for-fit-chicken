namespace ForFitChicken.Domain.Enums;

public enum StockLocationKind
{
    Warehouse = 1,
    Gym = 2
}

public enum StockCondition
{
    Good = 1,
    Quarantine = 2,
    Damaged = 3
}

public enum OrderSource
{
    Website = 1,
    Facebook = 2,
    Zalo = 3,
    Phone = 4,
    Other = 5
}

public enum OrderStatus
{
    PendingConfirmation = 1,
    Confirmed = 2,
    Preparing = 3,
    HandedOver = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PaymentMethod
{
    COD = 1
}

public enum AllocationState
{
    Reserved = 1,
    Issued = 2,
    Released = 3
}

public enum StockDocumentType
{
    ProductionReceipt = 1,
    GymDelivery = 2,
    GymRecall = 3,
    Reclassify = 4,
    WriteOff = 5,
    Adjustment = 6,
    OrderIssue = 7
}

public enum PostingStatus
{
    Draft = 1,
    Posted = 2,
    Cancelled = 3
}

public enum StockCountKind
{
    Regular = 1,
    OpeningBaseline = 2
}

public enum VarianceDirection
{
    Missing = 1,
    Surplus = 2
}

public enum VarianceCategory
{
    Sale = 1,
    Loss = 2,
    Other = 3
}

public enum NotificationEventType
{
    OrderRequested = 1
}

public enum NotificationChannel
{
    Telegram = 1
}

public enum NotificationStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4
}
