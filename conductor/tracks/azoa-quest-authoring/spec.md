# AZOA Quest Authoring — Technical Specification

## Overview

Publish the **scrum-lifecycle quest definitions/templates** (project →
fund → work → tasks) to the shared AZOA node, and wire ArdaNova's board/lifecycle
events to the **gate signals** that advance an avatar's run. ArdaNova is the
**author** of the definitions; **avatars self-run** their own runs against them
(the publish-vs-run distinction, §4).

> Source contract: [`conductor/ARDANOVA-AZOA-INTEGRATION-CONTRACT.md`](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md) §4, §5.

## Key principle: author ≠ runner

- **ArdaNova authors** a reusable quest **definition** (a DAG) or a public
  **template** any avatar may instantiate. Authoring does NOT take custody of any
  avatar.
- **An avatar self-runs** its own run: `start-workflow` sets `Run.avatar_id` =
  the calling avatar; `advance`/`signal` are caller-scoped. The avatar drives its
  own run under its own keys.
- ArdaNova (the consumer) only **signals gates** ("goal met" / "task approved")
  — it does not act *as* the avatar (no consent/acting-as path, §11.2).

## Dependencies

- `azoa-avatar-onboarding` — avatars exist; wallet-bound for Tier-2 nodes.
- `azoa-provider-adapter` — for any value-bearing node execution paths.
- Track 02 (Projects) — Project/Sprint/Epic/PBI/Task lifecycle + records of truth
  (these STAY in ArdaNova; AZOA never computes economics, §1).
- Track 09 (Tokenomics) — token/equity *valuation* stays consumer-side; AZOA
  receives already-decided amounts (§1, §5.1).

## The lifecycle as a Quest DAG (§5.1)

ArdaNova authors a definition mapping each phase to a node; navigation is a gated
edge. Domain → node-type (ArdaNova owns the state machine + records of truth):

| Concept | State machine | Quest DAG | Runner |
|---|---|---|---|
| Create project | `DRAFT→PUBLISHED` | `HolonCreate`(Project) → `Emit`(`project.created`) | creator avatar |
| Seek support / fund | `SEEKING_SUPPORT→FUNDED` | `GateCheck`(funding goal met, injected via `reads`) → `FungibleTokenCreate`/`Grant` | supporter avatars; **ArdaNova signals the gate** |
| Start work | `FUNDED→IN_PROGRESS` | `GateCheck`(`status=="FUNDED"`) → `Emit`(`sprint.started`) | creator/lead avatar |
| Task / PBI / Bounty | `TODO→…→COMPLETED`; escrow `NONE→FUNDED→RELEASED/REFUNDED` | `GateCheck`(submission accepted) → `Transfer`/`Grant`(reward) → `Emit`(`task.completed`); reject → `Refund`/`Emit` | contributor avatar |
| Membership credential | soulbound ASA | `Grant`(soulbound) → credential Holon | member avatar |

**Tier note:** `GateCheck`/`Emit`/`HolonCreate` are Tier-0/1 (no chain).
`Grant`/`Transfer`/`Refund`/`FungibleTokenCreate` are **Tier-2** — require the
run's actor avatar to have a wallet bound (node-side `ChainCapabilityGate`,
fail-closed). Because runs are self-scoped, keys/wallet are the avatar's own;
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
| `POST /api/quest` / `POST /api/quest/templates`(`is_public`) | Author definition / publish template | **author** (ArdaNova) |
| `POST /api/quest/templates/{id}/instantiate` | Avatar materializes a public template | (avatar) |
| `POST /api/quest/{id}/validate` | Structural DAG validation (Kahn) | author |
| `POST /api/quest/{id}/start-workflow` | Start run (`Run.avatar_id`=caller) | (avatar self-runs) |
| `POST /api/quest/runs/{runId}/advance` | Resume Suspended run | (avatar) |
| `POST /api/quest/runs/{runId}/signal` | Un-park `AwaitingSignal` gate | **ArdaNova pushes "goal met"/"approved"** |
| `GET /api/quest/runs/{runId}/execution-state` | Poll node states for the board | reader (ArdaNova UI) |

Parking states ArdaNova must handle in the board UI: `AwaitingSignal`,
`AwaitingTimer`, `AwaitingReconciliation` (pending settlement — non-error, §7),
`Suspended`; terminals `Succeeded`/`Failed`/`Cancelled`/`Forked`.

## ArdaNova responsibilities

1. **Authoring service (.NET)** — `IAzoaQuestAuthoringService` that publishes the
   scrum-lifecycle definition(s)/template(s) to the node (idempotent /
   version-aware so re-publish doesn't duplicate). Keep definitions as
   source-controlled JSON/DTOs in `ArdaNova.Application`.
2. **Event → signal mapping** — when an ArdaNova domain event fires (funding goal
   met, sprint started, task accepted/rejected), translate it into a
   `signal`/`advance` against the relevant run. Economics (how much, which asset)
   are decided in ArdaNova FIRST, then passed; AZOA never computes them.
3. **Board read-through** — `GET execution-state` to render run/node status,
   treating `AwaitingReconciliation` as a non-error "pending settlement" state.

## Out of scope

- Acting-as/consent path (locked out, §11.2) — ArdaNova never starts a run *as*
  an avatar; avatars self-run.
- The double-mint reconcile wiring itself (AZOA-side `quest-reconcile-retry-wiring`,
  P7); ArdaNova only honors the obligations (stable idempotency keys, treat
  `AwaitingReconciliation` as non-terminal).
- Direct allocation outside a run (`treasury-reward-to-azoa-allocation`).

## Acceptance criteria

- The scrum-lifecycle definition(s)/template(s) publish to the node and pass
  `validate` (Kahn structural check); re-publish is idempotent.
- An avatar can instantiate + self-run a published template; `Run.avatar_id` is
  the avatar, not ArdaNova.
- ArdaNova domain events correctly signal the matching gate (goal-met →
  fund gate; task-accepted → reward branch; task-rejected → refund branch).
- Tier-2 nodes refuse to execute when the actor avatar has no wallet bound
  (fail-closed), with a clear surfaced reason.
- The board UI renders all parking/terminal states, treating
  `AwaitingReconciliation` as pending-settlement (non-error).
