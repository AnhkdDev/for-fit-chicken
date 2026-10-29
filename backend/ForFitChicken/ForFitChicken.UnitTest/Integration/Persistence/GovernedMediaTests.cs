using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Infrastructure;
using ForFitChicken.Infrastructure.Identity;
using ForFitChicken.Infrastructure.Media;
using ForFitChicken.Infrastructure.Persistence;
using ForFitChicken.UnitTest.Unit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForFitChicken.UnitTest.Integration.Persistence;
[Trait("Category", "SqlServerIntegration")]
public sealed class GovernedMediaTests
{
    [SqlServerFact]
    public async Task FailedSnapshot_FailsClosedAcrossDatabase_UntilSuccessfulRetry()
    {
        await using var s = await Scenario.Create(); s.Cloud.FailList = true;
        await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<IMediaMaintenance>().ReconcileAsync(default)));
        Assert.False((await s.Status()).Initialized);
        Assert.Equal("MEDIA_RECONCILE_REQUIRED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p =>
            p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default)))).Code);
        s.Cloud.FailList = false;
        Assert.True((await s.Run(p => p.GetRequiredService<IMediaMaintenance>().ReconcileAsync(default))).NodeReady);
        Assert.True((await s.Status()).Initialized);
    }
    [SqlServerFact]
    public async Task UnusedAssetDelete_RequiresNoProductReference_AndKeepsQuotaIfCloudDeleteFails()
    {
        await using var s = await Scenario.Create();
        var asset = await s.Run(p => p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default));
        Assert.Single((await s.Run(p => p.GetRequiredService<CatalogService>().MediaAssetsAsync(true, 1, 20, default))).Items);
        var product = await s.Run(p => p.GetRequiredService<CatalogService>().SaveAsync(null, new ProductInput("Test", "test", null, null, null, null, null, null, true, false, false, 0, null), s.Actor, default));
        var image = await s.Run(p => p.GetRequiredService<CatalogService>().AddImageAsync(product.Id, new ImageInput(asset.Id, null, null, 0), product.Version!, s.Actor, default));
        Assert.Empty((await s.Run(p => p.GetRequiredService<CatalogService>().MediaAssetsAsync(true, 1, 20, default))).Items);
        s.Clock.Value = s.Clock.Value.AddDays(2);
        Assert.Equal("MEDIA_STILL_REFERENCED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<CatalogService>().DeleteUnusedAssetAsync(asset.Id, s.Actor, default)))).Code);
        product = await s.Run(p => p.GetRequiredService<CatalogService>().GetAsync(product.Id, false, default));
        await s.Run(p => p.GetRequiredService<CatalogService>().RemoveImageAsync(product.Id, image.Id, product.Version!, s.Actor, default));
        s.Cloud.FailDelete = true;
        var deleted = await s.Run(p => p.GetRequiredService<CatalogService>().DeleteUnusedAssetAsync(asset.Id, s.Actor, default));
        Assert.True(deleted.MetadataRemoved); Assert.False(deleted.ObjectDeleted); Assert.True(deleted.CleanupRequired);
        Assert.True((await s.Status()).ChargedBytes > 0); Assert.Single(s.Cloud.Objects);
        var orphan = Assert.Single(await s.Run(p => p.GetRequiredService<MediaPolicyService>().OrphansAsync(default)));
        s.Clock.Value = s.Clock.Value.AddMinutes(11); s.Cloud.FailDelete = false;
        await s.Run(async p => { await p.GetRequiredService<IMediaMaintenance>().DeleteOrphanAsync(orphan.Id, default); return true; });
        Assert.Empty(s.Cloud.Objects); Assert.Equal(0, (await s.Status()).ChargedBytes);
    }
    [SqlServerFact]
    public async Task NewNodeCannotUploadUntilItsOwnSnapshotCompletes()
    {
        await using var s = await Scenario.Create();
        await s.Run(p => { p.GetRequiredService<MediaWorkGates>().R2Ready = false; return Task.FromResult(true); });
        Assert.Equal("MEDIA_RECONCILE_REQUIRED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p =>
            p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default)))).Code);
        Assert.Empty(s.Cloud.Objects);
        Assert.True((await s.Run(p => p.GetRequiredService<IMediaMaintenance>().ReconcileAsync(default))).NodeReady);
    }
    [SqlServerFact]
    public async Task MetadataFailure_CompensatesCloudObjectWithFreshUow_AndReleasesQuotaOnlyAfterDelete()
    {
        await using var s = await Scenario.Create();
        await Assert.ThrowsAsync<DbUpdateException>(() => s.Run(p => p.GetRequiredService<CatalogService>().UploadAsync(
            new MemoryStream(ImageOptimizationTests.Png(8, 8)), long.MaxValue, default)));
        Assert.Empty(s.Cloud.Objects); Assert.Equal(0, (await s.Status()).ChargedBytes);
        await using var db = s.Database.Context(); Assert.False(await db.MediaAssets.AnyAsync());
        Assert.Equal("Deleted", (await db.MediaObjectReservations.SingleAsync()).State);
    }
    [SqlServerFact]
    public async Task ProductionUpload_OptimizesAndChargesActualBytes_ProtectsMetadataFromOrphanCleanup()
    {
        await using var s = await Scenario.Create();
        var asset = await s.Run(p => p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default));
        Assert.Equal("image/webp", asset.ContentType);
        var status = await s.Status(); Assert.Equal(asset.ByteSize, status.ChargedBytes); Assert.Equal(0, status.UncertainBytes);
        await using var db = s.Database.Context();
        var row = await db.MediaObjectReservations.SingleAsync(); Assert.Equal("Stored", row.State);
        s.Clock.Value = s.Clock.Value.AddDays(2);
        Assert.Equal("MEDIA_STILL_REFERENCED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(async p => {
            await p.GetRequiredService<IMediaMaintenance>().DeleteOrphanAsync(row.Id, default); return true;
        }))).Code);
        Assert.Single(s.Cloud.Objects);
    }
    [SqlServerFact]
    public async Task AmbiguousPut_KeepsQuota_AndVerifiedOrphanDeleteReturnsCapacity()
    {
        await using var s = await Scenario.Create(); s.Cloud.FailAfterPut = true;
        Assert.Equal("MEDIA_STORAGE_UNAVAILABLE", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p =>
            p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default)))).Code);
        var status = await s.Status(); Assert.True(status.ChargedBytes > 0); Assert.Equal(status.ChargedBytes, status.UncertainBytes);
        await using var db = s.Database.Context(); Assert.False(await db.MediaAssets.AnyAsync());
        var row = await db.MediaObjectReservations.SingleAsync(); Assert.Equal("Unknown", row.State);
        s.Clock.Value = s.Clock.Value.AddDays(2);
        await s.Run(async p => { await p.GetRequiredService<IMediaMaintenance>().DeleteOrphanAsync(row.Id, default); return true; });
        Assert.Empty(s.Cloud.Objects); Assert.Equal(0, (await s.Status()).ChargedBytes);
    }
    [SqlServerFact]
    public async Task PublicCache_CollapsesCloudReads_WhileUncachedReadsHitPersistedOperationBudget()
    {
        await using var s = await Scenario.Create(readBudget: 1);
        var asset = await s.Run(p => p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default));
        await using var db = s.Database.Context(); var key = (await db.MediaAssets.SingleAsync(x => x.Id == asset.Id)).StorageKey;
        async Task<bool> ReadPublic() => await s.Run(async p => {
            await using var stream = await p.GetRequiredService<GovernedMediaStorage>().OpenPublicAsync(key, default);
            Assert.True(stream.Length > 0); return true;
        });
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ReadPublic()));
        Assert.Equal(1, s.Cloud.Gets); Assert.Equal(1, (await s.Status()).ClassBThisMonth);
        Assert.Equal("MEDIA_OPERATION_BUDGET", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(async p => {
            await using var stream = await p.GetRequiredService<IMediaStorage>().OpenAsync(key, default); return true;
        }))).Code);
    }
    [SqlServerFact]
    public async Task PauseAndInputLimit_StopBeforeCloudPut()
    {
        await using var s = await Scenario.Create();
        await s.Run(p => p.GetRequiredService<MediaPolicyService>().PauseAsync(true, s.Actor, default));
        Assert.Equal("MEDIA_UPLOADS_PAUSED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p =>
            p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(ImageOptimizationTests.Png(8, 8)), s.Actor, default)))).Code);
        Assert.Equal("IMAGE_TOO_LARGE", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p =>
            p.GetRequiredService<CatalogService>().UploadAsync(new MemoryStream(new byte[5 * 1024 * 1024 + 1]), s.Actor, default)))).Code);
        Assert.Empty(s.Cloud.Objects);
    }
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Value = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Value; }
    private sealed class Scenario : IAsyncDisposable
    {
        public MigratedDatabaseFixture Database { get; } = new(); public FakeCloud Cloud { get; } = new();
        public Clock Clock { get; } = new(); private ServiceProvider _provider = null!; public long Actor;
        public static async Task<Scenario> Create(int readBudget = 1000)
        {
            var s = new Scenario(); await s.Database.InitializeAsync();
            var settings = new R2Options { Endpoint = "https://" + new string('a', 32) + ".r2.cloudflarestorage.com", BucketName = "test-media", AccessKeyId = "TEST_ONLY", SecretAccessKey = "TEST_ONLY" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:DefaultConnection"] = s.Database.ConnectionString, ["Media:Provider"] = "R2",
                ["Media:R2:Endpoint"] = settings.Endpoint, ["Media:R2:BucketName"] = settings.BucketName,
                ["Media:R2:AccessKeyId"] = settings.AccessKeyId, ["Media:R2:SecretAccessKey"] = settings.SecretAccessKey,
                ["Media:Security:MonthlyClassB"] = readBudget.ToString(), ["Telegram:Enabled"] = "false"
            }).Build();
            var services = new ServiceCollection().AddLogging(); services.AddSingleton<IConfiguration>(config);
            services.AddInfrastructure(config); services.AddSingleton<TimeProvider>(s.Clock);
            services.AddSingleton(new R2MediaStorage(s.Cloud, settings));
            s._provider = services.BuildServiceProvider();
            await using (var db = s.Database.Context()) {
                var user = new ApplicationUser { UserName = Guid.NewGuid().ToString("N"), DisplayName = "Disposable test user" };
                db.Users.Add(user); await db.SaveChangesAsync(); s.Actor = user.Id;
            }
            await s.Run(p => p.GetRequiredService<IMediaMaintenance>().ReconcileAsync(default)); return s;
        }
        public async Task<T> Run<T>(Func<IServiceProvider, Task<T>> action)
        { using var scope = _provider.CreateScope(); return await action(scope.ServiceProvider); }
        public Task<MediaStorageStatus> Status() => Run(p => p.GetRequiredService<MediaPolicyService>().StatusAsync(default));
        public async ValueTask DisposeAsync() { await _provider.DisposeAsync(); await Database.DisposeAsync(); }
    }
    private sealed class FakeCloud() : AmazonS3Client(new BasicAWSCredentials("TEST", "TEST"), new AmazonS3Config { ServiceURL = "https://example.invalid" })
    {
        public readonly Dictionary<string, byte[]> Objects = new(); public bool FailAfterPut; public bool FailDelete; public bool FailList; public int Gets;
        public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            using var copy = new MemoryStream(); await request.InputStream.CopyToAsync(copy, cancellationToken); Objects[request.Key] = copy.ToArray();
            if (FailAfterPut) throw new AmazonS3Exception("test ambiguous PUT") { StatusCode = HttpStatusCode.InternalServerError };
            return new PutObjectResponse();
        }
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Gets); return Task.FromResult(new GetObjectResponse { ResponseStream = new MemoryStream(Objects[request.Key]) }); }
        public override Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken cancellationToken = default)
        {
            if (FailList) throw new AmazonS3Exception("test list unavailable") { StatusCode = HttpStatusCode.ServiceUnavailable };
            return Task.FromResult(new ListObjectsV2Response { S3Objects = Objects.Select(x => new S3Object { Key = x.Key, Size = x.Value.Length }).ToList() });
        }
        public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default)
        {
            if (FailDelete) throw new AmazonS3Exception("test delete failure") { StatusCode = HttpStatusCode.InternalServerError };
            Objects.Remove(request.Key); return Task.FromResult(new DeleteObjectResponse());
        }
    }
}
