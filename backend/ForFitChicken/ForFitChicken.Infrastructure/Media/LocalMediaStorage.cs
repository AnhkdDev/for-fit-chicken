using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Configuration;

namespace ForFitChicken.Infrastructure.Media;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly string _root;
    public LocalMediaStorage(IConfiguration configuration)
    {
        _root = Path.GetFullPath(configuration["Media:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "media"));
    }
    public async Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token)
    {
        var image = await ValidatedImage.ReadAsync(stream, token);
        var bytes = image.Bytes; var ext = image.Extension; var type = image.ContentType;
        Directory.CreateDirectory(_root); var key = Guid.NewGuid().ToString("N") + "." + ext;
        await File.WriteAllBytesAsync(Path.Combine(_root, key), bytes, token);
        return new(key, type, bytes.Length);
    }
    public Task DeleteAsync(string storageKey, CancellationToken token) { File.Delete(PathFor(storageKey)); return Task.CompletedTask; }
    public Task<Stream> OpenAsync(string storageKey, CancellationToken token)
    {
        var file = PathFor(storageKey);
        if (!File.Exists(file)) throw new BusinessException(404, "MEDIA_MISSING", "Tệp ảnh chưa có trên máy chủ.");
        return Task.FromResult<Stream>(File.OpenRead(file));
    }
    private string PathFor(string key)
    {
        if (key != Path.GetFileName(key) || key.Contains("..") || key.Contains('/') || key.Contains('\\')) throw new InvalidOperationException("Invalid media key.");
        return Path.Combine(_root, key);
    }
}
