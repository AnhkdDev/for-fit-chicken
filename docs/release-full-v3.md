# Offline full-v3 release —31/10/2026

Archive: `backend/ForFitChicken/artifacts/phase1-update/forfit-full-v3-20261031.tar.gz`
(ignored binary artifact, **245,778,710 bytes**).
SHA256: `989d77637e04ee33b47586bc8a3009c68d2606073d35afae14d309cffd2566de`.
Sibling `.sha256` and unpacked folder contain README, IMAGES, internal SHA256SUMS.
No source/DB backup/env/private config/key/certificate in archive. JPEG brand artwork is public.

## Upload and update

Upload only the archive to `/opt/forfit/incoming` with Bitvise. Run as root:

```bash
cd /opt/forfit/incoming && printf '%s\n' '989d77637e04ee33b47586bc8a3009c68d2606073d35afae14d309cffd2566de  forfit-full-v3-20261031.tar.gz' | sha256sum -c - && tar -xzf forfit-full-v3-20261031.tar.gz && bash forfit-full-v3-20261031/update.sh
```

This is **three services**: API + Node public SSR + web/Nginx. Do not run operator-v2 updater.
Expect a brief ForFit interruption. No SSH/root password is stored or required by this bundle.
Do not delete the old archives/images/backup during update. Insufficient resources stops preflight,
does not prune or restart other projects. Do not reset SQL/quota to make startup pass.

Prerequisites: existing forfit-phase1 project/api+web, optional old ssr; exact local publication
127.0.0.1:18081→8081; Docker Compose v2/curl/flock/sha256sum; novastay-net; existing private JSON;
at least1.5GiB available Docker disk and650MiB available RAM. Host domain/TLS config stays unchanged.

Updater verifies checksums/config/container ownership/port/resources; saves current compose and exact image IDs
under `/opt/forfit/releases/full-v3-TIMESTAMP-PID`; loads images offline; removes only ForFit dependents
web/ssr before API recreation; starts api→ssr→web. Tests localhost health/products/SSR HTML/sitemap.
Only then installs new canonical `/opt/forfit/incoming/compose.images.yaml`.
Preserves project volume names/keys/local media, SQL/R2 ledger, bot config, host Nginx and other projects.
No migration/DB restore, no secret edits, no quota reset or global Docker commands.

Failure after replacement automatically attempts rollback. If rollback also fails, it prints snapshot path
and exits failure; inspect before retrying. Manual rollback (use actual printed snapshot path):

```bash
bash /opt/forfit/incoming/forfit-full-v3-20261031/rollback.sh /opt/forfit/releases/SNAPSHOT
```

Rollback restores exact image IDs and canonical compose. It does **not** reverse business data written
after update. Only apply version-compatible app rollback; this release adds no EF migration.

## Verification performed locally

- Latest API/web/SSR images Linux amd64 built; production API and SSR users remain app/node.
- .dockerignore expanded to exclude private config, env, keys, certs and private tool directories.
  Runtime API lacks production JSON/secrets/.env/.certs; SSR lacks .env/.certs/.git.
- Frontend23 unit +2mockSSR tests passed on final source. Backend25SQLCommerce tests +focused HTTP
  complete-paid rerun passed at last business checkpoint, GUID disposable DB; API image compiled Linux Release.
- Actual web/SSR images ran in shared Linux namespace with **mock API only**, no production config or
  SQL/R2/Telegram. Checked SSR text/schema/bootstrap/static JS/favicon/brand/sitemap, product404,
  admin noindex, cookie+HTTPS forwarding through /api and no cookie forwarding into SSR public loader.
- Nginx-t, Compose config, Bash syntax and internal bundle SHA256 all passed.
- Updater mocks in disposable container:2→3 success/manualrollback;2→3 failure rollback;3→3 failure rollback.
  These are simulations, not actual production rollback evidence. All temporary runtime containers removed.
- Archive contains only images.tar/compose.yaml/update.sh/rollback.sh/README.txt/IMAGES.txt/SHA256SUMS.

Not yet live browser hydration/login/cart/manual order/crop/touch UAT, VPS load/rate-limit measurement,
real cloud/Telegram or SearchConsole/CWV acceptance. After update inspect Owner/Operator flows and public
pages yourself; deliberately submitted real website orders can trigger the existing real Telegram group.
Do not claim these tests certified all production security, visual behavior or SEO rankings.

## Rebuild reference

Use Dockerfiles api/ssr/web-ssr with tags `forfit-{api,ssr,web}:full-v3-20261031`, platform linux/amd64.
Source includes the current uncommitted working tree; do not rebuild from Git HEAD alone.
Copy compose.full-v3.yaml→compose.yaml, update-release.sh→update.sh, rollback-release.sh→rollback.sh.
Save the three images with docker save, include only explicit bundle files, recalculate internal SHA256SUMS,
tar gzip the folder and recalculate outer digest. **Any changed bytes require a new package checksum**;
prefer a new version/tag/file name over replacing the published release. Never copy VPS/private JSON into bundle.
Tests live in deploy/phase1/tests. Linux runtime requires no real credentials. Do not use old two-service
update scripts for SSR. Remaining staging gates are documented in seo-ssr.md.

Prepared locally only; no upload/deployment/commit/push was performed.
