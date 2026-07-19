# Local development — smoke runbook

**Purpose:** Reproducible steps to run the Next.js app, .NET API, and dependencies for manual smoke testing.  
**Companion:** [PHASE_A_TEST_MATRIX.md](./PHASE_A_TEST_MATRIX.md), [PHASE_A_BASELINE_CRITIQUE.md](./PHASE_A_BASELINE_CRITIQUE.md).

---

## Prerequisites

| Tool           | Notes                                                                                           |
| -------------- | ----------------------------------------------------------------------------------------------- |
| **Node.js**    | 20+ (see `packageManager` in [`ardanova-client/package.json`](../ardanova-client/package.json)) |
| **npm**        | 10+ (bundled with Node)                                                                         |
| **.NET SDK**   | 10.x — for [`ardanova-backend-api-mcp`](../ardanova-backend-api-mcp/)                           |
| **PostgreSQL** | 14+ **or** use a remote DB URL (e.g. team Railway) via `DATABASE_URL`                           |
| **Docker**     | Optional — [`docker-compose.yml`](../docker-compose.yml) at repo root                           |

---

## Environment variables

1. Copy the root template:  
   `cp .env.example .env` (from [repository root](../))
2. Fill at minimum:

| Variable                                    | Role                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DATABASE_URL`                              | Prisma + EF — PostgreSQL connection string                                                                                                                                                                                                                                                                                                                                                                                                      |
| `API_URL`                                   | Base URL for .NET API — **must match the URL printed when the API starts** (scheme + host + port). Default in app is `http://127.0.0.1:5147` (see `ardanova-client/src/env.js`). `localhost` in `API_URL` is rewritten to `127.0.0.1` for server-side `fetch` so Node targets IPv4 (Kestrel often listens on `127.0.0.1` only; `::1` can cause `fetch failed`). Use port `8080` only if you start the API with `ASPNETCORE_URLS=http://+:8080`. |
| `API_KEY`                                   | **Required and must exactly match** in the Next.js BFF and .NET API processes. The API reads `API_KEY` first, then an optional `ApiKey:Key` configuration value; the committed `appsettings.json` intentionally contains no default secret. Next sends it as `X-Api-Key`. If you see `Invalid API Key`, the two process values differ.                                                                                                          |
| `ADMIN_API_KEY`                             | Distinct random 32+ byte server-only key. Use the same value in the frontend BFF and .NET API; never expose it as `NEXT_PUBLIC_*`. Production readiness fails without it.                                                                                                                                                                                                                                                                       |
| `ACTOR_ASSERTION_HMAC_KEY`                  | Random 32+ byte server-only signing key shared by the frontend BFF and .NET API.                                                                                                                                                                                                                                                                                                                                                                |
| `AUTH_SECRET`                               | NextAuth — long random string (32+ chars)                                                                                                                                                                                                                                                                                                                                                                                                       |
| `AUTH_URL`                                  | App origin (e.g. `http://localhost:3000`)                                                                                                                                                                                                                                                                                                                                                                                                       |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` | Required by startup validation. Use real local OAuth credentials for auth testing; for a preview-only local shell, use explicit non-empty local placeholders and `DEV_AUTH_BYPASS=true` without attempting the provider flow.                                                                                                                                                                                                                   |
| `DEV_AUTH_BYPASS`                           | Optional frontend-only UI preview. Set to `true` only in a local development environment; production ignores it by design.                                                                                                                                                                                                                                                                                                                      |
| `STORAGE_PROVIDER`                          | `Local` or `S3` (+ S3 vars if not local)                                                                                                                                                                                                                                                                                                                                                                                                        |
| `ASPNETCORE_ENVIRONMENT`                    | Usually `Development`                                                                                                                                                                                                                                                                                                                                                                                                                           |

Details: [.env.example](../.env.example).

**Frontend env:** `src/env.js` loads the repository-root environment first and
then any client-local environment. Keep shared secrets in one source to avoid
drift; a client-local file is only for intentional frontend overrides.

**Backend database config:** The API and MCP server do not read a database
credential from `appsettings.json`. Docker Compose passes root `.env`
`DATABASE_URL` through automatically. For a direct `dotnet run`, export
`DATABASE_URL` (or `ConnectionStrings__DefaultConnection`) in the terminal.
Missing database configuration stops startup before either process accepts
requests.

**Optional:** `NEXT_PUBLIC_TRPC_DEBUG=true` — enables verbose tRPC client logs (`>>` / `<<` per procedure). Off by default so the browser console is not flooded; use when debugging network/tRPC issues.

### Preview the authenticated shell without Google OAuth

For design and browser testing, set `DEV_AUTH_BYPASS=true` in `ardanova-client/.env` and restart `npm run dev`. The app shows a preview-session banner so the state cannot be mistaken for real authentication.

The preview is deliberately narrow: it is honored only when `NODE_ENV=development`, it does not bypass API or database authorization, and it must never be set in Railway. Remove the flag before checking the real NextAuth sign-in flow.

### Isolated local QA database

Do not point routine browser QA at a shared, Railway, or locally restored production database. Create a loopback PostgreSQL database named `ardanova_local_demo` (or `ardanova_local_demo_<suffix>`), set `DATABASE_URL` in the calling shell, then synchronize it from `ardanova-client`:

```bash
npx prisma db push --skip-generate
npm run seed:admins
```

Then explicitly enable the demo seed for that invocation:

```bash
ALLOW_LOCAL_DEMO_SEED=true npm run seed:local-demo
```

In PowerShell, set and remove the process-only flag around the command:

```powershell
$env:ALLOW_LOCAL_DEMO_SEED = "true"
npm run seed:local-demo
Remove-Item Env:ALLOW_LOCAL_DEMO_SEED
```

`seed:local-demo` is additive and repeatable. Its synthetic preview identity is fixed at `local-preview-user` / `preview@local.ardanova.test` so `DEV_AUTH_BYPASS=true` opens projects that the preview user actually owns. It creates three projects, tasks, a guild, and events that exercise the discover -> define -> iterate experience. Neighborhood Heat Commons also includes a deterministic, linked work hierarchy (milestone -> epic -> active sprint -> feature -> PBI), two assigned heat tasks, a founder membership, and a project-targeted steward comment. Re-running the seed updates those fixed fixtures instead of duplicating them.

Before constructing Prisma, the seed requires the explicit opt-in, refuses `NODE_ENV=production`, requires PostgreSQL on a loopback host (`localhost`, `127.0.0.1`, or `::1`), and checks the dedicated database name. `prisma db push` can still change schema, so use it only against that same database.

The `dev-up` launchers enforce the same safe default. They refuse a hosted `DATABASE_URL` in local mode before starting either service. For a deliberate, coordinated remote-development session only, set `ALLOW_REMOTE_DEV_DATABASE=true` in the calling process; do not store that override in `.env`. The launchers deliberately ignore that variable if it appears in a dotenv file.

`ADMIN_API_KEY` and `ACTOR_ASSERTION_HMAC_KEY` must use the same respective 32+ byte values in the frontend and .NET API processes. Starting both through the repository `dev-up` launcher from one shell ensures they inherit the same process values.

---

## Recommended launcher

From the repository root, validate prerequisites and then start both services
through the guarded launcher:

```powershell
.\scripts\dev-up.ps1 -Check
.\scripts\dev-up.ps1
```

```bash
./scripts/dev-up.sh --check
./scripts/dev-up.sh
```

Use `-Mode docker` (PowerShell) or `--docker` (bash) only after setting a
reachable PostgreSQL `DATABASE_URL`. The launchers load the shared root `.env`,
preserve explicit process overrides, refuse hosted databases in routine local
mode, and keep the API/client secrets aligned without printing them.

---

## Manual run (two terminals)

**Terminal 1 — .NET API**

```bash
cd ardanova-backend-api-mcp
dotnet run --project api-server/src/ArdaNova.API
```

- The API prints **“Now listening on: …”** — use that exact origin as `API_URL` in your root `.env` (e.g. `http://localhost:5147` from [`launchSettings.json`](../ardanova-backend-api-mcp/api-server/src/ArdaNova.API/Properties/launchSettings.json)). If you instead set `ASPNETCORE_URLS=http://+:8080` in `.env`, the API listens on **8080** and `API_URL` should be `http://localhost:8080`.
- **Common mistake:** a copied environment still points to port **8080** while
  `dotnet run` without `ASPNETCORE_URLS` uses **5147**. The application default
  is `http://127.0.0.1:5147`; an explicit `API_URL` always wins and must match
  the origin printed by Kestrel.

