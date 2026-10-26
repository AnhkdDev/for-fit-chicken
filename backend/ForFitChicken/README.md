# ForFitChicken backend

Clean Architecture, one deployable API, SQL Server for development.

## Projects and dependency direction

```text
ForFitChicken.API             -> Application, Infrastructure (composition root)
ForFitChicken.Application     -> Domain
ForFitChicken.Domain          -> no project dependencies
ForFitChicken.Infrastructure  -> Application, Domain
ForFitChicken.UnitTest        -> Application, Domain; API/Infrastructure for categorized integration tests
```

API owns HTTP controllers, request authentication/authorization and composition.
Application owns use cases, DTOs and interfaces. Domain owns business rules.
Infrastructure implements persistence, Identity, Telegram and durable notification workers.
Controllers call application use cases, not DbContext directly.
Infrastructure must never be referenced by Domain or Application.

## Unit of Work and API rate limiting

Implemented on 2026-10-27: Application-level `IUnitOfWork`, backed by `EfUnitOfWork`
and the same scoped EF Core DbContext in Infrastructure. Repositories must share that
context and must not commit independently. Do not inject EF types into Application.

`SaveChangesAsync` saves tracked changes. `ExecuteInTransactionAsync<T>` begins a local
transaction, invokes the database-only callback, saves remaining changes and commits.
Intermediate saves are allowed (e.g. to obtain an IDENTITY Id before constructing a result).
If the callback returns an identity-derived scalar before saving, that scalar has not yet
been generated; explicitly save before reading it. Failures roll back earlier saves, clear
tracking and invalidate the scope. Do not catch a persistence failure and continue using it.
Nested/ambient transactions and retrying execution strategies are rejected; retry an
idempotent whole use case with a fresh scope after verifying ambiguous commit outcomes.
No external Telegram/HTTP calls belong inside the callback.

Business API endpoints have a global per-IP baseline; named policies are available via
`RateLimitPolicies`: `Catalog`, `Login`, `Checkout`, `Admin`, `Reports`. Attach a policy with
`[EnableRateLimiting(RateLimitPolicies.Checkout)]` or `.RequireRateLimiting(...)` when adding
the corresponding real endpoints. Admin/reports use authenticated user ID, falling back to IP.
Reports limit concurrent work per user/client, not a global system-wide worker count.

`appsettings.json` provides initial tuning values: global/catalog/admin 120/minute,
login 5/minute, checkout 10/minute, two concurrent reports per user/client; no waiting queue.
These are development starting points, not validated production capacity. Settings validate
at startup; restart the API after changing quota configuration. Rejection is HTTP 429,
`application/problem+json`, code `RATE_LIMIT_EXCEEDED`, with `Retry-After` when available.
Health is explicitly exempt for liveness; Swagger is Development-only middleware before
the limiter. Forwarded headers are not trusted automatically. Configure trusted proxies
before deployment; do not read arbitrary X-Forwarded-For in partition keys.
Authentication middleware is in the correct position but no real Identity scheme is configured yet.

Counters are local to each API process. Multiple instances need a shared/gateway strategy later,
not Redis locks. Rate limits do not replace authorization, idempotency or database concurrency.
See [backend architecture requirements](../../docs/backend-architecture.md) for
transaction boundaries, policy behavior and verification requirements.

## Development

Requirements: .NET 8 SDK (global.json selects the installed 8.0 SDK), SQL Server
instance `KDADEPTRAI\KDADEPTRAI`, and NuGet access for restore.

From this directory:

```powershell
dotnet restore ForFitChicken.sln
dotnet build ForFitChicken.sln --no-restore
dotnet run --project ForFitChicken.API --launch-profile https
dotnet test ForFitChicken.UnitTest --no-restore
```

Open `https://localhost:7010/swagger` or `https://localhost:7010/health`.
The health endpoint is process liveness only, not database readiness.
Use a trusted local ASP.NET development certificate for HTTPS.

