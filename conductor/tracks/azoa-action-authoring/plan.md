---
type: plan
---

# Track — AZOA Action Authoring

> Contract: [`ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §4, §5.
> Depends on `azoa-user-onboarding` + `azoa-provider-adapter`.
> Author ≠ runner: ArdaNova publishes definitions; users self-run.
>
> **Reconciled 2026-09-07** against `f2e4bc5`: AZOA routes are `/api/action*`;
> ArdaNova C# type names still say `Quest` by design. Boxes reflect disk state.
> Terminology rule: [`spec.md`](spec.md).

## 1. Action definitions (source-controlled)

- [x] **[P0] Define scrum-lifecycle DAG(s) as DTOs/JSON**
  - `ArdaNova.Application/Azoa/Quests/` (directory name predates the rename) —
    `ScrumLifecycleQuests.cs` + `QuestDefinitionDtos.cs`, create→fund→work→tasks
  - Use node config shapes from §5.2 (GateCheck/Emit/HolonCreate/Grant/Transfer/Refund/FungibleTokenCreate)
  - Author membership-credential `Grant` (soulbound) variant
  - Mark public template(s) with `is_public: true`

## 2. Authoring service (.NET)

- [x] **[P0] `IAzoaQuestAuthoringService` + implementation**
  - `ArdaNova.Application/Services/Interfaces/IAzoaQuestAuthoringService.cs`
  - `ArdaNova.Application/Services/Implementations/AzoaQuestAuthoringService.cs`
  - `PublishDefinitionsAsync` — idempotent/version-aware publish via the action-only `AzoaQuestNodeClient`
  - Calls `POST /api/action` / `POST /api/action/templates`, then `validate`
  - Register in `ArdaNova.Application/DependencyInjection.cs`

## 3. Event → gate-signal mapping

- [x] **[P0] `IAzoaQuestSignalService` + implementation**
  - Map ArdaNova domain events → run signals/advances:
    - funding goal met → fund `GateCheck` signal
    - sprint started → start-work gate
    - task accepted → reward branch signal; task rejected → refund branch
  - Economics decided in ArdaNova FIRST (token/equity valuation, amounts), then passed
  - Calls `POST /api/action/runs/{runId}/signal` / `/advance`

## 4. Board read-through

- [ ] **[P0] Execution-state read**
  - `GET /api/action/runs/{runId}/execution-state` wrapper
  - Map all nine SDK `0.3.0` statuses one-for-one; preserve unknown raw values

## 5. API + frontend (thin proxy)

- [ ] **[P1] Controller** — `ArdaNova.API/Controllers/AzoaQuestController.cs` (publish admin op + run state read; IDOR-safe). **Not on disk.**
- [ ] **[P1] API client + tRPC thin proxy** — **not on disk**; the only AZOA frontend surface today is `endpoints/azoa-custodial-account.ts` + `routers/azoa-custodial-account.ts`
- [ ] **[P1] Board UI run-state rendering** — surface run/node status on the project board

## 6. Tests (.NET)

- [x] **[P0] `AzoaQuestAuthoringServiceTests`** — publish is idempotent; definitions pass structural validate (mock node)
- [x] **[P0] `AzoaQuestSignalServiceTests`** — each domain event maps to the correct gate signal/branch; amounts passed, never computed by AZOA
- [ ] **[P0] Tier-2 fail-closed** — no-wallet actor blocks Tier-2 node with clear reason

## 7. Verification

- [ ] **[P0] Build + test sweep (run ONCE at end)**
  - `dotnet build` + `dotnet test`; `npm run build`
  - Manual (sim ok): publish template → a user instantiates + self-runs → ArdaNova signals fund gate → run advances
  - ⚠ `npm run build` is not reproducible from a clean checkout: `azoa-sdk@^0.3.0`
    is unpublished. See `conductor/RUNBOOK.md` → "Open item 1".
