using System.ComponentModel.DataAnnotations;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed record Page<T>(T[] Items, int Total, int PageNumber, int PageSize);
public sealed record ProductInput(
    [Required, StringLength(200)] string Name,
    [Required, RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$"), StringLength(200)] string Slug,
    [StringLength(600)] string? ShortDescription, [StringLength(20000)] string? Description,
    [StringLength(10000)] string? Ingredients, [StringLength(10000)] string? NutritionText,
    [StringLength(10000)] string? UsageInstructions, [StringLength(10000)] string? StorageInstructions,
    bool IsActive, bool IsPublished, bool IsFeatured, [Range(0, 100000)] int SortOrder, string? Version);
public sealed record VariantInput([Required, StringLength(50), RegularExpression("^[A-Za-z0-9_-]+$")] string SkuCode,
    [Required, StringLength(150)] string Label, [Range(1, 100000)] int NetWeightGrams,
    [Range(typeof(decimal), "1", "999999999")] decimal SellingPrice, bool IsActive, [Range(0, 100000)] int SortOrder, string? Version);
public sealed record VariantView(long Id, string SkuCode, string Label, int? NetWeightGrams, decimal SellingPrice, bool IsActive, int Available, string? Version);
public sealed record ImageInput(long AssetId, long? VariantId, [StringLength(300)] string? AltText, [Range(0, 100000)] int SortOrder);
public sealed record ImageView(long Id, long AssetId, long? VariantId, string? AltText, int SortOrder, string Url);
public sealed record ProductView(long Id, string Name, string Slug, string? ShortDescription, string? Description,
    string? Ingredients, string? NutritionText, string? UsageInstructions, string? StorageInstructions,
    bool IsActive, bool IsPublished, bool IsFeatured, int SortOrder, string? Version, VariantView[] Variants, ImageView[] Images);
public sealed record AssetView(long Id, string ContentType, long ByteSize, string Url);
public sealed record StoredMedia(string StorageKey, string ContentType, long ByteSize);
public interface IMediaStorage
{
    Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token);
    Task DeleteAsync(string storageKey, CancellationToken token);
    Task<Stream> OpenAsync(string storageKey, CancellationToken token);
}
public sealed record BatchInput(long VariantId, [Required, StringLength(60), RegularExpression("^[A-Za-z0-9_-]+$")] string BatchCode,
    DateOnly ManufacturedOn, DateTime ExpiresAtUtc, [StringLength(1000)] string? Notes);
public sealed record BatchView(long Id, long VariantId, string BatchCode, DateOnly ManufacturedOn, DateTime ExpiresAtUtc, bool IsSaleBlocked, string? BlockReason, string Version);
public sealed record BatchCostView(long Id, decimal? UnitCost, string Version);
public sealed record LocationView(long Id, string LocationCode, string Name, StockLocationKind Kind, long? GymId, bool IsActive, string Version);
public sealed record VersionInput([Required] string Version);
public sealed record ReasonInput([Required] string Version, [Required, StringLength(500)] string Reason);
public sealed record BlockBatchInput([Required] string Version, bool Blocked, [StringLength(500)] string? Reason);
public sealed record CostInput([Required] string Version, [Range(typeof(decimal), "0", "999999999")] decimal? UnitCost);
public sealed record BalanceView(long Id, long LocationId, long BatchId, string BatchCode, long VariantId, StockCondition Condition,
    int OnHand, int Reserved, int Available, DateTime ExpiresAtUtc, string Version);
public sealed record MovementLine(long BatchId, [Range(1, 100000)] int Quantity,
    long? FromLocationId, StockCondition? FromCondition, long? ToLocationId, StockCondition? ToCondition, [StringLength(500)] string? Note);
public sealed record MovementInput([Required] Guid OperationKey, StockDocumentType DocumentType, [Required, StringLength(1000)] string Reason,
    [Required, MinLength(1), MaxLength(100)] MovementLine[] Lines, long? CorrectsDocumentId = null);
public sealed record DocumentView(long Id, string DocumentNo, Guid OperationKey, StockDocumentType DocumentType, PostingStatus Status, DateTime EffectiveAtUtc, MovementLine[] Lines, long? CorrectsDocumentId, string? Reason);
public sealed record CartLine(long VariantId, [Range(1, 100)] int Quantity);
public sealed record CheckoutInput([Required, StringLength(150)] string RecipientName,
    [Required, RegularExpression("^(?:0[0-9]{9}|\\+84[0-9]{9})$")] string RecipientPhone,
    [Required, StringLength(600)] string RecipientAddress, [StringLength(1000)] string? CustomerNote,
    [Required, MinLength(1), MaxLength(20)] CartLine[] Items);
