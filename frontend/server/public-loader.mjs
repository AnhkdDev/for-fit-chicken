// Public GET-only loader. No cookies/auth/PII requests; no stale HTML cache.
export class PublicLoadError extends Error {
  constructor(status) {
    super('Public data unavailable');
    this.status = status;
  }
}
const pick = (o, keys) =>
  Object.fromEntries(keys.filter((k) => o[k] !== undefined).map((k) => [k, o[k]]));
// Coalesce only simultaneous anonymous reads; never retain stale price/visibility snapshots.
const flights = new Map();
export function publicProduct(p) {
  return {
    ...pick(p, [
      'id',
      'name',
      'slug',
      'shortDescription',
      'description',
      'ingredients',
      'nutritionText',
      'usageInstructions',
      'storageInstructions',
      'isActive',
      'isPublished',
      'isFeatured',
      'sortOrder',
    ]),
    variants: p.variants.map((v) =>
      pick(v, [
        'id',
        'skuCode',
        'label',
        'netWeightGrams',
        'sellingPrice',
        'isActive',
        'available',
      ]),
    ),
    images: p.images.map((i) => pick(i, ['id', 'assetId', 'altText', 'url'])),
  };
}
export async function loadPublicData(origin, path, request = fetch) {
  const deadline = Date.now() + 10000;
  const get = async (endpoint) => {
    const key = origin + '/api' + endpoint;
    if (request === fetch && flights.has(key)) return flights.get(key);
    const read = async () => {
      const r = await request(key, {
        headers: { 'X-Forwarded-Proto': 'https' },
        signal: AbortSignal.timeout(Math.max(1, Math.min(6000, deadline - Date.now()))),
        redirect: 'error',
      });
      if (!r.ok)
        throw new PublicLoadError(
          r.status === 404 && /^\/products\/\d+$/.test(endpoint) ? 404 : 503,
        );
      return r.json();
    };
    if (request !== fetch) return read();
    const promise = read().finally(() => flights.delete(key));
    flights.set(key, promise);
    return promise;
  };
  const all = async (endpoint) => {
    const rows = [];
    for (let page = 1; page <= 20; page++) {
      const r = await get(endpoint + '?size=100&page=' + page);
      rows.push(...r.items);
      if (rows.length >= r.total) return rows;
      if (!r.items.length) throw new PublicLoadError(503);
    }
    throw new PublicLoadError(503); // Never silently omit catalog entries from sitemap.
  };
  const match = /^\/san-pham\/([1-9]\d*)$/.exec(path);
  const detail = match ? publicProduct(await get('/products/' + match[1])) : undefined;
  const [products, sections, gyms] = await Promise.all([
    all('/products'),
    path === '/sitemap.xml' ? Promise.resolve([]) : get('/content'),
    path === '/sitemap.xml' ? Promise.resolve([]) : all('/gyms'),
  ]);
  const result = {
    products: products.map(publicProduct),
    sections: sections.map((s) => ({
      sectionKey: s.sectionKey,
      content: pick(s.content, [
        'title',
        'body',
        'assetIds',
        'productIds',
        'productMainImages',
        'productImageFrames',
        'enabled',
        'linkLabel',
        'linkUrl',
      ]),
      publishedAtUtc: s.publishedAtUtc,
    })),
    gyms: gyms.map((g) =>
      pick(g, ['id', 'name', 'publicAddress', 'publicPhone', 'publicDescription', 'coverAssetId']),
    ),
    path,
  };
  if (detail) result.products = [...result.products.filter((p) => p.id !== detail.id), detail];
  return result;
}
export const publicRoute = (path) =>
  ['/', '/san-pham', '/cau-chuyen', '/diem-ban'].includes(path) ||
  /^\/san-pham\/[1-9]\d*$/.test(path);
export const xmlEscape = (s) =>
  String(s).replace(
    /[&<>"']/g,
    (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' })[c],
  );
export function sitemap(data, origin) {
  const urls = [
    '/',
    '/san-pham',
    '/cau-chuyen',
    '/diem-ban',
    ...data.products.map((p) => '/san-pham/' + p.id),
  ];
  return (
    '<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">' +
    urls.map((p) => '<url><loc>' + xmlEscape(origin + p) + '</loc></url>').join('') +
    '</urlset>'
  );
}
