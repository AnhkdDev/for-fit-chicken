namespace ForFitChicken.Application.Commerce;
public interface IMediaMaintenance
{
    bool IsReady { get; }
    Task<MediaStorageStatus> ReconcileAsync(CancellationToken token);
    Task DeleteOrphanAsync(long id, CancellationToken token);
}
