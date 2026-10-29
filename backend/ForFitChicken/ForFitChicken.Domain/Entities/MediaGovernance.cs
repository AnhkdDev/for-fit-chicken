namespace ForFitChicken.Domain.Entities;

public sealed class MediaStorageState
{
    public long Id { get; set; } = 1;
    public string ScopeHash { get; set; } = "";
    public long ChargedBytes { get; set; }
    public bool Initialized { get; set; }
    public bool UploadsPaused { get; set; }
    public DateTime? LastReconciledAtUtc { get; set; }
    public Guid? ReconcileToken { get; set; }
    public DateTime? ReconcileUntilUtc { get; set; }
}
public sealed class MediaObjectReservation
{
    public long Id { get; set; }
    public string StorageKey { get; set; } = "";
    public long ByteSize { get; set; }
    // Reserved/Unknown/Stored/Deleting/Deleted. Unknown never auto-expires from quota.
    public string State { get; set; } = "Reserved";
    public long? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ActiveUntilUtc { get; set; }
}
public sealed class MediaUsageCounter
{
    public string Id { get; set; } = "";
    public long Count { get; set; }
    public long Bytes { get; set; }
}
