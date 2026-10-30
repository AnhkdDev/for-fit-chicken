using System.Text.Json;
using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed class ContentReportService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock) : BusinessService(store, unit, clock)
{
    private static readonly string[] Keys = ["HomeProducts", "HomeHero", "Story", "Usage", "Faq", "Contact", "SiteSettings"];
    private async Task ValidateProducts(SiteSectionInput input, CancellationToken token)
    {
        var ids = input.ProductIds ?? [];
        Check(ids.Length <= 50 && ids.All(x => x > 0) && ids.Distinct().Count() == ids.Length, "INVALID_PRODUCTS", "Tối đa 50 sản phẩm, ID hợp lệ và không trùng.", 400);
        foreach(var choice in input.ProductMainImages ?? new Dictionary<long,long>()) {
            Check(ids.Contains(choice.Key) && await Store.AnyAsync(Store.Query<ProductImage>().Where(x => x.ProductId == choice.Key && x.AssetId == choice.Value), token), "INVALID_PRODUCT_IMAGE", "Ảnh chính phải thuộc sản phẩm được chọn.", 400);
        }
        foreach(var id in ids) { var product = await Need<Product>(id, token); Check(product.IsActive && product.IsPublished, "PRODUCT_UNAVAILABLE", "Chỉ chọn sản phẩm hoạt động và đã công bố.", 400); }
    }
    public async Task<SectionView[]> ContentAsync(bool publicOnly, CancellationToken token) =>
        (await Store.ListAsync(Store.Query<SiteContent>().Where(x => !publicOnly || x.PublishedJson != null).OrderBy(x => x.SectionKey), token))
        .Select(x => new SectionView(x.SectionKey, JsonSerializer.Deserialize<SiteSectionInput>(publicOnly ? x.PublishedJson! : x.DraftJson)!, x.PublishedAtUtc, publicOnly ? null : Revision(x.Version))).ToArray();

    public Task<SectionView> SaveSectionAsync(string key, SiteSectionInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); Check(Keys.Contains(key), "INVALID_SECTION", "Section không được hỗ trợ.", 400);
        Check(input.AssetIds is not null && input.AssetIds.Length <= 20 && input.AssetIds.Distinct().Count() == input.AssetIds.Length, "INVALID_ASSETS", "Tối đa 20 ảnh, không trùng.", 400);
        Check(input.LinkUrl is null || (input.LinkUrl.StartsWith('/') && !input.LinkUrl.StartsWith("//") && !input.LinkUrl.Contains('\\')) ||
            (Uri.TryCreate(input.LinkUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps), "INVALID_LINK", "Chỉ chấp nhận đường dẫn nội bộ hoặc HTTPS.", 400);
        if(key == "HomeProducts") await ValidateProducts(input, token);
        foreach (var id in input.AssetIds!) await Need<MediaAsset>(id, token);
        var section = (await Store.ListAsync(Store.Query<SiteContent>().Where(x => x.SectionKey == key), token)).SingleOrDefault();
        if (section is null) { section = new SiteContent { SectionKey = key, SchemaVersion = 1 }; Store.Add(section); }
        else Version(section.Version, input.Version);
        section.DraftJson = JsonSerializer.Serialize(input with { Version = null }); section.UpdatedAtUtc = Now; section.UpdatedByUserId = actor;
        await Unit.SaveChangesAsync(token); Audit(actor, "SiteDraftSaved", nameof(SiteContent), section.Id);
        return new SectionView(key, input with { Version = null }, section.PublishedAtUtc, Revision(section.Version));
    }, token);

    public Task<SectionView> PublishAsync(string key, string version, long actor, CancellationToken token) => Write(async () =>
    {
        var section = (await Store.ListAsync(Store.Query<SiteContent>().Where(x => x.SectionKey == key), token)).SingleOrDefault()
            ?? throw new BusinessException(404, "NOT_FOUND", "Chưa có bản nháp.");
        Version(section.Version, version); var content = JsonSerializer.Deserialize<SiteSectionInput>(section.DraftJson)!;
        if(key == "HomeProducts") await ValidateProducts(content, token);
        foreach (var asset in content.AssetIds) await Need<MediaAsset>(asset, token);
        section.PublishedJson = section.DraftJson; section.PublishedAtUtc = Now; section.PublishedByUserId = actor; section.UpdatedAtUtc = Now;
        await Unit.SaveChangesAsync(token); Audit(actor, "SitePublished", nameof(SiteContent), section.Id);
        return new SectionView(key, content, section.PublishedAtUtc, Revision(section.Version));
    }, token);

    public async Task<SalesReport> ReportAsync(DateTime from, DateTime to, CancellationToken token)
    {
        Check(from.Kind == DateTimeKind.Utc && to.Kind == DateTimeKind.Utc && from < to && (to - from).TotalDays <= 366,
            "INVALID_DATE_RANGE", "Cần khoảng UTC [from,to), tối đa 366 ngày.", 400);
        var requested = await Store.CountAsync(Store.Query<Order>().Where(x => x.CreatedAtUtc >= from && x.CreatedAtUtc < to), token);
        var completed = await Store.ListAsync(Store.Query<Order>().Where(x => x.Status == OrderStatus.Completed && x.CompletedAtUtc >= from && x.CompletedAtUtc < to)
            .GroupBy(x => 1).Select(g => new { Count = g.Count(), Value = g.Sum(x => x.ItemsSubtotal) }), token);
        var received = await Store.ListAsync(Store.Query<CodReceipt>().Where(x => x.VoidedAtUtc == null && x.ReceivedAtUtc >= from && x.ReceivedAtUtc < to)
            .GroupBy(x => 1).Select(g => g.Sum(x => x.Amount)), token);
        var costs = await Store.ListAsync(Store.Query<OrderAllocation>().Where(x => x.State == AllocationState.Issued && x.IssuedAtUtc >= from && x.IssuedAtUtc < to)
            .GroupBy(x => 1).Select(g => new { Known = g.Sum(x => x.UnitCostSnapshot == null ? 0m : x.UnitCostSnapshot.Value * x.Quantity), Unknown = g.Sum(x => x.UnitCostSnapshot == null ? x.Quantity : 0) }), token);
        return new(from, to, requested, completed.FirstOrDefault()?.Count ?? 0, completed.FirstOrDefault()?.Value ?? 0,
            received.FirstOrDefault(), costs.FirstOrDefault()?.Known ?? 0, costs.FirstOrDefault()?.Unknown ?? 0,
            "Tiền hàng online/COD; không phải lợi nhuận ròng. Doanh số hoàn tất, tiền nhận và giá vốn xuất dùng mốc thời gian riêng. Không gồm phí giao, hoa hồng, tiền gym hoặc chi phí vận hành; giá vốn chưa biết không bằng 0.");
    }

    public async Task<Page<NotificationJobView>> NotificationsAsync(int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size); var query = Store.Query<NotificationJob>();
        var rows = await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new NotificationJobView(x.Id, x.OrderId, x.EventType, x.Status, x.AttemptCount, x.NextAttemptAtUtc, x.LastError, x.CreatedAtUtc, x.SentAtUtc)), token);
        return new(rows, await Store.CountAsync(query, token), page, size);
    }
    public Task<bool> RetryNotificationAsync(long id, long actor, CancellationToken token) => Write(async () =>
    {
        var job = await Need<NotificationJob>(id, token); Check(job.Status == NotificationStatus.Failed, "JOB_NOT_FAILED", "Chỉ retry thủ công job Failed.");
        job.Status = NotificationStatus.Pending; job.AttemptCount = 0; job.NextAttemptAtUtc = Now; job.LastError = null;
        Audit(actor, "NotificationRetried", nameof(NotificationJob), id); return true;
    }, token);
}
