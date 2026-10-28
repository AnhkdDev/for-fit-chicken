using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using ForFitChicken.Infrastructure.Notifications;
using ForFitChicken.Infrastructure.Persistence;
using ForFitChicken.UnitTest.Integration.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ForFitChicken.UnitTest.Integration.Persistence;

[Trait("Category", "SqlServerIntegration")]
public sealed class CommerceTests(MigratedDatabaseFixture database) : IClassFixture<MigratedDatabaseFixture>
{
    [SqlServerFact]
    public async Task Checkout_IsIdempotent_SnapshotsPrice_AndDoesNotReserve()
    {
        await using var s = await Setup(); var key = Guid.NewGuid(); var request = s.Cart(3);
        var result = await s.Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(request, key, default));
        var duplicate = await s.Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(request, key, default));
        Assert.Equal(result, duplicate);
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>();
            Assert.Equal(1, await db.Orders.CountAsync(x => x.OrderNumber == result.OrderNumber));
            Assert.Equal(0, (await db.WarehouseBalances.SingleAsync(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id)).Reserved);
            var variant = await db.ProductVariants.FindAsync(s.Variant.Id); variant!.SellingPrice = 99000; await db.SaveChangesAsync();
            Assert.Equal(35000, (await db.OrderItems.SingleAsync(x => x.OrderId == db.Orders.Where(o => o.OrderNumber == result.OrderNumber).Select(o => o.Id).Single())).UnitPrice);
            return true;
        });
        var conflict = await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(s.Cart(2), key, default)));
        Assert.Equal("IDEMPOTENCY_CONFLICT", conflict.Code);
    }

    [SqlServerFact]
    public async Task CompetingConfirmations_CannotOversell_AndLosingOrderStaysPending()
    {
        await using var s = await Setup(stock: 2); var first = await s.Order(2); var second = await s.Order(2);
        async Task<bool> Confirm(OrderView order)
        {
            try { await s.Run(p => p.GetRequiredService<OrderService>().TransitionAsync(order.Id, "confirm", order.Version, null, s.Actor, default)); return true; }
            catch (BusinessException e) when (e.Code == "INSUFFICIENT_STOCK") { return false; }
        }
        var results = await Task.WhenAll(Confirm(first), Confirm(second)); Assert.Single(results.Where(x => x));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>();
            Assert.Equal(2, (await db.WarehouseBalances.SingleAsync(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id)).Reserved);
            var ids = new[] { first.Id, second.Id };
            Assert.Equal(1, await db.Orders.CountAsync(x => ids.Contains(x.Id) && x.Status == OrderStatus.PendingConfirmation));
            Assert.Equal(2, await db.OrderAllocations.Where(x => db.OrderItems.Where(i => ids.Contains(i.OrderId)).Select(i => i.Id).Contains(x.OrderItemId) && x.State == AllocationState.Reserved).SumAsync(x => x.Quantity));
            return true;
        });
    }

    [SqlServerFact]
    public async Task Confirm_UsesFefo_HandoverOnce_CapturesCost_AndCodIsIndependentOfCompletion()
    {
        await using var s = await Setup(stock: 2);
        var earlier = await s.Run(p => p.GetRequiredService<InventoryService>().CreateBatchAsync(new(s.Variant.Id, "EARLY-" + Guid.NewGuid().ToString("N"), DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow.AddDays(1), null), s.Actor, default));
        await s.Receive(earlier.Id, 3);
        await s.Run(p => p.GetRequiredService<InventoryService>().SetCostAsync(earlier.Id, new(earlier.Version, 20000), s.Actor, default));
        var order = await s.Order(2); var confirmed = await s.Transition(order, "confirm");
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var balanceId = confirmed.Allocations.Single().BalanceId;
            Assert.Equal(earlier.Id, (await db.WarehouseBalances.FindAsync(balanceId))!.BatchId); return true;
        });
        var issued = await s.Transition(confirmed, "handover");
        await s.Run(p => p.GetRequiredService<OrderService>().TransitionAsync(issued.Id, "handover", confirmed.Version, null, s.Actor, default));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>();
            Assert.Equal(1, await db.StockDocuments.CountAsync(x => x.OrderId == order.Id));
            Assert.Equal(20000, (await db.OrderAllocations.SingleAsync(x => x.OrderItemId == issued.Items.Single().Id)).UnitCostSnapshot); return true;
        });
        var receipt = await s.Run(p => p.GetRequiredService<OrderService>().ReceiveAsync(order.Id, new(issued.Version, DateTime.UtcNow, "cash"), s.Actor, default));
        Assert.Equal(70000, receipt.Amount);
        var fresh = await s.Run(p => p.GetRequiredService<OrderService>().GetAsync(order.Id, default));
        Assert.Equal(OrderStatus.HandedOver, fresh.Status);
        var completed = await s.Transition(fresh, "complete"); Assert.Equal(OrderStatus.Completed, completed.Status);
    }

    [SqlServerFact]
    public async Task Cancel_ReleasesStock_AndWriteOffCannotConsumeReservedPackages()
    {
        await using var s = await Setup(stock: 5); var order = await s.Transition(await s.Order(4), "confirm");
        var invalid = new MovementInput(Guid.NewGuid(), StockDocumentType.WriteOff, "damaged", [new(s.Batch.Id, 2, s.Location, StockCondition.Good, null, null, null)]);
        var failure = await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<InventoryService>().PostAsync(invalid, s.Actor, true, default)));
        Assert.Equal("INSUFFICIENT_STOCK", failure.Code);
        await s.Transition(order, "cancel", "Khách đổi ý");
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var balance = await db.WarehouseBalances.SingleAsync(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id);
            Assert.Equal(5, balance.OnHand); Assert.Equal(0, balance.Reserved);
            Assert.False(await db.StockDocuments.AnyAsync(x => x.OperationKey == invalid.OperationKey)); return true;
        });
    }

    [SqlServerFact]
    public async Task BlockedBatch_PreventsHandover_AndFailedReallocationRollsBackReleases()
    {
        await using var s = await Setup(); var order = await s.Transition(await s.Order(3), "confirm");
        await s.Run(p => p.GetRequiredService<InventoryService>().BlockAsync(s.Batch.Id, new(s.Batch.Version, true, "Quality check"), s.Actor, default));
        Assert.Equal("BATCH_NOT_SELLABLE", (await Assert.ThrowsAsync<BusinessException>(() => s.Transition(order, "handover"))).Code);
        Assert.Equal("INSUFFICIENT_STOCK", (await Assert.ThrowsAsync<BusinessException>(() => s.Transition(order, "reallocate"))).Code);
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); Assert.Equal(3, (await db.WarehouseBalances.SingleAsync(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id)).Reserved);
            Assert.Equal(AllocationState.Reserved, (await db.OrderAllocations.SingleAsync(x => x.OrderItemId == order.Items.Single().Id)).State); return true;
        });
    }

    [SqlServerFact]
    public async Task GymDelivery_CountAndRecall_DoNotCreateWarehouseStockOrAutomaticSales()
    {
        await using var s = await Setup(stock: 10); var gym = await s.Gym();
        await s.Move(new(Guid.NewGuid(), StockDocumentType.GymDelivery, "Ký gửi", [new(s.Batch.Id, 4, s.Location, StockCondition.Good, gym.LocationId, StockCondition.Good, null)]));
        var stock = await s.Run(p => p.GetRequiredService<GymService>().StockAsync(gym.Id, default));
        var baseline = await s.Run(p => p.GetRequiredService<GymService>().CountAsync(gym.Id, new(stock.Version, [new(s.Batch.Id, 4, 0, 0, null)], null), s.Actor, true, default));
        Assert.Equal(StockCountKind.OpeningBaseline, baseline.Kind); Assert.Null(baseline.Lines.Single().ExpectedGood);
        stock = await s.Run(p => p.GetRequiredService<GymService>().StockAsync(gym.Id, default));
        var regular = await s.Run(p => p.GetRequiredService<GymService>().CountAsync(gym.Id, new(stock.Version, [new(s.Batch.Id, 2, 0, 0, null)], null), s.Actor, false, default));
        Assert.Equal(4, regular.Lines.Single().ExpectedGood); Assert.Equal(2, regular.Lines.Single().ActualGood);
        var unresolved = await s.Run(p => p.GetRequiredService<GymService>().ResolutionsAsync(regular.Lines.Single().Id, default)); Assert.Empty(unresolved);
        var line = regular.Lines.Single();
        var resolutionRequest = new ResolutionInput(line.Version, VarianceDirection.Missing, VarianceCategory.Sale, 1, "Đã kiểm tra", "loa/QR ngoài hệ thống", Guid.NewGuid());
        var resolved = await s.Run(p => p.GetRequiredService<GymService>().ResolveAsync(line.Id, resolutionRequest, s.Actor, default));
        var replay = await s.Run(p => p.GetRequiredService<GymService>().ResolveAsync(line.Id, resolutionRequest, s.Actor, default)); Assert.Equal(resolved.Id, replay.Id);
        Assert.Equal("VARIANCE_EXCEEDED", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<GymService>().ResolveAsync(line.Id, new(line.Version, VarianceDirection.Missing, VarianceCategory.Loss, 2, "mất", null, Guid.NewGuid()), s.Actor, default)))).Code);
        await s.Move(new(Guid.NewGuid(), StockDocumentType.GymRecall, "Thu hồi", [new(s.Batch.Id, 1, gym.LocationId, StockCondition.Good, s.Location, StockCondition.Quarantine, null)]));
        stock = await s.Run(p => p.GetRequiredService<GymService>().StockAsync(gym.Id, default));
        Assert.False(stock.IsRealtime); Assert.NotNull(stock.LastCountedAtUtc); Assert.Equal(2, stock.LastCountLines.Single().ActualGood); Assert.Equal(1, stock.BookEstimate.Single().Good);
        var balances = await s.Run(p => p.GetRequiredService<InventoryService>().BalancesAsync(1, 100, default));
        Assert.DoesNotContain(balances.Items, x => x.LocationId == gym.LocationId);
        Assert.Equal(6, balances.Items.Single(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id && x.Condition == StockCondition.Good).Available);
        Assert.Equal(0, balances.Items.Single(x => x.LocationId == s.Location && x.BatchId == s.Batch.Id && x.Condition == StockCondition.Quarantine).Available);
        Assert.Equal("INSUFFICIENT_STOCK", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(s.Cart(7), Guid.NewGuid(), default)))).Code);
    }

    [SqlServerFact]
    public async Task GymCount_RejectsStalePreviewAfterMovement_AndMissingLines()
    {
        await using var s = await Setup(); var gym = await s.Gym(); var preview = await s.Run(p => p.GetRequiredService<GymService>().StockAsync(gym.Id, default));
        await s.Move(new(Guid.NewGuid(), StockDocumentType.GymDelivery, "Ký gửi", [new(s.Batch.Id, 1, s.Location, StockCondition.Good, gym.LocationId, StockCondition.Good, null)]));
        Assert.Equal("VERSION_CONFLICT", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<GymService>().CountAsync(gym.Id, new(preview.Version, [new(s.Batch.Id, 1, 0, 0, null)], null), s.Actor, true, default)))).Code);
    }

    [SqlServerFact]
    public async Task PostedMovement_IsIdempotent_AndRejectsDifferentPayloadForSameKey()
    {
        await using var s = await Setup(); var request = new MovementInput(Guid.NewGuid(), StockDocumentType.ProductionReceipt, "Thêm hàng", [new(s.Batch.Id, 2, null, null, s.Location, StockCondition.Good, null)]);
        var one = await s.Move(request); var two = await s.Move(request); Assert.Equal(one.Id, two.Id);
        Assert.Equal("IDEMPOTENCY_CONFLICT", (await Assert.ThrowsAsync<BusinessException>(() => s.Move(request with { Reason = "other" }))).Code);
    }

    [SqlServerFact]
    public async Task Catalog_RejectsStaleEditAndUsedWeightChange_AndPublicContentHidesDrafts()
    {
        await using var s = await Setup();
        var bad = s.ProductInput with { Version = "stale" };
        Assert.Equal("VERSION_CONFLICT", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<CatalogService>().SaveAsync(s.Product.Id, bad, s.Actor, default)))).Code);
        Assert.Equal("PACK_IMMUTABLE", (await Assert.ThrowsAsync<BusinessException>(() => s.Run(p => p.GetRequiredService<CatalogService>().SaveVariantAsync(s.Product.Id, s.Variant.Id, new(s.Variant.SkuCode, "200g", 250, 35000, true, 0, s.Variant.Version), s.Actor, default)))).Code);
        var draft = await s.Run(p => p.GetRequiredService<ContentReportService>().SaveSectionAsync("HomeHero", new("Ức gà", "Câu chuyện", [], "Xem sản phẩm", "/products", null), s.Actor, default));
        Assert.DoesNotContain(await s.Run(p => p.GetRequiredService<ContentReportService>().ContentAsync(true, default)), x => x.SectionKey == "HomeHero" && x.Content.Title == "Ức gà");
        await s.Run(p => p.GetRequiredService<ContentReportService>().PublishAsync("HomeHero", draft.Version!, s.Actor, default));
        Assert.Contains(await s.Run(p => p.GetRequiredService<ContentReportService>().ContentAsync(true, default)), x => x.Content.Title == "Ức gà" && x.Version == null);
    }

    [SqlServerFact]
    public async Task TelegramJob_IsAtomicWithOrder_AndWorkerRetriesThenMarksSent()
    {
        await using var s = await Setup(notify: true); var order = await s.Order(1);
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var job = await db.NotificationJobs.SingleAsync(x => x.OrderId == order.Id);
            using var payload = JsonDocument.Parse(job.PayloadJson);
            Assert.Equal(new[] { "ItemsSubtotal", "OrderNumber", "PackageCount" }, payload.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal(NotificationStatus.Pending, job.Status); return true;
        });
        s.Sender.Result = new(false, null, false, 1, "TEST_TRANSIENT");
        Assert.True(await s.Run(p => p.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(default)));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var job = await db.NotificationJobs.SingleAsync(x => x.OrderId == order.Id);
            Assert.Equal(1, job.AttemptCount); Assert.Equal(NotificationStatus.Pending, job.Status); Assert.Null(job.LeaseToken);
            job.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1); await db.SaveChangesAsync(); return true;
        });
        s.Sender.Result = new(true, 123, false, null, null);
        Assert.True(await s.Run(p => p.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(default)));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var job = await db.NotificationJobs.SingleAsync(x => x.OrderId == order.Id);
            Assert.Equal(NotificationStatus.Sent, job.Status); Assert.Equal(123, job.TelegramMessageId); Assert.Null(job.LeaseToken); return true;
        });
    }

    [SqlServerFact]
    public async Task DurableJobSaveFailure_RollsBackOrderItemsEventsAndRequestResult()
    {
        await using var s = await Setup(notify: true, failJob: true); var key = Guid.NewGuid();
        var before = await s.Run(p => p.GetRequiredService<ForFitChickenDbContext>().Orders.CountAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(s.Cart(1), key, default)));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); Assert.Equal(before, await db.Orders.CountAsync());
            Assert.False(await db.IdempotencyRequests.AnyAsync(x => x.RequestKey == key.ToString("N"))); return true;
        });
    }

    [SqlServerFact]
    public async Task PublicHttp_RequiresCsrfForCheckout_AndProtectsPrivateApi()
    {
        await using var s = await Setup(); using var client = s.Factory.Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/inventory/balances")).StatusCode);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/orders", s.Cart(1))).StatusCode);
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/csrf"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
        var result = await client.PostAsJsonAsync("/api/orders", s.Cart(1)); Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var body = await result.Content.ReadAsStringAsync(); Assert.DoesNotContain("recipient", body.ToLowerInvariant()); Assert.DoesNotContain("unitCost", body);
        var json = JsonDocument.Parse(body); Assert.Equal("PendingConfirmation", json.RootElement.GetProperty("status").GetString());
        var schema = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var checkoutSchema = schema.GetProperty("components").GetProperty("schemas").GetProperty("CheckoutInput");
        Assert.Equal(1, checkoutSchema.GetProperty("properties").GetProperty("items").GetProperty("minItems").GetInt32());
        Assert.Contains(checkoutSchema.GetProperty("required").EnumerateArray(), x => x.GetString() == "items");
        Assert.Contains(schema.GetProperty("paths").GetProperty("/api/orders").GetProperty("post").GetProperty("parameters").EnumerateArray(),
            x => x.GetProperty("name").GetString() == "X-CSRF-TOKEN");
        var invalid = s.Cart(0);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/orders", invalid)).StatusCode);
    }

    [SqlServerFact]
    public async Task Report_UsesSeparateDatesAndMoneyStreams_WithoutInventingMissingCost()
    {
        await using var s = await Setup(); var order = await s.Transition(await s.Order(1), "confirm");
        order = await s.Transition(order, "handover"); order = await s.Transition(order, "complete");
        var from = DateTime.UtcNow.Date; var to = from.AddDays(1);
        var report = await s.Run(p => p.GetRequiredService<ContentReportService>().ReportAsync(from, to, default));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>();
            Assert.Equal(await db.Orders.CountAsync(x => x.Status == OrderStatus.Completed && x.CompletedAtUtc >= from && x.CompletedAtUtc < to), report.OrdersCompleted);
            Assert.Equal(await db.CodReceipts.Where(x => x.VoidedAtUtc == null && x.ReceivedAtUtc >= from && x.ReceivedAtUtc < to).SumAsync(x => x.Amount), report.CodReceived);
            Assert.True(report.IssuedPackagesWithUnknownCost >= 1); Assert.Contains("không phải lợi nhuận", report.Note); return true;
        });
    }

    [SqlServerFact]
    public async Task AdminHttp_ValidatesRecords_AndOperatorCannotReadFinance()
    {
        await using var s = await Setup(); using var owner = s.Factory.Client();
        var userName = await s.Run(async p => (await p.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(s.Actor.ToString()))!.UserName!);
        async Task Login(HttpClient client, string name)
        {
            var csrf = await client.GetFromJsonAsync<JsonElement>("/api/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/admin/auth/login", new { userName = name, password = "TestOnly-StrongPassword42!" })).StatusCode);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); csrf = await client.GetFromJsonAsync<JsonElement>("/api/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
        }
        await Login(owner, userName);
        var created = await owner.PostAsJsonAsync("/api/admin/products", s.ProductInput with { Slug = Guid.NewGuid().ToString("N"), IsPublished = false, Version = null });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var data = await created.Content.ReadFromJsonAsync<JsonElement>(); var id = data.GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/admin/products/{id}/variants", new VariantInput("BAD", "200g", 200, 0, true, 0, null))).StatusCode);
        var manual = await s.Run(p => p.GetRequiredService<OrderService>().ManualAsync(s.Cart(1), Guid.NewGuid(), OrderSource.Facebook, s.Actor, default));
        Assert.Equal(OrderStatus.PendingConfirmation, manual.Status);
        var orderList = await owner.GetFromJsonAsync<JsonElement>("/api/admin/orders?search=" + manual.OrderNumber);
        var httpOrder = orderList.GetProperty("items")[0];
        var httpOrderId = httpOrder.GetProperty("id").GetInt64();
        foreach (var command in new[] { "confirm", "prepare", "handover", "complete" })
        {
            var transitioned = await owner.PostAsJsonAsync($"/api/admin/orders/{httpOrderId}/actions/{command}",
                new { version = httpOrder.GetProperty("version").GetString(), reason = (string?)null });
            Assert.Equal(HttpStatusCode.OK, transitioned.StatusCode);
            httpOrder = await transitioned.Content.ReadFromJsonAsync<JsonElement>();
        }
        Assert.Equal("Completed", httpOrder.GetProperty("status").GetString());
        var name = await s.Run(async p =>
        {
            var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var account = new ApplicationUser { UserName = Guid.NewGuid().ToString("N"), DisplayName = "Operator" };
            Assert.True((await users.CreateAsync(account, "TestOnly-StrongPassword42!")).Succeeded); Assert.True((await users.AddToRoleAsync(account, "Operator")).Succeeded); return account.UserName;
        });
        using var op = s.Factory.Client(); await Login(op, name);
        Assert.Equal(HttpStatusCode.Forbidden, (await op.GetAsync("/api/admin/reports/sales?fromUtc=2026-10-01T00%3A00%3A00Z&toUtc=2026-11-01T00%3A00%3A00Z")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await op.GetAsync($"/api/admin/orders/1/receipts")).StatusCode);
        var publicProduct = await op.GetStringAsync($"/api/products/{s.Product.Id}"); Assert.DoesNotContain("unitCost", publicProduct);
    }

    [SqlServerFact]
    public async Task NotificationLease_IsExclusive_AndStaleWorkerCannotOverwriteNewLeaseResult()
    {
        await using var s = await Setup(notify: true); var order = await s.Order(1);
        s.Sender.BlockFirst = true; s.Sender.Result = new(true, 111, false, null, null);
        var first = s.Run(p => p.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(default));
        await s.Sender.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            Assert.False(await s.Run(p => p.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(default)));
            await s.Run(async p =>
            {
                var db = p.GetRequiredService<ForFitChickenDbContext>(); var job = await db.NotificationJobs.SingleAsync(x => x.OrderId == order.Id);
                job.LeaseUntilUtc = DateTime.UtcNow.AddSeconds(-1); await db.SaveChangesAsync(); return true;
            });
            s.Sender.Result = new(true, 222, false, null, null);
            Assert.True(await s.Run(p => p.GetRequiredService<NotificationDispatcher>().ProcessOneAsync(default)));
        }
        finally { s.Sender.Release.TrySetResult(true); await first; }
        await s.Run(async p =>
        {
            var job = await p.GetRequiredService<ForFitChickenDbContext>().NotificationJobs.SingleAsync(x => x.OrderId == order.Id);
            Assert.Equal(NotificationStatus.Sent, job.Status); Assert.Equal(222, job.TelegramMessageId); Assert.Equal(2, job.AttemptCount); return true;
        });
    }

    private async Task<Scenario> Setup(int stock = 10, bool notify = false, bool failJob = false)
    {
        var sender = new FakeSender();
        var factory = new AdminAuthApiFactory(database, configure: services =>
        {
            services.RemoveAll<INotificationSettings>(); services.AddSingleton<INotificationSettings>(new TestNotifications(notify));
            services.RemoveAll<ITelegramSender>(); services.AddSingleton<ITelegramSender>(sender);
        }, contextOptions: options => { if (failJob) options.AddInterceptors(new FailJobSaveInterceptor()); });
        var s = new Scenario(factory, sender); var code = Guid.NewGuid().ToString("N");
        s.Actor = await s.Run(async p =>
        {
            var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { UserName = code, DisplayName = "Test owner" };
            Assert.True((await users.CreateAsync(user, "TestOnly-StrongPassword42!")).Succeeded); Assert.True((await users.AddToRoleAsync(user, "Owner")).Succeeded); return user.Id;
        });
        s.Location = await s.Run(p => p.GetRequiredService<InventoryService>().InitializeWarehouseAsync(s.Actor, default));
        s.ProductInput = new("Chicken " + code, code, "Giới thiệu", "Câu chuyện", null, null, null, null, true, false, false, 0, null);
        s.Product = await s.Run(p => p.GetRequiredService<CatalogService>().SaveAsync(null, s.ProductInput, s.Actor, default));
        s.Variant = await s.Run(p => p.GetRequiredService<CatalogService>().SaveVariantAsync(s.Product.Id, null, new("SKU-" + code, "200g", 200, 35000, true, 0, null), s.Actor, default));
        var asset = await s.Run(async p =>
        {
            var db = p.GetRequiredService<ForFitChickenDbContext>(); var image = new MediaAsset { StorageKey = code + ".png", ContentType = "image/png", ByteSize = 100, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = s.Actor };
            db.Add(image); await db.SaveChangesAsync(); return image.Id;
        });
        await s.Run(p => p.GetRequiredService<CatalogService>().AddImageAsync(s.Product.Id, new(asset, null, "Ảnh", 0), s.Product.Version!, s.Actor, default));
        s.Product = await s.Run(p => p.GetRequiredService<CatalogService>().GetAsync(s.Product.Id, false, default));
        s.ProductInput = s.ProductInput with { IsPublished = true, Version = s.Product.Version };
        s.Product = await s.Run(p => p.GetRequiredService<CatalogService>().SaveAsync(s.Product.Id, s.ProductInput, s.Actor, default));
        s.Batch = await s.Run(p => p.GetRequiredService<InventoryService>().CreateBatchAsync(new(s.Variant.Id, "B-" + code, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow.AddDays(5), null), s.Actor, default));
        if (stock > 0) await s.Receive(s.Batch.Id, stock);
        return s;
    }
    private sealed class Scenario(AdminAuthApiFactory factory, FakeSender sender) : IAsyncDisposable
    {
        public AdminAuthApiFactory Factory { get; } = factory;
        public FakeSender Sender { get; } = sender;
        public long Actor { get; set; }
        public long Location { get; set; }
        public ProductInput ProductInput { get; set; } = null!;
        public ProductView Product { get; set; } = null!;
        public VariantView Variant { get; set; } = null!;
        public BatchView Batch { get; set; } = null!;
        public async Task<T> Run<T>(Func<IServiceProvider, Task<T>> work) { using var scope = Factory.Services.CreateScope(); return await work(scope.ServiceProvider); }
        public CheckoutInput Cart(int count) => new("Khách thử", "0900000000", "Hà Nội", null, [new(Variant.Id, count)]);
        public async Task<OrderView> Order(int count)
        {
            var result = await Run(p => p.GetRequiredService<OrderService>().CheckoutAsync(Cart(count), Guid.NewGuid(), default));
            var list = await Run(p => p.GetRequiredService<OrderService>().ListAsync(null, result.OrderNumber, 1, 10, default)); return Assert.Single(list.Items);
        }
        public Task<OrderView> Transition(OrderView order, string action, string? reason = null) => Run(p => p.GetRequiredService<OrderService>().TransitionAsync(order.Id, action, order.Version, reason, Actor, default));
        public Task<DocumentView> Move(MovementInput input) => Run(p => p.GetRequiredService<InventoryService>().PostAsync(input, Actor, true, default));
        public Task<DocumentView> Receive(long batch, int count) => Move(new(Guid.NewGuid(), StockDocumentType.ProductionReceipt, "Nhập thành phẩm", [new(batch, count, null, null, Location, StockCondition.Good, null)]));
        public Task<GymView> Gym() => Run(p => p.GetRequiredService<GymService>().SaveAsync(null,
            new("G-" + Guid.NewGuid().ToString("N"), "Gym", "Hà Nội", null, null, null, null, "Nội bộ", null, true, true, null), Actor, default));
        public ValueTask DisposeAsync() => Factory.DisposeAsync();
    }
    private sealed class TestNotifications(bool enabled) : INotificationSettings { public NotificationSettings Current => new(enabled, -100123456); }
    private sealed class FailJobSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<NotificationJob>().Any(x => x.State == EntityState.Added))
                throw new InvalidOperationException("Simulated durable notification write failure.");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class FakeSender : ITelegramSender
    {
        private int _calls;
        public bool BlockFirst { get; set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TelegramSendResult Result { get; set; } = new(true, 123, false, null, null);
        public async Task<TelegramSendResult> SendAsync(long chatId, string payloadJson, CancellationToken token)
        {
            var result = Result;
            if (Interlocked.Increment(ref _calls) == 1 && BlockFirst)
            { Started.TrySetResult(true); await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), token); }
            return result;
        }
    }
}
