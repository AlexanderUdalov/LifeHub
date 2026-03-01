# AGENTS.md

## Cursor Cloud specific instructions

### Overview

LifeHub is a monorepo with two services:

| Service | Path | Tech | Dev Port |
|---------|------|------|----------|
| Backend API | `Backend/` | .NET 10 (ASP.NET Core, SQLite) | 5091 |
| Frontend SPA | `Frontend/` | Vue 3 + Vite + TypeScript + PrimeVue | 5173 |

### Running services

Start both services together from the repo root: `npm run dev` (uses `concurrently`).
Or individually: `npm run dev:backend`, `npm run dev:frontend`. See `README.md` for details.

**Per workspace rules** (`.cursor/rules/backend-stop-after-use.mdc`): always stop the backend after use to avoid port conflicts.

### Database (SQLite)

The SQLite database file lives at `Backend/bin/Debug/database/lifehub.db`. The directory must exist before the backend can write to it. Create it if missing:

```bash
mkdir -p Backend/bin/Debug/database
```

Migrations are **not** auto-applied. After cloning or pulling new migrations, run:

```bash
dotnet ef database update --project Backend
```

This requires the `dotnet-ef` global tool (`dotnet tool install --global dotnet-ef`).

### Lint / Type-check / Test / Build

| Check | Command | Working dir |
|-------|---------|-------------|
| Type-check | `npx vue-tsc --build` | `Frontend/` |
| Format check | `npx prettier --check --experimental-cli src/` | `Frontend/` |
| Tests | `npm test` | `Frontend/` |
| Build backend | `dotnet build Backend` | repo root |
| Build frontend | `npm run build` | `Frontend/` |

### Non-obvious caveats

- The `.NET 10` SDK is required (not .NET 8). The update script installs it to `~/.dotnet`.
- The frontend Vite dev server proxies `/api` requests to `http://localhost:5091` — the backend must be running for API calls to work.
- API schema types at `Frontend/src/api/schema.ts` are auto-generated. To regenerate: start the backend, then run `npm run api:gen` from `Frontend/`. See `.cursor/rules/backend-dto-schema-gen.mdc`.
- Node.js engine requirement: `^20.19.0 || >=22.12.0` (see `Frontend/package.json`).
