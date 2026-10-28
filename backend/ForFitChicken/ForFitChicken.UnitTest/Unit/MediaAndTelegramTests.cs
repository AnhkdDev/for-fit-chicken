using System.Net;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Media;
using ForFitChicken.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ForFitChicken.UnitTest.Unit;

public sealed class MediaAndTelegramTests
{
    [Fact]
    public async Task Media_ValidatesMagicAndSize_StoresRandomKey_AndPreventsTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "ForFitChicken_MediaTests_" + Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Media:RootPath"] = root }).Build();
        var storage = new LocalMediaStorage(config);
        try
        {
            var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz1kAAAAASUVORK5CYII=");
            var asset = await storage.SaveAsync(new MemoryStream(image), default);
            Assert.Equal("image/png", asset.ContentType); Assert.EndsWith(".png", asset.StorageKey);
            await using (var stream = await storage.OpenAsync(asset.StorageKey, default))
            { Assert.Equal(image.Length, stream.Length); }
            await storage.DeleteAsync(asset.StorageKey, default);
            Assert.Equal("UNSUPPORTED_IMAGE", (await Assert.ThrowsAsync<BusinessException>(() => storage.SaveAsync(new MemoryStream("<svg/>"u8.ToArray()), default))).Code);
            Assert.Equal("IMAGE_TOO_LARGE", (await Assert.ThrowsAsync<BusinessException>(() => storage.SaveAsync(new MemoryStream(new byte[10 * 1024 * 1024 + 1]), default))).Code);
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenAsync("../appsettings.json", default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root); } // Non-recursive; only this test's empty GUID directory.
    }

    [Fact]
    public async Task TelegramSender_ParsesSuccessAndRetryAfter_AndStoresOnlySafeFailureCodes()
    {
        var handler = new StubHandler();
        using var sender = new TelegramSender(new HttpClient(handler), Options.Create(new TelegramOptions { BotToken = "0:TEST_ONLY" }));
        const string payload = "{\"OrderNumber\":\"FC-test\",\"ItemsSubtotal\":35000,\"PackageCount\":1}";
        handler.Response = "{\"ok\":true,\"result\":{\"message_id\":42}}";
        var success = await sender.SendAsync(-1001, payload, default); Assert.True(success.Success); Assert.Equal(42, success.MessageId);
        handler.Status = HttpStatusCode.TooManyRequests; handler.Response = "{\"ok\":false,\"parameters\":{\"retry_after\":90}}";
        var retry = await sender.SendAsync(-1001, payload, default); Assert.False(retry.PermanentFailure); Assert.Equal(90, retry.RetryAfterSeconds);
        handler.Status = HttpStatusCode.Unauthorized; handler.Response = "{\"ok\":false,\"description\":\"untrusted secret text\"}";
        var permanent = await sender.SendAsync(-1001, payload, default); Assert.True(permanent.PermanentFailure); Assert.Equal("TELEGRAM_HTTP_401", permanent.SafeError);
    }
    private sealed class StubHandler : HttpMessageHandler
    {
        public string Response { get; set; } = "{}";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Response) });
    }
}
