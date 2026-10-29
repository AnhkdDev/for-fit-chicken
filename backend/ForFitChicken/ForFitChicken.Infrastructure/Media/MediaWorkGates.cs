using Microsoft.Extensions.Caching.Memory;

namespace ForFitChicken.Infrastructure.Media;
public sealed class MediaWorkGates : IDisposable
{
    public volatile bool R2Ready;
    public SemaphoreSlim Uploads { get; } = new(2);
    public SemaphoreSlim Reads { get; } = new(4);
    private readonly SemaphoreSlim _cacheMiss = new(1);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64 * 1024 * 1024 });
    public async Task<byte[]> PublicBytesAsync(string key, Func<Task<byte[]>> fetch, CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out byte[]? hit)) return hit!;
        await _cacheMiss.WaitAsync(ct);
        try {
            if (_cache.TryGetValue(key, out hit)) return hit!;
            var bytes = await fetch();
            _cache.Set(key, bytes, new MemoryCacheEntryOptions { Size = bytes.Length, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            return bytes;
        } finally { _cacheMiss.Release(); }
    }
    public void Evict(string key) => _cache.Remove(key);
    public void Dispose() { Uploads.Dispose(); Reads.Dispose(); _cacheMiss.Dispose(); _cache.Dispose(); }
}
