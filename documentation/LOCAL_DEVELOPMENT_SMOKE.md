# Local development — smoke runbook

**Purpose:** Reproducible steps to run the Next.js app, .NET API, and dependencies for manual smoke testing.  
**Companion:** [PHASE_A_TEST_MATRIX.md](./PHASE_A_TEST_MATRIX.md), [PHASE_A_BASELINE_CRITIQUE.md](./PHASE_A_BASELINE_CRITIQUE.md).

---

## Prerequisites

| Tool | Notes |
|------|--------|
| **Node.js** | 20+ (see `packageManager` in [`ardanova-client/package.json`](../ardanova-client/package.json)) |
| **npm** | 10+ (bundled with Node) |
| **.NET SDK** | 8.x — for [`ardanova-backend-api-mcp`](../ardanova-backend-api-mcp/) |
| **PostgreSQL** | 14+ **or** use a remote DB URL (e.g. team Railway) via `DATABASE_URL` |
| **Docker** | Optional — [`docker-compose.yml`](../docker-compose.yml) at repo root |

---

## Environment variables

1. Copy the root template:  
   `cp .env.example .env` (from [repository root](../))
2. Fill at minimum:

| Variable | Role |
|----------|------|
| `DATABASE_URL` | Prisma + EF — PostgreSQL connection string |
| `API_URL` | Base URL for .NET API — **must match the URL printed when the API starts** (scheme + host + port). Default in app is `http://127.0.0.1:5147` (see `ardanova-client/src/env.js`). `localhost` in `API_URL` is rewritten to `127.0.0.1` for server-side `fetch` so Node targets IPv4 (Kestrel often listens on `127.0.0.1` only; `::1` can cause `fetch failed`). Use port `8080` only if you start the API with `ASPNETCORE_URLS=http://+:8080`. |
| `API_KEY` | **Must exactly match** the .NET API: `Environment.GetEnvironmentVariable("API_KEY")` if you set it when starting `dotnet run`, otherwise `ApiKey:Key` in [`appsettings.json`](../ardanova-backend-api-mcp/api-server/src/ArdaNova.API/appsettings.json) (default **`dev-api-key-12345`**). Next sends it as `X-Api-Key`. If you see `Invalid API Key` in API logs, the client and server keys differ. |
| `AUTH_SECRET` | NextAuth — long random string (32+ chars) |
| `AUTH_URL` | App origin (e.g. `http://localhost:3000`) |
| `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` | Google OAuth (optional for unauthenticated smoke) |
| `STORAGE_PROVIDER` | `Local` or `S3` (+ S3 vars if not local) |
| `ASPNETCORE_ENVIRONMENT` | Usually `Development` |

Details: [.env.example](../.env.example).

**Frontend env:** If you run only from `ardanova-client`, ensure Next sees these variables (e.g. symlink or copy root `.env` to `ardanova-client/.env`, or use tooling your team documents).

**Optional:** `NEXT_PUBLIC_TRPC_DEBUG=true` — enables verbose tRPC client logs (`>>` / `<<` per procedure). Off by default so the browser console is not flooded; use when debugging network/tRPC issues.

---

## Manual run (two terminals)

**Terminal 1 — .NET API**

```bash
cd ardanova-backend-api-mcp
dotnet run --project api-server/src/ArdaNova.API
```

- The API prints **“Now listening on: …”** — use that exact origin as `API_URL` in your root `.env` (e.g. `http://localhost:5147` from [`launchSettings.json`](../ardanova-backend-api-mcp/api-server/src/ArdaNova.API/Properties/launchSettings.json)). If you instead set `ASPNETCORE_URLS=http://+:8080` in `.env`, the API listens on **8080** and `API_URL` should be `http://localhost:8080`.
- **Common mistake:** Next defaults to `API_URL=http://localhost:8080` while `dotnet run` without `ASPNETCORE_URLS` uses **5147** → tRPC `fetch failed` until `API_URL` matches.

**Terminal 2 — Next.js**

```bash
cd ardanova-client
npm install   # first time
npm run dev
```

- App: `http://localhost:3000`
- Uses Turbopack (`next dev --turbo` per package script).

---

## Smoke checks (fast)

| # | Check | Expected |
|---|--------|----------|
| 1 | `curl -sS http://localhost:5147/health` (or whatever port your API logged; try `:8080` if you use `ASPNETCORE_URLS`) | HTTP 200, JSON with health info |
| 2 | Open `http://localhost:3000` | Home page renders |
| 3 | `http://localhost:3000/api/health` | Next health route responds ([`app/api/health`](../ardanova-client/src/app/api/health)) |
| 4 | `http://localhost:3000/api/trpc` | tRPC handler reachable (may need POST for procedures; loading app that uses tRPC is enough for smoke) |

**Optional:** Sign in via Google — only works if OAuth app includes `http://localhost:3000` and redirect URI for NextAuth.

---

## Docker (alternative)

From repository root (with `.env` present):

```bash
docker-compose up
```

Services and ports are defined in [`docker-compose.yml`](../docker-compose.yml). Ensure `DATABASE_URL` points to a reachable Postgres (compose file may not start Postgres; confirm with your team).

---

## Known issues & triage

| Symptom | Likely cause | Mitigation |
|---------|----------------|------------|
| **Turbopack “multiple lockfiles” / wrong workspace root** | Root `package-lock.json` + `ardanova-client/package-lock.json` | [`next.config.js`](../ardanova-client/next.config.js) sets `turbopack.root` to `process.cwd()` — run `npm run dev` from `ardanova-client` |
| **`npm` / `node` not found** | nvm not loaded in shell | `source ~/.nvm/nvm.sh && nvm use 20` or ensure Node 20 is on `PATH` |
| **`prisma db push` warns about data loss** | Schema vs shared DB | Use a personal DB, or coordinate with team; do not `--accept-data-loss` on shared data without approval |
| **API 401 / 403 from Next to .NET** | `API_KEY` mismatch | Align `.env` `API_KEY` with backend configuration |
| **Google sign-in fails** | OAuth console misconfiguration | Add authorized origins + redirect for `http://localhost:3000` |
| **CORS / SignalR** | Cross-origin | Backend allows configured origins; test from same host first |
| **`NU1301` / Unable to load the service index for `api.nuget.org`** | No route to NuGet, proxy, firewall, DNS, or TLS/CA issues | Run [`scripts/verify-nuget-feed.sh`](../ardanova-backend-api-mcp/scripts/verify-nuget-feed.sh); see [NUGET_NU1301.md](./NUGET_NU1301.md) |

---

## Related

- [README.md](../README.md) — full stack overview and installation
- [QA-TESTING-CHECKLIST.md](../QA-TESTING-CHECKLIST.md) — comprehensive regression (not required for initial smoke)
- [NUGET_NU1301.md](./NUGET_NU1301.md) — when `dotnet restore` cannot reach nuget.org
