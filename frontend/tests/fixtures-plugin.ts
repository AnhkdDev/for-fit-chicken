// Explicit isolated UI harness. Never imported into the browser bundle or production build.
// All /api requests terminate here: no database, R2, Telegram, or backend credentials.
import type { Plugin } from 'vite';
export function fixturesPlugin(): Plugin {
  const image = '/reference-asin-hero.webp';
  const variants = [1, 2, 3, 4, 5, 6].map((id) => ({
    id,
    skuCode: `UI-ONLY-${id}`,
    label: 'Gói minh họa 150g',
    netWeightGrams: 150,
    sellingPrice: 35000 + id * 1000,
    isActive: true,
    available: 20,
    version: 'AAAAAAAABAE=',
  }));
  const products = variants.map((v, i) => ({
    id: v.id,
    name: [
      'Ức gà · Vị nguyên bản (UI test)',
      'Ức gà · Tiêu đen (UI test)',
      'Ức gà · Thảo mộc (UI test)',
      'Ức gà · Gói thử vị (UI test)',
      'Ức gà · Gói bữa trưa (UI test)',
      'Ức gà · Gói sau tập (UI test)',
    ][i],
    slug: `ui-test-${v.id}`,
    shortDescription: 'Dữ liệu kiểm tra giao diện, không phải sản phẩm đang bán.',
    description: 'Nội dung mô tả chỉ để kiểm tra bố cục.',
    ingredients: 'Nội dung minh họa, không dùng làm nhãn thực phẩm.',
    nutritionText: 'Chưa có dữ liệu dinh dưỡng thực.',
    usageInstructions: 'Xem hướng dẫn của sản phẩm thật khi mở bán.',
    storageInstructions: 'Dữ liệu kiểm thử UI.',
    isActive: true,
    isPublished: true,
    isFeatured: true,
    sortOrder: i,
    version: 'AAAAAAAABAE=',
    variants: [v],
    images: [{ id: i + 1, assetId: i + 1, url: image, altText: 'Ảnh reference tạm cho UI test' }],
  }));
  products[0].images.push({
    id: 101,
    assetId: 101,
    url: '/api/media/101',
    altText: 'Góc ảnh thứ hai UI test',
  });
  const extraVariants = [
    {
      ...variants[0],
      id: 101,
      skuCode: 'UI-ONLY-101',
      label: 'Phân loại thứ hai UI',
      netWeightGrams: 200,
      sellingPrice: 48000,
      available: 8,
    },
    {
      ...variants[0],
      id: 102,
      skuCode: 'UI-ONLY-102',
      label: 'Phân loại hết hàng UI',
      available: 0,
    },
  ];
  products[0].variants.push(...extraVariants);
  const orders = [
    {
      id: 9001,
      orderNumber: 'UI-ONLY-ORDER-9001',
      status: 'PendingConfirmation',
      recipientName: 'KHÁCH GIẢ LẬP UI',
      recipientPhone: '0000000000',
      recipientAddress: 'Địa chỉ giả lập trong môi trường UI, Hà Nội',
      itemsSubtotal: 72000,
      createdAtUtc: '2026-10-29T01:00:00Z',
      version: 'AAAAAAAABAE=',
      items: [
        {
          id: 1,
          variantId: 1,
          productName: products[0].name,
          label: variants[0].label,
          quantity: 2,
          lineTotal: 72000,
        },
      ],
      allocations: [],
    },
  ];
  const gym = {
    id: 9001,
    gymCode: 'UI-GYM',
    name: 'Phòng gym minh họa UI',
    publicAddress: 'Địa chỉ giả lập · Hà Nội',
    publicDescription: 'Không phải điểm bán thực tế.',
    isActive: true,
    isPublished: true,
    locationId: 2,
    version: 'AAAAAAAABAE=',
  };
  let section: unknown[] = [
    {
      sectionKey: 'HomeHero',
      content: {
        title: 'Banner minh họa UI',
        body: 'Chỉ dùng kiểm tra giao diện',
        assetIds: [1, 2],
        linkLabel: 'Xem sản phẩm',
        linkUrl: '/san-pham',
      },
      version: 'AAAAAAAABAE=',
    },
  ];
  let publishedSections: any[] = structuredClone(section);
  let paused = false;
  let authenticated = true;
  const page = (items: unknown[], n = 1) => ({
    items,
    total: items.length,
    pageNumber: n,
    pageSize: 20,
  });
  return {
    name: 'isolated-ui-fixtures',
    transformIndexHtml(html) {
      return html.replace(
        '<body>',
        '<body><div style="position:fixed;bottom:0;left:0;z-index:99999;background:#8b422e;color:white;font:10px sans-serif;padding:6px 10px;pointer-events:none">UI TEST · DỮ LIỆU GIẢ LẬP · KHÔNG GHI BACKEND</div>',
      );
    },
    configureServer(server) {
      server.middlewares.use(async (req, res, next) => {
        if (!req.url?.startsWith('/api')) return next();
        const url = new URL(req.url, 'https://localhost');
        const p = url.pathname.replace('/api', '');
        if (/^\/media\/\d+$/.test(p)) {
          res.statusCode = 302;
          res.setHeader('Location', image);
          res.end();
          return;
        }
        let data: unknown;
        let code = 200;
        const chunks: Buffer[] = [];
        for await (const chunk of req) chunks.push(Buffer.from(chunk));
        let body: Record<string, unknown> = {};
        try {
          body = JSON.parse(Buffer.concat(chunks).toString() || '{}');
        } catch {}
        if (p === '/csrf') data = { requestToken: 'UI-FIXTURE-ONLY-NOT-A-CREDENTIAL' };
        else if (p === '/admin/auth/me' && !authenticated) {
          code = 401;
          data = { title: 'Chưa đăng nhập' };
        } else if (p === '/admin/auth/me')
          data = {
            id: 9999,
            userName: 'ui-fixture',
            displayName: 'UI Test Owner',
            roles: ['Owner'],
          };
        else if (p === '/admin/auth/logout') {
          authenticated = false;
          code = 204;
        } else if (/^\/admin\/products\/\d+$/.test(p) && req.method === 'DELETE') {
          if (p === '/admin/products/1') {
            code = 409;
            data = {
              code: 'PRODUCT_IN_USE',
              title: 'Sản phẩm đã có lô hoặc đơn hàng. Hãy ngừng hoạt động/ẩn thay vì xóa lịch sử.',
            };
            res.statusCode = code;
            res.setHeader('Content-Type', 'application/problem+json');
            res.end(JSON.stringify(data));
            return;
          }
          const index = products.findIndex((x) => x.id === Number(p.split('/').at(-1)));
          if (index >= 0) products.splice(index, 1);
          data = true;
        } else if (/^\/admin\/inventory\/batches\/\d+$/.test(p) && req.method === 'DELETE') {
          data = true;
        } else if (/^\/admin\/media\/assets\/\d+$/.test(p) && req.method === 'DELETE') {
          code = 409;
          data = {
            code: 'MEDIA_ORPHAN_TOO_NEW',
            title: 'Ảnh chưa đủ thời gian chờ trước khi dọn.',
          };
        } else if (p === '/media' && req.method === 'POST')
          data = { id: 700, url: image, byteSize: 2048, contentType: 'image/webp' };
        else if (p === '/admin/products' && req.method === 'POST') {
          const value = {
            ...products[0],
            ...body,
            id: 700,
            variants: [],
            images: [],
            version: 'AAAAAAAABAI=',
          };
          products.push(value as (typeof products)[number]);
          data = value;
        } else if (/^\/admin\/products\/\d+$/.test(p) && req.method === 'PUT') {
          const value = products.find((x) => x.id === Number(p.split('/').at(-1)))!;
          Object.assign(value, body, { version: 'AAAAAAAABAI=' });
          data = value;
        } else if (
          /^\/admin\/products\/\d+\/variants(?:\/\d+)?$/.test(p) &&
          ['POST', 'PUT'].includes(req.method ?? '')
        ) {
          const value = products.find((x) => x.id === Number(p.split('/')[3]))!;
          if (req.method === 'POST') {
            value.variants.push({
              ...variants[0],
              ...body,
              id: 701,
              available: 0,
            } as (typeof variants)[number]);
          } else {
            Object.assign(
              value.variants.find((v) => v.id === Number(p.split('/').at(-1)))!,
              body,
            );
          }
          data = value.variants.at(-1);
        } else if (/^\/admin\/products\/\d+\/images(?:\/\d+)?$/.test(p)) {
          const value = products.find((x) => x.id === Number(p.split('/')[3]))!;
          if (req.method === 'POST') {
            const im = body.image as { assetId: number; altText: string };
            value.images.push({ id: 702, assetId: im.assetId, altText: im.altText, url: image });
          } else {
            value.images = value.images.filter((im) => im.id !== Number(p.split('/').at(-1)));
          }
          code = 204;
        } else if (p === '/products' || p === '/admin/products') data = page(products);
        else if (/^\/(admin\/)?products\/\d+$/.test(p)) {
          data = products.find((x) => x.id === Number(p.split('/').at(-1)));
          if (!data) {
            code = 404;
            data = { detail: 'Không có sản phẩm fixture này.' };
          }
        } else if (p === '/content' || p === '/admin/content')
          data = p === '/content' ? publishedSections : section;
        else if (/^\/admin\/content\//.test(p)) {
          const key = p.split('/')[3];
          const old = section.find((x: any) => x.sectionKey === key) as any;
          const value = p.endsWith('/publish')
            ? { ...old, publishedAtUtc: new Date().toISOString(), version: 'AAAAAAAABAM=' }
            : { sectionKey: key, content: body, version: 'AAAAAAAABAI=' };
          section = [...section.filter((x: any) => x.sectionKey !== key), value];
          if (p.endsWith('/publish'))
            publishedSections = [
              ...publishedSections.filter((x) => x.sectionKey !== key),
              structuredClone(value),
            ];
          data = value;
        } else if (p === '/gyms' || p === '/admin/gyms') data = page([gym]);
        else if (p === '/orders' && req.method === 'POST') {
          data = {
            orderNumber: 'UI-ONLY-CHECKOUT',
            status: 'PendingConfirmation',
            itemsSubtotal: ((body.items as { variantId: number; quantity: number }[]) ?? []).reduce(
              (n, l) =>
                n +
                ([...variants, ...extraVariants].find((v) => v.id === l.variantId)?.sellingPrice ??
                  0) *
                  l.quantity,
              0,
            ),
            createdAtUtc: new Date().toISOString(),
          };
          code = 201;
        } else if (p === '/admin/orders' && req.method === 'POST') {
          const input = body.order as any;
          const value = {
            ...orders[0],
            ...input,
            id: 9002,
            orderNumber: 'UI-ONLY-ORDER-9002',
            items: input.items.map((l: any) => {
              const v = products.flatMap((x) => x.variants).find((v) => v.id === l.variantId)!;
              return {
                ...l,
                id: l.variantId,
                productName: products.find((p) => p.variants.some((x) => x.id === v.id))!.name,
                label: v.label,
                lineTotal: v.sellingPrice * l.quantity,
              };
            }),
          };
          value.itemsSubtotal = value.items.reduce((sum: number, l: any) => sum + l.lineTotal, 0);
          orders.push(value);
          data = value;
        } else if (/^\/admin\/orders\/\d+$/.test(p) && req.method === 'PUT') {
          const value = orders.find((x) => x.id === Number(p.split('/').at(-1)))!;
          Object.assign(value, body);
          data = value;
        } else if (/^\/admin\/orders\/\d+\/actions\//.test(p)) {
          const value = orders.find((x) => x.id === Number(p.split('/')[3]))!;
          const statuses: Record<string, string> = {
            confirm: 'Confirmed',
            prepare: 'Preparing',
            handover: 'HandedOver',
            complete: 'Completed',
            cancel: 'Cancelled',
          };
          value.status = statuses[p.split('/').at(-1)!] ?? value.status;
          data = value;
        } else if (p === '/admin/orders')
          data = page(
            orders.filter(
              (o) =>
                (!url.searchParams.get('status') || o.status === url.searchParams.get('status')) &&
                (!url.searchParams.get('search') ||
                  `${o.orderNumber} ${o.recipientPhone}`.includes(url.searchParams.get('search')!)),
            ),
          );
        else if (p.endsWith('/events'))
          data = [{ id: 1, eventType: 'Created (UI test)', createdAtUtc: '2026-10-29T01:00:00Z' }];
        else if (p.endsWith('/receipts')) data = [];
        else if (/^\/admin\/orders\/\d+$/.test(p))
          data = orders.find((x) => x.id === Number(p.split('/').at(-1)));
        else if (p === '/admin/inventory/locations')
          data = [
            { id: 1, name: 'Kho MAIN minh họa', locationCode: 'MAIN', kind: 'Warehouse' },
            { id: 2, name: gym.name, locationCode: 'UI-GYM', kind: 'Gym' },
          ];
        else if (p === '/admin/inventory/batches')
          data = page([
            {
              id: 1,
              batchCode: 'UI-BATCH',
              variantId: 1,
              manufacturedOn: '2026-10-29',
              expiresAtUtc: '2026-11-01T10:00:00Z',
              isSaleBlocked: false,
              version: 'AAAAAAAABAE=',
            },
          ]);
        else if (p === '/admin/inventory/balances')
          data = page([
            {
              id: 1,
              batchId: 1,
              batchCode: 'UI-BATCH',
              variantId: 1,
              condition: 'Good',
              onHand: 25,
              reserved: 5,
              available: 20,
              expiresAtUtc: '2026-11-01T10:00:00Z',
            },
          ]);
        else if (p === '/admin/inventory/documents') data = page([]);
        else if (p.endsWith('/stock'))
          data = {
            gymId: gym.id,
            lastCountedAtUtc: '2026-10-28T08:00:00Z',
            isRealtime: false,
            version: 'AAAAAAAABAE=',
            lastCountLines: [
              {
                id: 1,
                batchId: 1,
                actualGood: 10,
                actualQuarantine: 0,
                actualDamaged: 1,
                version: 'AAAAAAAABAE=',
              },
            ],
            bookEstimate: [{ batchId: 1, good: 15, quarantine: 0, damaged: 1 }],
          };
        else if (p.endsWith('/counts')) data = page([]);
        else if (p === '/admin/media/storage')
          data = {
            chargedBytes: 320000000,
            limitBytes: 9000000000,
            uncertainBytes: 0,
            activeUploads: 0,
            initialized: true,
            uploadsPaused: paused,
            lastReconciledAtUtc: '2026-10-29T01:00:00Z',
            classAThisMonth: 12,
            classBThisMonth: 42,
            warnings: [],
            nodeReady: true,
          };
        else if (p === '/admin/media/uploads') {
          paused = !!body.paused;
          data = true;
        } else if (p === '/admin/media/assets')
          data = page([
            {
              id: 1,
              url: image,
              byteSize: 324108,
              contentType: 'image/webp',
              createdAtUtc: '2026-10-27T01:00:00Z',
            },
          ]);
        else if (p === '/admin/reports/sales')
          data = {
            ordersRequested: 1,
            ordersCompleted: 0,
            completedItemsValue: 0,
            codReceived: 0,
            knownIssuedCost: 0,
            issuedPackagesWithUnknownCost: 0,
            note: 'Số liệu fixture kiểm tra UI, không phải báo cáo kinh doanh.',
          };
        else if (p === '/admin/notifications') data = page([]);
        else {
          code = 404;
          data = {
            code: 'UI_FIXTURE_NOT_IMPLEMENTED',
            detail: 'Thao tác chưa có fixture; không gửi tới backend thật.',
          };
        }
        res.statusCode = code;
        res.setHeader('Content-Type', 'application/json');
        res.setHeader('Cache-Control', 'no-store');
        res.end(code === 204 ? undefined : JSON.stringify(data));
      });
    },
  };
}
