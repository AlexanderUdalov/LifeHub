# AGENTS.md

## Cursor Cloud specific instructions

### Architecture

LifeHub is a two-service monorepo: a .NET 10 backend API (port 5091) and a Vue 3 + Vite frontend SPA (port 5173). The database is SQLite (file-based, auto-created by EF Core). See `README.md` for standard dev commands (`npm run dev` runs both).

### .NET SDK

The .NET 10 SDK is installed to `$HOME/.dotnet`. The PATH and `DOTNET_ROOT` are configured in `~/.bashrc`.

### Database initialization

On first run, the backend auto-creates the SQLite database directory and file via EF Core migrations applied at startup. If registration or API calls fail with 500 errors, check that the `database/` directory exists relative to the backend binary output (typically `Backend/bin/Debug/database/`). You can run `dotnet ef database update --project Backend` to apply migrations manually.

### Running services

- **Both services**: `npm run dev` from repo root (uses `concurrently`)
- **Backend only**: `dotnet run --project Backend` (or `npm run dev:backend`)
- **Frontend only**: `npm run dev --prefix Frontend` (or `npm run dev:frontend`)
- Per workspace rules in `.cursor/rules/backend-stop-after-use.mdc`, always stop the backend after use.

### Testing & linting

- **Frontend tests**: `npm test --prefix Frontend` (vitest, 22 tests)
- **Type-check**: `npx vue-tsc --build` in `Frontend/`
- **Format check**: `npx prettier --check --experimental-cli src/` in `Frontend/`
- **Backend build check**: `dotnet build Backend`

### Gotchas

- The UI is in Russian by default. Field labels use Russian text (e.g., "Никнейм" = Nickname, "Задачи" = Tasks).
- The frontend proxies `/api` requests to `http://localhost:5091` via Vite config — the backend must be running for API calls to work.
- `Frontend/src/api/schema.ts` is auto-generated; see `.cursor/rules/backend-dto-schema-gen.mdc` for regeneration steps.