`appsettings.Development.json` contains only a local integrated-security connection:
`ForFitChicken_Development` on `KDADEPTRAI\KDADEPTRAI` using Windows Authentication.
The development certificate trust setting for SQL is scoped to that file.
Production must supply its own connection/security configuration.
Keep secrets out of tracked files; use user-secrets locally or external secret configuration.

## Scope of this foundation

- Existing API renamed to `ForFitChicken.API`; reopen the solution if Visual Studio still shows the old project.
- EF Core DbContext and SQL Server registration live in Infrastructure.
- OpenAPI/Swagger UI are enabled only in Development.
- ProblemDetails, exception/status-code handlers and liveness are registered.
- Unit of Work and rate-limit policies are implemented and covered by categorized tests.
- Domain entities, EF configurations and the initial migration are implemented.
- Identity account/role tables exist; authentication endpoints/scheme, business endpoints
  and the Telegram worker are not implemented yet.

Implement Identity authentication next, then one complete application use case at a time.
Do not call EnsureCreated or run migrations automatically on API startup.

The project retains its original net8.0 target. Before production, revisit the previously
discussed .NET 10 LTS upgrade; this restructuring does not resolve .NET 8's approaching end of support.

## Foundation verification (2026-10-26)

- Solution restore and Debug build succeeded: five projects, zero warnings/errors.
- A temporary Development process returned HTTP 200 for `/health`, `/swagger/index.html`
  and `/swagger/v1/swagger.json`; OpenAPI version 3.0.1. That process was stopped.
- The xUnit runner started and reported no tests, as expected for the empty test project.
- LocalDB instance `MSSQLLocalDB` was listed. No database was created, connection to the
  application database was not exercised, and no migration was applied.

References: [dependency direction and Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures),
[EF Core DbContext configuration](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/).

## Unit of Work / rate-limit verification (2026-10-27)

- Debug solution build: zero warnings/errors.
- Six actual LocalDB tests passed: commit, rollback after save, unique-constraint failure,
  cancellation, nested-transaction rejection and shared scoped context.
- Six HTTP integration tests passed: baseline/429 response and spoofed header, independent
  IP buckets, independent named policies, authenticated user buckets, window reset and
  concurrency permit release. Five invalid-configuration cases passed.
- SQL fixtures created/removed only their own generated temporary databases. No ERD migration
  was applied and no real business endpoint was added for testing.
- See the test project README for opt-in SQL instructions and test-only dependencies.

## Initial schema verification (2026-10-27)

- `InitialSchema` creates 22 business tables and 7 ASP.NET Identity tables. Optional
  `OperatingExpenses` is excluded. Seed data contains Owner/Operator roles only, no user/password.
- Applied to `ForFitChicken_Development` on LocalDB after testing in a separate temporary DB.
- All 26 tests passed: 9 migration/schema tests, 6 transaction tests and 11 HTTP/config cases.
- `dotnet ef migrations has-pending-model-changes` reports no changes.
- SQL rowversion rejects stale updates. Multi-row inventory/order concurrency still requires
  application transactions and dedicated use-case tests; it is not complete merely from this schema.

From this solution directory, on a new development machine:

```powershell
dotnet tool restore
dotnet ef database update --project ForFitChicken.Infrastructure --startup-project ForFitChicken.API --context ForFitChickenDbContext -- --environment Development
```

Do not edit a migration after it has been applied; create a new migration for schema changes.
See [schema implementation notes](../../docs/database/implementation.md) for coverage and remaining rules.

## Development server change (2026-10-27)

Development and SQL integration-test defaults now use `KDADEPTRAI\KDADEPTRAI`
with Windows Authentication. The server/instance spelling was verified on this machine.
Applied InitialSchema to `ForFitChicken_Development` there and verified 29 tables and both
roles. All 26 tests passed with SQL tests enabled on the new server. The old LocalDB
`ForFitChicken_Development` contained no business data and was deleted at the user's request.
