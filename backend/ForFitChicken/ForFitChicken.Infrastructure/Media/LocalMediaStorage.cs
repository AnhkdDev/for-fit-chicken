using ForFitChicken.Application.Commerce;
using Microsoft.Extensions.Configuration;

namespace ForFitChicken.Infrastructure.Media;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly string _root;
    private const int Limit = 10 * 1024 * 1024;
    public LocalMediaStorage(IConfiguration configuration)
    {
        _root = Path.GetFullPath(configuration["Media:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "media"));
    }
    public async Task<StoredMedia> SaveAsync(Stream stream, CancellationToken token)
    {
        using var buffer = new MemoryStream(); var chunk = new byte[81920]; int count;
        while ((count = await stream.ReadAsync(chunk, token)) != 0)
        {
            if (buffer.Length + count > Limit) throw new BusinessException(400, "IMAGE_TOO_LARGE", "Ảnh tối đa 10 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), token);
        }
        var bytes = buffer.ToArray();
        var (ext, type) = bytes switch
        {
            [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, ..] => ("png", "image/png"),
            [0xff, 0xd8, 0xff, ..] => ("jpg", "image/jpeg"),
            [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => ("webp", "image/webp"),
            _ => throw new BusinessException(400, "UNSUPPORTED_IMAGE", "Chỉ nhận PNG, JPEG hoặc WebP; không nhận SVG/HTML.")
        };
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
