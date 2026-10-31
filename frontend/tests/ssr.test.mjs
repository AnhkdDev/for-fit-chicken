import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { loadPublicData } from '../server/public-loader.mjs';
const p = {
  id: 2,
  name: 'Ức gà tiêu đen',
  slug: 'uc-ga',
  isActive: true,
  isPublished: true,
  isFeatured: true,
  shortDescription: 'Ức gà nấu chậm',
  variants: [
    {
      id: 3,
      skuCode: 'GA150',
      label: 'Gói 150g',
      netWeightGrams: 150,
      sellingPrice: 35000,
      isActive: true,
      available: 2,
    },
  ],
  images: [{ id: 1, assetId: 7, url: '/api/media/7' }],
  internalNote: 'PRIVATE_SENTINEL',
};
test('SSR HTML metadata/schema, fresh sitemap, HTTP errors and private-route isolation', async () => {
  let fail = false,
    hidden = false,
    calls = 0;
  const upstream = createServer((req, res) => {
    calls++;
    assert.equal(req.headers.cookie, undefined);
    assert.equal(req.headers.authorization, undefined);
    res.setHeader('Content-Type', 'application/json');
    if (fail) {
      res.writeHead(500);
      res.end('{}');
      return;
    }
    if (req.url === '/api/products/999' || (hidden && req.url === '/api/products/2')) {
      res.writeHead(404);
      res.end('{}');
      return;
    }
    if (req.url === '/api/products/2') {
      res.end(JSON.stringify(p));
      return;
    }
    if (req.url.startsWith('/api/products?')) {
      res.end(JSON.stringify({ items: hidden ? [] : [p], total: hidden ? 0 : 1 }));
      return;
    }
    if (req.url === '/api/content') {
      res.end(
        JSON.stringify([
          {
            sectionKey: 'HomeHero',
            content: { title: 'Ức gà cho người bận rộn', body: 'Bữa ăn mỗi ngày', assetIds: [] },
          },
          {
            sectionKey: 'HomeProducts',
            content: {
              title: 'Tiêu đề Owner đã chọn',
              body: 'Lời giới thiệu Owner đã nhập',
              assetIds: [],
              productIds: [2],
              enabled: true,
              linkLabel: 'Xem bộ sưu tập Owner',
              linkUrl: '/san-pham?bo-suu-tap=owner',
              productImageFrames: {
                2: {
                  assetId: 7,
                  desktop: { fit: 'cover', x: 25, y: 70, zoom: 1.3 },
                  mobile: { fit: 'contain', x: 50, y: 50, zoom: 1 },
                },
              },
            },
          },
        ]),
      );
      return;
    }
    res.end(JSON.stringify({ items: [], total: 0 }));
  });
  upstream.listen(0, '127.0.0.1');
  await once(upstream, 'listening');
  const child = spawn(process.execPath, ['server/index.mjs'], {
    cwd: new URL('../', import.meta.url),
    env: {
      ...process.env,
      PORT: '0',
      PUBLIC_API_ORIGIN: `http://127.0.0.1:${upstream.address().port}`,
    },
    stdio: ['ignore', 'pipe', 'pipe'],
  });
  let errors = '';
  child.stderr.on('data', (b) => (errors += b));
  try {
    const port = await new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(Error('Start timeout ' + errors)), 15000);
      child.on('exit', () => {
        clearTimeout(timer);
        reject(Error(errors));
      });
      child.stdout.on('data', (b) => {
        const m = /SSR_READY (\d+)/.exec(String(b));
        if (m) {
          clearTimeout(timer);
          resolve(m[1]);
        }
      });
    });
    const get = (path, opts = {}) => fetch(`http://127.0.0.1:${port}` + path, opts);
    const home = await get('/');
    assert.equal(home.status, 200);
    const h = await home.text();
    assert.match(h, /Ức gà cho người bận\u00a0rộn/);
    assert.match(h, /lang="vi-VN"/);
    assert.match(h, /rel="canonical"/);
    assert.doesNotMatch(h, /PRIVATE_SENTINEL/);
    assert.match(h, /<h2>Sản phẩm nổi bật<\/h2>/);
    assert.match(h, /<p class="featured-products-subtitle">Tiêu đề Owner đã chọn<\/p>/);
    assert.match(h, /Lời giới thiệu Owner đã nhập/);
    assert.match(h, /href="\/san-pham\?bo-suu-tap=owner"[^>]*>Xem bộ sưu tập Owner/);
    const showcase = /<article class="product-showcase">([\s\S]*?)<\/article>/.exec(h)[1];
    assert.match(showcase.replace(/<!--.*?-->/g, ''), /Gói 150 g/);
    assert.doesNotMatch(showcase, /class="eyebrow"/);
    assert.doesNotMatch(showcase, /Gói 150g/);
    assert.match(showcase, /--desktop-fit:cover/);
    assert.match(showcase, /--mobile-fit:contain/);
    assert.match(showcase, /--desktop-zoom:1.3/);
    const detail = await get('/san-pham/2');
    assert.equal(detail.status, 200);
    const d = await detail.text();
    assert.match(d, /<h1>Ức gà tiêu đen<\/h1>/);
    assert.match(d, /<title>Ức gà tiêu đen \| 4FitChicken<\/title>/);
    assert.match(d, /35000/);
    assert.match(d, /schema.org\/InStock/);
    assert.doesNotMatch(d, /PRIVATE_SENTINEL/);
    const originalName = p.name;
    p.name = '</script><script>alert(1)</script>';
    const escaped = await (await get('/san-pham/2')).text();
    assert.doesNotMatch(escaped, /<script>alert\(1\)<\/script>/);
    const payload = JSON.parse(
      /<script id="public-data" type="application\/json">([\s\S]*?)<\/script>/.exec(escaped)[1],
    );
    assert.equal(payload.products[0].name, p.name);
    for (const match of escaped.matchAll(
      /<script type="application\/ld\+json">([\s\S]*?)<\/script>/g,
    ))
      JSON.parse(match[1]);
    p.name = originalName;
    p.variants[0].sellingPrice = 42000;
    p.variants[0].available = 0;
    const changed = await get('/san-pham/2');
    assert.equal(changed.status, 200);
    const changedHtml = await changed.text();
    assert.match(changedHtml, /42000/);
    assert.match(changedHtml, /schema.org\/OutOfStock/);
    p.variants[0].sellingPrice = 35000;
    p.variants[0].available = 2;
    for (const path of ['/san-pham/999', '/unknown']) {
      const r = await get(path);
      assert.equal(r.status, 404);
      assert.match(await r.text(), /noindex/);
    }
    const sm = await get('/sitemap.xml');
    assert.match(sm.headers.get('content-type'), /xml/);
    assert.match(await sm.text(), /\/san-pham\/2</);
    const robots = await get('/robots.txt');
    assert.match(robots.headers.get('content-type'), /text\/plain/);
    assert.match(await robots.text(), /Sitemap:/);
    const before = calls;
    const admin = await get('/quan-tri', { headers: { Cookie: 'SECRET' } });
    assert.equal(admin.headers.get('x-robots-tag'), 'noindex, follow');
    assert.match(await admin.text(), /noindex/);
    assert.equal(calls, before);
    assert.equal((await get('/.env')).status, 404);
    assert.equal((await get('/assets/missing.js')).status, 404);
    assert.equal((await get('/', { method: 'POST' })).status, 405);
    hidden = true;
    assert.equal((await get('/san-pham/2')).status, 404);
    assert.doesNotMatch(await (await get('/sitemap.xml')).text(), /\/san-pham\/2</);
    fail = true;
    assert.equal((await get('/san-pham')).status, 503);
  } finally {
    child.kill();
    await once(child, 'exit').catch(() => {});
    await new Promise((resolve) => upstream.close(resolve));
  }
});
test('loader rejects truncated catalog instead of publishing incomplete sitemap', async () => {
  const mock = async (url) => ({
    ok: true,
    json: async () =>
      url.endsWith('/content')
        ? []
        : {
            items: url.includes('/products?') ? [p] : [],
            total: url.includes('/products?') ? 99999 : 0,
          },
  });
  await assert.rejects(loadPublicData('http://unused', '/', mock));
});
