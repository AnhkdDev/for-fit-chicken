using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ForFitChicken.UnitTest.Integration.Persistence;
[Trait("Category", "SqlServerIntegration")]
public sealed class MediaPolicyTests
{
    [SqlServerFact]
    public async Task ConcurrentReservations_NeverExceedDurableQuota_AndRequireInitialSnapshot()
    {
        await using var s = await Scenario.Create();
        var missing = await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.ReserveAsync("r2/missing.webp", 60, 1, default)));
        Assert.Equal("MEDIA_RECONCILE_REQUIRED", missing.Code);
        await s.Snapshot([]);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i => {
            try { await s.Run(p => p.ReserveAsync("r2/" + i + ".webp", 60, 1, default)); return true; }
            catch (BusinessException) { return false; }
        }));
        Assert.Single(results, x => x);
        var status = await s.Run(p => p.StatusAsync(default));
        Assert.Equal(60, status.ChargedBytes); Assert.Equal(1, status.ClassAThisMonth);
    }
    [SqlServerFact]
    public async Task UnknownUpload_RemainsChargedAcrossRestartMonthAndAbsentSnapshot_UntilConfirmedDeletion()
    {
        await using var s = await Scenario.Create(); await s.Snapshot([]);
        const string key = "r2/unknown.webp";
        await s.Run(p => p.ReserveAsync(key, 60, 1, default));
        await s.Run(p => p.FinishUploadAsync(key, false, default));
        s.Clock.Value = s.Clock.Value.AddDays(32);
        await s.Snapshot([]);
        var status = await s.Run(p => p.StatusAsync(default));
        Assert.Equal(60, status.ChargedBytes); Assert.Equal(60, status.UncertainBytes);
        await s.Run(p => p.BeginDeleteAsync(key, true, default));
        Assert.Equal(60, (await s.Run(p => p.StatusAsync(default))).ChargedBytes);
        await s.Run(p => p.CompleteDeleteAsync(key, default));
        Assert.Equal(0, (await s.Run(p => p.StatusAsync(default))).ChargedBytes);
    }
    [SqlServerFact]
    public async Task Snapshot_CountsUnmanagedObjects_RejectsBucketSwitch_AndBlocksWhenFull()
    {
        await using var s = await Scenario.Create();
        await s.Snapshot([new("unmanaged-file.bin", 99)]);
        var error = await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.ReserveAsync("r2/new.webp", 2, 1, default)));
        Assert.Equal("MEDIA_STORAGE_QUOTA", error.Code);
        Assert.Equal(99, (await s.Run(p => p.StatusAsync(default))).ChargedBytes);
        s.Settings.Value = s.Settings.Value with { ScopeHash = "different-bucket" };
        Assert.Equal("MEDIA_SCOPE_CHANGED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.StatusAsync(default)))).Code);
    }
    [SqlServerFact]
    public async Task ReconcileLease_FencesOldScan_AndBlocksUploadsWithoutHoldingHttpTransaction()
    {
        await using var s = await Scenario.Create(); await s.Snapshot([]);
        var lease = await s.Run(p => p.BeginReconcileAsync(default));
        Assert.Equal("MEDIA_RECONCILING", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.ReserveAsync("r2/x.webp", 1, 1, default)))).Code);
        s.Clock.Value = s.Clock.Value.AddMinutes(11);
        var newer = await s.Run(p => p.BeginReconcileAsync(default));
        Assert.Equal("MEDIA_RECONCILE_STALE", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.CompleteReconcileAsync(lease, [], default)))).Code);
        await s.Run(p => p.CompleteReconcileAsync(newer, [], default));
    }
    [SqlServerFact]
    public async Task DailyAndMonthlyBudgets_AreSharedAcrossScopes_AndResetOnlyTheirOwnPeriod()
    {
        await using var s = await Scenario.Create(); await s.Snapshot([]);
        await s.Run(p => p.AdmitAsync(1, 10, default)); await s.Run(p => p.AdmitAsync(1, 10, default));
        Assert.Equal("MEDIA_DAILY_LIMIT", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.AdmitAsync(1, 10, default)))).Code);
        await s.Run(p => p.CountReadAsync(default)); await s.Run(p => p.CountReadAsync(default));
        Assert.Equal("MEDIA_OPERATION_BUDGET", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.CountReadAsync(default)))).Code);
        s.Clock.Value = s.Clock.Value.AddDays(1);
        await s.Run(p => p.AdmitAsync(1, 10, default));
        Assert.Equal("MEDIA_OPERATION_BUDGET", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.CountReadAsync(default)))).Code);
        s.Clock.Value = s.Clock.Value.AddMonths(1);
        await s.Run(p => p.CountReadAsync(default));
        Assert.Equal(1, (await s.Run(p => p.StatusAsync(default))).ClassBThisMonth);
    }
    [SqlServerFact]
    public async Task ExpiredReservation_StillConsumesBytes_AndStoredQuotaDoesNotResetNextMonth()
    {
        await using var s = await Scenario.Create(); await s.Snapshot([]);
        await s.Run(p => p.ReserveAsync("r2/stored.webp", 60, 1, default));
        await s.Run(p => p.FinishUploadAsync("r2/stored.webp", true, default));
        s.Clock.Value = s.Clock.Value.AddMonths(1);
        await s.Snapshot([new("r2/stored.webp", 60)]);
        Assert.Equal("MEDIA_STORAGE_QUOTA", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.ReserveAsync("r2/new.webp", 41, 1, default)))).Code);
        Assert.Equal(60, (await s.Run(p => p.StatusAsync(default))).ChargedBytes);
    }
    [SqlServerFact]
    public async Task ActiveUploadBlocksScan_AndGracePeriodPreventsPrematureOrphanDelete()
    {
        await using var s = await Scenario.Create(); await s.Snapshot([]);
        await s.Run(p => p.ReserveAsync("r2/pending.webp", 10, 1, default));
        Assert.Equal("MEDIA_UPLOAD_BUSY", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.BeginReconcileAsync(default)))).Code);
        await s.Run(p => p.FinishUploadAsync("r2/pending.webp", false, default));
        Assert.Equal("MEDIA_ORPHAN_TOO_NEW", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.BeginDeleteAsync("r2/pending.webp", true, default)))).Code);
        Assert.Equal(10, (await s.Run(p => p.StatusAsync(default))).ChargedBytes);
    }
    private sealed class MutableSettings : IMediaPolicySettings
    {
        public MediaPolicySettings Value = new(true, "test-bucket", MaxStoredBytes: 100, DailyUploads: 2, DailyInputBytes: 20, PerUserDailyUploads: 2, MonthlyClassA: 10, MonthlyClassB: 2);
        public MediaPolicySettings Current => Value;
    }
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Value = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Value; }
    private sealed class Scenario : IAsyncDisposable
    {
        private readonly MigratedDatabaseFixture _database = new();
        private ServiceProvider _provider = null!;
        public MutableSettings Settings { get; } = new(); public Clock Clock { get; } = new();
        public static async Task<Scenario> Create()
        {
            var s = new Scenario(); await s._database.InitializeAsync();
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<ForFitChickenDbContext>(o => o.UseSqlServer(s._database.ConnectionString));
            services.AddScoped<IUnitOfWork, EfUnitOfWork>(); services.AddScoped<ICommerceStore, CommerceStore>();
            services.AddScoped<MediaPolicyService>(); services.AddSingleton<TimeProvider>(s.Clock); services.AddSingleton<IMediaPolicySettings>(s.Settings);
            s._provider = services.BuildServiceProvider(); return s;
        }
        public async Task<T> Run<T>(Func<MediaPolicyService, Task<T>> action)
        { using var scope = _provider.CreateScope(); return await action(scope.ServiceProvider.GetRequiredService<MediaPolicyService>()); }
        public async Task Snapshot(CloudObject[] objects)
        { var lease = await Run(p => p.BeginReconcileAsync(default)); await Run(p => p.CompleteReconcileAsync(lease, objects, default)); }
        public async ValueTask DisposeAsync() { await _provider.DisposeAsync(); await _database.DisposeAsync(); }
    }
}
