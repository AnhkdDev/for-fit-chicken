# Tests

Keep the five requested projects. This test project contains clearly separated categories:

- `Unit`: fast configuration/rule checks; future Domain/Application tests go here.
- `HttpIntegration`: WebApplicationFactory exercises the real API middleware pipeline.
- `SqlServerIntegration`: actual EF Core transactions on an isolated SQL Server/LocalDB database.

Test-only references to API and Infrastructure enable integration checks; production dependency
direction is unchanged. Never call a mock/in-memory test proof of SQL Server rollback or locking.
Probe controllers and fake authentication/IP middleware are defined in this assembly only;
they are not endpoints or authentication methods in the production API.

From the solution directory:

```powershell
# Default: SQL integration tests are explicitly skipped.
dotnet test ForFitChicken.UnitTest

# Actual SQL Server/LocalDB verification:
$env:FORFITCHICKEN_RUN_SQL_TESTS = '1'
dotnet test ForFitChicken.UnitTest
```

SQL tests default to `KDADEPTRAI\KDADEPTRAI`. Optional `FORFITCHICKEN_TEST_SQL_CONNECTION`
selects another test SQL Server connection; the fixture always overrides the database name
with its generated `ForFitChicken_UowTests_<guid>` or `ForFitChicken_SchemaTests_<guid>`
and only deletes that exact database. Schema tests use real migrations; transaction probe
tests use EnsureCreated only on their disposable database with an extra probe table.
No application/development database is migrated, seeded or deleted by these tests.
Do not run SQL tests where the account cannot create/drop a disposable test database.

Verification on 2026-10-27: six SQL transaction tests passed; after fixing deferred rate-limit
configuration, all eleven affected HTTP/configuration tests passed. These tests do not validate
inventory business rules that have not been implemented yet.

Schema verification on 2026-10-27: nine additional SQL tests passed, covering migration/table
creation, role seeds, NO ACTION foreign keys, rowversion stale writes, false boolean inserts,
composite image FK, stock reservation checks, computed item totals, unique item variants,
filtered allocation/receipt uniqueness, gym predecessor boundaries and notification JSON/lease.
Full run: 26 passed, 0 failed, 0 skipped with SQL opt-in enabled.
