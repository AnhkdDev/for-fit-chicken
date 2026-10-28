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

Build/test commands and setup are in `backend/ForFitChicken/README.md` and `docs/HANDOFF.md`.
