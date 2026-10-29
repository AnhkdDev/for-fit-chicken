using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Media;
using SkiaSharp;

namespace ForFitChicken.UnitTest.Unit;
public sealed class ImageOptimizationTests
{
    [Theory]
    [InlineData(2, 4, 2)]
    [InlineData(6, 2, 4)]
    [InlineData(8, 2, 4)]
    public async Task JpegOrientation_IsAppliedAndRemoved(byte orientation, int width, int height)
    {
        using var bitmap = new SKBitmap(4, 2); bitmap.Erase(SKColors.Orange);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        var jpeg = data.ToArray();
        byte[] exif = [0xff, 0xe1, 0, 34, 69, 120, 105, 102, 0, 0,
            0x49, 0x49, 42, 0, 8, 0, 0, 0, 1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0, orientation, 0, 0, 0, 0, 0, 0, 0];
        var source = jpeg.Take(2).Concat(exif).Concat(jpeg.Skip(2)).ToArray();
        var result = await new ImageOptimizer().OptimizeAsync(source, default);
        using var codec = SKCodec.Create(new MemoryStream(result));
        Assert.Equal(width, codec.Info.Width); Assert.Equal(height, codec.Info.Height); Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }
    [Fact]
    public async Task LargePng_BecomesBoundedWebp_WithNoUpscalingOfSmallImages()
    {
        var optimizer = new ImageOptimizer();
        var result = await optimizer.OptimizeAsync(Png(3000, 2000), default);
        using var codec = SKCodec.Create(new MemoryStream(result));
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal(1920, codec.Info.Width); Assert.Equal(1280, codec.Info.Height);
        var small = await optimizer.OptimizeAsync(Png(2, 3), default);
        using var smallCodec = SKCodec.Create(new MemoryStream(small)); Assert.Equal(2, smallCodec.Info.Width); Assert.Equal(3, smallCodec.Info.Height);
    }
    [Fact]
    public async Task InvalidOrExtremeImages_AreRejectedBeforeFullDecode()
    {
        var optimizer = new ImageOptimizer();
        Assert.Equal("INVALID_IMAGE", (await Assert.ThrowsAsync<BusinessException>(() => optimizer.OptimizeAsync("<svg/>"u8.ToArray(), default))).Code);
        Assert.Equal("IMAGE_DIMENSIONS", (await Assert.ThrowsAsync<BusinessException>(() => optimizer.OptimizeAsync(Png(10001, 1), default))).Code);
    }
    [Fact]
    public async Task PngSignatureAlone_DoesNotAcceptCorruptRaster()
    {
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz1kAAAAASUVORK5CYII=");
        Assert.Equal("INVALID_IMAGE", (await Assert.ThrowsAsync<BusinessException>(() => new ImageOptimizer().OptimizeAsync(bytes, default))).Code);
    }
    public static byte[] Png(int w, int h)
    {
        using var bitmap = new SKBitmap(w, h); bitmap.Erase(SKColors.Orange);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
    }
}
