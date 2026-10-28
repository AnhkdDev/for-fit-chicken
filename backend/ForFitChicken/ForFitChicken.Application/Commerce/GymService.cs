using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed class GymService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock, InventoryService inventory) : BusinessService(store, unit, clock)
{
    public async Task<Page<GymView>> ListAsync(bool publicOnly, int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); var query = Store.Query<Gym>().Where(x => !publicOnly || (x.IsActive && x.IsPublished));
        var rows = await Store.ListAsync(query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size), token);
        var result = new List<GymView>();
        foreach (var row in rows) result.Add(await View(row, publicOnly, token));
        return new(result.ToArray(), await Store.CountAsync(query, token), page, size);
    }
    public Task<GymView> SaveAsync(long? id, GymInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var gym = id is null ? new Gym { CreatedAtUtc = Now } : await Need<Gym>(id.Value, token);
        if (id is not null) { Version(gym.Version, input.Version); Check(gym.GymCode == input.GymCode.ToUpperInvariant(), "CODE_IMMUTABLE", "Không thay mã gym đã tạo."); }
        Check(!await Store.AnyAsync(Store.Query<Gym>().Where(x => x.GymCode == input.GymCode && x.Id != gym.Id), token), "DUPLICATE_GYM", "Mã gym đã tồn tại.");
        if (input.CoverAssetId is not null) await Need<MediaAsset>(input.CoverAssetId.Value, token);
        Check(!input.IsPublished || input.IsActive, "INACTIVE_GYM", "Gym ngừng hoạt động không được công bố.");
        gym.GymCode = input.GymCode.ToUpperInvariant(); gym.Name = input.Name.Trim(); gym.PublicAddress = input.PublicAddress.Trim();
        gym.PublicPhone = input.PublicPhone; gym.PublicDescription = input.PublicDescription; gym.InternalContactName = input.InternalContactName;
        gym.InternalPhone = input.InternalPhone; gym.InternalNotes = input.InternalNotes; gym.CoverAssetId = input.CoverAssetId;
        gym.IsActive = input.IsActive; gym.IsPublished = input.IsPublished; gym.UpdatedAtUtc = Now;
        if (id is null) { Store.Add(gym); await Unit.SaveChangesAsync(token); }
        var locations = await Store.ListAsync(Store.Query<StockLocation>().Where(x => x.GymId == gym.Id), token);
        var location = locations.SingleOrDefault();
        if (location is null)
        { location = new StockLocation { LocationCode = "GYM-" + gym.Id, GymId = gym.Id, Kind = StockLocationKind.Gym, CreatedAtUtc = Now }; Store.Add(location); }
        location.Name = gym.Name; location.IsActive = gym.IsActive; location.UpdatedAtUtc = Now;
        await Unit.SaveChangesAsync(token); Audit(actor, "GymSaved", nameof(Gym), gym.Id); return await View(gym, false, token);
    }, token);

    public async Task<GymStockView> StockAsync(long gymId, CancellationToken token)
    {
        await Need<Gym>(gymId, token); var location = await Location(gymId, token);
        var last = (await Store.ListAsync(Store.Query<GymStockCount>().Where(x => x.GymId == gymId && x.Status == PostingStatus.Posted).OrderByDescending(x => x.CountedAtUtc).Take(1), token)).SingleOrDefault();
        var lines = last is null ? [] : await Store.ListAsync(Store.Query<GymStockCountLine>().Where(x => x.CountId == last.Id).OrderBy(x => x.BatchId), token);
        var buckets = await inventory.GymBuckets(location, token);
        var book = buckets.Keys.Select(x => x.Batch).Distinct().Order().Select(batch => new GymBookLine(batch,
            buckets.GetValueOrDefault((batch, StockCondition.Good)), buckets.GetValueOrDefault((batch, StockCondition.Quarantine)), buckets.GetValueOrDefault((batch, StockCondition.Damaged)))).ToArray();
        return new(gymId, last?.CountedAtUtc, false, Revision(location.Version), lines.Select(Line).ToArray(), book);
    }

    public Task<CountView> CountAsync(long gymId, CountInput input, long actor, bool owner, CancellationToken token) => Write(async () =>
    {
        Validate(input); foreach (var line in input.Lines) Validate(line);
        Check(input.Lines.Select(x => x.BatchId).Distinct().Count() == input.Lines.Length, "DUPLICATE_BATCH", "Một lô chỉ có một dòng kiểm đếm.", 400);
        await Need<Gym>(gymId, token); var location = await Location(gymId, token); Version(location.Version, input.Version);
        var previous = (await Store.ListAsync(Store.Query<GymStockCount>().Where(x => x.GymId == gymId && x.Status == PostingStatus.Posted).OrderByDescending(x => x.CountedAtUtc).Take(1), token)).SingleOrDefault();
        Check(previous is not null || owner, "OWNER_REQUIRED", "Kiểm đếm mở đầu cần Owner.", 403);
        var expected = await inventory.GymBuckets(location, token); var inputIds = input.Lines.Select(x => x.BatchId).ToHashSet();
        Check(expected.Keys.All(x => inputIds.Contains(x.Batch)), "COUNT_LINES_MISSING", "Phải nhập rõ mọi lô theo sổ, kể cả số thực tế bằng 0.");
        var time = await inventory.NextInventoryTime(token);
        var count = new GymStockCount { CountNumber = "GC-" + Guid.NewGuid().ToString("N"), GymId = gymId, PreviousCountId = previous?.Id,
            Kind = previous is null ? StockCountKind.OpeningBaseline : StockCountKind.Regular, Status = PostingStatus.Posted,
            CountedAtUtc = time, CountedByUserId = actor, CreatedAtUtc = Now, CreatedByUserId = actor,
            PostedAtUtc = Now, PostedByUserId = actor, SourceFingerprint = input.Version, Note = input.Note };
        Store.Add(count); await Unit.SaveChangesAsync(token);
        foreach (var line in input.Lines)
        {
            await Need<ProductBatch>(line.BatchId, token);
            Store.Add(new GymStockCountLine { CountId = count.Id, BatchId = line.BatchId,
                ExpectedGood = previous is null ? null : expected.GetValueOrDefault((line.BatchId, StockCondition.Good)),
                ExpectedQuarantine = previous is null ? null : expected.GetValueOrDefault((line.BatchId, StockCondition.Quarantine)),
                ExpectedDamaged = previous is null ? null : expected.GetValueOrDefault((line.BatchId, StockCondition.Damaged)),
                ActualGood = line.ActualGood, ActualQuarantine = line.ActualQuarantine, ActualDamaged = line.ActualDamaged, Note = line.Note });
        }
        location.UpdatedAtUtc = time; await Unit.SaveChangesAsync(token); Audit(actor, "GymCountPosted", nameof(GymStockCount), count.Id);
        return await CountView(count, token);
    }, token);

    public async Task<Page<CountView>> CountsAsync(long gymId, int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); var query = Store.Query<GymStockCount>().Where(x => x.GymId == gymId); var result = new List<CountView>();
        foreach (var row in await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size), token)) result.Add(await CountView(row, token));
        return new(result.ToArray(), await Store.CountAsync(query, token), page, size);
    }

    public Task<ResolutionView> ResolveAsync(long lineId, ResolutionInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); Check(Enum.IsDefined(input.Direction) && Enum.IsDefined(input.Category), "INVALID_RESOLUTION", "Loại xác minh không hợp lệ.", 400);
        Check(input.OperationKey != Guid.Empty, "IDEMPOTENCY_KEY_REQUIRED", "OperationKey bắt buộc khi xác minh.", 400);
        static string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
        var scope = Hash("GymResolution:" + actor); var key = input.OperationKey.ToString("N");
        var payloadHash = Hash(lineId + ":" + System.Text.Json.JsonSerializer.Serialize(input));
        var replay = (await Store.ListAsync(Store.Query<IdempotencyRequest>().Where(x => x.ScopeHash == scope && x.Operation == "GymResolution" && x.RequestKey == key), token)).SingleOrDefault();
        if (replay is not null)
        {
            Check(replay.RequestHash == payloadHash, "IDEMPOTENCY_CONFLICT", "OperationKey đã dùng cho nội dung khác.");
            return System.Text.Json.JsonSerializer.Deserialize<ResolutionView>(replay.SafeResponseJson!)!;
        }
        var line = await Need<GymStockCountLine>(lineId, token); Version(line.Version, input.Version);
        var count = await Need<GymStockCount>(line.CountId, token);
        Check(count.Status == PostingStatus.Posted && count.Kind == StockCountKind.Regular, "BASELINE_HAS_NO_VARIANCE", "Chỉ xác minh chênh lệch của kỳ Regular đã chốt.");
        var missing = Math.Max(0, line.ExpectedGood!.Value - line.ActualGood!.Value) + Math.Max(0, line.ExpectedQuarantine!.Value - line.ActualQuarantine!.Value) + Math.Max(0, line.ExpectedDamaged!.Value - line.ActualDamaged!.Value);
        var surplus = Math.Max(0, line.ActualGood.Value - line.ExpectedGood.Value) + Math.Max(0, line.ActualQuarantine.Value - line.ExpectedQuarantine.Value) + Math.Max(0, line.ActualDamaged.Value - line.ExpectedDamaged.Value);
        var existing = await Store.ListAsync(Store.Query<GymVarianceResolution>().Where(x => x.CountLineId == lineId && x.VoidedAtUtc == null), token);
        var limit = input.Direction == VarianceDirection.Missing ? missing : surplus;
        Check(existing.Where(x => x.Direction == input.Direction).Sum(x => (long)x.Quantity) + input.Quantity <= limit, "VARIANCE_EXCEEDED", "Số xác minh vượt chênh lệch còn lại.");
        if (input.Category == VarianceCategory.Sale)
        {
            Check(input.Direction == VarianceDirection.Missing, "SALE_REQUIRES_MISSING", "Chỉ xác nhận bán từ phần thiếu.", 400);
            // A cross-condition mismatch may be unrecorded spoilage, not a sale.
            var netMissing = Math.Max(0, (line.ExpectedGood.Value + line.ExpectedQuarantine.Value + line.ExpectedDamaged.Value) -
                (line.ActualGood.Value + line.ActualQuarantine.Value + line.ActualDamaged.Value));
            var goodMissing = Math.Max(0, line.ExpectedGood.Value - line.ActualGood.Value);
            Check(existing.Where(x => x.Category == VarianceCategory.Sale).Sum(x => (long)x.Quantity) + input.Quantity <= Math.Min(netMissing, goodMissing),
                "SALE_NOT_VERIFIED", "Số bán không được vượt thiếu ròng và thiếu hàng Good; cần xác minh chuyển điều kiện hàng trước.");
        }
        var resolution = new GymVarianceResolution { CountLineId = lineId, Direction = input.Direction, Category = input.Category,
            Quantity = input.Quantity, Reason = input.Reason, EvidenceReference = input.EvidenceReference, VerifiedAtUtc = Now, CreatedAtUtc = Now, CreatedByUserId = actor };
        Store.Add(resolution); await Unit.SaveChangesAsync(token); Audit(actor, "GymVarianceVerified", nameof(GymVarianceResolution), resolution.Id);
        var result = Resolution(resolution);
        Store.Add(new IdempotencyRequest { ScopeHash = scope, Operation = "GymResolution", RequestKey = key, RequestHash = payloadHash,
            ResponseStatus = 200, SafeResponseJson = System.Text.Json.JsonSerializer.Serialize(result), CreatedAtUtc = Now, ExpiresAtUtc = Now.AddDays(7) });
        return result;
    }, token);

    public async Task<ResolutionView[]> ResolutionsAsync(long lineId, CancellationToken token) =>
        (await Store.ListAsync(Store.Query<GymVarianceResolution>().Where(x => x.CountLineId == lineId).OrderBy(x => x.Id), token)).Select(Resolution).ToArray();
    public Task<ResolutionView> VoidAsync(long id, ReceiptVoidInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var resolution = await Need<GymVarianceResolution>(id, token);
        if (resolution.VoidedAtUtc is null) { resolution.VoidedAtUtc = Now; resolution.VoidedByUserId = actor; resolution.VoidReason = input.Reason; Audit(actor, "GymVarianceVoided", nameof(GymVarianceResolution), id); }
        return Resolution(resolution);
    }, token);

    private async Task<StockLocation> Location(long id, CancellationToken token) =>
        (await Store.ListAsync(Store.Query<StockLocation>().Where(x => x.GymId == id && x.Kind == StockLocationKind.Gym), token)).SingleOrDefault()
        ?? throw new BusinessException(409, "GYM_LOCATION_MISSING", "Gym chưa có địa điểm kho.");
    private async Task<GymView> View(Gym gym, bool publicOnly, CancellationToken token)
    {
        var location = await Location(gym.Id, token);
        return new(gym.Id, gym.GymCode, gym.Name, gym.PublicAddress, gym.PublicPhone, gym.PublicDescription, gym.CoverAssetId,
            gym.IsActive, gym.IsPublished, publicOnly ? 0 : location.Id, publicOnly ? null : Revision(gym.Version),
            publicOnly ? null : gym.InternalContactName, publicOnly ? null : gym.InternalPhone, publicOnly ? null : gym.InternalNotes);
    }
    private async Task<CountView> CountView(GymStockCount count, CancellationToken token) => new(count.Id, count.GymId, count.CountNumber, count.Kind, count.CountedAtUtc,
        (await Store.ListAsync(Store.Query<GymStockCountLine>().Where(x => x.CountId == count.Id).OrderBy(x => x.BatchId), token)).Select(Line).ToArray(), count.Note);
    private static CountLineView Line(GymStockCountLine x) => new(x.Id, x.BatchId, x.ExpectedGood, x.ExpectedQuarantine, x.ExpectedDamaged, x.ActualGood!.Value, x.ActualQuarantine!.Value, x.ActualDamaged!.Value, Revision(x.Version), x.Note);
    private static ResolutionView Resolution(GymVarianceResolution x) => new(x.Id, x.CountLineId, x.Direction, x.Category, x.Quantity, x.Reason, x.VerifiedAtUtc, x.VoidedAtUtc, x.EvidenceReference, x.VoidReason);
}
