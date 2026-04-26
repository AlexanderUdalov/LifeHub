# LifeHub

[Russian version](README.ru.md)

LifeHub is a personal growth command center for people who want more than another isolated to-do list.

It brings tasks, habits, goals, journaling, addiction tracking, life areas, and AI-assisted reflection into one connected workspace. Instead of spreading your plans, routines, notes, and setbacks across separate apps, LifeHub helps you see the full picture: what you want to change, what you are doing today, what is getting in the way, and how your story is evolving over time.

## Why LifeHub

Most self-improvement tools focus on one narrow problem. LifeHub is built around the idea that personal change is connected:

- Goals become concrete through tasks, habits, and recovery plans.
- Habits build consistency instead of living in a separate tracker.
- Journal entries preserve context, emotions, lessons, and turning points.
- Addiction tracking treats relapse, triggers, and recovery progress as first-class data.
- AI support helps turn raw notes and behavior into reflection prompts, summaries, and practical next steps.

LifeHub is designed for people who want a private, structured place to plan better days, understand patterns, and keep moving without losing the human context behind the numbers.

## Product Highlights

- Unified personal dashboard domains: tasks, habits, addictions, life areas, goals, journal, profile, and authentication.
- Goal-centered planning: connect what you want with the actions and routines that move it forward.
- Habit tracking for recurring behaviors, consistency, and long-term discipline.
- Addiction recovery tracking with resets, urges, triggers, notes, and dedicated detail pages.
- Journaling for daily reflection, context, and progress narratives.
- AI-assisted reflection powered by an API-based LLM integration when an API key is configured.
- API-first architecture with generated TypeScript types for the frontend.
- Web-first experience with optional Tauri desktop packaging support.

## Project Status

LifeHub is a personal project with a complete core feature set for everyday use. Future work is expected to focus mainly on maintenance, bug fixes, and small quality-of-life improvements.

## Tech Stack

| Area | Technology |
| --- | --- |
| Frontend | Vue 3, Vite 7, TypeScript, Pinia, Vue Router, PrimeVue, vue-i18n, Zod |
| Backend | ASP.NET Core Web API, .NET 10, JWT Bearer authentication |
| Database | SQLite via Entity Framework Core |
| AI | Microsoft.Extensions.AI with OpenAI-compatible chat client |
| API Contracts | ASP.NET OpenAPI + `openapi-typescript` generated frontend schema |
| Testing | Vitest for frontend, xUnit for backend |
| Desktop | Optional Tauri 2 build flow |
| Deployment | GitHub Actions, VPS artifact upload, rsync, systemd backend restart |

## Repository Structure

```text
LifeHub/
├── Backend/          # ASP.NET Core API, EF Core models, controllers, services
├── Backend.Tests/    # xUnit backend tests
├── Frontend/         # Vue 3 + Vite application
├── Docs/             # Product documentation
└── .github/          # CI/CD workflow for deployment
```

## Getting Started

### Prerequisites

- .NET SDK 10.x
- Node.js 20.19+ or 22.12+
- npm
- EF Core CLI tools for database migrations
- Optional: Python or `sqlite3` for clearing a stale SQLite migration lock
- Optional: Tauri prerequisites for desktop builds

### Install Dependencies

```bash
npm install
npm install --prefix Frontend
dotnet restore
```

### Configure Local Secrets

The backend requires a JWT signing key. AI is optional, but AI features need an API key.

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-a-long-random-secret" --project Backend/LifeHub-Backend.csproj
dotnet user-secrets set "Ai:ApiKey" "replace-with-your-openai-key" --project Backend/LifeHub-Backend.csproj
```

See [`Backend/SECRETS.md`](Backend/SECRETS.md) for the local and production secret conventions.

### Apply Database Migrations

```bash
dotnet ef database update --project Backend/LifeHub-Backend.csproj
```

The application currently uses SQLite. At runtime the database is stored under the backend output directory in a `database/lifehub.db` file.

### Run the App

From the repository root:

```bash
npm run dev
```

This starts both services:

- Backend API: `http://localhost:5091`
- Frontend Vite dev server: usually `http://localhost:5173`

The Vite dev server proxies `/api` requests to the backend.

You can also run each side separately:

```bash
npm run dev:backend
npm run dev:frontend
```

## Useful Commands

| Command | Location | Description |
| --- | --- | --- |
| `npm run dev` | root | Start backend and frontend together |
| `npm run dev:backend` | root | Start only the ASP.NET Core API |
| `npm run dev:frontend` | root | Start only the Vite frontend |
| `npm run build` | `Frontend/` | Type-check and build the frontend |
| `npm run preview` | `Frontend/` | Preview the production frontend build |
| `npm run type-check` | `Frontend/` | Run Vue TypeScript checks |
| `npm run test` | `Frontend/` | Run frontend tests with Vitest |
| `npm run format` | `Frontend/` | Format frontend source files |
| `npm run api:gen` | `Frontend/` | Regenerate TypeScript API types from backend OpenAPI |
| `dotnet test` | root | Run backend tests |
| `npm run dev:tauri` | `Frontend/` | Run the Tauri-oriented frontend mode |
| `npm run build:tauri` | `Frontend/` | Build the Tauri-oriented frontend bundle |

## API Schema Workflow

The frontend consumes generated API types from `Frontend/src/api/schema.ts`.

When backend DTOs, controllers, or API contracts change:

1. Start the backend in Development mode.
2. Run `npm run api:gen` from `Frontend/`.
3. Commit the generated schema together with the API change.
4. Stop the backend process after generation.

OpenAPI is exposed at `http://localhost:5091/openapi/v1.json` in Development.

## Deployment

The repository includes a GitHub Actions workflow that deploys on pushes to `main`.

The workflow:

- Publishes the backend with `dotnet publish`.
- Installs and builds the frontend with npm.
- Uploads artifacts to a VPS.
- Syncs backend and frontend files into `/var/www/lifehub`.
- Writes production secrets into the backend `.env`.
- Restarts the `lifehub-backend` systemd service.

Required GitHub Secrets:

- `VPS_HOST`
- `VPS_USER`
- `VPS_KEY`
- `JWT_KEY`
- `AI_API_KEY`

## SQLite Migration Notes

EF Core protects SQLite migrations with a `__EFMigrationsLock` table. If a migration is interrupted or another process keeps the database open, a stale lock can block future migrations.

First stop the backend and any process using `lifehub.db`, then clear the lock:

```bash
sqlite3 Backend/bin/Debug/database/lifehub.db "DROP TABLE IF EXISTS \"__EFMigrationsLock\";"
```

On machines without `sqlite3`, use the helper script:

```bash
python Backend/scripts/drop_migrations_lock.py
```

Then run the migration command again.

## Maintenance

The project is intended to stay focused on its current product scope. Maintenance work may include bug fixes, dependency updates, documentation updates, and small refinements that improve the existing experience.

## Documentation

- [`Frontend/docs/design-system.md`](Frontend/docs/design-system.md) - frontend design system notes.
- [`Backend/SECRETS.md`](Backend/SECRETS.md) - local and production secret setup.

## Contributing

LifeHub is a personal project. Bug reports, documentation fixes, and focused improvements are welcome.

Before opening a pull request, please run the relevant checks:

```bash
npm run build --prefix Frontend
npm run test --prefix Frontend
dotnet test
```

## License

LifeHub is licensed under the [MIT License](LICENSE).