**Terminal 2 — Next.js**

```bash
cd ardanova-client
npm ci   # first time or after package-lock.json changes
npm run dev
```

- App: `http://localhost:3000`
- Uses Turbopack (`next dev --turbo` per package script).
- Run `npm run generate:prisma` after changing the Prisma schema.

---

## Smoke checks (fast)

| #   | Check                                                                     | Expected                                                                                              |
| --- | ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| 1   | `curl -sS http://localhost:5147/health` (or the origin logged by the API) | HTTP 200, process liveness JSON                                                                       |
| 2   | `curl -sS http://localhost:5147/ready`                                    | HTTP 200 only when API configuration and PostgreSQL connectivity are ready                            |
| 3   | Open `http://localhost:3000`                                              | Home page renders                                                                                     |
| 4   | `http://localhost:3000/api/health`                                        | Next process liveness responds ([`app/api/health`](../ardanova-client/src/app/api/health))            |
| 5   | `http://localhost:3000/api/ready`                                         | HTTP 200 only when required frontend configuration and bounded API readiness succeed                  |
| 6   | `http://localhost:3000/api/trpc`                                          | tRPC handler reachable (may need POST for procedures; loading app that uses tRPC is enough for smoke) |

**Optional:** Sign in via Google — only works if the OAuth app includes `http://localhost:3000` and the NextAuth callback URI. Turn off `DEV_AUTH_BYPASS` for this check.

