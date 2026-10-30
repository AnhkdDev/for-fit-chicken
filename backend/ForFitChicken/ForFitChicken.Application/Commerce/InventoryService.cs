using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed class InventoryService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock) : BusinessService(store, unit, clock)
{
    public Task<bool> DeleteBatchAsync(long id, string version, long actor, CancellationToken token) => Write(async () =>
    {
        var batch = await Need<ProductBatch>(id, token); Version(batch.Version, version);
        Check(!await Store.AnyAsync(Store.Query<WarehouseBalance>().Where(x => x.BatchId == id), token) &&
            !await Store.AnyAsync(Store.Query<StockDocumentLine>().Where(x => x.BatchId == id), token) &&
            !await Store.AnyAsync(Store.Query<GymStockCountLine>().Where(x => x.BatchId == id), token),
            "BATCH_IN_USE", "Lô đã phát sinh tồn kho, chứng từ hoặc kiểm đếm; không thể xóa lịch sử. Có thể khóa bán lô.");
        Store.Remove(batch); Audit(actor, "BatchDeleted", nameof(ProductBatch), id); return true;
    }, token);

    public Task<long> InitializeWarehouseAsync(long actor, CancellationToken token) => Write(async () =>
    {
        var locations = await Store.ListAsync(Store.Query<StockLocation>().Where(x => x.Kind == StockLocationKind.Warehouse), token);
        if (locations.Length != 0)
        {
            Check(locations.Length == 1 && locations[0].LocationCode == "MAIN", "WAREHOUSE_CONFIGURATION", "MVP cần đúng một kho MAIN.");
            return locations[0].Id;
        }
        var location = new StockLocation { LocationCode = "MAIN", Name = "Kho chính", Kind = StockLocationKind.Warehouse, CreatedAtUtc = Now, UpdatedAtUtc = Now };
        Store.Add(location); await Unit.SaveChangesAsync(token); Audit(actor, "WarehouseInitialized", nameof(StockLocation), location.Id); return location.Id;
    }, token);

    public async Task<LocationView[]> LocationsAsync(CancellationToken token) => (await Store.ListAsync(Store.Query<StockLocation>().OrderBy(x => x.Id), token))
        .Select(x => new LocationView(x.Id, x.LocationCode, x.Name, x.Kind, x.GymId, x.IsActive, Revision(x.Version))).ToArray();

    public Task<BatchView> CreateBatchAsync(BatchInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var variant = await Need<ProductVariant>(input.VariantId, token);
        Check(variant.IsActive, "INACTIVE_VARIANT", "SKU đã ngừng hoạt động.");
        Check(input.ExpiresAtUtc.Kind == DateTimeKind.Utc && input.ExpiresAtUtc > Now &&
            DateOnly.FromDateTime(input.ExpiresAtUtc.AddHours(7)) >= input.ManufacturedOn && input.ManufacturedOn <= DateOnly.FromDateTime(Now.AddHours(7)),
            "INVALID_EXPIRY", "Ngày sản xuất không ở tương lai; hạn dùng phải là UTC, sau hiện tại và sau thời điểm sản xuất.", 400);
        var code = input.BatchCode.ToUpperInvariant();
        Check(!await Store.AnyAsync(Store.Query<ProductBatch>().Where(x => x.BatchCode == code), token), "DUPLICATE_BATCH", "Mã lô đã tồn tại.");
        var batch = new ProductBatch { VariantId = input.VariantId, BatchCode = code, ManufacturedOn = input.ManufacturedOn,
            ExpiresAtUtc = input.ExpiresAtUtc, Notes = input.Notes, CreatedAtUtc = Now, UpdatedAtUtc = Now, CreatedByUserId = actor };
        Store.Add(batch); await Unit.SaveChangesAsync(token); Audit(actor, "BatchCreated", nameof(ProductBatch), batch.Id); return Batch(batch);
    }, token);

    public async Task<Page<BatchView>> BatchesAsync(int page, int size, CancellationToken token, string? search = null, string? status = null)
    {
        page = Number(page); size = Size(size); var query = Store.Query<ProductBatch>(); var now=Now;
        if(status=="active")query=query.Where(x=>!x.IsSaleBlocked&&x.ExpiresAtUtc>now);
        if(status=="blocked")query=query.Where(x=>x.IsSaleBlocked);
        if(status=="expired")query=query.Where(x=>x.ExpiresAtUtc<=now);
        if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();query=query.Where(x=>x.BatchCode.Contains(term)||Store.Query<ProductVariant>().Any(v=>v.Id==x.VariantId&&(v.SkuCode.Contains(term)||Store.Query<Product>().Any(p=>p.Id==v.ProductId&&p.Name.Contains(term)))));}
        return new((await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size), token)).Select(Batch).ToArray(), await Store.CountAsync(query, token), page, size);
    }

    public Task<BatchView> BlockAsync(long id, BlockBatchInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var batch = await Need<ProductBatch>(id, token); Version(batch.Version, input.Version);
        Check(!input.Blocked || !string.IsNullOrWhiteSpace(input.Reason), "REASON_REQUIRED", "Cần lý do khóa lô.", 400);
        batch.IsSaleBlocked = input.Blocked; batch.BlockReason = input.Blocked ? input.Reason : null;
        batch.BlockedAtUtc = input.Blocked ? Now : null; batch.BlockedByUserId = input.Blocked ? actor : null; batch.UpdatedAtUtc = Now;
        await Unit.SaveChangesAsync(token); Audit(actor, "BatchSaleBlockChanged", nameof(ProductBatch), id); return Batch(batch);
    }, token);

    public Task<BatchView> SetCostAsync(long id, CostInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); Check(input.UnitCost is null || decimal.Round(input.UnitCost.Value, 4) == input.UnitCost, "INVALID_COST", "Giá vốn tối đa 4 số lẻ.", 400);
        var batch = await Need<ProductBatch>(id, token); Version(batch.Version, input.Version);
        batch.UnitCost = input.UnitCost; batch.UpdatedAtUtc = Now; await Unit.SaveChangesAsync(token);
        Audit(actor, "BatchCostChanged", nameof(ProductBatch), id); return Batch(batch);
    }, token);

    public async Task<BatchCostView> CostAsync(long id, CancellationToken token)
    {
        var batch = await Need<ProductBatch>(id, token); return new(batch.Id, batch.UnitCost, Revision(batch.Version));
    }

    public async Task<Page<BalanceView>> BalancesAsync(int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); var now = Now;
        var query = from b in Store.Query<WarehouseBalance>() join batch in Store.Query<ProductBatch>() on b.BatchId equals batch.Id
            join loc in Store.Query<StockLocation>() on b.LocationId equals loc.Id
            join variant in Store.Query<ProductVariant>() on batch.VariantId equals variant.Id
            join product in Store.Query<Product>() on variant.ProductId equals product.Id
            where loc.Kind == StockLocationKind.Warehouse && loc.LocationCode == "MAIN"
            select new { Balance = b, Batch = batch, Eligible = loc.IsActive && variant.IsActive && product.IsActive && !batch.IsSaleBlocked && batch.ExpiresAtUtc > now && b.Condition == StockCondition.Good };
        var total = await Store.CountAsync(query, token);
        var values = await Store.ListAsync(query.OrderBy(x => x.Balance.Id).Skip((page - 1) * size).Take(size), token);
        return new(values.Select(x => new BalanceView(x.Balance.Id, x.Balance.LocationId, x.Batch.Id, x.Batch.BatchCode, x.Batch.VariantId, x.Balance.Condition,
            x.Balance.OnHand, x.Balance.Reserved, x.Eligible ? x.Balance.OnHand - x.Balance.Reserved : 0, x.Batch.ExpiresAtUtc, Revision(x.Balance.Version))).ToArray(), total, page, size);
    }

    public Task<DocumentView> PostAsync(MovementInput input, long actor, bool owner, CancellationToken token) => Write(async () =>
    {
        Validate(input); foreach (var line in input.Lines) Validate(line);
        Check(input.OperationKey != Guid.Empty && Enum.IsDefined(input.DocumentType) && input.DocumentType != StockDocumentType.OrderIssue,
            "INVALID_MOVEMENT", "OperationKey bắt buộc; không xuất đơn qua API chứng từ thủ công.", 400);
        Check(input.DocumentType != StockDocumentType.Adjustment || owner, "OWNER_REQUIRED", "Điều chỉnh kho cần Owner.", 403);
        Check(owner || input.CorrectsDocumentId is null, "OWNER_REQUIRED", "Chỉ Owner được lập phiếu sửa chứng từ.", 403);
        var old = (await Store.ListAsync(Store.Query<StockDocument>().Where(x => x.OperationKey == input.OperationKey), token)).SingleOrDefault();
        if (old is not null)
        {
            var oldView = await Document(old, token);
            Check(old.DocumentType == input.DocumentType && old.Reason == input.Reason && old.CorrectsDocumentId == input.CorrectsDocumentId &&
                System.Text.Json.JsonSerializer.Serialize(oldView.Lines) == System.Text.Json.JsonSerializer.Serialize(input.Lines),
                "IDEMPOTENCY_CONFLICT", "OperationKey đã dùng cho nội dung khác.");
            return oldView;
        }

        if (input.CorrectsDocumentId is not null)
        {
            var corrected = await Need<StockDocument>(input.CorrectsDocumentId.Value, token);
            Check(owner && corrected.Status == PostingStatus.Posted && corrected.DocumentType != StockDocumentType.OrderIssue,
                "CORRECTION_NOT_ALLOWED", "Owner chỉ sửa bằng phiếu mới liên kết phiếu kho thủ công đã chốt.", 403);
        }
        var time = await NextInventoryTime(token);
        var doc = new StockDocument { DocumentNo = "ST-" + Guid.NewGuid().ToString("N"), OperationKey = input.OperationKey,
            DocumentType = input.DocumentType, Status = PostingStatus.Posted, CorrectsDocumentId = input.CorrectsDocumentId, EffectiveAtUtc = time, CreatedAtUtc = Now, CreatedByUserId = actor, PostedAtUtc = Now, PostedByUserId = actor, Reason = input.Reason };
        Store.Add(doc); await Unit.SaveChangesAsync(token);
        foreach (var line in input.Lines)
        {
            var batch = await Need<ProductBatch>(line.BatchId, token);
            var from = line.FromLocationId is null ? null : await Need<StockLocation>(line.FromLocationId.Value, token);
            var to = line.ToLocationId is null ? null : await Need<StockLocation>(line.ToLocationId.Value, token);
            Check((from is null) == (line.FromCondition is null) && (to is null) == (line.ToCondition is null) && (from is not null || to is not null), "INVALID_ENDPOINT", "Nguồn/đích và điều kiện hàng không hợp lệ.", 400);
            Check((line.FromCondition is null || Enum.IsDefined(line.FromCondition.Value)) && (line.ToCondition is null || Enum.IsDefined(line.ToCondition.Value)), "INVALID_CONDITION", "Điều kiện hàng không hợp lệ.", 400);
            Check(from is null || to is null || from.Id != to.Id || line.FromCondition != line.ToCondition, "NO_MOVEMENT", "Nguồn và đích không được giống nhau.", 400);
            bool Main(StockLocation? x) => x is { Kind: StockLocationKind.Warehouse, LocationCode: "MAIN", IsActive: true };
            var valid = input.DocumentType switch
            {
                StockDocumentType.ProductionReceipt => from is null && Main(to) && line.ToCondition == StockCondition.Good,
                StockDocumentType.GymDelivery => Main(from) && to is { Kind: StockLocationKind.Gym, IsActive: true } && line.FromCondition == StockCondition.Good && line.ToCondition == StockCondition.Good,
                StockDocumentType.GymRecall => from is { Kind: StockLocationKind.Gym } && Main(to) && line.ToCondition == StockCondition.Quarantine,
                StockDocumentType.Reclassify => from is not null && to?.Id == from.Id && line.FromCondition != line.ToCondition &&
                    (line.ToCondition != StockCondition.Good || (owner && line.FromCondition == StockCondition.Quarantine)),
                StockDocumentType.WriteOff => from is not null && to is null,
                StockDocumentType.Adjustment => (from is null && Main(to) && line.ToCondition == StockCondition.Quarantine) || (Main(from) && to is null),
                _ => false
            };
            Check(valid, "INVALID_MOVEMENT", "Loại phiếu không phù hợp nguồn/đích/điều kiện hàng.", 400);
            if (input.DocumentType == StockDocumentType.GymDelivery || (input.DocumentType == StockDocumentType.Reclassify && line.ToCondition == StockCondition.Good))
            {
                var variant = await Need<ProductVariant>(batch.VariantId, token); var product = await Need<Product>(variant.ProductId, token);
                Check(!batch.IsSaleBlocked && batch.ExpiresAtUtc > Now && variant.IsActive && product.IsActive, "BATCH_NOT_SELLABLE", "Lô/SKU không đủ điều kiện bán.");
            }
            if (from is not null) await Apply(from, batch.Id, line.FromCondition!.Value, -line.Quantity, token);
            if (to is not null) await Apply(to, batch.Id, line.ToCondition!.Value, line.Quantity, token);
            Store.Add(new StockDocumentLine { DocumentId = doc.Id, BatchId = batch.Id, Quantity = line.Quantity,
                FromLocationId = from?.Id, FromCondition = line.FromCondition, ToLocationId = to?.Id, ToCondition = line.ToCondition, Note = line.Note });
            // Persist each line so subsequent lines see it when deriving gym stock; all remain in one transaction.
            await Unit.SaveChangesAsync(token);
        }
        Audit(actor, "StockDocumentPosted", nameof(StockDocument), doc.Id); return await Document(doc, token);
    }, token);

    public async Task<Page<DocumentView>> DocumentsAsync(int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); var query = Store.Query<StockDocument>(); var result = new List<DocumentView>();
        foreach (var doc in await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size), token)) result.Add(await Document(doc, token));
        return new(result.ToArray(), await Store.CountAsync(query, token), page, size);
    }

    internal async Task<StockLocation> Main(CancellationToken token)
    {
        var values = await Store.ListAsync(Store.Query<StockLocation>().Where(x => x.Kind == StockLocationKind.Warehouse && x.LocationCode == "MAIN" && x.IsActive), token);
        Check(values.Length == 1, "WAREHOUSE_NOT_READY", "Owner cần khởi tạo kho MAIN trước."); return values[0];
    }
    internal async Task<WarehouseBalance> Balance(long location, long batch, StockCondition condition, CancellationToken token)
    {
        var balance = (await Store.ListAsync(Store.Query<WarehouseBalance>().Where(x => x.LocationId == location && x.BatchId == batch && x.Condition == condition), token)).SingleOrDefault();
        if (balance is not null) return balance;
        balance = new WarehouseBalance { LocationId = location, BatchId = batch, Condition = condition, UpdatedAtUtc = Now };
        Store.Add(balance); await Unit.SaveChangesAsync(token); return balance;
    }
    internal async Task Apply(StockLocation location, long batch, StockCondition condition, int delta, CancellationToken token)
    {
        if (location.Kind == StockLocationKind.Warehouse)
        {
            Check(location.LocationCode == "MAIN", "INVALID_WAREHOUSE", "Chỉ dùng kho MAIN.");
            var balance = await Balance(location.Id, batch, condition, token);
            var next = checked(balance.OnHand + delta);
            Check(next >= balance.Reserved && next >= 0, "INSUFFICIENT_STOCK", "Không đủ hàng chưa giữ cho đơn.");
            balance.OnHand = next; balance.UpdatedAtUtc = Now;
        }
        else if (delta < 0)
        {
            var buckets = await GymBuckets(location, token);
            Check(buckets.GetValueOrDefault((batch, condition)) + delta >= 0, "GYM_COUNT_REQUIRED", "Số theo sổ không đủ. Kiểm đếm trước khi ghi thu hồi/hỏng.");
        }
        location.UpdatedAtUtc = Now;
    }
    internal async Task<DateTime> NextInventoryTime(CancellationToken token)
    {
        var docs = await Store.ListAsync(Store.Query<StockDocument>().Where(x => x.Status == PostingStatus.Posted).OrderByDescending(x => x.EffectiveAtUtc).Select(x => x.EffectiveAtUtc).Take(1), token);
        var counts = await Store.ListAsync(Store.Query<GymStockCount>().Where(x => x.Status == PostingStatus.Posted).OrderByDescending(x => x.CountedAtUtc).Select(x => x.CountedAtUtc).Take(1), token);
        var last = docs.Concat(counts).DefaultIfEmpty(DateTime.MinValue).Max();
        var now = new DateTime(Now.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        return now > last ? now : DateTime.SpecifyKind(last.AddMilliseconds(1), DateTimeKind.Utc);
    }
    internal async Task<Dictionary<(long Batch, StockCondition Condition), int>> GymBuckets(StockLocation location, CancellationToken token)
    {
        var last = (await Store.ListAsync(Store.Query<GymStockCount>().Where(x => x.GymId == location.GymId && x.Status == PostingStatus.Posted).OrderByDescending(x => x.CountedAtUtc).Take(1), token)).SingleOrDefault();
        var buckets = new Dictionary<(long, StockCondition), int>();
        if (last is not null)
            foreach (var line in await Store.ListAsync(Store.Query<GymStockCountLine>().Where(x => x.CountId == last.Id), token))
            {
                buckets[(line.BatchId, StockCondition.Good)] = line.ActualGood!.Value;
                buckets[(line.BatchId, StockCondition.Quarantine)] = line.ActualQuarantine!.Value;
                buckets[(line.BatchId, StockCondition.Damaged)] = line.ActualDamaged!.Value;
            }
        var cutoff = last?.CountedAtUtc ?? DateTime.MinValue;
        var moves = await Store.ListAsync(from line in Store.Query<StockDocumentLine>() join doc in Store.Query<StockDocument>() on line.DocumentId equals doc.Id
            where doc.Status == PostingStatus.Posted && doc.EffectiveAtUtc > cutoff && (line.FromLocationId == location.Id || line.ToLocationId == location.Id) select line, token);
        foreach (var move in moves)
        {
            if (move.FromLocationId == location.Id) { var key = (move.BatchId, move.FromCondition!.Value); buckets[key] = checked(buckets.GetValueOrDefault(key) - move.Quantity); }
            if (move.ToLocationId == location.Id) { var key = (move.BatchId, move.ToCondition!.Value); buckets[key] = checked(buckets.GetValueOrDefault(key) + move.Quantity); }
        }
        return buckets;
    }
    private async Task<DocumentView> Document(StockDocument doc, CancellationToken token) => new(doc.Id, doc.DocumentNo, doc.OperationKey, doc.DocumentType, doc.Status, doc.EffectiveAtUtc,
        (await Store.ListAsync(Store.Query<StockDocumentLine>().Where(x => x.DocumentId == doc.Id).OrderBy(x => x.Id), token))
        .Select(x => new MovementLine(x.BatchId, x.Quantity, x.FromLocationId, x.FromCondition, x.ToLocationId, x.ToCondition, x.Note)).ToArray(), doc.CorrectsDocumentId, doc.Reason);
    private static BatchView Batch(ProductBatch x) => new(x.Id, x.VariantId, x.BatchCode, x.ManufacturedOn, x.ExpiresAtUtc, x.IsSaleBlocked, x.BlockReason, Revision(x.Version));
}
