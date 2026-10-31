import assert from 'node:assert/strict';
const origin = 'http://127.0.0.1:8081';
for (let i = 0; i < 30; i++) {
  try { if ((await fetch(origin + '/health')).status === 200) break; } catch {}
  await new Promise(r => setTimeout(r, 500));
}
for (const path of ['/health', '/api/products', '/favicon.svg', '/brand/4fit-approved.jpg', '/quan-tri'])
  assert.equal((await fetch(origin + path)).status, 200, path);
const html = await (await fetch(origin + '/', { headers: { Cookie: 'SMOKE_ONLY=1' } })).text();
assert.match(html, /<h1>SMOKE_HERO<\/h1>/);
assert.match(html, /SMOKE_ONLY_CHICKEN/);
assert.match(html, /id="public-data"/);
assert.match(html, /application\/ld\+json/);
const js = /<script[^>]+src="([^"]+\.js)"/.exec(html)?.[1];
assert.ok(js); assert.equal((await fetch(origin + js)).status, 200);
const map = await fetch(origin + '/sitemap.xml');
assert.match(map.headers.get('content-type'), /xml/);
assert.match(await map.text(), /\/san-pham\/1</);
assert.equal((await fetch(origin + '/san-pham/999')).status, 404);
assert.match((await fetch(origin + '/quan-tri')).headers.get('x-robots-tag'), /noindex/);
const proxy = await (await fetch(origin + '/api/test-proxy', { headers: { Cookie: 'SMOKE_ONLY=1' } })).json();
assert.equal(proxy.cookie, 'SMOKE_ONLY=1'); assert.equal(proxy.proto, 'https');
console.log('Linux three-service namespace: SSR HTML/schema/assets/sitemap/404/private noindex/API cookie+HTTPS proxy OK.');
