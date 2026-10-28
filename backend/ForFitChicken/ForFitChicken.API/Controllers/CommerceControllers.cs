using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ForFitChicken.API.Authentication;
using ForFitChicken.API.RateLimiting;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Domain.Enums;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ForFitChicken.API.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class StaffController : ControllerBase
{
    protected long Actor => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    protected bool Owner => User.IsInRole(AdminRoles.Owner);
}

[ApiController, Route("api"), AllowAnonymous, EnableRateLimiting(RateLimitPolicies.Catalog)]
public sealed class StorefrontController(CatalogService catalog, GymService gyms, ContentReportService content) : ControllerBase
{
    [HttpGet("csrf")]
    public CsrfTokenResponse Csrf([FromServices] IAntiforgery antiforgery) => new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);
    [HttpGet("products")]
    public Task<Page<ProductView>> Products(int page = 1, int size = 20, CancellationToken token = default) => catalog.ListAsync(true, page, size, token);
    [HttpGet("products/{id:long}")]
    public Task<ProductView> Product(long id, CancellationToken token) => catalog.GetAsync(id, true, token);
    [HttpGet("gyms")]
    public async Task<Page<PublicGymView>> Gyms(int page = 1, int size = 20, CancellationToken token = default)
    {
        var result = await gyms.ListAsync(true, page, size, token);
        return new(result.Items.Select(x => new PublicGymView(x.Id, x.Name, x.PublicAddress, x.PublicPhone, x.PublicDescription, x.CoverAssetId)).ToArray(), result.Total, result.PageNumber, result.PageSize);
    }
    [HttpGet("content")]
    public Task<SectionView[]> Content(CancellationToken token) => content.ContentAsync(true, token);
}

