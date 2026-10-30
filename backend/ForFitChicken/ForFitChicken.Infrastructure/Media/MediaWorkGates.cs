using Microsoft.Extensions.Caching.Memory;

namespace ForFitChicken.Infrastructure.Media;
public sealed class MediaWorkGates : IDisposable
{
    public volatile bool R2Ready;
    public SemaphoreSlim Uploads { get; } = new(2);
    public SemaphoreSlim Reads { get; } = new(4);
    private readonly SemaphoreSlim _pending = new(24);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string,Lazy<Task<byte[]>>> _flights = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64 * 1024 * 1024 });
    public async Task<byte[]> PublicBytesAsync(string key, Func<Task<byte[]>> fetch, CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out byte[]? hit)) return hit!;
        if(!await _pending.WaitAsync(0,ct)) throw new ForFitChicken.Application.Commerce.BusinessException(429,"MEDIA_BUSY","Có nhiều ảnh đang chờ tải, vui lòng thử lại.");
        Lazy<Task<byte[]>>? flight=null;
        try {
            flight=_flights.GetOrAdd(key,_=>new Lazy<Task<byte[]>>(async()=>{
                if(_cache.TryGetValue(key,out byte[]? cached))return cached!;
                var bytes=await fetch();
                _cache.Set(key,bytes,new MemoryCacheEntryOptions{Size=bytes.Length,AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(5)});
                return bytes;
            }));
            return await flight.Value;
        } finally {
            if(flight is not null)((ICollection<KeyValuePair<string,Lazy<Task<byte[]>>>>)_flights).Remove(new(key,flight));
            _pending.Release();
        }
    }

    public void Evict(string key) => _cache.Remove(key);
    public void Dispose() { Uploads.Dispose(); Reads.Dispose(); _pending.Dispose(); _cache.Dispose(); }
}
