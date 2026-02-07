## 1. Project Management & Lifecycle
- [ ] **[P0] Lifecycle**: `DRAFT` -> `PUBLISHED` -> `SEEKING_SUPPORT` -> `FUNDED` -> `IN_PROGRESS` -> `COMPLETED`/`CANCELLED`.
- [ ] **[P0] Setup Wizard**: Agreements (Operating, Membership, Task), Slug uniqueness, Pre-live access.
- [ ] **[P1] Agile Hierarchy**: Project -> Milestone -> Epic -> Sprint -> Feature -> PBI -> Task.
- [ ] **[P1] Task Execution**:
    - [ ] **Compensation**: `FixedShares`, `Hourly`, `Bounty` models with `TaskCompensation` entity.
    - [ ] **Escrow**: `FUNDED` -> `RELEASED` workflow for paid tasks.
- [ ] **[P1] Status Propagation**: Completion roll-up (Child -> Parent), Dependency validation.

## 2. Opportunities Marketplace
- [ ] **[P0] Opportunity CRUD**: Create/Update/Delete with `type` (`GUILD_POSITION`, `PROJECT_ROLE`, `TASK_BOUNTY`, `FREELANCE`).
- [ ] **[P1] Opportunity Interaction**:
    - [ ] **Updates**: CRUD for opportunities updates (e.g., "Deadline extended").
    - [ ] **Comments**: Public Q&A on opportunities.
    - [ ] **Applications**: `PENDING` -> `ACCEPTED`/`REJECTED` workflow.
- [ ] **[P1] Bids**: `OpportunityBid` for bounties/contracts with `proposedAmount` and `timeline`.
- [ ] **[P1] Constraints**: `maxApplications`, `deadline`, `isRemote`/`location`.

## 3. DAO Governance (Dual-Asset Model)
- [ ] **[P0] Membership Credentials (Rights)**: 
    - [ ] 1 Member = 1 Vote (Governance).
    - [ ] Vote weight based on Credential status (`ACTIVE`), NOT Share balance.
- [ ] **[P0] Proposal System**: 
    - [ ] Types: `TREASURY`, `GOVERNANCE`, `STRATEGIC`, `OPERATIONAL`, `CONSTITUTIONAL`.
    - [ ] Lifecycle: `DRAFT` -> `ACTIVE` -> `PASSED`/`REJECTED` -> `EXECUTED`.
- [ ] **[P1] Voting Logic**:
    - [ ] Quorum (% of members), Threshold (% approval).
    - [ ] Delegation: Delegate voting power (Credential-based or Share-based depending on config, defined as Credential-based for Governance).
    - [ ] **Correction**: Dual-Asset rules state Credentials = Governance, Shares = Economics. Ensure voting respects this separation.

## 4. Project Extras (API Alignment)
- [ ] **[P1] Project Resources**: 
    - [ ] CRUD for `ProjectResource` (Links, Docs, Assets).
    - [ ] `obtained` toggle for tracking resource acquisition.
- [ ] **[P1] Project Updates**:
    - [ ] CRUD for `ProjectUpdate` (Status reports, News).
    - [ ] Notifications triggered on update creation.
- [ ] **[P1] Project Support**:
    - [ ] "Backing" mechanism (`ProjectSupport` entity).
    - [ ] Toggle support status (Active/Inactive).
