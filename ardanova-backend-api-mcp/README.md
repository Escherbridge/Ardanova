# ArdaNova backend (API + MCP server)

This is the second tier of ArdaNova's four-tier architecture. It is a **backend-for-frontend**:
the HTTP API [`ardanova-client`](../ardanova-client/) calls, and **also** a Model Context
Protocol server intended for the Nova AI service
([`ardanova-ai-client`](../ardanova-ai-client/)) and other MCP hosts. It owns business logic,
authorization, persistence (EF Core/PostgreSQL), SignalR, the economic outbox, and is the only
tier that holds Azoa credentials. Next.js never talks to the database or to Azoa directly; it
always goes through this service.

The MCP host's stdio transport has no trusted end-user principal, so its tools
(`ArdaNova.MCP/Tools/`) are **discovery-only**: read/list operations on users, projects, guilds,
and membership credentials. Escrow, exchange, vote-delegation, and reward-claim tools were
removed from this host because they would bypass the HTTP API's actor/funder authorization; they
are not available here under any name. As of this writing the AI service's MCP client is not yet
wired to this host (its tool definitions and MCP calls are stubs), so treat the integration as
planned rather than live — verify against `ardanova-ai-client/src/mcp/client.py` before citing it
as working.

## Project layout (`api-server/src/`)

| Project | Role |
| --- | --- |
| [`ArdaNova.Domain`](api-server/src/ArdaNova.Domain/) | Entities, enums, and value objects — framework-light, no EF runtime dependency beyond abstractions. |
| [`ArdaNova.Application`](api-server/src/ArdaNova.Application/) | Business logic, authorization rules, DTOs, and service orchestration; depends only on Domain and the ports Infrastructure implements. |
| [`ArdaNova.Infrastructure`](api-server/src/ArdaNova.Infrastructure/) | EF Core/PostgreSQL, SignalR, the outbox, and the Azoa/Algorand typed clients; the only project that touches external credentials. |
| [`ArdaNova.API`](api-server/src/ArdaNova.API/) | ASP.NET Core HTTP host: controllers, middleware, health/readiness, the realtime WebSocket hub. |
| [`ArdaNova.MCP`](api-server/src/ArdaNova.MCP/) | Stdio MCP host exposing discovery-only tools (`Tools/`) to Nova AI and other MCP clients. |

`ArdaNova.API` and `ArdaNova.MCP` both depend on `ArdaNova.Application` and
`ArdaNova.Infrastructure`; `ArdaNova.Infrastructure` depends on `ArdaNova.Application` and
`ArdaNova.Domain`. Business rules stay in `Application`; `Infrastructure` is where those rules
reach the outside world.

## Directory-level AGENTS.md index

Each links to the module's own rationale; this README stays an index, not a duplicate.

- [`ArdaNova.API/Controllers/AGENTS.md`](api-server/src/ArdaNova.API/Controllers/AGENTS.md) — controller contracts, assignment boundaries.
- [`ArdaNova.API/Middleware/AGENTS.md`](api-server/src/ArdaNova.API/Middleware/AGENTS.md) — middleware dependency-lifetime rules.
- [`ArdaNova.API/Readiness/AGENTS.md`](api-server/src/ArdaNova.API/Readiness/AGENTS.md) — hosted readiness/schema gate.
- [`ArdaNova.API/WebSocket/AGENTS.md`](api-server/src/ArdaNova.API/WebSocket/AGENTS.md) — realtime (SignalR) trust boundary.
- [`ArdaNova.API/HealthChecks/AGENTS.md`](api-server/src/ArdaNova.API/HealthChecks/AGENTS.md) — economy-mode health projection.
- [`ArdaNova.Application/AGENTS.md`](api-server/src/ArdaNova.Application/AGENTS.md) — application dependency notes (AutoMapper, generated secrets, AZOA run-state).
- [`ArdaNova.Application/Services/Implementations/AGENTS.md`](api-server/src/ArdaNova.Application/Services/Implementations/AGENTS.md) — opportunity role conversion.
- [`ArdaNova.Domain/Models/Entities/AGENTS.md`](api-server/src/ArdaNova.Domain/Models/Entities/AGENTS.md) — entities are DBML-generated; edit the DBML, not these files.
- [`ArdaNova.Infrastructure/Azoa/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Azoa/AGENTS.md) — Azoa transport boundaries, typed clients, on-ramp, settlement gateway.
- [`ArdaNova.Infrastructure/Algorand/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Algorand/AGENTS.md) — Algorand provider boundary (`Simulated` / `Azoa` / `Legacy`).
- [`ArdaNova.Infrastructure/Outbox/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Outbox/AGENTS.md) — economic outbox persistence/leasing.
- [`ArdaNova.Infrastructure/Security/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Security/AGENTS.md) — actor-assertion replay ledger.
- [`ArdaNova.MCP/Tools/AGENTS.md`](api-server/src/ArdaNova.MCP/Tools/AGENTS.md) — MCP tool boundaries (discovery-only; no trusted end-user principal).

## Azoa: the financial-workflow node

Azoa is an external, self-hostable blockchain-operations node (separate repo) that ArdaNova
leverages for identity/KYC, custodial accounts, managed wallets, minting, allocations, reserve
partitions, credential issuance, cash-out, and quests. It is reached from exactly one place in
this codebase: `ArdaNova.Infrastructure/Azoa/`, via scoped typed clients keyed by capability
(custody, value, issuance, quest — separate secrets, enforced distinct in Production).
`ArdaNova.Application` never calls Azoa directly; it depends on the ports `Infrastructure`
implements. The canonical contract is
[`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md);
transport rules are in
[`ArdaNova.Infrastructure/Azoa/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Azoa/AGENTS.md).
Algorand is the chain underneath; value operations go through Azoa, not a direct Algorand call
(see [`ArdaNova.Infrastructure/Algorand/AGENTS.md`](api-server/src/ArdaNova.Infrastructure/Algorand/AGENTS.md)
for the allow-listed provider modes).

## Build, run, test

```bash
cd ardanova-backend-api-mcp
dotnet restore ardanova.sln
dotnet build ardanova.sln
```

Run the HTTP API:

```bash
dotnet run --project api-server/src/ArdaNova.API
```

Run the MCP stdio server (for a local MCP client/host):

```bash
dotnet run --project api-server/src/ArdaNova.MCP
```

Run the test suites (`api-server/tests/ArdaNova.API.Tests`, `api-server/tests/ArdaNova.Application.Tests`):

```bash
dotnet test ardanova.sln
```

`DATABASE_URL` (or `ConnectionStrings__DefaultConnection`) must be set in the environment; neither
host reads a database credential from `appsettings.json`. See the repository-root
[local development smoke runbook](../documentation/LOCAL_DEVELOPMENT_SMOKE.md) for the full
two-service (Next.js + API) local loop, and
[`scripts/verify-nuget-feed.sh`](scripts/verify-nuget-feed.sh) if `dotnet restore` cannot reach
`nuget.org`.

[`Dockerfile`](Dockerfile) builds and publishes `ArdaNova.API` only (the production container
image); it does not currently package the MCP host.
