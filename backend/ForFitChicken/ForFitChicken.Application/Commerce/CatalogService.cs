using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Domain.Entities;
using ForFitChicken.Domain.Enums;

namespace ForFitChicken.Application.Commerce;

public sealed class CatalogService(ICommerceStore store, IUnitOfWork unit, TimeProvider clock, IMediaStorage media, IMediaPolicySettings policySettings)
    : BusinessService(store, unit, clock)
{
    public async Task<Page<ProductView>> ListAsync(bool publicOnly, int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size);
        var query = Store.Query<Product>().Where(x => !publicOnly || (x.IsActive && x.IsPublished));
        var total = await Store.CountAsync(query, token);
        var products = await Store.ListAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size), token);
        var result = new List<ProductView>();
        foreach (var product in products) result.Add(await View(product, publicOnly, token));
        return new(result.ToArray(), total, page, size);
    }

    public async Task<ProductView> GetAsync(long id, bool publicOnly, CancellationToken token)
    {
        var entity = await Need<Product>(id, token);
        Check(!publicOnly || (entity.IsActive && entity.IsPublished), "NOT_FOUND", "Không tìm thấy sản phẩm.", 404);
        return await View(entity, publicOnly, token);
    }

    public Task<ProductView> SaveAsync(long? id, ProductInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input);
        Check(!string.IsNullOrWhiteSpace(input.Name), "INVALID_INPUT", "Tên sản phẩm bắt buộc.", 400);
        var product = id is null ? new Product { CreatedAtUtc = Now } : await Need<Product>(id.Value, token);
        if (id is not null) Version(product.Version, input.Version);
        Check(!await Store.AnyAsync(Store.Query<Product>().Where(x => x.Slug == input.Slug && x.Id != product.Id), token), "DUPLICATE_SLUG", "Slug đã tồn tại.");
        if (input.IsPublished)
        {
            Check(input.IsActive && id is not null, "NOT_READY_TO_PUBLISH", "Tạo bản nháp, thêm SKU và ảnh trước khi công bố.");
            Check(await Store.AnyAsync(Store.Query<ProductVariant>().Where(x => x.ProductId == product.Id && x.IsActive), token) &&
                await Store.AnyAsync(Store.Query<ProductImage>().Where(x => x.ProductId == product.Id), token), "NOT_READY_TO_PUBLISH", "Cần SKU hoạt động và ảnh sản phẩm.");
            product.FirstPublishedAtUtc ??= Now;
        }
        product.Name = input.Name.Trim(); product.Slug = input.Slug; product.ShortDescription = input.ShortDescription;
        product.Description = input.Description; product.Ingredients = input.Ingredients; product.NutritionText = input.NutritionText;
        product.UsageInstructions = input.UsageInstructions; product.StorageInstructions = input.StorageInstructions;
        product.IsActive = input.IsActive; product.IsPublished = input.IsPublished; product.IsFeatured = input.IsFeatured;
        product.SortOrder = input.SortOrder; product.UpdatedAtUtc = Now;
        if (id is null) Store.Add(product);
        await Unit.SaveChangesAsync(token);
        Audit(actor, "ProductSaved", nameof(Product), product.Id);
        return await View(product, false, token);
    }, token);

    public Task<VariantView> SaveVariantAsync(long productId, long? id, VariantInput input, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input);
        Check(input.SellingPrice == decimal.Truncate(input.SellingPrice), "INVALID_PRICE", "Giá VND phải là số nguyên.", 400);
        var product = await Need<Product>(productId, token);
        var variant = id is null ? new ProductVariant { ProductId = productId, CreatedAtUtc = Now } : await Need<ProductVariant>(id.Value, token);
        Check(variant.ProductId == productId, "NOT_FOUND", "SKU không thuộc sản phẩm.", 404);
        var sku = input.SkuCode.ToUpperInvariant();
        if (id is not null)
        {
            Version(variant.Version, input.Version);
            Check(variant.SkuCode == sku, "SKU_IMMUTABLE", "Không được thay mã SKU đã tạo.");
            var used = await Store.AnyAsync(Store.Query<ProductBatch>().Where(x => x.VariantId == variant.Id), token) ||
                await Store.AnyAsync(Store.Query<OrderItem>().Where(x => x.VariantId == variant.Id), token);
            Check(!used || variant.NetWeightGrams == input.NetWeightGrams, "PACK_IMMUTABLE", "Quy cách đã dùng; tạo SKU mới nếu đổi khối lượng.");
        }
        Check(!await Store.AnyAsync(Store.Query<ProductVariant>().Where(x => x.Id != variant.Id && (x.SkuCode == sku || (x.ProductId == productId && x.Label == input.Label))), token),
            "DUPLICATE_SKU", "SKU hoặc tên quy cách đã tồn tại.");
        variant.SkuCode = sku; variant.Label = input.Label.Trim(); variant.NetWeightGrams = input.NetWeightGrams;
        variant.SellingPrice = input.SellingPrice; variant.IsActive = input.IsActive; variant.SortOrder = input.SortOrder; variant.UpdatedAtUtc = Now;
        if (id is null) Store.Add(variant);
        await Unit.SaveChangesAsync(token);
        Audit(actor, "VariantSaved", nameof(ProductVariant), variant.Id);
        return (await View(product, false, token)).Variants.Single(x => x.Id == variant.Id);
    }, token);

    public Task<ImageView> AddImageAsync(long productId, ImageInput input, string version, long actor, CancellationToken token) => Write(async () =>
    {
        Validate(input); var product = await Need<Product>(productId, token); Version(product.Version, version);
        await Need<MediaAsset>(input.AssetId, token);
        if (input.VariantId is not null) Check((await Need<ProductVariant>(input.VariantId.Value, token)).ProductId == productId, "INVALID_VARIANT", "Ảnh và SKU phải cùng sản phẩm.", 400);
        var image = new ProductImage { ProductId = productId, VariantId = input.VariantId, AssetId = input.AssetId, AltText = input.AltText, SortOrder = input.SortOrder };
        Store.Add(image); product.UpdatedAtUtc = Now;
        await Unit.SaveChangesAsync(token); Audit(actor, "ImageAttached", nameof(Product), productId);
        return new ImageView(image.Id, image.AssetId, image.VariantId, image.AltText, image.SortOrder, $"/api/media/{image.AssetId}");
    }, token);

    public Task<bool> RemoveImageAsync(long productId, long imageId, string version, long actor, CancellationToken token) => Write(async () =>
    {
        var product = await Need<Product>(productId, token); Version(product.Version, version);
        var image = await Need<ProductImage>(imageId, token);
        Check(image.ProductId == productId, "NOT_FOUND", "Không tìm thấy ảnh.", 404);
        Check(!product.IsPublished || await Store.CountAsync(Store.Query<ProductImage>().Where(x => x.ProductId == productId), token) > 1,
            "PUBLISHED_IMAGE_REQUIRED", "Ngừng công bố trước khi bỏ ảnh cuối.");
        Store.Remove(image); product.UpdatedAtUtc = Now; Audit(actor, "ImageDetached", nameof(Product), productId); return true;
    }, token);

    public async Task<AssetView> UploadAsync(Stream stream, long actor, CancellationToken token)
    {
        var file = media is IMediaUploadContext secured
            ? await secured.SaveAsync(stream, actor, token) : await media.SaveAsync(stream, token);
        try
        {
            return await Write(async () =>
            {
                var asset = new MediaAsset { StorageKey = file.StorageKey, ContentType = file.ContentType, ByteSize = file.ByteSize, CreatedAtUtc = Now, CreatedByUserId = actor };
                Store.Add(asset); await Unit.SaveChangesAsync(token); Audit(actor, "MediaUploaded", nameof(MediaAsset), asset.Id);
                return new AssetView(asset.Id, asset.ContentType, asset.ByteSize, $"/api/media/{asset.Id}");
            }, token);
        }
        catch { await media.DeleteAsync(file.StorageKey, CancellationToken.None); throw; }
    }

    public async Task<Page<MediaAssetStatus>> MediaAssetsAsync(bool unusedOnly, int page, int size, CancellationToken token)
    {
        page = Number(page); size = Size(size);
        var query = Store.Query<MediaAsset>();
        if (unusedOnly) {
            var sections = await Store.ListAsync(Store.Query<SiteContent>(), token);
            var contentIds = new HashSet<long>();
            foreach (var json in sections.SelectMany(x => new[] { x.DraftJson, x.PublishedJson }).Where(x => !string.IsNullOrWhiteSpace(x))) {
                try {
                    var section = System.Text.Json.JsonSerializer.Deserialize<SiteSectionInput>(json!);
                    Check(section?.AssetIds is not null, "MEDIA_REFERENCE_UNVERIFIED", "Nội dung không hợp lệ; chưa xác minh ảnh thừa.");
                    foreach (var id in section!.AssetIds) contentIds.Add(id);
                } catch (System.Text.Json.JsonException) { throw new BusinessException(409, "MEDIA_REFERENCE_UNVERIFIED", "Không xác minh được nội dung tham chiếu."); }
            }
            var ids = contentIds.ToArray();
            var images = Store.Query<ProductImage>(); var gyms = Store.Query<Gym>();
            query = query.Where(a => !images.Any(i => i.AssetId == a.Id) && !gyms.Any(g => g.CoverAssetId == a.Id) && !ids.Contains(a.Id));
        }
        var count = await Store.CountAsync(query, token);
        var items = await Store.ListAsync(query.OrderByDescending(x => x.Id).Skip((page - 1) * size).Take(size), token);
        return new(items.Select(x => new MediaAssetStatus(x.Id, x.ContentType, x.ByteSize, x.CreatedAtUtc, $"/api/media/{x.Id}")).ToArray(), count, page, size);
    }

    public async Task<MediaDeleteResult> DeleteUnusedAssetAsync(long id, long actor, CancellationToken token)
    {
        var key = await Write(async () => {
            var asset = await Need<MediaAsset>(id, token);
            Check(asset.CreatedAtUtc <= Now.AddHours(-policySettings.Current.OrphanGraceHours), "MEDIA_ORPHAN_TOO_NEW", "Ảnh chưa đủ thời gian chờ trước khi dọn.");
            Check(!await Store.AnyAsync(Store.Query<ProductImage>().Where(x => x.AssetId == id), token) &&
                !await Store.AnyAsync(Store.Query<Gym>().Where(x => x.CoverAssetId == id), token),
                "MEDIA_STILL_REFERENCED", "Ảnh còn được sản phẩm/gym sử dụng.");
            var sections = await Store.ListAsync(Store.Query<SiteContent>(), token);
            foreach (var json in sections.SelectMany(x => new[] { x.DraftJson, x.PublishedJson }).Where(x => !string.IsNullOrWhiteSpace(x))) {
                bool referenced;
                try {
                    var section = System.Text.Json.JsonSerializer.Deserialize<SiteSectionInput>(json!);
                    Check(section?.AssetIds is not null, "MEDIA_REFERENCE_UNVERIFIED", "Nội dung không hợp lệ; chưa xóa ảnh.");
                    referenced = section!.AssetIds.Contains(id);
                }
                catch (System.Text.Json.JsonException) { throw new BusinessException(409, "MEDIA_REFERENCE_UNVERIFIED", "Không xác minh được nội dung tham chiếu; chưa xóa ảnh."); }
                Check(!referenced, "MEDIA_STILL_REFERENCED", "Ảnh còn được nội dung nháp/công khai sử dụng.");
            }
            Store.Remove(asset); Audit(actor, "UnusedMediaMetadataRemoved", nameof(MediaAsset), id); return asset.StorageKey;
        }, token);
        // SQL deletion commits first. A crash leaves a quota-charged orphan, never an uncharged object.
        try { await media.DeleteAsync(key, token); return new(true, true, false); }
        catch { return new(true, false, true); } // Owner must review orphan queue; no raw storage errors in response.
    }

    public async Task<(Stream Stream, string ContentType)> OpenMediaAsync(long id, bool admin, CancellationToken token)
    {
        var asset = await Need<MediaAsset>(id, token);
        if (!admin)
        {
            var published = await Store.AnyAsync(from image in Store.Query<ProductImage>() join product in Store.Query<Product>() on image.ProductId equals product.Id
                where image.AssetId == id && product.IsPublished && product.IsActive select image, token) ||
                await Store.AnyAsync(Store.Query<Gym>().Where(x => x.CoverAssetId == id && x.IsActive && x.IsPublished), token);
            if (!published)
            {
                var jsons = await Store.ListAsync(Store.Query<SiteContent>().Where(x => x.PublishedJson != null).Select(x => x.PublishedJson!), token);
                published = jsons.Any(json => System.Text.Json.JsonSerializer.Deserialize<SiteSectionInput>(json)?.AssetIds.Contains(id) == true);
            }
            Check(published, "NOT_FOUND", "Không tìm thấy ảnh công khai.", 404);
        }
        return (!admin && media is IMediaUploadContext secured
            ? await secured.OpenPublicAsync(asset.StorageKey, token) : await media.OpenAsync(asset.StorageKey, token), asset.ContentType);
    }

    private async Task<ProductView> View(Product p, bool publicOnly, CancellationToken token)
    {
        var variants = await Store.ListAsync(Store.Query<ProductVariant>().Where(x => x.ProductId == p.Id && (!publicOnly || x.IsActive)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id), token);
        var ids = variants.Select(x => x.Id).ToArray(); var now = Now;
        var free = await Store.ListAsync(from b in Store.Query<WarehouseBalance>() join batch in Store.Query<ProductBatch>() on b.BatchId equals batch.Id
            join loc in Store.Query<StockLocation>() on b.LocationId equals loc.Id
            where ids.Contains(batch.VariantId) && b.Condition == StockCondition.Good && !batch.IsSaleBlocked && batch.ExpiresAtUtc > now && loc.LocationCode == "MAIN" && loc.Kind == StockLocationKind.Warehouse && loc.IsActive
            select new { batch.VariantId, Quantity = (long)b.OnHand - b.Reserved }, token);
        var images = await Store.ListAsync(Store.Query<ProductImage>().Where(x => x.ProductId == p.Id && (x.VariantId == null || ids.Contains(x.VariantId.Value))).OrderBy(x => x.SortOrder).ThenBy(x => x.Id), token);
        return new ProductView(p.Id, p.Name, p.Slug, p.ShortDescription, p.Description, p.Ingredients, p.NutritionText, p.UsageInstructions, p.StorageInstructions,
            p.IsActive, p.IsPublished, p.IsFeatured, p.SortOrder, publicOnly ? null : Revision(p.Version),
            variants.Select(v => new VariantView(v.Id, v.SkuCode, v.Label, v.NetWeightGrams, v.SellingPrice, v.IsActive,
                p.IsActive && v.IsActive ? (int)Math.Min(int.MaxValue, free.Where(x => x.VariantId == v.Id).Sum(x => x.Quantity)) : 0, publicOnly ? null : Revision(v.Version))).ToArray(),
            images.Select(x => new ImageView(x.Id, x.AssetId, x.VariantId, x.AltText, x.SortOrder, $"/api/media/{x.AssetId}")).ToArray());
    }
}
