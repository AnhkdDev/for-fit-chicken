import type { PublicData } from './public-data';
export const ORIGIN = 'https://4fitchicken.io.vn';
export const escapeHtml = (s: string) =>
  s.replace(
    /[&<>"']/g,
    (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!,
  );
export const safeJson = (v: unknown) =>
  JSON.stringify(v)
    .replace(/</g, '\\u003c')
    .replace(/\u2028/g, '\\u2028')
    .replace(/\u2029/g, '\\u2029');
export function metadata(path: string, data: PublicData, status = 200) {
  const info: Record<string, string[]> = {
    '/': [
      'Ức gà nấu chậm tại Hà Nội | 4FitChicken',
      'Khám phá ức gà nấu chậm 4FitChicken, các hương vị và điểm bán tại Hà Nội. Đặt hàng trực tuyến, nhóm liên hệ xác nhận.',
    ],
    '/san-pham': [
      'Mua ức gà nấu chậm – Các hương vị | 4FitChicken',
      'Xem sản phẩm ức gà nấu chậm, hương vị, giá bán và thông tin bảo quản từ 4FitChicken.',
    ],
    '/cau-chuyen': [
      'Câu chuyện 4FitChicken | Ức gà nấu chậm',
      'Tìm hiểu câu chuyện của nhóm bốn người và ý tưởng ức gà nấu chậm dành cho bữa ăn thường ngày.',
    ],
    '/diem-ban': [
      'Điểm bán ức gà nấu chậm tại Hà Nội | 4FitChicken',
      'Tìm điểm bán 4FitChicken tại Hà Nội. Liên hệ điểm bán trước khi ghé để kiểm tra sản phẩm đang có.',
    ],
  };
  const p = data.products.find((p) => path === `/san-pham/${p.id}`);
  const isPublic = !!info[path] || !!p;
  const title =
    status !== 200
      ? 'Không tìm thấy trang | 4FitChicken'
      : p
        ? `${p.name} | 4FitChicken`
        : (info[path]?.[0] ?? '4FitChicken');
  const description = (p?.shortDescription || info[path]?.[1] || 'Ức gà nấu chậm 4FitChicken.')
    .replace(/\s+/g, ' ')
    .slice(0, 180);
  const canonical = ORIGIN + path;
  const image =
    p?.images[0]?.url ??
    (data.sections.find((s) => s.sectionKey === 'HomeHero')?.content.assetIds[0]
      ? `/api/media/${data.sections.find((s) => s.sectionKey === 'HomeHero')!.content.assetIds[0]}`
      : undefined);
  const absoluteImage =
    image && (image.startsWith('/api/media/') || image.startsWith('/brand/'))
      ? ORIGIN + image
      : undefined;
  const graph: unknown[] = [];
  if (isPublic && status === 200) {
    graph.push({
      '@type': 'Organization',
      '@id': ORIGIN + '/#organization',
      name: '4FitChicken',
      url: ORIGIN,
    });
    graph.push({
      '@type': 'WebSite',
      '@id': ORIGIN + '/#website',
      name: '4FitChicken',
      url: ORIGIN,
      inLanguage: 'vi-VN',
    });
    if (path !== '/')
      graph.push({
        '@type': 'BreadcrumbList',
        itemListElement: [
          { '@type': 'ListItem', position: 1, name: 'Trang chủ', item: ORIGIN + '/' },
          ...(p
            ? [{ '@type': 'ListItem', position: 2, name: 'Sản phẩm', item: ORIGIN + '/san-pham' }]
            : []),
          {
            '@type': 'ListItem',
            position: p ? 3 : 2,
            name: p?.name ?? title.split('|')[0].trim(),
            item: canonical,
          },
        ],
      });
    if (p)
      graph.push({
        '@type': 'Product',
        name: p.name,
        description: p.shortDescription || p.description,
        image: p.images
          .filter((im) => im.url.startsWith('/api/media/'))
          .map((im) => ORIGIN + im.url),
        brand: { '@type': 'Brand', name: '4FitChicken' },
        offers: p.variants
          .filter((v) => v.isActive && Number.isFinite(v.sellingPrice))
          .map((v) => ({
            '@type': 'Offer',
            sku: v.skuCode,
            name: v.label,
            url: canonical,
            priceCurrency: 'VND',
            price: String(v.sellingPrice),
            availability: 'https://schema.org/' + (v.available > 0 ? 'InStock' : 'OutOfStock'),
          })),
      });
  }
  return {
    title,
    description,
    canonical,
    image: absoluteImage,
    noindex: !isPublic || status !== 200,
    graph,
  };
}
export function headMarkup(path: string, data: PublicData, status = 200) {
  const m = metadata(path, data, status);
  const e = escapeHtml;
  return `<title>${e(m.title)}</title><meta name="description" content="${e(m.description)}"><link rel="canonical" href="${e(m.canonical)}"><meta name="robots" content="${m.noindex ? 'noindex,follow' : 'index,follow'}"><meta property="og:locale" content="vi_VN"><meta property="og:type" content="website"><meta property="og:title" content="${e(m.title)}"><meta property="og:description" content="${e(m.description)}"><meta property="og:url" content="${e(m.canonical)}">${m.image ? `<meta property="og:image" content="${e(m.image)}">` : ''}${m.graph.length ? `<script type="application/ld+json">${safeJson({ '@context': 'https://schema.org', '@graph': m.graph })}</script>` : ''}`;
}
