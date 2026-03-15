# Secrets and API keys

Secrets are **never** committed to the repo. Use the following approaches.

## Local development (any machine)

Use **ASP.NET Core User Secrets**. They are stored per machine (outside the repo).

From the `Backend` folder:

```bash
dotnet user-secrets set "Jwt:Key" "your-dev-jwt-secret-at-least-32-characters-long"
dotnet user-secrets set "Ai:ApiKey" "sk-your-openai-api-key"
```

- Each developer / machine runs these once. Values stay in the local User Secrets store.
- To list or remove: `dotnet user-secrets list`, `dotnet user-secrets clear`.

## Production (GitHub Actions → VPS)

Secrets are stored in **GitHub repository secrets** and injected at deploy time.

1. In GitHub: **Settings → Secrets and variables → Actions** add:
   - `JWT_KEY` — key for signing JWT tokens (prod)
   - `AI_API_KEY` — OpenAI API key for production

2. The deploy workflow writes them into `/var/www/lifehub/backend/.env` on the VPS as:
   - `Jwt__Key`
   - `Ai__ApiKey`

The backend loads these via environment variables (e.g. from `.env` if your process reads it, or from the system environment).

## If a key was ever committed

If a key was pushed to the repo (even in the past):

1. **Revoke it immediately** in the provider’s dashboard (OpenAI, etc.).
2. Create a new key and set it only via User Secrets (dev) or GitHub Secrets (prod).
3. Consider rotating the JWT key in production and updating `JWT_KEY` in GitHub Secrets.
