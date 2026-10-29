using ForFitChicken.Application.Commerce;
using SkiaSharp;

namespace ForFitChicken.Infrastructure.Media;
public sealed class ImageOptimizer
{
    public Task<byte[]> OptimizeAsync(byte[] input, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var data = SKData.CreateCopy(input);
        using var codec = SKCodec.Create(data);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Webp)) throw Invalid();
        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || info.Width > 10000 || info.Height > 10000 || (long)info.Width * info.Height > 24_000_000 || codec.FrameCount > 1)
            throw new BusinessException(400, "IMAGE_DIMENSIONS", "Anh toi da 24 megapixel, moi chieu toi da 10000 pixel; khong nhan anh dong.");
        var scale = Math.Min(1f, 1920f / Math.Max(info.Width, info.Height));
        var dimensions = codec.GetScaledDimensions(scale);
        var decodeInfo = new SKImageInfo(dimensions.Width, dimensions.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var decoded = new SKBitmap(decodeInfo);
        if (codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success) throw Invalid();
        ct.ThrowIfCancellationRequested();
        var w = decoded.Width; var h = decoded.Height;
        var swap = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var ow = swap ? h : w; var oh = swap ? w : h;
        var resize = Math.Min(1d, 1920d / Math.Max(ow, oh));
        using var output = new SKBitmap(new SKImageInfo(Math.Max(1, (int)Math.Round(ow * resize)), Math.Max(1, (int)Math.Round(oh * resize)), SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(output)) {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)output.Width / ow, (float)output.Height / oh);
            canvas.Concat(Orientation(codec.EncodedOrigin, w, h));
            using var paint = new SKPaint { IsAntialias = true };
            using var decodedImage = SKImage.FromBitmap(decoded);
            canvas.DrawImage(decodedImage, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None), paint);
        }
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Webp, 80);
        ct.ThrowIfCancellationRequested();
        if (encoded is null) throw Invalid();
        if (encoded.Size > 5 * 1024 * 1024) throw new BusinessException(400, "IMAGE_TOO_LARGE", "Anh sau toi uu van qua lon.");
        // Re-encoding pixels strips source EXIF/GPS/metadata.
        return Task.FromResult(encoded.ToArray());
    }
    private static SKMatrix Orientation(SKEncodedOrigin origin, int w, int h) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity
    };
    private static BusinessException Invalid() => new(400, "INVALID_IMAGE", "Khong giai ma duoc anh hoac anh khong hop le.");
}
