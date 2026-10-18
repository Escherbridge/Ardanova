---
type: handoff
---

# ArdaNova Financial Hardening Passoff

Use this document to resume the bounded ArdaNova financial-engine slice. The
target is an integration-ready, fail-closed stopping point, not authorization
to deploy real-value settlement.

## Current state

- Canonical ArdaNova `main` is clean at `9e1d5c3` in
  `C:\Users\atooz\Documents\Escherbridge\ardanova`.
- The isolated financial worktree is
  `C:\Users\atooz\Documents\Escherbridge\ardanova-financial-domain` on
  `codex/financial-engine-domain`. Resume from its remote branch only after
  confirming the branch head contains this file and `git status --short` is
  empty.
- The financial **implementation series** contains exactly two commits that are
  not on current `main`; the documentation handoff commit is deliberately not
  part of the replay:
  - `0bb1ec3` -- immutable asset, policy, and eligibility domain.
  - `a6c3ecc` -- dual-asset funding plus ordered exchange settlement legs.
- The implementation series is based on `6cf61fe`; at drafting it was two
  implementation commits ahead of, but six mainline commits behind,
  `origin/main`. Commit `9e1d5c3` changed overlapping generated schema, services,
  and integration code.
- No financial migration has been approved or applied. The new persistence
  vocabulary is inert: no writer, controller, dispatcher, payment, token award,
  or swap execution was activated by these commits.
- Verification at `a6c3ecc` was green: Prisma validation, 81 client tests,
  TypeScript typecheck, and 726 .NET tests. Re-run the full current-main gates
  after integration; this evidence is historical, not approval of the combined
  tree.
- The known `AutoMapper` 12.0.1 high-severity advisory remains release debt.
- [The AZOA integration contract](../../ARDANOVA-AZOA-INTEGRATION-CONTRACT.md)
  remains the launch-minimum boundary. This handoff narrows the ArdaNova work;
  it does not supersede any contract acceptance criterion.

## Safe integration procedure

Do not merge the old financial branch directly into `main`. Start from current
`origin/main` and replay only the implementation commits:

```powershell
cd C:\Users\atooz\Documents\Escherbridge\ardanova
git fetch origin
git status --short
git switch -c codex/ardanova-financial-integration-YYYYMMDD origin/main
git cherry-pick 0bb1ec3 a6c3ecc
```

Stop if the status command reports changes. Replace `YYYYMMDD` with the current
date and choose another unused `codex/` branch name if it already exists.

Resolve generator, DBML, Prisma, generated Zod, EF-model, and service conflicts
as one contract. Regenerate from DBML, inspect every generated diff, and retain
the current-main security and deployment changes from `9e1d5c3`. Never resolve a
generated-file conflict without checking its source generator and DBML input.

Do not modify, stage, clean, or reset the dirty AZOA worktree at
`C:\Users\atooz\Programming\Projects\oasis-sleek`. Consume AZOA only through
its documented integration contract during this slice.

## The ArdaNova stopping gate

ArdaNova may pause feature development when all five conditions below are true:

1. The two financial commits are integrated onto current main without losing
   the actor-assertion, wallet, outbox, deployment, or generated-artifact
   hardening already present there.
2. A reviewed additive migration is generated and rehearsed against a
   disposable or non-production PostgreSQL clone. Baseline and additive
   preflights, authoritative scale backfill, readback, reconciliation, and
   rollback evidence are retained. Shared databases never use `prisma db push`.
3. Funding, task-award, and project-token/ARDA/project-token swap authorization
   atomically pin immutable assets, policies, eligibility/gate decisions,
   terms, amounts, and settlement legs. Their end-to-end tests stop at inert
   outbox or simulated reconciliation; no real provider or AZOA dispatch is
   enabled. Acceptance evidence must show a successful authorization creates
   only a pending/inert record, runtime resolution selects a disabled or
   simulated sink, and no payment, AZOA, custody, chain, or exchange network
   call occurs. Without an explicit simulated reconciliation action, tests must
   also prove no terminal `paid`, `awarded`, `exchanged`, or `confirmed` state
   can be reached.
4. The UI exposes server-owned readiness and settlement states and never calls
   a redirect, submitted operation, task completion, or pending settlement
   "paid," "awarded," "exchanged," or "confirmed."
5. Generation is reproducible, the integrated verification sweep is green, an
   independent reviewer finds no correctness or security blocker, and both
   active conductor tracks accurately identify every deferred activation gate.

This is a development stopping point and the last safe pause before operational
hardening. It makes the ArdaNova side testable in inert mode, but it does not by
itself satisfy the production launch minimum. The full integration contract and
its live acceptance evidence remain release gates.

## Work required before that gate

- Re-audit current `main` before editing. Several gated-commerce items advanced
  in `9e1d5c3`; do not repeat them or trust this older branch's checkbox state.
- Document the compatibility projections and deletion criteria for
  `ProjectTokenConfig`, `ProjectEquity`, legacy funding values, and legacy
  settlement values.
- Finish canonical fixed-scale replacement and enforce scales 0--18, positive
  canonical atom strings, non-negative leg positions, append-only asset/policy
  versions, and policy effective-window ordering.
