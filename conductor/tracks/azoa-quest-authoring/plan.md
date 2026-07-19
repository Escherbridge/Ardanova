---
type: plan
---

# Track — AZOA Quest Authoring

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §4, §5.
> Depends on `azoa-avatar-onboarding` + `azoa-provider-adapter`.
> Author ≠ runner: ArdaNova publishes definitions; avatars self-run.

## 1. Quest definitions (source-controlled)

- [ ] **[P0] Define scrum-lifecycle DAG(s) as DTOs/JSON**
  - `ArdaNova.Application/Azoa/Quests/` — create→fund→work→tasks definition
  - Use node config shapes from §5.2 (GateCheck/Emit/HolonCreate/Grant/Transfer/Refund/FungibleTokenCreate)
  - Author membership-credential `Grant` (soulbound) variant
  - Mark public template(s) with `is_public: true`

## 2. Authoring service (.NET)

- [ ] **[P0] `IAzoaQuestAuthoringService` + implementation**
  - `ArdaNova.Application/Services/Interfaces/IAzoaQuestAuthoringService.cs`
  - `ArdaNova.Application/Services/Implementations/AzoaQuestAuthoringService.cs`
  - `PublishDefinitionsAsync` — idempotent/version-aware publish via the quest-only `AzoaQuestNodeClient`
  - Calls `POST /api/quest` / `POST /api/quest/templates`, then `validate`
  - Register in `ArdaNova.Application/DependencyInjection.cs`

## 3. Event → gate-signal mapping

- [ ] **[P0] `IAzoaQuestSignalService` + implementation**
  - Map ArdaNova domain events → run signals/advances:
    - funding goal met → fund `GateCheck` signal
    - sprint started → start-work gate
    - task accepted → reward branch signal; task rejected → refund branch
  - Economics decided in ArdaNova FIRST (token/equity valuation, amounts), then passed
  - Calls `POST /api/quest/runs/{runId}/signal` / `/advance`

## 4. Board read-through

- [ ] **[P0] Execution-state read**
  - `GET /api/quest/runs/{runId}/execution-state` wrapper
  - Map parking/terminal states; `AwaitingReconciliation` = "pending settlement" (non-error)

## 5. API + frontend (thin proxy)

- [ ] **[P1] Controller** — `ArdaNova.API/Controllers/AzoaQuestController.cs` (publish admin op + run state read; IDOR-safe)
- [ ] **[P1] API client + tRPC thin proxy** — `endpoints/azoa-quest.ts`, `routers/azoaQuest.ts`, wire into `root.ts`
- [ ] **[P1] Board UI run-state rendering** — surface run/node status on the project board

## 6. Tests (.NET)

- [ ] **[P0] `AzoaQuestAuthoringServiceTests`** — publish is idempotent; definitions pass structural validate (mock node)
- [ ] **[P0] `AzoaQuestSignalServiceTests`** — each domain event maps to the correct gate signal/branch; amounts passed, never computed by AZOA
- [ ] **[P0] Tier-2 fail-closed** — no-wallet actor blocks Tier-2 node with clear reason

## 7. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
  - `dotnet build` + `dotnet test`; `npm run build`
  - Manual (sim ok): publish template → avatar instantiates + self-runs → ArdaNova signals fund gate → run advances
