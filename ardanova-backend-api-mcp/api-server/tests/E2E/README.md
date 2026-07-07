# ArdaNova ⇄ AZOA cross-process golden-path E2E

A **live, cross-process** end-to-end test that drives the real ArdaNova transport
(`AzoaNodeClient`) against a **real AZOA node** (oasis-sleek, `AZOA.WebAPI`) over
HTTP. It proves the two-system contract end to end instead of mocking the node.

- Test: [`../ArdaNova.Application.Tests/E2E/AzoaGoldenPathE2ETests.cs`](../ArdaNova.Application.Tests/E2E/AzoaGoldenPathE2ETests.cs)
- Runner: [`../run-azoa-e2e.ps1`](../run-azoa-e2e.ps1)

## What it proves

1. **Avatar self-register** (contract §4 / §11.2) — an anonymous end user becomes a
   self-sovereign AZOA avatar via `POST /api/avatar/register`, and the node returns
   a concrete `avatarId`.
2. **Exactly-once allocation** (contract §6 / §7) — the *must-have* assertion. The
   same avatar + the **same `Idempotency-Key`** delivered twice:
   - first delivery → `replayed == false`, a fresh `operationId`;
   - second delivery → `replayed == true`, the **same `operationId`**, i.e. the
     asset moved **exactly once** even though the trigger was redelivered.

The node runs in **Simulated** mode (deterministic `sim:` ids, no real chain I/O),
which is already the default in oasis-sleek's `appsettings.Development.json`
(`Blockchain:Mode = Simulated`). Simulated mode auto-approves KYC, so the
fail-closed KYC 403 path (§6 / §11.4) is **not** observable from this harness — it
is covered by AZOA's own test suite. This e2e's unique value is the exactly-once
proof across a process boundary.

## The gate (why it doesn't break `dotnet test`)

The test is tagged `[Trait("Category","E2E")]` and is **gated** behind three
environment conditions. It skips cleanly (passes trivially, logs
`[E2E SKIPPED: <reason>]` via `ITestOutputHelper`) unless **all** hold:

| Condition | Meaning |
| --- | --- |
| `AZOA_E2E == "1"` | explicit developer opt-in |
| `http://localhost:5000` reachable | the AZOA node process is up |
| `AZOA_E2E_API_KEY` set | an `nft:mint`-scoped `X-Api-Key` |

Plain xUnit has no runtime-skip primitive and we deliberately add **no** NuGet
dependency (e.g. `Xunit.SkippableFact`) to get one. The gate is a dependency-free
early-return: when gated off the test **passes green** — it never fails or errors —
so a normal `dotnet test` run is unaffected. Filter it explicitly:

```bash
dotnet test <sln> --filter "Category=E2E"    # run ONLY the e2e
dotnet test <sln> --filter "Category!=E2E"   # exclude the e2e
```

## Prerequisites

1. **.NET SDK** on `PATH`.
2. **SurrealDB** running at `http://127.0.0.1:8000`. AZOA persists to SurrealDB
   (see oasis-sleek `appsettings.Development.json`: ns/db `azoa`/`azoa`, user/pass
   `root`/`root`). Register/login/allocation will fail without it. This harness
   does **not** install or start SurrealDB. Start it however you prefer, e.g.:

   ```bash
   surreal start --user root --pass root --bind 127.0.0.1:8000 memory
   # (or a durable backend: `rocksdb://<path>` instead of `memory`)
   ```

3. **oasis-sleek** checkout (default path
   `C:\Users\atooz\Programming\Projects\oasis-sleek`, project `AZOA.WebAPI.csproj`).
   Its `http` launch profile serves `http://localhost:5000`.

## How to get a dev `nft:mint` API key

There is **no anonymous/dev auto-seed** for API keys in AZOA. Keys are minted
through the normal authenticated flow (`ApiKeyController.Create` requires an
authenticated avatar; `AllocationController` requires the `nft:mint` **or**
`wallet:manage` scope). The runner automates this end to end using only AZOA's
public HTTP API:

1. `POST /api/avatar/register` (anonymous) — create a throwaway dev avatar.
2. `POST /api/avatar/login` (anonymous) — the JWT comes back in the `result`
   field of the `{ isError, message, result }` envelope.
3. `POST /api/apikey` with `Authorization: Bearer <jwt>` and body
   `{ "name": "...", "scopes": "nft:mint" }` — the **raw key is returned once** in
   `result.key` (prefix `azoa_...`). AZOA stores only its SHA-256 hash
   (`ApiKeyAuthenticationHandler.HashKey`).

You can do this by hand and export the raw key, or let the runner do it for you.

## Running it

### Option A — let the runner do everything

```powershell
# from api-server/tests
pwsh ./run-azoa-e2e.ps1
```

The runner will:
- start AZOA (`dotnet run --project <oasis-sleek>` with `ASPNETCORE_ENVIRONMENT=Development`)
  only if it is not already answering on `localhost:5000`, and wait for it;
- mint a fresh `nft:mint` dev key via the HTTP flow above (unless `-ApiKey` or an
  existing `AZOA_E2E_API_KEY` is provided);
- set `AZOA_E2E=1` + `AZOA_E2E_API_KEY` for the child test process;
- run `dotnet test <sln> --filter Category=E2E`;
- stop **only** the AZOA process it started (leaves an already-running node alone).

Useful switches:

```powershell
pwsh ./run-azoa-e2e.ps1 -NoStartAzoa                       # reuse a running node
pwsh ./run-azoa-e2e.ps1 -ApiKey "azoa_deadbeef..."         # reuse an existing key
pwsh ./run-azoa-e2e.ps1 -AzoaProjectPath "D:\src\oasis-sleek\AZOA.WebAPI.csproj"
```

### Option B — manual

```powershell
# 1. (in a separate shell) start SurrealDB, then AZOA:
dotnet run --project C:\Users\atooz\Programming\Projects\oasis-sleek\AZOA.WebAPI.csproj --launch-profile http

# 2. mint / export an nft:mint key (see above), then:
$env:AZOA_E2E = "1"
$env:AZOA_E2E_API_KEY = "azoa_...yourkey..."

# 3. run only the e2e:
dotnet test C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-backend-api-mcp\ardanova.sln --filter "Category=E2E"
```

## Interpreting results

- **Gated off** (no env vars): the E2E test reports **passed** and its output
  contains `[E2E SKIPPED: <reason>]`. This is the state during normal CI/local
  `dotnet test`.
- **Gated on** but the node/SurrealDB is down: the reachability probe fails and the
  test skips with a reason naming the missing dependency — it still does not fail.
- **Live run**: the output logs the registered `avatarId`, the first `operationId`,
  and the replay confirmation (`operationId == first`, `replayed=true`). A failed
  assertion here is a **real contract regression**.

> Note: there is a known, unrelated pre-existing failing unit test
> (`GuildServiceTests.DeleteAsync_WhenGuildExists_ReturnsSuccess`) in the baseline.
> It is not part of this harness.