public sealed record CheckoutResult(string OrderNumber, OrderStatus Status, decimal ItemsSubtotal, DateTime CreatedAtUtc);
public sealed record OrderLineView(long Id, long VariantId, string SkuCode, string ProductName, string Label, int WeightGrams, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record AllocationView(long Id, long OrderItemId, long BalanceId, int Quantity, AllocationState State);
public sealed record OrderEventView(long Id, string EventType, OrderStatus? FromStatus, OrderStatus? ToStatus, string? Note, DateTime CreatedAtUtc, long? ActorUserId);
public sealed record OrderView(long Id, string OrderNumber, OrderStatus Status, string RecipientName, string RecipientPhone,
    string RecipientAddress, string? CustomerNote, string? InternalNote, decimal ItemsSubtotal, DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc, string Version, OrderLineView[] Items, AllocationView[] Allocations);
public sealed record OrderEditInput([Required] string Version, [Required, StringLength(150)] string RecipientName,
    [Required, RegularExpression("^(?:0[0-9]{9}|\\+84[0-9]{9})$")] string RecipientPhone,
    [Required, StringLength(600)] string RecipientAddress, [StringLength(1000)] string? InternalNote);
public sealed record ReceiptInput([Required] string Version, DateTime ReceivedAtUtc, [StringLength(200)] string? Reference);
public sealed record CompleteAndReceiveInput([Required] string Version, [StringLength(200)] string? Reason);
public sealed record ReceiptView(long Id, long OrderId, decimal Amount, DateTime ReceivedAtUtc, DateTime? VoidedAtUtc, string? VoidReason, string? Reference);
public sealed record ReceiptVoidInput([Required, StringLength(500)] string Reason);
public sealed record GymInput([Required, StringLength(40), RegularExpression("^[A-Za-z0-9_-]+$")] string GymCode,
    [Required, StringLength(200)] string Name, [Required, StringLength(600)] string PublicAddress,
    [StringLength(30)] string? PublicPhone, [StringLength(1000)] string? PublicDescription,
    [StringLength(150)] string? InternalContactName, [StringLength(30)] string? InternalPhone,
    [StringLength(1000)] string? InternalNotes, long? CoverAssetId, bool IsActive, bool IsPublished, string? Version);
public sealed record GymView(long Id, string GymCode, string Name, string PublicAddress, string? PublicPhone, string? PublicDescription,
    long? CoverAssetId, bool IsActive, bool IsPublished, long LocationId, string? Version,
    string? InternalContactName, string? InternalPhone, string? InternalNotes);
public sealed record PublicGymView(long Id, string Name, string PublicAddress, string? PublicPhone, string? PublicDescription, long? CoverAssetId);
public sealed record CountLineInput(long BatchId, [Range(0, 100000)] int ActualGood,
    [Range(0, 100000)] int ActualQuarantine, [Range(0, 100000)] int ActualDamaged, [StringLength(500)] string? Note);
public sealed record CountInput([Required] string Version, [Required, MinLength(1), MaxLength(500)] CountLineInput[] Lines, [StringLength(1000)] string? Note);
public sealed record CountLineView(long Id, long BatchId, int? ExpectedGood, int? ExpectedQuarantine, int? ExpectedDamaged,
    int ActualGood, int ActualQuarantine, int ActualDamaged, string Version, string? Note);
public sealed record CountView(long Id, long GymId, string CountNumber, StockCountKind Kind, DateTime CountedAtUtc, CountLineView[] Lines, string? Note);
public sealed record GymBookLine(long BatchId, int Good, int Quarantine, int Damaged);
public sealed record GymStockView(long GymId, DateTime? LastCountedAtUtc, bool IsRealtime, string Version, CountLineView[] LastCountLines, GymBookLine[] BookEstimate);
public sealed record ResolutionInput([Required] string Version, VarianceDirection Direction, VarianceCategory Category,
    [Range(1, 100000)] int Quantity, [Required, StringLength(1000)] string Reason, [StringLength(1000)] string? EvidenceReference, [Required] Guid OperationKey);
public sealed record ResolutionView(long Id, long CountLineId, VarianceDirection Direction, VarianceCategory Category, int Quantity,
    string Reason, DateTime VerifiedAtUtc, DateTime? VoidedAtUtc, string? EvidenceReference, string? VoidReason);
public sealed record SiteSectionInput([Required, StringLength(200)] string Title, [StringLength(20000)] string? Body,
    long[] AssetIds, [StringLength(100)] string? LinkLabel, [StringLength(500)] string? LinkUrl, string? Version, long[]? ProductIds = null, bool Enabled = true, Dictionary<long, long>? ProductMainImages = null,
    Dictionary<long, HomeImageFrame>? ProductImageFrames = null);
public sealed record HomeImageFrame(long AssetId, ImageFrameSettings Desktop, ImageFrameSettings? Mobile = null);
public sealed record ImageFrameSettings(string Fit = "contain", decimal X = 50, decimal Y = 50, decimal Zoom = 1);
public sealed record SectionView(string SectionKey, SiteSectionInput Content, DateTime? PublishedAtUtc, string? Version);
public sealed record SalesReport(DateTime FromUtc, DateTime ToUtc, int OrdersRequested, int OrdersCompleted, decimal CompletedItemsValue,
    decimal CodReceived, decimal KnownIssuedCost, int IssuedPackagesWithUnknownCost, string Note);
public sealed record NotificationSettings(bool Enabled, long? TargetChatId);
public sealed record NotificationJobView(long Id, long OrderId, NotificationEventType EventType, NotificationStatus Status, int AttemptCount,
    DateTime NextAttemptAtUtc, string? LastError, DateTime CreatedAtUtc, DateTime? SentAtUtc);
public interface INotificationSettings { NotificationSettings Current { get; } }
