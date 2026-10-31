import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { resolve, extname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadPublicData, publicRoute, PublicLoadError, sitemap } from './public-loader.mjs';
const root = resolve(fileURLToPath(new URL('../dist/client/', import.meta.url)));
const { render, headMarkup, safeJson } = await import('../dist/server/entry-server.js');
const template = await readFile(resolve(root, 'index.html'), 'utf8');
const origin = 'https://4fitchicken.io.vn';
const api = process.env.PUBLIC_API_ORIGIN || 'http://127.0.0.1:8080';
if (!/^https?:\/\//.test(api)) throw new Error('Invalid public API origin');
let busy = 0;
const empty = { products: [], sections: [], gyms: [] };
const types = {
  '.js': 'application/javascript',
  '.css': 'text/css',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.webp': 'image/webp',
  '.jpg': 'image/jpeg',
  '.woff2': 'font/woff2',
};
function html(path, data, status = 200, csr = false) {
  const base = template
    .replace(/<title>[\s\S]*?<\/title>/, '')
    .replace(/<meta\s+name="description"[\s\S]*?>/, '');
  return base
    .replace('</head>', headMarkup(path, data, status) + '</head>')
    .replace(
      '<div id="root"></div>',
      `<div id="root">${csr ? '' : render(path, data)}</div>${csr ? '' : `<script id="public-data" type="application/json">${safeJson(data)}</script>`}`,
    );
}
const server = createServer(async (req, res) => {
  const send = (status, body, type = 'text/html; charset=utf-8') => {
    res.writeHead(status, {
      'Content-Type': type,
      'Cache-Control': 'no-store',
      'X-Content-Type-Options': 'nosniff',
      ...(status === 503 ? { 'Retry-After': '30' } : {}),
    });
    res.end(req.method === 'HEAD' ? '' : body);
  };
  if (!['GET', 'HEAD'].includes(req.method)) {
    send(405, 'Method not allowed', 'text/plain');
    return;
  }
  let path;
  try {
    path = decodeURIComponent(new URL(req.url, 'http://local').pathname);
  } catch {
    send(400, 'Bad request', 'text/plain');
    return;
  }
  if (
    path.startsWith('/assets/') ||
    path.startsWith('/brand/') ||
    path.startsWith('/decor/') ||
    ['/favicon.svg', '/reference-asin-hero.webp'].includes(path)
  ) {
    if (path.includes('..') || path.includes('\\') || !types[extname(path)]) {
      send(404, 'Not found', 'text/plain');
      return;
    }
    try {
      const target = resolve(root, '.' + path);
      if (!target.startsWith(root + '/') && !target.startsWith(root + '\\')) throw Error();
      if (!(await stat(target)).isFile()) throw Error();
      const content = await readFile(target);
      res.writeHead(200, {
        'Content-Type': types[extname(path)],
        'Cache-Control': path.startsWith('/assets/')
          ? 'public,max-age=31536000,immutable'
          : 'public,max-age=3600',
      });
      res.end(req.method === 'HEAD' ? undefined : content);
    } catch {
      send(404, 'Not found', 'text/plain');
    }
    return;
  }
  if (path === '/robots.txt') {
    send(
      200,
      `User-agent: *\nAllow: /\nSitemap: ${origin}/sitemap.xml\n`,
      'text/plain; charset=utf-8',
    );
    return;
  }
  if (path.length > 1 && path.endsWith('/')) {
    res.writeHead(301, { Location: origin + path.replace(/\/+$/, '') });
    res.end();
    return;
  }
  if (
    path === '/quan-tri' ||
    path.startsWith('/quan-tri/') ||
    ['/gio-hang', '/dat-hang'].includes(path)
  ) {
    res.setHeader('X-Robots-Tag', 'noindex, follow');
    send(200, html(path, empty, 200, true));
    return;
  }
  if (!publicRoute(path) && path !== '/sitemap.xml') {
    res.setHeader('X-Robots-Tag', 'noindex');
    send(404, html(path, { ...empty, path, status: 404 }, 404));
    return;
  }
  if (busy >= 4) {
    send(503, '<h1>Vui lòng thử lại sau</h1>');
    return;
  }
  busy++;
  try {
    const data = await loadPublicData(api, path);
    if (path === '/sitemap.xml') send(200, sitemap(data, origin), 'application/xml; charset=utf-8');
    else send(200, html(path, data));
  } catch (e) {
    if (e instanceof PublicLoadError && e.status === 404) {
      res.setHeader('X-Robots-Tag', 'noindex');
      send(404, html(path, { ...empty, path, status: 404 }, 404));
    } else
      send(
        503,
        '<!doctype html><html lang="vi-VN"><head><meta name="robots" content="noindex"></head><body><h1>Chưa tải được nội dung</h1><p>Vui lòng thử lại sau.</p></body></html>',
      );
  } finally {
    busy--;
  }
});
server.requestTimeout = 15000;
server.headersTimeout = 10000;
server.listen(Number(process.env.PORT || 8082), process.env.SSR_HOST || '127.0.0.1', () =>
  console.log('SSR_READY ' + server.address().port),
);
for (const signal of ['SIGTERM', 'SIGINT'])
  process.on(signal, () => server.close(() => process.exit(0)));
