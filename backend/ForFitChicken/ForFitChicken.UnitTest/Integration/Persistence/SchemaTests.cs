using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;
using ForFitChicken.Infrastructure.Identity;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ForFitChicken.UnitTest.Integration.Persistence;

[Trait("Category", "SqlServerIntegration")]
public sealed class SchemaTests(MigratedDatabaseFixture fixture) : IClassFixture<MigratedDatabaseFixture>
{
    [SqlServerFact]
    public async Task Migration_CreatesCoreAndIdentityTables_WithoutOptionalExpenses()
    {
        await using var db = fixture.Context();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        var tableCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] <> '__EFMigrationsHistory'").SingleAsync();
        Assert.Equal(29, tableCount);
        Assert.Equal(new[] { "Operator", "Owner" }, await db.Roles.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE delete_referential_action <> 0").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = 'OperatingExpenses'").SingleAsync());
    }

    [SqlServerFact]
    public async Task RowVersion_RejectsStaleUpdate_AndFalseActiveIsPreserved()
    {
        await using var first = fixture.Context();
        var product = new Product { Name = "Ức gà", Slug = Guid.NewGuid().ToString("N"), IsActive = false };
        first.Products.Add(product);
        await first.SaveChangesAsync();
        await using var second = fixture.Context();
        var stale = await second.Products.SingleAsync(x => x.Id == product.Id);
        Assert.False(stale.IsActive);
        Assert.Equal(8, stale.Version.Length);
        product.Name = "Ức gà tiêu đen";
        await first.SaveChangesAsync();
        stale.Name = "Overwritten";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task CompositeImageForeignKey_RejectsVariantFromAnotherProduct()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        var other = new Product { Name = "Other", Slug = Guid.NewGuid().ToString("N") };
        var asset = new MediaAsset { StorageKey = Guid.NewGuid().ToString("N"), ContentType = "image/webp", ByteSize = 100, CreatedByUserId = data.User.Id };
        db.AddRange(other, asset);
        await db.SaveChangesAsync();
        db.ProductImages.Add(new ProductImage { ProductId = other.Id, VariantId = data.Variant.Id, AssetId = asset.Id });
        await RejectSaveAsync(db, 547);
    }

    [SqlServerFact]
    public async Task Balances_RejectOverReservationAndNonGoodReservation()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        data.Balance.Reserved = data.Balance.OnHand + 1;
        await RejectSaveAsync(db, 547);
        db.ChangeTracker.Clear();
        var balance = await db.WarehouseBalances.SingleAsync(x => x.Id == data.Balance.Id);
        balance.Condition = StockCondition.Quarantine;
        balance.Reserved = 1;
        await RejectSaveAsync(db, 547);
    }

    [SqlServerFact]
    public async Task OrderItem_ComputesTotal_AndRejectsDuplicateVariant()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        Assert.Equal(70000m, data.Item.LineTotal);
        db.OrderItems.Add(new OrderItem { OrderId = data.Order.Id, VariantId = data.Variant.Id,
            SkuCodeSnapshot = "SKU", ProductNameSnapshot = "Ức gà", VariantLabelSnapshot = "200g",
            NetWeightGramsSnapshot = 200, Quantity = 1, UnitPrice = 35000 });
        await RejectSaveAsync(db, 2601, 2627);
    }

    [SqlServerFact]
    public async Task Allocations_AllowReleasedHistory_ButOnlyOneActiveItemBalancePair()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        var active = new OrderAllocation { OrderItemId = data.Item.Id, BalanceId = data.Balance.Id, Quantity = 1,
            State = AllocationState.Reserved, ReservedAtUtc = DateTime.UtcNow };
        var history = new OrderAllocation { OrderItemId = data.Item.Id, BalanceId = data.Balance.Id, Quantity = 1,
            State = AllocationState.Released, ReservedAtUtc = DateTime.UtcNow, ReleasedAtUtc = DateTime.UtcNow, ReleaseReason = "Correction" };
        db.AddRange(active, history);
        await db.SaveChangesAsync();
        db.OrderAllocations.Add(new OrderAllocation { OrderItemId = data.Item.Id, BalanceId = data.Balance.Id,
            Quantity = 1, State = AllocationState.Reserved, ReservedAtUtc = DateTime.UtcNow });
        await RejectSaveAsync(db, 2601, 2627);
    }

    [SqlServerFact]
    public async Task GymCounts_RejectSecondPostedRoot_AndCrossGymPredecessor()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        var gym = new Gym { GymCode = Guid.NewGuid().ToString("N"), Name = "Gym A", PublicAddress = "Hà Nội" };
        var other = new Gym { GymCode = Guid.NewGuid().ToString("N"), Name = "Gym B", PublicAddress = "Hà Nội" };
        db.AddRange(gym, other);
        await db.SaveChangesAsync();
        GymStockCount Count(long gymId) => new() { CountNumber = Guid.NewGuid().ToString("N"), GymId = gymId,
            Kind = StockCountKind.OpeningBaseline, Status = PostingStatus.Posted, CountedAtUtc = DateTime.UtcNow,
            CountedByUserId = data.User.Id, CreatedByUserId = data.User.Id, PostedAtUtc = DateTime.UtcNow, PostedByUserId = data.User.Id };
        var root = Count(gym.Id);
        db.Add(root);
        await db.SaveChangesAsync();
        db.Add(Count(gym.Id));
        await RejectSaveAsync(db, 2601, 2627);
        db.ChangeTracker.Clear();
        var crossGym = Count(other.Id);
        crossGym.PreviousCountId = root.Id;
        db.Add(crossGym);
        await RejectSaveAsync(db, 547);
    }

    [SqlServerFact]
    public async Task Notification_RejectsInvalidJson_AndProcessingWithoutLease()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        NotificationJob Job() => new() { OrderId = data.Order.Id, TargetChatId = -1001234567890,
            PayloadJson = "{}", NextAttemptAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
        var invalidJson = Job();
        invalidJson.PayloadJson = "invalid";
        db.Add(invalidJson);
        await RejectSaveAsync(db, 547);
        db.ChangeTracker.Clear();
        var missingLease = Job();
        missingLease.Status = NotificationStatus.Processing;
        db.Add(missingLease);
        await RejectSaveAsync(db, 547);
        db.ChangeTracker.Clear();
        db.Add(Job());
        await db.SaveChangesAsync();
    }

    [SqlServerFact]
    public async Task Receipt_AllowsVoidedHistory_ButOnlyOneEffectiveReceipt()
    {
        await using var db = fixture.Context();
        var data = await SeedAsync(db);
        CodReceipt Receipt() => new() { OrderId = data.Order.Id, Amount = 70000, ReceivedAtUtc = DateTime.UtcNow,
            RecordedAtUtc = DateTime.UtcNow, RecordedByUserId = data.User.Id };
        var history = Receipt();
        history.VoidedAtUtc = DateTime.UtcNow;
        history.VoidedByUserId = data.User.Id;
        history.VoidReason = "Wrong entry";
        db.AddRange(history, Receipt());
        await db.SaveChangesAsync();
        db.Add(Receipt());
        await RejectSaveAsync(db, 2601, 2627);
    }

    private static async Task RejectSaveAsync(ForFitChickenDbContext db, params int[] errorNumbers)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var sql = Assert.IsType<SqlException>(error.InnerException);
        Assert.Contains(sql.Number, errorNumbers);
    }

    private static async Task<Scenario> SeedAsync(ForFitChickenDbContext db)
    {
        var code = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser { UserName = code, NormalizedUserName = code.ToUpperInvariant(), DisplayName = "Test" };
        var product = new Product { Name = "Ức gà tiêu đen", Slug = code };
        var location = new StockLocation { LocationCode = code, Name = "Main", Kind = StockLocationKind.Warehouse };
        var order = new Order { OrderNumber = code, RecipientName = "Test", RecipientPhone = "0900000000",
            RecipientAddress = "Hà Nội", ItemsSubtotal = 70000 };
        db.AddRange(user, product, location, order);
        await db.SaveChangesAsync();
        var variant = new ProductVariant { ProductId = product.Id, SkuCode = code.ToUpperInvariant(), Label = "200g",
            NetWeightGrams = 200, SellingPrice = 35000 };
        db.Add(variant);
        await db.SaveChangesAsync();
        var batch = new ProductBatch { VariantId = variant.Id, BatchCode = code.ToUpperInvariant(),
            ManufacturedOn = DateOnly.FromDateTime(DateTime.UtcNow), ExpiresAtUtc = DateTime.UtcNow.AddDays(5), CreatedByUserId = user.Id };
        var item = new OrderItem { OrderId = order.Id, VariantId = variant.Id, SkuCodeSnapshot = variant.SkuCode,
            ProductNameSnapshot = product.Name, VariantLabelSnapshot = variant.Label, NetWeightGramsSnapshot = 200,
            Quantity = 2, UnitPrice = 35000 };
        db.AddRange(batch, item);
        await db.SaveChangesAsync();
        var balance = new WarehouseBalance { LocationId = location.Id, BatchId = batch.Id,
            Condition = StockCondition.Good, OnHand = 10 };
        db.Add(balance);
        await db.SaveChangesAsync();
        return new Scenario(user, variant, balance, order, item);
    }

    private sealed record Scenario(ApplicationUser User, ProductVariant Variant, WarehouseBalance Balance, Order Order, OrderItem Item);
}
