using ForFitChicken.Application.Commerce;

namespace ForFitChicken.Infrastructure.Media;

internal sealed record ValidatedImage(byte[] Bytes, string Extension, string ContentType)
{
    public const int Limit = 10 * 1024 * 1024;
    public static async Task<ValidatedImage> ReadAsync(Stream stream, CancellationToken token, int limit = Limit)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920]; int count;
        while ((count = await stream.ReadAsync(chunk, token)) != 0)
        {
            if (buffer.Length + count > limit) throw new BusinessException(400, "IMAGE_TOO_LARGE", "Ảnh vượt giới hạn dung lượng.");
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
        return new(bytes, ext, type);
    }
}
