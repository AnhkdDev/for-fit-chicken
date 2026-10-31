# Public SEO / SSR —30/10/2026

## Scope and architecture
User approved React/Vite SSR public pages, existing ID URLs, sales pages first, unchanged ASP.NET API/database and storefront structure. No Next.js/blog/schema migration. Code is local only; existing VPS remains on previous CSR image until explicit staging/deployment.

Node SSR returns HTML for /, /san-pham, /san-pham/:id, /cau-chuyen, /diem-ban. React hydrates same components. /quan-tri/*, /gio-hang, /dat-hang stay CSR + noindex. Admin JS/CSS lazy chunks. Existing public GET APIs only, no credentials/cookies forwarded. Initial-data whitelist excludes Identity/order/internal notes/costs. JSON bootstrap/JSON-LD escapes < and Unicode separators; React escapes content. No API/business/CSRF/stock/SQL changes.

## Freshness and HTTP
No HTML/data TTL cache; simultaneous anonymous GETs coalesced until completion only. Every HTML public request reloads anonymous published catalog/content/gyms (pagination100, ceiling20pages/2000rows; incomplete catalog fails503). Detail additionally calls existing public detail API to enforce404/visibility. Product hidden/deleted=>404/noindex, unknown routes=>404, upstream/network/429=>503/Retry-After, no fake product or fallback200 error. Dynamic sitemap fetches only current public products (not content/gyms) without rebuild, XML content-type and absolute canonical URLs. No fabricated lastmod. Out-of-stock public product stays200/OfferOutOfStock. Cannot promise immediate removal from Google index; HTTP404 + recrawl required.

Current loader intentionally keeps full public dataset for CSR route transitions; this is not yet optimal for large catalogs. SSR max4concurrent, total data deadline10s/perrequest6s, no unbounded queue. API rate limits unchanged; all SSR calls share node IP partition so load-test on actual VPS before release. API origin server-only PUBLIC_API_ORIGIN defaults127.0.0.1:8080, not VITE_*. Do not set it to a public frontend URL (loop risk). Never add an Owner/API credential to the loader.

## SEO
Canonical origin fixedhttps://4fitchicken.io.vn, IDs retained; no slug migration/redirect invented. Per-route title/description/canonical/robots/OG, Organization/WebSite/Product/Offer/BreadcrumbList; Offer derives SKU/price/stock from actual public DTO. No rating/reviews/certifications/nutrition inventions. Breadcrumb visible on detail; HTML vi-VN. Admin/private noindex header + head; robots allows crawler to read noindex, not a security control. No Article module yet. Google does not guarantee ranking/rich-result display.

## Build/run
- npm run build: strict TS + dist/client static bundle and dist/server SSR entry.
- npm test: existing14frontend unit tests.
- npm run test:ssr: built server against temporary mock API, no SQL/R2/Telegram. Build first. Own ephemeral listeners terminate in finally.
- npm run start:ssr: built SSR server loopback8082, requires PUBLIC_API_ORIGIN pointing trusted API.
- npm run dev / dev:fixtures: existing Vite CSR workflow unchanged; NOT SSR proof.
- npm run preview: CSR static build only; NOT SSR proof.
- Dynamic SSR private paths shell has no user data. API/auth/checkout remain same-origin via existing Nginx/api proxy preserving cookies+CSRF. SSR proxy strips Cookie/Authorization only on HTML rendering hop.

## Docker plan
Dockerfile.ssr adds non-root node runtime, internal8082; Dockerfile.web-ssr/nginx-ssr.conf route HTML/robots/sitemap to SSR, /api unchanged, assets immutable. compose.ssr.yaml overlays existing image compose:3services api/web/ssr share API namespace. SSR memory256MiB/cpu.5 draft, measure on2CPU sharedVPS. Source build paths are LOCAL checkout references; do not build on crowded VPS blindly. Existing Dockerfile.web remains CSR-compatible using dist/client.

Do NOT use old update-operator-v2.sh to deploy3services: it assumes2services and old tags. Need a new release-specific3service updater + rollback incl web/ssr namespace ordering and offline-image checksums when deployment is requested. Preserve production JSON, DB, keys and same R2 ledger; never boot local copied DB/bucket to test. No new port exposed publicly. Existing host-domain TLS remains unchanged. Back up/validate existing compose before changing services.

## Verification and remaining gates
Build/clientSSR +14existing tests and2SSR mock tests passed initially. SSR tests cover HTML text/H1/head, canonical/JSON-LD parse, fake cost/internal sentinel excluded, script-injection escape,404/nonexistent/hidden, upstream503, fresh sitemap,robotsMIME,private route no publicAPI/cookies,asset404/POST405,pagination ceiling. These are not browser hydration/live auth/real stock/cloud tests. No production mutations made.

Remaining: browser hydration/cart persistence/CSRnavigation/Owner+Operator+checkout regression, no-JS visual/images,320/390/768/desktop compare, full three-service Linux runtime integration, resource/load/rate-limit testing, Lighthouse lab+real CWV, Rich Results/SchemaValidator external tools, actual deployment+sitemapURLs and SearchConsole ownership. Goals LCP<=2.5s/INP<=200ms/CLS<=.1 are NOT measured achievements. Route payloads and status require staging check before public rollout. No blanket promise of SEO rank.

Search Console not configured. After rollout user creates Domain property, adds exact Google TXT at registrar (do not invent token), verifies, submits https://4fitchicken.io.vn/sitemap.xml and inspects homepage/category/actual product URL. Do not submit missing knowledge/blog URLs. No numeric nutrition/calorie pages without verified source.

Linux Node/Nginx images built; non-root render smoke, nginx-t and Compose parse passed. Docker checks are isolated, not actual VPS integration. Price change and zero-stock HTML/schema fresh mock test passed (zero stock stays200/OutOfStock). Non-detail upstream404 is503 rather than fake page404.


## Offline full-v3 ready for user upload —31/10/2026
Three-image archive and release-specific update/rollback scripts now prepared and tested locally; see release-full-v3.md for exact path/digest/one-command update. Docker full runtime used mock API, not real production sources. Actual host deployment and browser/load/CWV acceptance remain unverified. Old operator-v2 updater remains unsuitable for this package.
