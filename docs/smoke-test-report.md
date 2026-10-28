# Live HTTPS smoke-test report

Date: 2026-10-27 13:30:20 +07:00
Result: PASSED

Real Kestrel HTTPS API, real Identity cookie/CSRF and isolated SQL Server database.
Server: KDADEPTRAI\KDADEPTRAI. No development/business data was created or modified.

## Checks

- PASS: Real HTTPS API started and health returned 200
- PASS: Live Swagger/OpenAPI is available
- PASS: Owner login succeeded with real cookie and CSRF
- PASS: Product, SKU, real image upload and publication succeeded
- PASS: Guest checkout is idempotent and does not reserve stock
- PASS: Confirmation reserves exactly two packages
- PASS: Handover decrements stock once even when repeated
- PASS: Completed COD order and cash/report values match 70000 VND
- PASS: Cancellation releases reservation without reducing physical stock
- PASS: Gym shortage remains unresolved instead of becoming automatic sales
- PASS: Explicit gym sale verification succeeds
- PASS: Recall enters Quarantine and does not overwrite the counted gym snapshot
- PASS: Checkout cannot borrow consignment or Quarantine stock
- PASS: Logout revokes the live admin session

## Scope and cleanup

- Temporary Owner password was randomly generated and never written to this report or source.
- Telegram was explicitly disabled; no real message was sent.
- API process stopped; only the generated smoke database and its own media/temp files were cleaned up.
- This is automated API smoke testing, not frontend/browser UAT.
