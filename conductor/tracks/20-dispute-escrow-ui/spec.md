---
type: spec
---

# Dispute Resolution & Escrow UI — Technical Specification

## Overview

Escrow is **HOLD-backed**, not a Postgres ledger of record. Under D14/D47 (`W3.4`/`W7.3` in
`ardanova-production-readiness-waves_20260907`), an escrow is a hold window on the AZOA node
against units already issued from the platform reserve — the same primitive that gates a fresh
stake before it becomes liquid — not a `TaskEscrow` row this codebase settles on its own. No
frontend UI exists for any escrow or dispute functionality yet.

## Current State (verified 2026-09-07 against source, not against the old claim below)

The prior version of this spec claimed "Backend Complete" for escrow creation, release, and
refund. That was wrong at the time it was verified and is exactly the "claim in prose is not a
control in code" pattern this codebase has hit before — `TaskEscrowsController.cs` shows:

| Operation | Actual behavior | Evidence |
|---|---|---|
| Create | **501 Not Implemented** — "disabled until task, funding instrument, amount, and provider settlement are verified server-side" | `TaskEscrowsController.cs:44-52` |
| Release | **503 Service Unavailable** — "unavailable until provider-verified settlement and reconciliation are available" | `TaskEscrowsController.cs:54-61,86-90` |
| Refund | **503 Service Unavailable**, same reason | `TaskEscrowsController.cs:77-90` |
| Get by id / by task / mine | Live, reads the Postgres `TaskEscrow` row | `TaskEscrowsController.cs:24-42` |
| Dispute | Live, writes to the Postgres row | `TaskEscrowsController.cs:63-75` |

So today: an escrow's *existence* and a *dispute* on it are real; the money movements
(fund/release/refund) that would make the dispute matter are fail-closed stubs, not silent
mocks. Anything this spec describes as "complete" beyond reads and disputes is aspirational.

- **Backend**: reads + dispute filing only; funding, release, and refund are fail-closed (see
  table above) pending the HOLD-backed settlement path (`W3.4`/`W7.3`)
- **tRPC Router**: `taskEscrow` router proxies to .NET API
- **Frontend**: Zero escrow UI — no status displays, no release buttons, no dispute forms

## Target shape (HOLD-backed, D14/D47)

- An escrow's funded amount is a **HOLD** on the node against units already issued from the
  platform reserve or a worker's earned stake — not a balance ArdaNova credits and debits itself.
- **Release** moves the held units to the recipient and clears the HOLD on the node; **refund**
  returns them to the reserve partition they came from. Both are node operations this codebase
  triggers and reconciles, not operations it performs on its own ledger.
- A **dispute** pauses release/refund (the HOLD stays open) until an admin resolution decision;
  it does not by itself move anything.
- The UI must show the HOLD/settlement distinction plainly: "funds are held, pending release" is
  a different, disclosed state from "released" — never presented as already-settled while a HOLD
  is open (the same draft/submitted/confirmed/reconciled distinction the frontend keeps
  everywhere else; see `documentation/FRONTEND_EXPERIENCE.md`).

## What Needs to Be Built

### Escrow Status Display
- Show escrow status badge on tasks that have compensation
- States: PENDING, FUNDED, PARTIALLY_RELEASED, DISPUTED, RESOLVED, RELEASED
- Amount locked, amount released, remaining balance

### Milestone-Based Release
- Project owner can release escrow funds on milestone completion
- Partial release support (percentage or fixed amount)
- Release confirmation with amount preview
- Release history timeline

### Dispute Creation
- Either contributor or project owner can raise a dispute
- Dispute form: reason category, description, evidence (attachments)
- Dispute status tracking: OPEN, UNDER_REVIEW, RESOLVED
- Both parties can add comments to dispute

### Admin Arbitration
- Admin dashboard for viewing open disputes
- Dispute detail view with both sides' evidence
- Resolution actions: release to contributor, return to owner, split
- Resolution notes and audit trail
