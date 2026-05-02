# Phase A baseline — architecture & product critique

**Purpose:** Summarize what the repository documentation claims versus what the codebase actually exposes, and recommend MVP scope.  
**Audience:** Contributors and leads (Discord / internal review).  
**Date:** Baseline snapshot (Phase A).

---

## Executive summary

| Area | Doc / expectation | Code reality |
|------|---------------------|--------------|
| **Next.js app** | “All features consolidated” in `ardanova-client` | Large App Router surface: projects, guilds, governance, opportunities, tasks, events, chats, dashboard — **present** |
| **Backend API** | .NET 8 + controllers + MCP | `ardanova-backend-api-mcp` with broad controller coverage; **Application.Tests** exercise many services |
| **AI orchestrator** | `ardanova-ai-client` | Structure exists; **not** wired as a core dependency for day-to-day web flows |
| **Ledger / contracts** | Algorand / PyTeal stubs | **Stubbed** — not blocking web QA for projects/guilds |
| **tRPC surface** | — | [`ardanova-client/src/server/api/root.ts`](../ardanova-client/src/server/api/root.ts) wires: `post`, `project`, `guild`, `task`, `event`, `opportunity`, `governance`, `sprint`, `epic`, `backlog`, `chat`, `enum`, `feature`, `product`, `opportunityBid`, `membershipCredential` — **no** `dao`, `exchange`, `agent`, or `gamification` routers |

**Testing posture:** Backend has substantial **xUnit** coverage under `ardanova-backend-api-mcp/api-server/tests/`. The Next.js app has **Vitest** configured but very few tests (e.g. auth config); **manual QA** remains the primary gate for UI flows (see [PHASE_A_TEST_MATRIX.md](./PHASE_A_TEST_MATRIX.md) and [QA-TESTING-CHECKLIST.md](../QA-TESTING-CHECKLIST.md)).

---

## Doc vs code mismatches

### `documentation/ROADMAP.md` repository structure

The roadmap’s example tree lists under `ardanova-client/src/app/`:

- `marketplace/`, `dao/`, `studio/`, `exchange/`, `explorer/`, `agent/` as **TODO** routes.

**Actual `app/` routes (representative):** `auth/`, `chats/`, `dashboard/`, `events/`, `governance/`, `guilds/`, `opportunities/`, `projects/`, `tasks/` — **no** `dao/` or `marketplace/` folders; **governance** lives under `governance/`, not `dao/`.

The same doc references tRPC routers such as `dao.ts`, `exchange.ts`, `agent.ts`, `gamification.ts`. **None of these files exist** in [`ardanova-client/src/server/api/routers/`](../ardanova-client/src/server/api/routers/). Governance is implemented as [`governance.ts`](../ardanova-client/src/server/api/routers/governance.ts).

**Recommendation:** Update `documentation/ROADMAP.md` repository structure to match `root.ts` and `app/`, or add an explicit “historical / planned” subsection so new contributors are not misled.

### README “Project Structure” vs monorepo layout

Top-level README diagrams show `ardanova-client` and `ardanova-backend-api-mcp` — **accurate**. Minor drift: root may also contain `package-lock.json` alongside `ardanova-client/package-lock.json`, which can confuse Turbopack’s workspace root (mitigated in [`ardanova-client/next.config.js`](../ardanova-client/next.config.js) via `turbopack.root`).

### Architecture doc “Recent Updates”

[`documentation/ARCHITECTURE.md`](./ARCHITECTURE.md) correctly flags `ardanova-ai-client` as stubbed and notes consolidated UI. **Verify periodically** that “DAO / Studio / Exchange” language in diagrams matches routed features (today: governance UI, not a separate `dao` app folder).

---

## Frontend API surface (authoritative list)

From [`appRouter`](../ardanova-client/src/server/api/root.ts):

`post`, `project`, `guild`, `task`, `event`, `opportunity`, `governance`, `sprint`, `epic`, `backlog`, `chat`, `enum`, `feature`, `product`, `opportunityBid`, `membershipCredential`.

Anything documented as a separate “DAO router” or “marketplace router” should be treated as **planned or legacy naming** until a matching router is added to `root.ts`.

---

## Top five risks (for MVP delivery)

1. **Dual project creation UX** — [`app/projects/create/page.tsx`](../ardanova-client/src/app/projects/create/page.tsx) (multi-step wizard) vs [`app/dashboard/create/page.tsx`](../ardanova-client/src/app/dashboard/create/page.tsx) + [`components/project-form.tsx`](../ardanova-client/src/components/project-form.tsx). Risk: duplicate logic, inconsistent validation, and double maintenance.
2. **REST client vs .NET API** — tRPC routers call `apiClient.*` in [`lib/api/ardanova/`](../ardanova-client/src/lib/api/ardanova/). Endpoint modules must stay aligned with `ArdaNova.API` controllers; drift causes runtime failures that TypeScript may not catch.
3. **Shared / remote database** — `prisma db push` or migrations against a **shared** `DATABASE_URL` can warn about data loss or affect teammates. Prefer a personal or staging DB for schema experiments.
4. **Google OAuth** — Local sign-in requires correct **Authorized JavaScript origins** and **redirect URIs** in Google Cloud Console (`http://localhost:3000`, callback under `/api/auth/...`).
5. **Scope creep from QA checklist** — [`QA-TESTING-CHECKLIST.md`](../QA-TESTING-CHECKLIST.md) is exhaustive. Teams should use [**PHASE_A_TEST_MATRIX.md**](./PHASE_A_TEST_MATRIX.md) for baseline smoke tests before deep sections (SignalR, SSE, etc.).

---

## MVP recommendations (short)

1. **Stabilize one project creation path** — Either deprecate/dashboard-redirect the secondary flow or document it as “legacy” and test only the wizard.
2. **Fix documentation tree** — Align `documentation/ROADMAP.md` with actual `app/` and `routers/` names.
3. **Baseline smoke first** — Follow [LOCAL_DEVELOPMENT_SMOKE.md](./LOCAL_DEVELOPMENT_SMOKE.md) and [PHASE_A_TEST_MATRIX.md](./PHASE_A_TEST_MATRIX.md) before full QA checklist sections.
4. **Backend vs client contract** — Spot-check high-traffic flows (`project`, `guild`, `governance`) against OpenAPI/Swagger or controller routes when debugging.
5. **Defer ledger / AI / marketplace** — Until product priority shifts; not required for “projects + guilds + applications” hardening.

---

## References

- [README.md](../README.md)
- [documentation/ROADMAP.md](./ROADMAP.md)
- [documentation/ARCHITECTURE.md](./ARCHITECTURE.md)
- Optional team planning: `CONTRIBUTOR_USD400_ROADMAP.md` at repo root if maintained locally (may be gitignored).
