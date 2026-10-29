# Repository working notes

Read `docs/HANDOFF.md` first, then `docs/mvp-backend-progress.md` and the API contract
`docs/mvp-backend-api.md`. They contain the business decisions, current status and next work
so a new session can continue without the original chat history.

- Keep the five-project Clean Architecture. Business use cases live in Application;
  EF Core, file storage and Telegram adapters live in Infrastructure.
- SQL Server is `KDADEPTRAI\KDADEPTRAI`, Windows Authentication. Do not switch to LocalDB.
- Preserve uncommitted work. Do not reset files, rewrite applied migrations or delete the
  development database. Tests may create/delete only their own GUID-named disposable databases.
- No default passwords, real test accounts or bot credentials in tracked files. The user chooses
  their real Owner credentials through the bootstrap script.
- Update progress and handoff docs after meaningful checkpoints. State what was actually tested;
  fake Telegram tests do not prove a real bot message was sent.
- Use the existing Unit of Work and transaction-scoped SQL business lock for stock/order/gym
  writes. External HTTP must run outside database transactions.
- Keep gym stock separate from online warehouse availability. Missing gym stock is unresolved
  until verified; never count it automatically as sales.
- Public API DTOs must not expose recipient PII, internal notes, price costs or Identity internals.
- Do not commit or push new work unless the user requests it.
- Media: Owner and Operator may upload. Use governed IMediaStorage for business uploads, never
  call the raw R2 adapter from a new controller/use case. Keep the 9,000,000,000-byte total cap;
  unknown reservations never expire out of quota. Do not reset counters/scope to bypass a failure.
- Read docs/media-security.md and docs/vps-maintenance-runbook.md for media/deployment changes.
  All VPS nodes writing one bucket must share the SQL quota ledger and reconcile before uploads.
- Stop test API instances after verification unless the user asks to keep them running, so a
  later Visual Studio/dotnet run does not collide on 7010/5195. Verify process ownership before stopping.

Build/test commands and setup are in `backend/ForFitChicken/README.md` and `docs/HANDOFF.md`.
