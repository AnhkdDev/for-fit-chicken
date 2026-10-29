using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;

namespace ForFitChicken.Application.Commerce;

// All mutations use the existing SQL business lock. External storage I/O belongs to adapters, never here.
public sealed class MediaPolicyService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock, IMediaPolicySettings settings)
    : BusinessService(store, unit, clock)
{
    private MediaPolicySettings Limits => settings.Current;
    private string Month => Now.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
    private string Day => Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
    private async Task<MediaStorageState> State(CancellationToken ct)
    {
        Check(!string.IsNullOrWhiteSpace(Limits.ScopeHash), "MEDIA_R2_NOT_CONFIGURED", "Chưa cấu hình đủ R2; không tạo bộ đếm cho bucket rỗng.", 503);
        var state = await Store.FindAsync<MediaStorageState>(1, ct);
        if (state is null) { state = new() { ScopeHash = Limits.ScopeHash }; Store.Add(state); }
        Check(state.ScopeHash == Limits.ScopeHash, "MEDIA_SCOPE_CHANGED", "Bucket/endpoint khác bộ đếm hiện tại; cần quy trình chuyển dữ liệu.", 503);
        return state;
    }
    private void NoScan(MediaStorageState state) => Check(state.ReconcileUntilUtc is null || state.ReconcileUntilUtc <= Now,
        "MEDIA_RECONCILING", "Đang đối soát kho ảnh, thử lại sau.", 503);
    private async Task<MediaUsageCounter> Counter(string id, CancellationToken ct)
    {
        var item = (await Store.ListAsync(Store.Query<MediaUsageCounter>().Where(x => x.Id == id), ct)).SingleOrDefault();
        if (item is null) { item = new() { Id = id }; Store.Add(item); }
        return item;
    }
    private async Task Spend(string kind, long limit, CancellationToken ct)
    {
        var counter = await Counter(kind + ":" + Month, ct);
        Check(counter.Count < limit, "MEDIA_OPERATION_BUDGET", "Đã đạt ngân sách thao tác kho ảnh trong tháng.", 503);
        counter.Count++;
    }
    public Task<bool> AdmitAsync(long actor, long inputBytes, CancellationToken ct) => Write(async () =>
    {
        Check(inputBytes is > 0 and <= 5 * 1024 * 1024, "IMAGE_TOO_LARGE", "Ảnh đầu vào tối đa 5 MiB.", 400);
        // Also applies to Local uploads; a failed image decode still consumes daily admission.
        var all = await Counter("uploads:" + Day, ct);
        var user = await Counter("user:" + actor + ":" + Day, ct);
        Check(all.Count < Limits.DailyUploads && all.Bytes + inputBytes <= Limits.DailyInputBytes && user.Count < Limits.PerUserDailyUploads,
            "MEDIA_DAILY_LIMIT", "Đã đạt hạn mức upload hôm nay.", 429);
        if (Limits.IsR2) { var state = await State(ct); NoScan(state); Check(!state.UploadsPaused, "MEDIA_UPLOADS_PAUSED", "Upload ảnh đang tạm dừng.", 503); }
        all.Count++; all.Bytes += inputBytes; user.Count++; user.Bytes += inputBytes;
        return true;
    }, ct);
    public Task<bool> ReserveAsync(string key, long bytes, long actor, CancellationToken ct) => Write(async () =>
    {
        Check(bytes is > 0 and <= 5 * 1024 * 1024, "INVALID_MEDIA_SIZE", "Dung lượng ảnh không hợp lệ.", 400);
        var state = await State(ct); NoScan(state);
        Check(!state.UploadsPaused, "MEDIA_UPLOADS_PAUSED", "Upload ảnh đang tạm dừng.", 503);
        Check(state.Initialized && state.LastReconciledAtUtc >= Now.AddHours(-Limits.ReconcileMaxAgeHours),
            "MEDIA_RECONCILE_REQUIRED", "Cần đối soát bucket trước khi upload (định kỳ tối đa 24 giờ).", 503);
        Check(bytes <= Limits.MaxStoredBytes - state.ChargedBytes, "MEDIA_STORAGE_QUOTA", "Kho ảnh đã đạt giới hạn dung lượng.", 409);
        var active = await Store.CountAsync(Store.Query<MediaObjectReservation>().Where(x => x.ActiveUntilUtc > Now), ct);
        Check(active < Limits.ActiveUploads, "MEDIA_UPLOAD_BUSY", "Có quá nhiều upload đang xử lý.", 429);
        await Spend("A", Limits.MonthlyClassA, ct); // One reserved PUT, never refunded; SDK retries disabled.
        state.ChargedBytes += bytes;
        Store.Add(new MediaObjectReservation { StorageKey = key, ByteSize = bytes, ActorUserId = actor,
            CreatedAtUtc = Now, UpdatedAtUtc = Now, ActiveUntilUtc = Now.AddMinutes(10) });
        return true;
    }, ct);
    public Task<bool> FinishUploadAsync(string key, bool knownSuccess, CancellationToken ct) => Write(async () =>
    {
        await State(ct);
        var row = await Object(key, ct);
        Check(row.State is "Reserved" or "Unknown", "MEDIA_RESERVATION_STATE", "Trạng thái upload đã thay đổi.");
        row.State = knownSuccess ? "Stored" : "Unknown"; row.ActiveUntilUtc = null; row.UpdatedAtUtc = Now;
        return true; // Unknown keeps ALL charged bytes, including after restart.
    }, ct);
    private async Task<MediaObjectReservation> Object(string key, CancellationToken ct) =>
        (await Store.ListAsync(Store.Query<MediaObjectReservation>().Where(x => x.StorageKey == key), ct)).SingleOrDefault()
        ?? throw new BusinessException(409, "MEDIA_RESERVATION_MISSING", "Cần đối soát object trước.");
    public Task<bool> CountReadAsync(CancellationToken ct) => Write(async () => { await State(ct); await Spend("B", Limits.MonthlyClassB, ct); return true; }, ct);
    public Task<bool> CountListAsync(CancellationToken ct) => Write(async () => { await State(ct); await Spend("A", Limits.MonthlyClassA, ct); return true; }, ct);
    public Task<Guid> BeginReconcileAsync(CancellationToken ct) => Write(async () =>
    {
        var state = await State(ct); NoScan(state);
        Check(!await Store.AnyAsync(Store.Query<MediaObjectReservation>().Where(x => x.ActiveUntilUtc > Now), ct),
            "MEDIA_UPLOAD_BUSY", "Chờ các thao tác ảnh đang chạy kết thúc.", 409);
        var lease = Guid.NewGuid(); state.ReconcileToken = lease; state.ReconcileUntilUtc = Now.AddMinutes(10); return lease;
    }, ct);
    public Task<bool> AbortReconcileAsync(Guid lease, CancellationToken ct) => Write(async () =>
    {
        var state = await State(ct);
        if (state.ReconcileToken == lease) { state.ReconcileToken = null; state.ReconcileUntilUtc = null; state.Initialized = false; }
        return true;
    }, ct);
    public Task<bool> CompleteReconcileAsync(Guid lease, CloudObject[] objects, CancellationToken ct) => Write(async () =>
    {
        var state = await State(ct);
        Check(state.ReconcileToken == lease && state.ReconcileUntilUtc > Now, "MEDIA_RECONCILE_STALE", "Lượt đối soát đã hết hạn.");
        Check(objects.All(x => x.Bytes >= 0 && x.Key.Length <= 450) && objects.Select(x => x.Key).Distinct().Count() == objects.Length,
            "MEDIA_SNAPSHOT_INVALID", "Danh sách object không hợp lệ.", 503);
        var rows = await Store.ListAsync(Store.Query<MediaObjectReservation>(), ct);
        var byKey = rows.ToDictionary(x => x.StorageKey);
        var observed = objects.ToDictionary(x => x.Key);
        long total = 0;
        foreach (var obj in objects)
        {
            total = checked(total + obj.Bytes);
            if (!byKey.TryGetValue(obj.Key, out var row)) {
                row = new() { StorageKey = obj.Key, CreatedAtUtc = Now }; Store.Add(row);
            }
            row.ByteSize = obj.Bytes; row.State = "Stored"; row.ActiveUntilUtc = null; row.UpdatedAtUtc = Now;
        }
        foreach (var row in rows.Where(x => !observed.ContainsKey(x.StorageKey)))
        {
            if (row.State is "Reserved" or "Unknown" or "Deleting") {
                row.State = "Unknown"; row.ActiveUntilUtc = null; total = checked(total + row.ByteSize);
            } else { row.State = "Deleted"; row.ActiveUntilUtc = null; }
        }
        state.ChargedBytes = total; state.Initialized = true; state.LastReconciledAtUtc = Now;
        state.ReconcileToken = null; state.ReconcileUntilUtc = null;
        return true;
    }, ct);
    public Task<bool> BeginDeleteAsync(string key, bool graceRequired, CancellationToken ct) => Write(async () =>
    {
        var state = await State(ct); NoScan(state);
        var row = await Object(key, ct);
        Check(!await Store.AnyAsync(Store.Query<MediaAsset>().Where(x => x.StorageKey == key), ct),
            "MEDIA_STILL_REFERENCED", "Metadata ảnh còn tồn tại; không được xóa object trực tiếp.");
        Check(row.State != "Deleted" && (row.ActiveUntilUtc is null || row.ActiveUntilUtc <= Now || !graceRequired && row.State == "Stored"),
            "MEDIA_OBJECT_BUSY", "Object đã xóa hoặc đang xử lý.");
        if (graceRequired) Check(row.CreatedAtUtc <= Now.AddHours(-Limits.OrphanGraceHours), "MEDIA_ORPHAN_TOO_NEW", "Chưa đủ thời gian chờ dọn orphan.");
        row.State = "Deleting"; row.ActiveUntilUtc = Now.AddMinutes(10); row.UpdatedAtUtc = Now;
        return true;
    }, ct);
    public Task<bool> CompleteDeleteAsync(string key, CancellationToken ct) => Write(async () =>
    {
        var state = await State(ct); var row = await Object(key, ct);
        if (row.State == "Deleted") return true;
        Check(row.State == "Deleting", "MEDIA_RESERVATION_STATE", "Object không trong trạng thái xóa.");
        state.ChargedBytes = checked(state.ChargedBytes - row.ByteSize); row.State = "Deleted";
        row.ActiveUntilUtc = null; row.UpdatedAtUtc = Now; return true;
    }, ct);
    public Task<bool> PauseAsync(bool paused, long actor, CancellationToken ct) => Write(async () =>
    { Check(Limits.IsR2, "MEDIA_NOT_R2", "Chọn provider R2 trước khi dùng công tắc kho ảnh."); var state = await State(ct); state.UploadsPaused = paused; Audit(actor, paused ? "MediaUploadsPaused" : "MediaUploadsResumed", nameof(MediaStorageState), 1); return true; }, ct);
    public async Task<MediaStorageStatus> StatusAsync(CancellationToken ct)
    {
        var state = await Store.FindAsync<MediaStorageState>(1, ct);
        if (state is null) return new(0, Limits.MaxStoredBytes, 0, 0, false, false, null, 0, 0, ["RECONCILE_REQUIRED"]);
        Check(state.ScopeHash == Limits.ScopeHash, "MEDIA_SCOPE_CHANGED", "Bucket/endpoint không khớp bộ đếm.", 503);
        var uncertain = await Store.ListAsync(Store.Query<MediaObjectReservation>().Where(x => x.State == "Reserved" || x.State == "Unknown" || x.State == "Deleting"), ct);
        var counters = await Store.ListAsync(Store.Query<MediaUsageCounter>().Where(x => x.Id == "A:" + Month || x.Id == "B:" + Month), ct);
        var a = counters.FirstOrDefault(x => x.Id.StartsWith("A:"))?.Count ?? 0;
        var b = counters.FirstOrDefault(x => x.Id.StartsWith("B:"))?.Count ?? 0;
        var warnings = new List<string>();
        if (!state.Initialized || state.LastReconciledAtUtc < Now.AddHours(-Limits.ReconcileMaxAgeHours)) warnings.Add("RECONCILE_REQUIRED");
        if (state.ChargedBytes >= Limits.MaxStoredBytes * .8m) warnings.Add("STORAGE_NEAR_LIMIT");
        if (a >= Limits.MonthlyClassA * .8m || b >= Limits.MonthlyClassB * .8m) warnings.Add("OPERATIONS_NEAR_LIMIT");
        if (state.UploadsPaused) warnings.Add("UPLOADS_PAUSED");
        if (uncertain.Length != 0) warnings.Add("RESERVATIONS_REQUIRE_REVIEW");
        return new(state.ChargedBytes, Limits.MaxStoredBytes, uncertain.Sum(x => x.ByteSize), uncertain.Count(x => x.ActiveUntilUtc > Now),
            state.Initialized, state.UploadsPaused, state.LastReconciledAtUtc, a, b, warnings.ToArray());
    }
    public async Task<MediaObjectStatus[]> OrphansAsync(CancellationToken ct)
    {
        var assets = Store.Query<MediaAsset>();
        return (await Store.ListAsync(Store.Query<MediaObjectReservation>().Where(x => x.State != "Deleted" &&
            !assets.Any(a => a.StorageKey == x.StorageKey)).OrderBy(x => x.CreatedAtUtc).Take(100), ct))
            .Select(x => new MediaObjectStatus(x.Id, x.StorageKey, x.ByteSize, x.State, x.CreatedAtUtc)).ToArray();
    }
    public async Task<string> OrphanKeyAsync(long id, CancellationToken ct) => (await Need<MediaObjectReservation>(id, ct)).StorageKey;
}
