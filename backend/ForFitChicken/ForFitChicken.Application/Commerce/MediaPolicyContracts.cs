namespace ForFitChicken.Application.Commerce;

public sealed record MediaPolicySettings(bool IsR2, string ScopeHash, long MaxStoredBytes = 9_000_000_000,
    int DailyUploads = 100, long DailyInputBytes = 200_000_000, int PerUserDailyUploads = 50,
    int ActiveUploads = 2, int MonthlyClassA = 100_000, int MonthlyClassB = 1_000_000,
    int ReconcileMaxAgeHours = 24, int OrphanGraceHours = 24);
public interface IMediaPolicySettings { MediaPolicySettings Current { get; } }
public interface IMediaPolicyAccess
{
    Task<T> RunAsync<T>(Func<MediaPolicyService, Task<T>> action);
}
public interface IMediaUploadContext
{
    Task<StoredMedia> SaveAsync(Stream stream, long actor, CancellationToken token);
    Task<Stream> OpenPublicAsync(string key, CancellationToken token);
}
public sealed record CloudObject(string Key, long Bytes);
public sealed record MediaDeleteResult(bool MetadataRemoved, bool ObjectDeleted, bool CleanupRequired);
public sealed record MediaAssetStatus(long Id, string ContentType, long ByteSize, DateTime CreatedAtUtc, string Url);
public sealed record MediaStorageStatus(long ChargedBytes, long LimitBytes, long UncertainBytes, int ActiveUploads,
    bool Initialized, bool UploadsPaused, DateTime? LastReconciledAtUtc, long ClassAThisMonth, long ClassBThisMonth,
    string[] Warnings, bool NodeReady = false);
public sealed record MediaObjectStatus(long Id, string StorageKey, long ByteSize, string State, DateTime CreatedAtUtc);