- Add the reviewed additive migration and follow
  [the migration runbook](../../../documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md)
  plus [the asset-scale contract](../../../documentation/ASSET_SCALE_MIGRATION_CONTRACT.md).
- Implement atomic funding, task-award, quote, and order authorization writers.
  Foreign keys alone are insufficient: writers must prove project, asset kind,
  policy, eligibility owner, expiry, actor, and immutable replay consistency.
- Preserve the only supported swap shape: project utility -> ARDA -> project
  utility. Ownership, equity, and redemption rights must not enter this path.
- Complete simulated integration and concurrency tests for duplicate funding,
  task acceptance/award, quote acceptance, outbox claims, and reconciliation.
- Keep provider dispatch, AZOA allocation, payout, exchange execution, and task
  reward disabled unless every existing activation gate is independently met.

## Explicitly deferred extreme hardening

These items do not block the ArdaNova development stopping point, but they do
block production real-value launch where applicable:

- Live AZOA selected-node transport, node attestation, reconciliation evidence,
  and multi-process exactly-once/concurrency proof.
- Production payment-provider enablement, webhook secrets, refunds, payouts,
  chargeback operations, and operator runbooks.
- KMS/HSM-backed custody, key rotation, recovery, wallet export, browser wallet
  signing, verified payout-wallet addition, and audited operator access.
- Production database migration approval, backups, restore drill, observability,
  alerts, load/soak testing, incident response, and Railway rollback exercise.
- Federation/Holochain activation and cross-node economic settlement.
- Dependency remediation, including the `AutoMapper` advisory, plus a final
  repository-wide supply-chain and secret scan.

Do not silently convert any deferred item into an assumption. A future live
launch track must own it with evidence.

## Credentials boundary

No production credential is required to integrate the branch, regenerate
models, run unit tests, or keep all gateways inert.

The migration rehearsal requires separate non-production access:

- A read-only `DATABASE_URL` for baseline and additive preflights.
- A distinct, least-privilege write/deployment identity for applying the
  reviewed migration and rehearsing the backfill on a disposable clone. Never
  use this identity for the preflight.
- Read access to the selected chain's authoritative asset/scale manifest or
  provider; this may require a non-production provider credential.

None of these may target production. Do not place their values in source, track
files, command transcripts, test evidence, or commits.

Live activation will later require separately provisioned and rotated secrets,
at minimum:

- ArdaNova auth/session and actor-assertion signing secrets.
- The selected AZOA endpoint, tenant/service credential, and node-attestation
  verification material.
- Payment-provider API and webhook secrets if Stripe or another gateway is
  enabled.
- Custody/KMS or wallet-signing material and any chain-provider credentials.
- Railway project access plus production database, storage, telemetry, and
  alerting credentials.

Acquire these only when a reviewed activation plan names the owner, environment,
scope, rotation procedure, and rollback. No payment should be made merely to
finish the inert stopping gate; confirm any provider or Railway charge with the
user before purchase or plan changes.

## Final verification sweep

Apply all integration fixes before running the sweep once.

```powershell
cd C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-client
npm ci
$env:DATABASE_URL = '<disposable-or-validation-only-postgresql-url>'
npx prisma validate
npm run generate
npm test
npm run typecheck
npm run lint
npm run build

cd C:\Users\atooz\Documents\Escherbridge\ardanova\ardanova-backend-api-mcp
dotnet build ardanova.sln
dotnet test ardanova.sln --filter 'Category!=E2E'

cd C:\Users\atooz\Documents\Escherbridge\ardanova
git diff --check
git status --short
```

Run database integration/E2E categories only against the disposable migration
target and retain their commands and results in the active track. A clean
working tree after regeneration is required before handoff.

## Copy-paste resumption prompt

```text
Resume the ArdaNova financial-engine hardening from
conductor/tracks/financial-engine-domain-hardening/PASSOFF.md.

First read conductor/workflow.md, the canonical
ARDANOVA-AZOA-INTEGRATION-CONTRACT.md, any nested AGENTS.md for directories you
touch, and both the
financial-engine-domain-hardening and gated-commerce-and-azoa-settlement tracks,
and the two migration runbooks linked from the passoff. Inspect current
origin/main before changing anything.

Create a fresh codex/ integration branch from current origin/main and cherry-pick
0bb1ec3 followed by a6c3ecc. Do not merge the stale branch wholesale. Resolve
DBML/generator/Prisma/Zod/EF conflicts at their source and preserve all security,
deployment, wallet, outbox, and actor-assertion hardening on main. Do not touch
the dirty AZOA worktree.

Drive only to the five-part ArdaNova stopping gate in the passoff: integrated
immutable financial contracts, a disposable/non-production reviewed additive
migration rehearsal, atomic but inert funding/task/swap authorization,
truthful readiness UI, and one final green verification plus independent review.
Keep every live payment, payout, AZOA dispatch, token award, exchange, custody,
and federation path fail-closed. No production credentials or purchases are
authorized.

Maintain the conductor tracks as work advances. Create a new track for any
newly discovered feature rather than hiding it in this slice; archive only a
track whose acceptance criteria are fully evidenced. At the end, report what
met the stopping gate, what remains deferred as production/extreme hardening,
the exact verification evidence, and the credentials still needed for a later
live launch.
```
