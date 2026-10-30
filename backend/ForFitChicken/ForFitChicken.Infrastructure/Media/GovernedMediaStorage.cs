using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ForFitChicken.Infrastructure.Media;

public sealed class GovernedMediaStorage(ConfiguredMediaStorage raw, R2MediaStorage cloud, ImageOptimizer optimizer,
    IMediaPolicyAccess policy, MediaWorkGates gates, IOptions<MediaOptions> options, ILogger<GovernedMediaStorage> logger)
    : IMediaStorage, IMediaUploadContext, IMediaMaintenance
{
    private bool IsR2 => options.Value.Provider == "R2";
    public bool IsReady => !IsR2 || gates.R2Ready;
    private string CacheKey(string key) => options.Value.R2.Endpoint + "|" + options.Value.R2.BucketName + "|" + key;
    public Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token) => throw new InvalidOperationException("Upload actor required.");
    public async Task<StoredMedia> SaveAsync(Stream stream, long actor, CancellationToken token)
    {
        if (IsR2 && !gates.R2Ready) throw new BusinessException(503, "MEDIA_RECONCILE_REQUIRED", "Đang chờ đối soát bucket sau khởi động.");
        if (!await gates.Uploads.WaitAsync(0, token)) throw Busy();
        try {
            var image = await ValidatedImage.ReadAsync(stream, token, 5 * 1024 * 1024);
            await policy.RunAsync(p => p.AdmitAsync(actor, image.Bytes.Length, token));
            var optimized = await optimizer.OptimizeAsync(image.Bytes, token);
            if (!IsR2) return await raw.SaveAsync(new MemoryStream(optimized), token);
            var key = "r2/" + Guid.NewGuid().ToString("N") + ".webp";
            await policy.RunAsync(p => p.ReserveAsync(key, optimized.Length, actor, token));
            try {
                await cloud.PutAsync(key, optimized, "image/webp", token);
                await policy.RunAsync(p => p.FinishUploadAsync(key, true, token));
            } catch {
                // Never release an uncertain upload. A later reconciler/operator must verify/delete it.
                try { await policy.RunAsync(p => p.FinishUploadAsync(key, false, CancellationToken.None)); }
                catch { logger.LogWarning("Media reservation remains charged; reconciliation required."); }
                throw;
            }
            return new(key, "image/webp", optimized.Length);
        } finally { gates.Uploads.Release(); }
    }
    // Catalog authorizes the asset before either method; cached bytes never bypass that check.
    public Task<Stream> OpenAsync(string key, CancellationToken token) => OpenPublicAsync(key, token);
    public async Task<Stream> OpenPublicAsync(string key, CancellationToken token)
    {
        var bytes = await gates.PublicBytesAsync(CacheKey(key), async () => {
            if (!await gates.Reads.WaitAsync(TimeSpan.FromSeconds(5), token)) throw Busy();
            try { return await Fetch(key, token); } finally { gates.Reads.Release(); }
        }, token);
        return new MemoryStream(bytes, writable: false);
    }
    private async Task<byte[]> Fetch(string key, CancellationToken token)
    {
        if (key.StartsWith("r2/", StringComparison.Ordinal)) await policy.RunAsync(p => p.CountReadAsync(token));
        await using var stream = await raw.OpenAsync(key, token);
        // Raw adapters already enforce a 10 MiB legacy-file bound.
        using var copy = new MemoryStream(); await stream.CopyToAsync(copy, token); return copy.ToArray();
    }
    public async Task DeleteAsync(string key, CancellationToken token)
    {
        if (key.StartsWith("r2/", StringComparison.Ordinal)) {
            await policy.RunAsync(p => p.BeginDeleteAsync(key, false, token));
            await cloud.DeleteAsync(key, token);
            await policy.RunAsync(p => p.CompleteDeleteAsync(key, token));
        } else await raw.DeleteAsync(key, token);
        gates.Evict(CacheKey(key));
    }
    public async Task<MediaStorageStatus> ReconcileAsync(CancellationToken token)
    {
        if (!IsR2) throw new BusinessException(409, "MEDIA_NOT_R2", "Chọn provider R2 trước khi đối soát bucket.");
        var lease = await policy.RunAsync(p => p.BeginReconcileAsync(token));
        gates.R2Ready = false;
        try {
            string? continuation = null;
            var all = new List<CloudObject>(); var seen = new HashSet<string>();
            do {
                await policy.RunAsync(p => p.CountListAsync(token));
                var page = await cloud.ListPageAsync(continuation, token);
                all.AddRange(page.Objects); continuation = page.Next;
                if (all.Count > 100_000 || continuation is not null && !seen.Add(continuation))
                    throw new BusinessException(503, "MEDIA_RECONCILE_LIMIT", "Bucket quá nhiều object hoặc phân trang không hợp lệ; cần kiểm tra riêng.");
            } while (continuation is not null);
            await policy.RunAsync(p => p.CompleteReconcileAsync(lease, all.ToArray(), token));
            gates.R2Ready = true;
        } catch {
            try { await policy.RunAsync(p => p.AbortReconcileAsync(lease, CancellationToken.None)); }
            catch { logger.LogWarning("Media reconciliation lease will expire; uploads remain gated."); }
            throw;
        }
        return (await policy.RunAsync(p => p.StatusAsync(token))) with { NodeReady = IsReady };
    }
    public async Task DeleteOrphanAsync(long id, CancellationToken token)
    {
        var key = await policy.RunAsync(p => p.OrphanKeyAsync(id, token));
        await policy.RunAsync(p => p.BeginDeleteAsync(key, true, token));
        await cloud.DeleteAsync(key, token);
        await policy.RunAsync(p => p.CompleteDeleteAsync(key, token));
        gates.Evict(CacheKey(key));
    }
    private static BusinessException Busy() => new(429, "MEDIA_BUSY", "Có quá nhiều tác vụ ảnh đang xử lý; thử lại sau.");
}
