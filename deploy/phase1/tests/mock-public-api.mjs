// Isolated Linux namespace smoke only; never connects to SQL, R2 or Telegram.
import { createServer } from 'node:http';
const product = { id: 1, name: 'SMOKE_ONLY_CHICKEN', slug: 'smoke', isActive: true, isPublished: true, isFeatured: true,
  shortDescription: 'Temporary public API fixture', images: [], variants: [{ id: 1, skuCode: 'SMOKE', label: '150g', netWeightGrams: 150, sellingPrice: 35000, isActive: true, available: 3 }] };
createServer((req, res) => {
  res.setHeader('Content-Type', 'application/json');
  if (req.url === '/api/test-proxy') return res.end(JSON.stringify({ cookie: req.headers.cookie, proto: req.headers['x-forwarded-proto'] }));
  if (req.headers.cookie || req.headers.authorization) { res.writeHead(400); return res.end('{}'); }
  if (req.url === '/health') return res.end('{}');
  if (req.url.startsWith('/api/products?') || req.url === '/api/products') return res.end(JSON.stringify({ items: [product], total: 1 }));
  if (req.url === '/api/products/1') return res.end(JSON.stringify(product));
  if (req.url === '/api/content') return res.end(JSON.stringify([{ sectionKey: 'HomeHero', content: { title: 'SMOKE_HERO', body: 'Test only', assetIds: [] } }]));
  if (req.url.startsWith('/api/gyms')) return res.end(JSON.stringify({ items: [], total: 0 }));
  res.writeHead(404); res.end('{}');
}).listen(8080, '0.0.0.0');
