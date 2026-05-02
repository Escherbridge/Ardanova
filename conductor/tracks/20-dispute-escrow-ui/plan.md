# Track 20 — Dispute Resolution & Escrow UI

## 1. Escrow Status Display
- [ ] **[P2] Escrow status badge on task cards**
    - Query `api.taskEscrow.getByTask` for tasks with compensation
    - Badge showing: PENDING, FUNDED, PARTIALLY_RELEASED, DISPUTED, RESOLVED, RELEASED
    - Amount locked and released display
- [ ] **[P2] Escrow detail section on task detail page**
    - Full escrow breakdown: total, released, remaining
    - Release history timeline
    - Link to dispute if one exists

## 2. Milestone-Based Release
- [ ] **[P2] Release escrow button (project owner)**
    - Button on task detail page for project owners
    - Amount input (partial or full release)
    - Confirmation dialog with amount preview
    - Calls `api.taskEscrow.release` mutation
- [ ] **[P2] Release history display**
    - Timeline of releases with dates and amounts
    - Who released and for which milestone

## 3. Dispute Creation
- [ ] **[P2] Raise dispute button**
    - Available to both task assignee and project owner
    - Dispute form: reason category dropdown, description textarea, file attachments
    - Calls `api.taskEscrow.dispute` mutation
- [ ] **[P2] Dispute status tracking**
    - Dispute status on task detail page
    - Both parties can view dispute details and add comments
    - Status: OPEN → UNDER_REVIEW → RESOLVED

## 4. Admin Arbitration Dashboard
- [ ] **[P3] Dispute list page (admin)**
    - List all open disputes with filters (status, date, amount)
    - Priority indicators based on amount and age
- [ ] **[P3] Dispute resolution actions**
    - Release to contributor / return to owner / split
    - Resolution notes textarea
    - Calls `api.taskEscrow.resolve` mutation