---

## Docker (alternative)

From repository root (with `.env` present):

The `dev-up` launchers import the shared root `.env` in local mode without
overriding variables already set by the calling shell. This keeps the .NET API
and Next.js client on the same database, API key, and service origins. Secrets
are never printed by the launcher.

```bash
docker-compose up
```

Services and ports are defined in [`docker-compose.yml`](../docker-compose.yml). The compose file intentionally does not create PostgreSQL; `DATABASE_URL` must point to a reachable local or explicitly approved development database before startup.

---

## Known issues & triage

| Symptom                                                             | Likely cause                                                                                         | Mitigation                                                                                                                                                           |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Turbopack “multiple lockfiles” / wrong workspace root**           | Root `package-lock.json` + `ardanova-client/package-lock.json`                                       | [`next.config.js`](../ardanova-client/next.config.js) sets `turbopack.root` to `process.cwd()` — run `npm run dev` from `ardanova-client`                            |
| **`npm` / `node` not found**                                        | nvm not loaded in shell                                                                              | `source ~/.nvm/nvm.sh && nvm use 20` or ensure Node 20 is on `PATH`                                                                                                  |
| **`prisma db push` warns about data loss**                          | Schema vs shared DB                                                                                  | Use a personal DB, or coordinate with team; do not `--accept-data-loss` on shared data without approval                                                              |
| **API 401 / 403 from Next to .NET**                                 | `API_KEY` mismatch                                                                                   | Align `.env` `API_KEY` with backend configuration                                                                                                                    |
| **Google sign-in fails**                                            | OAuth console misconfiguration                                                                       | Add authorized origins + redirect for `http://localhost:3000`                                                                                                        |
| **Google reports `redirect_uri_mismatch`**                          | The app was started on a different origin, often `localhost:3100`                                    | Run the app at `http://localhost:3000` or register the exact emitted callback; verify `/api/auth/providers` reports `http://localhost:3000/api/auth/callback/google` |
| **Seed command refuses to run**                                     | Opt-in is absent, `NODE_ENV=production`, or `DATABASE_URL` is not a dedicated loopback demo database | Use `ardanova_local_demo`, verify the loopback URL, and set `ALLOW_LOCAL_DEMO_SEED=true` only for the seed invocation; do not weaken the guard                       |
| **Preview banner does not appear**                                  | Preview flag absent, misspelled, or server not restarted                                             | Set `DEV_AUTH_BYPASS=true` in the frontend local environment and restart; the bypass is intentionally unavailable in production                                      |
| **CORS / SignalR**                                                  | Cross-origin                                                                                         | Backend allows configured origins; test from same host first                                                                                                         |
| **`NU1301` / Unable to load the service index for `api.nuget.org`** | No route to NuGet, proxy, firewall, DNS, or TLS/CA issues                                            | Run [`scripts/verify-nuget-feed.sh`](../ardanova-backend-api-mcp/scripts/verify-nuget-feed.sh); see [NUGET_NU1301.md](./NUGET_NU1301.md)                             |

---

## Related

- [README.md](../README.md) — full stack overview and installation
- [QA-TESTING-CHECKLIST.md](../QA-TESTING-CHECKLIST.md) — comprehensive regression (not required for initial smoke)
- [NUGET_NU1301.md](./NUGET_NU1301.md) — when `dotnet restore` cannot reach nuget.org
