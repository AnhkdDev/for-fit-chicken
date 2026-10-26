# ForFitChicken backend

Clean Architecture, one deployable API, SQL Server LocalDB for development.

## Projects and dependency direction

```text
ForFitChicken.API             -> Application, Infrastructure (composition root)
ForFitChicken.Application     -> Domain
ForFitChicken.Domain          -> no project dependencies
ForFitChicken.Infrastructure  -> Application, Domain
ForFitChicken.UnitTest        -> Application, Domain
```

API owns HTTP controllers, request authentication/authorization and composition.
Application owns use cases, DTOs and interfaces. Domain owns business rules.
Infrastructure implements persistence, Identity, Telegram and durable notification workers.
Controllers call application use cases, not DbContext directly.
Infrastructure must never be referenced by Domain or Application.

## Development

Requirements: .NET 8 SDK (global.json selects the installed 8.0 SDK), SQL Server LocalDB
instance `MSSQLLocalDB`, and NuGet access for restore.

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
`ForFitChicken_Development` on `(localdb)\MSSQLLocalDB`.
The development certificate trust setting for SQL is scoped to that file.
Production must supply its own connection/security configuration.
Keep secrets out of tracked files; use user-secrets locally or external secret configuration.

## Scope of this foundation

- Existing API renamed to `ForFitChicken.API`; reopen the solution if Visual Studio still shows the old project.
- EF Core DbContext and SQL Server registration live in Infrastructure.
- OpenAPI/Swagger UI are enabled only in Development.
- ProblemDetails, exception/status-code handlers and liveness are registered.
- No domain entities, Identity setup, migrations, database creation, business endpoints,
  rate-limit policy or Telegram integration have been implemented yet.
- UnitTest is wired for xUnit but currently contains no tests. An empty test run is not business validation.

Implement the reviewed ERD next, with entity configurations/migrations in Infrastructure,
then Identity and one complete application use case at a time.
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