[ApiController, Route("api/orders"), AllowAnonymous, EnableRateLimiting(RateLimitPolicies.Checkout)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CheckoutController(OrderService orders) : ControllerBase
{
    [HttpPost, ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<CheckoutResult>> Checkout(CheckoutInput input, [Required, FromHeader(Name = "Idempotency-Key")] Guid key, CancellationToken token) =>
        StatusCode(201, await orders.CheckoutAsync(input, key, token));
}

[ApiController, Route("api/media"), EnableRateLimiting(RateLimitPolicies.Catalog)]
public sealed class MediaController(CatalogService catalog) : StaffController
{
    [HttpGet("{id:long}"), AllowAnonymous]
    public async Task<IActionResult> Get(long id, CancellationToken token)
    {
        var (stream, type) = await catalog.OpenMediaAsync(id, User.IsInRole(AdminRoles.Owner) || User.IsInRole(AdminRoles.Operator), token);
        Response.Headers["X-Content-Type-Options"] = "nosniff"; Response.Headers.CacheControl = "no-store";
        return File(stream, type);
    }
    [HttpPost, Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin), RequestSizeLimit(11 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<AssetView> Upload(IFormFile file, CancellationToken token)
    { await using var stream = file.OpenReadStream(); return await catalog.UploadAsync(stream, Actor, token); }
}

[ApiController, Route("api/admin/products"), Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminCatalogController(CatalogService catalog) : StaffController
{
    [HttpGet]
    public Task<Page<ProductView>> List(int page = 1, int size = 20, CancellationToken token = default) => catalog.ListAsync(false, page, size, token);
    [HttpGet("{id:long}")]
    public Task<ProductView> Get(long id, CancellationToken token) => catalog.GetAsync(id, false, token);
    [HttpPost]
    public Task<ProductView> Create(ProductInput input, CancellationToken token) => catalog.SaveAsync(null, input, Actor, token);
    [HttpPut("{id:long}")]
    public Task<ProductView> Update(long id, ProductInput input, CancellationToken token) => catalog.SaveAsync(id, input, Actor, token);
    [HttpPost("{id:long}/variants")]
    public Task<VariantView> CreateVariant(long id, VariantInput input, CancellationToken token) => catalog.SaveVariantAsync(id, null, input, Actor, token);
    [HttpPut("{id:long}/variants/{variantId:long}")]
    public Task<VariantView> UpdateVariant(long id, long variantId, VariantInput input, CancellationToken token) => catalog.SaveVariantAsync(id, variantId, input, Actor, token);
    [HttpPost("{id:long}/images")]
    public Task<ImageView> AddImage(long id, AttachImageRequest input, CancellationToken token) => catalog.AddImageAsync(id, input.Image, input.Version, Actor, token);
    [HttpDelete("{id:long}/images/{imageId:long}")]
    public Task<bool> RemoveImage(long id, long imageId, VersionInput input, CancellationToken token) => catalog.RemoveImageAsync(id, imageId, input.Version, Actor, token);
}
public sealed record AttachImageRequest([Required] string Version, [Required] ImageInput Image);

[ApiController, Route("api/admin/inventory"), Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminInventoryController(InventoryService inventory) : StaffController
{
    [HttpPost("warehouse"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<long> Initialize(CancellationToken token) => inventory.InitializeWarehouseAsync(Actor, token);
    [HttpGet("locations")]
    public Task<LocationView[]> Locations(CancellationToken token) => inventory.LocationsAsync(token);
    [HttpGet("batches")]
    public Task<Page<BatchView>> Batches(int page = 1, int size = 20, CancellationToken token = default) => inventory.BatchesAsync(page, size, token);
    [HttpPost("batches")]
    public Task<BatchView> CreateBatch(BatchInput input, CancellationToken token) => inventory.CreateBatchAsync(input, Actor, token);
    [HttpPut("batches/{id:long}/block")]
    public Task<BatchView> Block(long id, BlockBatchInput input, CancellationToken token) => inventory.BlockAsync(id, input, Actor, token);
    [HttpPut("batches/{id:long}/cost"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<BatchView> Cost(long id, CostInput input, CancellationToken token) => inventory.SetCostAsync(id, input, Actor, token);
    [HttpGet("batches/{id:long}/cost"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<BatchCostView> GetCost(long id, CancellationToken token) => inventory.CostAsync(id, token);
    [HttpGet("balances")]
    public Task<Page<BalanceView>> Balances(int page = 1, int size = 20, CancellationToken token = default) => inventory.BalancesAsync(page, size, token);
    [HttpGet("documents")]
    public Task<Page<DocumentView>> Documents(int page = 1, int size = 20, CancellationToken token = default) => inventory.DocumentsAsync(page, size, token);
    [HttpPost("documents")]
    public Task<DocumentView> Post(MovementInput input, CancellationToken token) => inventory.PostAsync(input, Actor, Owner, token);
}

[ApiController, Route("api/admin/orders"), Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminOrdersController(OrderService orders) : StaffController
{
    [HttpPost]
    public Task<CheckoutResult> Create(ManualOrderInput input, [Required, FromHeader(Name = "Idempotency-Key")] Guid key, CancellationToken token) => orders.ManualAsync(input.Order, key, input.Source, Actor, token);
    [HttpGet]
    public Task<Page<OrderView>> List(OrderStatus? status = null, string? search = null, int page = 1, int size = 20, CancellationToken token = default) => orders.ListAsync(status, search, page, size, token);
    [HttpGet("{id:long}")]
    public Task<OrderView> Get(long id, CancellationToken token) => orders.GetAsync(id, token);
    [HttpGet("{id:long}/events")]
    public Task<OrderEventView[]> Events(long id, CancellationToken token) => orders.EventsAsync(id, token);
    [HttpPut("{id:long}")]
    public Task<OrderView> Edit(long id, OrderEditInput input, CancellationToken token) => orders.EditAsync(id, input, Actor, token);
    // "action" is a reserved MVC route value; use a non-reserved parameter name.
    [HttpPost("{id:long}/actions/{command}")]
    public Task<OrderView> Transition(long id, string command, OrderActionInput input, CancellationToken token) => orders.TransitionAsync(id, command, input.Version, input.Reason, Actor, token);
    [HttpGet("{id:long}/receipts"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<ReceiptView[]> Receipts(long id, CancellationToken token) => orders.ReceiptsAsync(id, token);
    [HttpPost("{id:long}/receipts"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<ReceiptView> Receive(long id, ReceiptInput input, CancellationToken token) => orders.ReceiveAsync(id, input, Actor, token);
    [HttpPost("receipts/{id:long}/void"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<ReceiptView> Void(long id, ReceiptVoidInput input, CancellationToken token) => orders.VoidReceiptAsync(id, input, Actor, token);
}
public sealed record OrderActionInput([Required] string Version, [StringLength(500)] string? Reason);
public sealed record ManualOrderInput(OrderSource Source, [Required] CheckoutInput Order);

[ApiController, Route("api/admin/gyms"), Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminGymsController(GymService gyms) : StaffController
{
    [HttpGet]
    public Task<Page<GymView>> List(int page = 1, int size = 20, CancellationToken token = default) => gyms.ListAsync(false, page, size, token);
    [HttpPost]
    public Task<GymView> Create(GymInput input, CancellationToken token) => gyms.SaveAsync(null, input, Actor, token);
    [HttpPut("{id:long}")]
    public Task<GymView> Update(long id, GymInput input, CancellationToken token) => gyms.SaveAsync(id, input, Actor, token);
    [HttpGet("{id:long}/stock")]
    public Task<GymStockView> Stock(long id, CancellationToken token) => gyms.StockAsync(id, token);
    [HttpGet("{id:long}/counts")]
    public Task<Page<CountView>> Counts(long id, int page = 1, int size = 20, CancellationToken token = default) => gyms.CountsAsync(id, page, size, token);
    [HttpPost("{id:long}/counts")]
    public Task<CountView> Count(long id, CountInput input, CancellationToken token) => gyms.CountAsync(id, input, Actor, Owner, token);
    [HttpGet("count-lines/{id:long}/resolutions")]
    public Task<ResolutionView[]> Resolutions(long id, CancellationToken token) => gyms.ResolutionsAsync(id, token);
    [HttpPost("count-lines/{id:long}/resolutions")]
    public Task<ResolutionView> Resolve(long id, ResolutionInput input, CancellationToken token) => gyms.ResolveAsync(id, input, Actor, token);
    [HttpPost("resolutions/{id:long}/void"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<ResolutionView> Void(long id, ReceiptVoidInput input, CancellationToken token) => gyms.VoidAsync(id, input, Actor, token);
}

[ApiController, Route("api/admin/content"), Authorize(Policy = AdminPolicies.Staff), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminContentController(ContentReportService content) : StaffController
{
    [HttpGet]
    public Task<SectionView[]> List(CancellationToken token) => content.ContentAsync(false, token);
    [HttpPut("{key}")]
    public Task<SectionView> Save(string key, SiteSectionInput input, CancellationToken token) => content.SaveSectionAsync(key, input, Actor, token);
    [HttpPost("{key}/publish"), Authorize(Policy = AdminPolicies.Owner)]
    public Task<SectionView> Publish(string key, VersionInput input, CancellationToken token) => content.PublishAsync(key, input.Version, Actor, token);
}

[ApiController, Route("api/admin/reports"), Authorize(Policy = AdminPolicies.Owner), EnableRateLimiting(RateLimitPolicies.Reports)]
public sealed class AdminReportsController(ContentReportService reports) : StaffController
{
    [HttpGet("sales")]
    public Task<SalesReport> Sales(DateTime fromUtc, DateTime toUtc, CancellationToken token) => reports.ReportAsync(fromUtc, toUtc, token);
}
[ApiController, Route("api/admin/notifications"), Authorize(Policy = AdminPolicies.Owner), EnableRateLimiting(RateLimitPolicies.Admin)]
public sealed class AdminNotificationsController(ContentReportService content) : StaffController
{
    [HttpGet]
    public Task<Page<NotificationJobView>> List(int page = 1, int size = 20, CancellationToken token = default) => content.NotificationsAsync(page, size, token);
    [HttpPost("{id:long}/retry")]
    public Task<bool> Retry(long id, CancellationToken token) => content.RetryNotificationAsync(id, Actor, token);
}
