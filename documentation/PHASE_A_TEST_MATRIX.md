# Phase A — baseline test matrix

**Purpose:** A **narrow**, fill-in manual matrix for Phase A smoke / baseline QA.  
**Not a replacement for** the full [QA-TESTING-CHECKLIST.md](../QA-TESTING-CHECKLIST.md) (SSE, SignalR deep dives, edge cases, etc.).

**How to use:** Copy the table to a spreadsheet if you prefer filters; check **Pass/Fail**, note **Browser**, **Severity**, and **Notes**.

**Environment:** Follow [LOCAL_DEVELOPMENT_SMOKE.md](./LOCAL_DEVELOPMENT_SMOKE.md) first.

**Guild “set up” (manager Phase C):** See [GUILD_PHASE_C_MANAGER_BRIEF.md](./GUILD_PHASE_C_MANAGER_BRIEF.md).

---

## Deeper regression

After Phase A passes, use the comprehensive checklist:

- **[QA-TESTING-CHECKLIST.md](../QA-TESTING-CHECKLIST.md)** — Sections **1** (Authentication), **2** (Project Management), **4** (Guild Module), **6** (DAO Governance), **13–14** (Realtime / cross-cutting) as priorities for your sprint.

---

## Matrix (baseline)

| ID | Scenario | Priority | Steps (brief) | Pass/Fail | Browser | Severity | Notes |
|----|----------|----------|---------------|-----------|---------|----------|-------|
| A1 | Home loads | P0 | Open `/` | | | | |
| A2 | API health | P0 | `GET` `/health` on API base URL | | | | |
| A3 | Next API health | P1 | Open or `GET` `/api/health` | | | | |
| A4 | Sign-in page | P0 | Open `/auth/signin` | | | | |
| A5 | Google OAuth | P1 | Sign in with Google (if configured) | | | | |
| A6 | Session / protected route | P1 | After login, open `/dashboard` | | | | |
| A7 | Enum-driven UI | P1 | Open `/projects/create`, confirm category/type enums load (no console errors) | | | | |
| A8 | Projects list | P0 | Open `/projects` | | | | |
| A9 | Project create (wizard) | P0 | Open `/projects/create`, advance at least one step | | | | |
| A10 | Project create (alt path) | P2 | Open `/dashboard/create` — note UX vs wizard | | | | Duplication risk |
| A11 | Project detail | P1 | Open a project by slug `/projects/[slug]` | | | | |
| A12 | Guilds list | P0 | Open `/guilds` | | | | |
| A13 | Guild create | P1 | Open `/guilds/create` (authenticated) | | | | |
| A14 | Guild detail | P1 | Open `/guilds/[slug]` | | | | |
| A15 | Guild application | P2 | Submit or view application flow if UI exposed | | | | Mark **blocked** if not testable |
| A16 | Governance list | P2 | Open `/governance` | | | | |
| A17 | Opportunities list | P2 | Open `/opportunities` | | | | |
| A18 | Tasks list | P2 | Open `/tasks` | | | | |
| A19 | Events list | P2 | Open `/events` | | | | |
| A20 | Chats | P2 | Open `/chats` (auth) | | | | |

**Priority key:** P0 = blocking for baseline; P1 = high; P2 = should run before release; adjust with your team.

---

## Definition of done (Phase A testing)

- [ ] All **P0** rows executed or explicitly waived with reason  
- [ ] Failures logged with severity and repro steps  
- [ ] Link or paste summary to Discord / ticket tracker as required by your team
