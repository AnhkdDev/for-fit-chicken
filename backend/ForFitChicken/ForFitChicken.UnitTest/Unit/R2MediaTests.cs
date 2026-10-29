using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace ForFitChicken.UnitTest.Unit;

public sealed class R2MediaTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz1kAAAAASUVORK5CYII=");
    private static R2Options Settings() => new() { Endpoint = "https://" + new string('a', 32) + ".r2.cloudflarestorage.com", BucketName = "test-media", AccessKeyId = "TEST_ONLY", SecretAccessKey = "TEST_ONLY" };
    [Fact]
    public async Task Roundtrip_UsesPrivateObjectKeysContentTypeAndR2CompatibleUpload()
    {
        using var fake = new FakeS3();
        using var storage = new R2MediaStorage(fake, Settings());
        var file = await storage.SaveAsync(new MemoryStream(Png), default);
        Assert.Matches(@"^r2/[a-f0-9]{32}\.png$", file.StorageKey);
        Assert.Equal("test-media", fake.Put!.BucketName);
        Assert.Equal("image/png", fake.Put.ContentType);
        Assert.False(fake.Put.UseChunkEncoding);
        Assert.True(fake.Put.DisablePayloadSigning);
        Assert.True(fake.Put.DisableDefaultChecksumValidation);
        await using var stream = await storage.OpenAsync(file.StorageKey, default);
        using var copy = new MemoryStream(); await stream.CopyToAsync(copy);
        Assert.Equal(Png, copy.ToArray());
        await storage.DeleteAsync(file.StorageKey, default);
        Assert.Equal(file.StorageKey, fake.DeletedKey);
    }
    [Fact]
    public async Task InvalidUploads_DoNotCallCloud()
    {
        using var fake = new FakeS3(); using var storage = new R2MediaStorage(fake, Settings());
        Assert.Equal("UNSUPPORTED_IMAGE", (await Assert.ThrowsAsync<BusinessException>(() => storage.SaveAsync(new MemoryStream("<svg/>"u8.ToArray()), default))).Code);
        Assert.Equal("IMAGE_TOO_LARGE", (await Assert.ThrowsAsync<BusinessException>(() => storage.SaveAsync(new MemoryStream(new byte[10 * 1024 * 1024 + 1]), default))).Code);
        Assert.Null(fake.Put);
        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.OpenAsync("r2/../../secret", default));
    }
    [Fact]
    public async Task MissingObjectsAndCloudFailures_ReturnSafeErrors()
    {
        using var fake = new FakeS3(); using var storage = new R2MediaStorage(fake, Settings());
        fake.Error = new AmazonS3Exception("sensitive error") { StatusCode = HttpStatusCode.NotFound };
        var key = "r2/" + new string('a', 32) + ".png";
        Assert.Equal("MEDIA_MISSING", (await Assert.ThrowsAsync<BusinessException>(() => storage.OpenAsync(key, default))).Code);
        fake.Error = new AmazonS3Exception("sensitive error") { StatusCode = HttpStatusCode.Forbidden };
        var error = await Assert.ThrowsAsync<BusinessException>(() => storage.SaveAsync(new MemoryStream(Png), default));
        Assert.Equal("MEDIA_STORAGE_UNAVAILABLE", error.Code); Assert.DoesNotContain("sensitive", error.Message);
    }
    [Fact]
    public async Task ProviderSwitch_RetainsAccessToExistingLocalFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "ForFitChicken_R2Tests_" + Guid.NewGuid().ToString("N"));
        var local = new LocalMediaStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Media:RootPath"] = root }).Build());
        using var fake = new FakeS3(); using var remote = new R2MediaStorage(fake, Settings());
        var file = await local.SaveAsync(new MemoryStream(Png), default);
        try {
            var router = new ConfiguredMediaStorage(local, remote, Options.Create(new MediaOptions { Provider = "R2", R2 = Settings() }));
            await using (var old = await router.OpenAsync(file.StorageKey, default)) { Assert.Equal(Png.Length, old.Length); }
            var cloud = await router.SaveAsync(new MemoryStream(Png), default);
            Assert.StartsWith("r2/", cloud.StorageKey);
            var switchedBack = new ConfiguredMediaStorage(local, remote, Options.Create(new MediaOptions { Provider = "Local", R2 = Settings() }));
            await using var downloaded = await switchedBack.OpenAsync(cloud.StorageKey, default);
            Assert.Equal(Png.Length, downloaded.Length);
        } finally { await local.DeleteAsync(file.StorageKey, default); Directory.Delete(root); }
    }
    [Fact]
    public void Configuration_RejectsUnknownProvidersIncompleteR2AndNonR2Endpoints()
    {
        Assert.True(new MediaOptions().IsValid());
        Assert.False(new MediaOptions { Provider = "Other" }.IsValid());
        Assert.False(new MediaOptions { Provider = "R2" }.IsValid());
        var settings = Settings(); Assert.True(new MediaOptions { Provider = "R2", R2 = settings }.IsValid());
        settings.Endpoint = "https://localhost"; Assert.False(settings.IsValid());
    }
    private sealed class FakeS3() : AmazonS3Client(new BasicAWSCredentials("TEST", "TEST"), new AmazonS3Config { ServiceURL = "https://example.invalid" })
    {
        public PutObjectRequest? Put;
        public string? DeletedKey;
        public Exception? Error;
        private byte[] _bytes = [];
        public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            if (Error is not null) throw Error;
            Put = request;
            using var copy = new MemoryStream(); await request.InputStream.CopyToAsync(copy, cancellationToken); _bytes = copy.ToArray();
            return new PutObjectResponse();
        }
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default) =>
            Error is null ? Task.FromResult(new GetObjectResponse { ResponseStream = new MemoryStream(_bytes) }) : Task.FromException<GetObjectResponse>(Error);
        public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default)
        { DeletedKey = request.Key; return Task.FromResult(new DeleteObjectResponse()); }
    }
}
