---
type: spec
---

# AZOA Action Authoring — Technical Specification

> **Renamed 2026-09-07.** AZOA's nouns are now **user** (was avatar) and
> **action** (was quest); ArdaNova cut over in `f2e4bc5`. Routes, wire fields and
> SDK exports below use the new names. ArdaNova's own C# identifiers
> (`IAzoaQuestAuthoringService`, `AzoaQuestNodeAdapter`, `Azoa:QuestApiKey`,
> `User.azoaAvatarId`) were deliberately **not** renamed — see
> [`../azoa-user-onboarding/spec.md`](../azoa-user-onboarding/spec.md)
> §"Terminology after the `f2e4bc5` cutover".

## Overview

Publish the **scrum-lifecycle action definitions/templates** (project →
fund → work → tasks) to the shared AZOA node, and wire ArdaNova's board/lifecycle
events to the **gate signals** that advance a user's run. ArdaNova is the
**author** of the definitions; **users self-run** their own runs against them
(the publish-vs-run distinction, §4).

> Source contract: [`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §4, §5.

## Key principle: author ≠ runner

- **ArdaNova authors** a reusable action **definition** (a DAG) or a public
  **template** any user may instantiate. Authoring does NOT take custody of any
  user.
- **A user self-runs** its own run: `start-workflow` sets `Run.user_id` =
  the calling user; `advance`/`signal` are caller-scoped. The user drives its
  own run under its own keys.
- ArdaNova (the consumer) only **signals gates** ("goal met" / "task approved")
  — it does not act *as* the user (no consent/acting-as path, §11.2).

## Dependencies

- `azoa-user-onboarding` — AZOA users exist; wallet-bound for Tier-2 nodes.
- `azoa-provider-adapter` — for any value-bearing node execution paths.
- Track 02 (Projects) — Project/Sprint/Epic/PBI/Task lifecycle + records of truth
  (these STAY in ArdaNova; AZOA never computes economics, §1).
- Track 09 (Tokenomics) — token/equity *valuation* stays consumer-side; AZOA
  receives already-decided amounts (§1, §5.1).

## The lifecycle as an Action DAG (§5.1)

ArdaNova authors a definition mapping each phase to a node; navigation is a gated
edge. Domain → node-type (ArdaNova owns the state machine + records of truth):

| Concept | State machine | Action DAG | Runner |
|---|---|---|---|
| Create project | `DRAFT→PUBLISHED` | `HolonCreate`(Project) → `Emit`(`project.created`) | creator user |
| Seek support / fund | `SEEKING_SUPPORT→FUNDED` | `GateCheck`(funding goal met, injected via `reads`) → `FungibleTokenCreate`/`Grant` | supporter users; **ArdaNova signals the gate** |
| Start work | `FUNDED→IN_PROGRESS` | `GateCheck`(`status=="FUNDED"`) → `Emit`(`sprint.started`) | creator/lead user |
| Task / PBI / Bounty | `TODO→…→COMPLETED`; escrow `NONE→FUNDED→RELEASED/REFUNDED` | `GateCheck`(submission accepted) → `Transfer`/`Grant`(reward) → `Emit`(`task.completed`); reject → `Refund`/`Emit` | contributor user |
| Membership credential | soulbound ASA | `Grant`(soulbound) → credential Holon | member user |

**Tier note:** `GateCheck`/`Emit`/`HolonCreate` are Tier-0/1 (no chain).
`Grant`/`Transfer`/`Refund`/`FungibleTokenCreate` are **Tier-2** — require the
run's actor user to have a wallet bound (node-side `ChainCapabilityGate`,
fail-closed). Because runs are self-scoped, keys/wallet are the user's own;
ArdaNova never needs custody.

## Node config shapes (authoritative, §5.2)

Author DAGs using the node's config grammar:
- `GateCheckNodeConfig` → `{ "predicate": "<bool expr>", "reads": { "<name>": <json> } }`
- `EmitNodeConfig` → `{ "payload": <opaque consumer json> }`
- `GrantNodeConfig` → `{ "request": <NftMintRequest>, "holonId": "<guid?>" }` (actor from run context, NOT body)
- `TransferNodeConfig` / `RefundNodeConfig` → `{ "nftId": "<guid>", "request": <NftTransferRequest> }`
- `FungibleTokenCreateNodeConfig` → `{ "chainType":"Algorand", "name":"", "unitName":"", "total":<ulong>, "decimals":<int>, "holonId":"<guid?>" }`
  (total/decimals consumer-authoritative; AZOA derives no economic meaning)

## Run orchestration surface used (§5.3)

| Call | Purpose | ArdaNova role |
|---|---|---|
| `POST /api/action` / `POST /api/action/templates`(`is_public`) | Author definition / publish template | **author** (ArdaNova) |
| `POST /api/action/templates/{id}/instantiate` | A user materializes a public template | (user) |
| `POST /api/action/{id}/validate` | Structural DAG validation (Kahn) | author |
| `POST /api/action/{id}/start-workflow` | Start run (`Run.user_id`=caller) | (user self-runs) |
| `POST /api/action/runs/{runId}/advance` | Resume Suspended run | (user) |
| `POST /api/action/runs/{runId}/signal` | Un-park `AwaitingSignal` gate | **ArdaNova pushes "goal met"/"approved"** |
| `GET /api/action/runs/{runId}/execution-state` | Poll node states for the board | reader (ArdaNova UI) |

Run statuses ArdaNova must handle in the board UI match AZOA SDK `0.3.0` (unpublished — see `conductor/RUNBOOK.md`):
`Pending`, `Running`, `Suspended`, `AwaitingSignal`, `AwaitingTimer`,
`Succeeded`, `Failed`, `Forked`, and `Cancelled`.

## ArdaNova responsibilities

1. **Authoring service (.NET)** — `IAzoaQuestAuthoringService` (type name predates
   the rename; it targets `/api/action*`) that publishes the
   scrum-lifecycle definition(s)/template(s) to the node (idempotent /
   version-aware so re-publish doesn't duplicate). Keep definitions as
   source-controlled JSON/DTOs in `ArdaNova.Application`.
2. **Event → signal mapping** — when an ArdaNova domain event fires (funding goal
   met, sprint started, task accepted/rejected), translate it into a
   `signal`/`advance` against the relevant run. Economics (how much, which asset)
   are decided in ArdaNova FIRST, then passed; AZOA never computes them.
3. **Board read-through** — `GET execution-state` to render the nine canonical
   run statuses without synthesizing application-only workflow states.

## Out of scope

- Acting-as/consent path (locked out, §11.2) — ArdaNova never starts a run *as*
  a user; users self-run.
- The double-mint reconcile wiring itself (AZOA-side `action-reconcile-retry-wiring`,
  P7); ArdaNova tracks indeterminate value moves in its economic settlement
  state machine, not as a workflow-run status.
- Direct allocation outside a run (`treasury-reward-to-azoa-allocation`).

## Acceptance criteria

- The scrum-lifecycle definition(s)/template(s) publish to the node and pass
  `validate` (Kahn structural check); re-publish is idempotent.
- A user can instantiate + self-run a published template; `Run.user_id` is
  that user, not ArdaNova.
- ArdaNova domain events correctly signal the matching gate (goal-met →
  fund gate; task-accepted → reward branch; task-rejected → refund branch).
- Tier-2 nodes refuse to execute when the actor user has no wallet bound
  (fail-closed), with a clear surfaced reason.
- The board UI renders all nine canonical statuses and polls only the five
  non-terminal statuses.
