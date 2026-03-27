# Dispute Resolution & Escrow UI — Technical Specification

## Overview

Backend has complete TaskEscrowServices and TaskEscrowsController with escrow creation, milestone releases, dispute raising, and resolution. No frontend UI exists for any escrow or dispute functionality.

## Current State

- **Backend Complete**: TaskEscrowServices with Create, Release, Dispute, Resolve operations
- **Backend Complete**: TaskEscrowsController with REST endpoints for all operations
- **tRPC Router**: `taskEscrow` router proxies to .NET API
- **Frontend**: Zero escrow UI — no status displays, no release buttons, no dispute forms

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
