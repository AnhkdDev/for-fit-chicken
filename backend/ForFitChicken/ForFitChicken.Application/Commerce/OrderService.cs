using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed class OrderService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock, InventoryService inventory, INotificationSettings notifications)
    : BusinessService(store, unit, clock)
{
    public Task<CheckoutResult> CheckoutAsync(CheckoutInput input, Guid key, CancellationToken token) => Create(input, key, OrderSource.Website, null, token);
    public Task<CheckoutResult> ManualAsync(CheckoutInput input, Guid key, OrderSource source, long actor, CancellationToken token)
    {
        Check(Enum.IsDefined(source) && source != OrderSource.Website, "INVALID_SOURCE", "Đơn thủ công dùng Facebook/Zalo/Phone/Other.", 400);
        return Create(input, key, source, actor, token);
    }
    private Task<CheckoutResult> Create(CheckoutInput input, Guid key, OrderSource source, long? actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); foreach (var line in input.Items) Validate(line);
        Check(key != Guid.Empty, "IDEMPOTENCY_KEY_REQUIRED", "Cần Idempotency-Key dạng UUID.", 400);
        Check(input.Items.Select(x => x.VariantId).Distinct().Count() == input.Items.Length, "DUPLICATE_ITEM", "Gộp các dòng cùng SKU trước khi đặt.", 400);
        var requestHash = Hash(JsonSerializer.Serialize(input with { Items = input.Items.OrderBy(x => x.VariantId).ToArray() }));
        var scopeHash = Hash("Checkout:v1:" + source + ":" + actor); var keyText = key.ToString("N");
        var previous = (await Store.ListAsync(Store.Query<IdempotencyRequest>().Where(x => x.ScopeHash == scopeHash && x.Operation == "Checkout" && x.RequestKey == keyText), token)).SingleOrDefault();
        if (previous is not null)
        {
            Check(previous.RequestHash == requestHash, "IDEMPOTENCY_CONFLICT", "Khóa đã dùng cho yêu cầu khác.");
            return JsonSerializer.Deserialize<CheckoutResult>(previous.SafeResponseJson!)!;
        }
        var main = await inventory.Main(token);
        var now = Now;
        var order = new Order { OrderNumber = "FC-" + Guid.NewGuid().ToString("N"), RecipientName = input.RecipientName.Trim(),
            RecipientPhone = input.RecipientPhone, RecipientAddress = input.RecipientAddress.Trim(), CustomerNote = input.CustomerNote,
            CreatedAtUtc = now, UpdatedAtUtc = now, Status = OrderStatus.PendingConfirmation, Source = source, CreatedByUserId = actor };
        var lines = new List<OrderItem>();
        foreach (var requested in input.Items)
        {
            var variant = await Need<ProductVariant>(requested.VariantId, token); var product = await Need<Product>(variant.ProductId, token);
            Check(product.IsActive && (source != OrderSource.Website || product.IsPublished) && variant.IsActive && variant.NetWeightGrams > 0, "PRODUCT_UNAVAILABLE", "Sản phẩm không còn mở bán.");
            var balances = await Store.ListAsync(from balance in Store.Query<WarehouseBalance>() join batch in Store.Query<ProductBatch>() on balance.BatchId equals batch.Id
                where balance.LocationId == main.Id && balance.Condition == StockCondition.Good && batch.VariantId == variant.Id && !batch.IsSaleBlocked && batch.ExpiresAtUtc > now select balance, token);
            Check(balances.Sum(x => (long)x.OnHand - x.Reserved) >= requested.Quantity, "INSUFFICIENT_STOCK", "Số lượng hiện chưa đủ. Vui lòng điều chỉnh giỏ hàng.");
            lines.Add(new OrderItem { VariantId = variant.Id, SkuCodeSnapshot = variant.SkuCode, ProductNameSnapshot = product.Name,
                VariantLabelSnapshot = variant.Label, NetWeightGramsSnapshot = variant.NetWeightGrams!.Value, Quantity = requested.Quantity, UnitPrice = variant.SellingPrice });
        }
        order.ItemsSubtotal = lines.Sum(x => x.UnitPrice * x.Quantity);
        Store.Add(order); await Unit.SaveChangesAsync(token);
        foreach (var line in lines) { line.OrderId = order.Id; Store.Add(line); }
        Event(order.Id, "OrderRequested", null, order.Status, null, actor);
        if (actor is not null) Audit(actor.Value, "ManualOrderCreated", nameof(Order), order.Id);
        var settings = notifications.Current;
        if (settings.Enabled)
        {
            Check(settings.TargetChatId is not null and not 0, "NOTIFICATION_CONFIGURATION", "Cấu hình Telegram chưa đầy đủ.", 503);
            Store.Add(new NotificationJob { OrderId = order.Id, TargetChatId = settings.TargetChatId.GetValueOrDefault(),
                PayloadJson = JsonSerializer.Serialize(new { order.OrderNumber, order.ItemsSubtotal, PackageCount = lines.Sum(x => x.Quantity) }),
                NextAttemptAtUtc = now, CreatedAtUtc = now });
        }
        var result = new CheckoutResult(order.OrderNumber, order.Status, order.ItemsSubtotal, order.CreatedAtUtc);
        Store.Add(new IdempotencyRequest { ScopeHash = scopeHash, Operation = "Checkout", RequestKey = keyText, RequestHash = requestHash,
            OrderId = order.Id, ResponseStatus = 201, SafeResponseJson = JsonSerializer.Serialize(result), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(7) });
        return result;
    }, token);

    public async Task<Page<OrderView>> ListAsync(OrderStatus? status, string? search, int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); Check(search is null || search.Length <= 100, "INVALID_SEARCH", "Tìm kiếm tối đa 100 ký tự.", 400);
        Check(status is null || Enum.IsDefined(status.Value), "INVALID_STATUS", "Trạng thái không hợp lệ.", 400);
        var query = Store.Query<Order>().Where(x => (status == null || x.Status == status) && (search == null || x.OrderNumber.Contains(search) || x.RecipientPhone.Contains(search)));
        var rows = await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size), token); var views = new List<OrderView>();
        foreach (var row in rows) views.Add(await View(row, token));
        return new(views.ToArray(), await Store.CountAsync(query, token), page, size);
    }
    public async Task<OrderView> GetAsync(long id, CancellationToken token) => await View(await Need<Order>(id, token), token);
    public async Task<OrderEventView[]> EventsAsync(long id, CancellationToken token)
    {
        await Need<Order>(id, token);
        return await Store.ListAsync(Store.Query<OrderEvent>().Where(x => x.OrderId == id).OrderBy(x => x.Id)
            .Select(x => new OrderEventView(x.Id, x.EventType, x.FromStatus, x.ToStatus, x.Note, x.CreatedAtUtc, x.ActorUserId)), token);
    }

    public Task<OrderView> EditAsync(long id, OrderEditInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var order = await Need<Order>(id, token); Version(order.Version, input.Version);
        Check(order.Status == OrderStatus.PendingConfirmation, "ORDER_FROZEN", "Chỉ sửa thông tin khi đang chờ xác nhận.");
        order.RecipientName = input.RecipientName.Trim(); order.RecipientPhone = input.RecipientPhone;
        order.RecipientAddress = input.RecipientAddress.Trim(); order.InternalNote = input.InternalNote; order.UpdatedAtUtc = Now;
        Event(id, "RecipientUpdated", order.Status, order.Status, null, actor); await Unit.SaveChangesAsync(token); return await View(order, token);
    }, token);

    public Task<OrderView> TransitionAsync(long id, string action, string version, string? reason, long actor, CancellationToken token) => Write(async () =>
    {
        var order = await Need<Order>(id, token); var before = order.Status;
        var target = action switch { "confirm" => OrderStatus.Confirmed, "prepare" => OrderStatus.Preparing, "handover" => OrderStatus.HandedOver,
            "complete" => OrderStatus.Completed, "cancel" => OrderStatus.Cancelled, "reallocate" => order.Status, _ => throw new BusinessException(400, "INVALID_ACTION", "Thao tác không hợp lệ.") };
        if (action != "reallocate" && order.Status == target) return await View(order, token);
        Version(order.Version, version);
        var items = await Store.ListAsync(Store.Query<OrderItem>().Where(x => x.OrderId == id).OrderBy(x => x.Id), token);
        var itemIds = items.Select(x => x.Id).ToArray();
        var allocations = await Store.ListAsync(Store.Query<OrderAllocation>().Where(x => itemIds.Contains(x.OrderItemId) && x.State == AllocationState.Reserved).OrderBy(x => x.Id), token);
        if (action is "confirm" or "reallocate")
        {
            Check(action == "confirm" ? order.Status == OrderStatus.PendingConfirmation : order.Status is OrderStatus.Confirmed or OrderStatus.Preparing, "INVALID_ORDER_STATE", "Trạng thái không cho phép giữ/phân bổ lại.");
            if (action == "reallocate") await Release(allocations, "Reallocation", token);
            await Reserve(items, token);
            order.ConfirmedAtUtc ??= Now; order.ConfirmedByUserId ??= actor;
        }
        else if (action == "prepare") Check(order.Status == OrderStatus.Confirmed, "INVALID_ORDER_STATE", "Cần xác nhận đơn trước.");
        else if (action == "handover")
        {
            Check(order.Status is OrderStatus.Confirmed or OrderStatus.Preparing, "INVALID_ORDER_STATE", "Đơn chưa sẵn sàng giao.");
            foreach (var item in items) Check(allocations.Where(x => x.OrderItemId == item.Id).Sum(x => x.Quantity) == item.Quantity, "ALLOCATION_MISMATCH", "Phân bổ không khớp đơn.");
            var main = await inventory.Main(token);
            var doc = new StockDocument { DocumentNo = "ST-" + Guid.NewGuid().ToString("N"), OperationKey = Guid.NewGuid(),
                DocumentType = StockDocumentType.OrderIssue, Status = PostingStatus.Posted, OrderId = id,
                EffectiveAtUtc = await inventory.NextInventoryTime(token), CreatedAtUtc = Now, CreatedByUserId = actor, PostedAtUtc = Now, PostedByUserId = actor, Reason = "Order handover" };
            Store.Add(doc); await Unit.SaveChangesAsync(token);
            foreach (var allocation in allocations)
            {
                var balance = await Need<WarehouseBalance>(allocation.BalanceId, token); var batch = await Need<ProductBatch>(balance.BatchId, token);
                var item = items.Single(x => x.Id == allocation.OrderItemId);
                var variant = await Need<ProductVariant>(batch.VariantId, token); var product = await Need<Product>(variant.ProductId, token);
                Check(balance.LocationId == main.Id && balance.Condition == StockCondition.Good && batch.VariantId == item.VariantId && !batch.IsSaleBlocked && batch.ExpiresAtUtc > Now && variant.IsActive && product.IsActive,
                    "BATCH_NOT_SELLABLE", "Phân bổ đã hết hạn/khóa/ngừng bán; phân bổ lại hoặc hủy trước.");
                Check(balance.Reserved >= allocation.Quantity && balance.OnHand >= allocation.Quantity, "ALLOCATION_MISMATCH", "Tồn và lượng giữ không khớp.");
                balance.OnHand -= allocation.Quantity; balance.Reserved -= allocation.Quantity; balance.UpdatedAtUtc = Now;
                allocation.State = AllocationState.Issued; allocation.IssuedAtUtc = Now; allocation.UnitCostSnapshot = batch.UnitCost;
                Store.Add(new StockDocumentLine { DocumentId = doc.Id, BatchId = batch.Id, Quantity = allocation.Quantity,
                    FromLocationId = main.Id, FromCondition = StockCondition.Good });
            }
            order.HandedOverAtUtc = Now;
        }
        else if (action == "complete")
        { Check(order.Status == OrderStatus.HandedOver, "INVALID_ORDER_STATE", "Chỉ hoàn tất đơn đã bàn giao."); order.CompletedAtUtc = Now; }
        else if (action == "cancel")
        {
            Check(order.Status is OrderStatus.PendingConfirmation or OrderStatus.Confirmed or OrderStatus.Preparing, "INVALID_ORDER_STATE", "Không hủy đơn đã bàn giao/hoàn tất.");
            Check(!string.IsNullOrWhiteSpace(reason) && reason.Length <= 500, "REASON_REQUIRED", "Cần lý do hủy tối đa 500 ký tự.", 400);
            await Release(allocations, reason!, token); order.CancelledAtUtc = Now; order.CancellationReason = reason;
        }
        order.Status = target; order.UpdatedAtUtc = Now; Event(id, action, before, target, reason, actor);
        Audit(actor, "Order" + action, nameof(Order), id); await Unit.SaveChangesAsync(token); return await View(order, token);
    }, token);

    public Task<ReceiptView> ReceiveAsync(long id, ReceiptInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var order = await Need<Order>(id, token);
        var old = (await Store.ListAsync(Store.Query<CodReceipt>().Where(x => x.OrderId == id && x.VoidedAtUtc == null), token)).SingleOrDefault();
        if (old is not null) return Receipt(old);
        Version(order.Version, input.Version);
        Check(order.Status is OrderStatus.HandedOver or OrderStatus.Completed, "INVALID_ORDER_STATE", "Chỉ ghi tiền cho đơn đã bàn giao.");
        Check(input.ReceivedAtUtc.Kind == DateTimeKind.Utc && input.ReceivedAtUtc <= Now && input.ReceivedAtUtc >= order.CreatedAtUtc,
            "INVALID_RECEIVED_TIME", "Thời điểm thu tiền phải là UTC, không trước đặt đơn hoặc ở tương lai.", 400);
        var receipt = new CodReceipt { OrderId = id, Amount = order.ItemsSubtotal, ReceivedAtUtc = input.ReceivedAtUtc,
            RecordedAtUtc = Now, RecordedByUserId = actor, Reference = input.Reference };
        Store.Add(receipt); order.UpdatedAtUtc = Now; await Unit.SaveChangesAsync(token); Audit(actor, "CodReceived", nameof(CodReceipt), receipt.Id); return Receipt(receipt);
    }, token);
    public async Task<ReceiptView[]> ReceiptsAsync(long id, CancellationToken token) =>
        (await Store.ListAsync(Store.Query<CodReceipt>().Where(x => x.OrderId == id).OrderByDescending(x => x.Id), token)).Select(Receipt).ToArray();
    public Task<ReceiptView> VoidReceiptAsync(long id, ReceiptVoidInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var receipt = await Need<CodReceipt>(id, token); if (receipt.VoidedAtUtc is not null) return Receipt(receipt);
        receipt.VoidedAtUtc = Now; receipt.VoidedByUserId = actor; receipt.VoidReason = input.Reason;
        Audit(actor, "CodReceiptVoided", nameof(CodReceipt), id); return Receipt(receipt);
    }, token);

    private async Task Reserve(OrderItem[] items, CancellationToken token)
    {
        var main = await inventory.Main(token); var now = Now;
        foreach (var item in items)
        {
            var variant = await Need<ProductVariant>(item.VariantId, token); var product = await Need<Product>(variant.ProductId, token);
            Check(variant.IsActive && product.IsActive, "PRODUCT_UNAVAILABLE", "SKU đã ngừng hoạt động.");
            var candidates = await Store.ListAsync(from balance in Store.Query<WarehouseBalance>() join batch in Store.Query<ProductBatch>() on balance.BatchId equals batch.Id
                where balance.LocationId == main.Id && balance.Condition == StockCondition.Good && batch.VariantId == item.VariantId && !batch.IsSaleBlocked && batch.ExpiresAtUtc > now
                orderby batch.ExpiresAtUtc, batch.Id select balance, token);
            var needed = item.Quantity;
            foreach (var balance in candidates)
            {
                var take = Math.Min(needed, balance.OnHand - balance.Reserved); if (take <= 0) continue;
                balance.Reserved += take; balance.UpdatedAtUtc = Now;
                Store.Add(new OrderAllocation { OrderItemId = item.Id, BalanceId = balance.Id, Quantity = take, ReservedAtUtc = Now }); needed -= take;
                if (needed == 0) break;
            }
            Check(needed == 0, "INSUFFICIENT_STOCK", "Không đủ tồn để xác nhận toàn đơn. Đơn chưa được giữ hàng.");
        }
    }
    private async Task Release(OrderAllocation[] allocations, string reason, CancellationToken token)
    {
        foreach (var allocation in allocations)
        {
            var balance = await Need<WarehouseBalance>(allocation.BalanceId, token);
            Check(balance.Reserved >= allocation.Quantity, "ALLOCATION_MISMATCH", "Lượng giữ không khớp.");
            balance.Reserved -= allocation.Quantity; balance.UpdatedAtUtc = Now;
            allocation.State = AllocationState.Released; allocation.ReleasedAtUtc = Now; allocation.ReleaseReason = reason;
        }
        // Persist releases before reinserting a Reserved pair under the filtered unique index.
        await Unit.SaveChangesAsync(token);
    }
    private async Task<OrderView> View(Order order, CancellationToken token)
    {
        var lines = await Store.ListAsync(Store.Query<OrderItem>().Where(x => x.OrderId == order.Id).OrderBy(x => x.Id), token);
        var ids = lines.Select(x => x.Id).ToArray(); var allocations = await Store.ListAsync(Store.Query<OrderAllocation>().Where(x => ids.Contains(x.OrderItemId)).OrderBy(x => x.Id), token);
        return new(order.Id, order.OrderNumber, order.Status, order.RecipientName, order.RecipientPhone, order.RecipientAddress, order.CustomerNote, order.InternalNote,
            order.ItemsSubtotal, order.CreatedAtUtc, order.CompletedAtUtc, Revision(order.Version),
            lines.Select(x => new OrderLineView(x.Id, x.VariantId, x.SkuCodeSnapshot, x.ProductNameSnapshot, x.VariantLabelSnapshot, x.NetWeightGramsSnapshot, x.Quantity, x.UnitPrice, x.LineTotal)).ToArray(),
            allocations.Select(x => new AllocationView(x.Id, x.OrderItemId, x.BalanceId, x.Quantity, x.State)).ToArray());
    }
    private void Event(long order, string action, OrderStatus? from, OrderStatus to, string? note, long? actor) => Store.Add(new OrderEvent
    { OrderId = order, EventType = action, FromStatus = from, ToStatus = to, Note = note, CreatedAtUtc = Now, ActorUserId = actor });
    private static ReceiptView Receipt(CodReceipt r) => new(r.Id, r.OrderId, r.Amount, r.ReceivedAtUtc, r.VoidedAtUtc, r.VoidReason, r.Reference);
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
