# Application

Organize use cases by feature: Catalog, Inventory, Orders, Gyms and Notifications.
Keep request/response DTOs, application validation and persistence/service interfaces here.
Depend on Domain; do not reference Infrastructure, API, DbContext or SQL Server types.
Add abstractions when the first real use case needs them, rather than generating empty repositories.
