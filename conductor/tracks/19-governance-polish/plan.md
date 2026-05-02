# Track 19 — Governance & Voting Polish

## 1. Credential-Gated Voting
- [ ] **[P1] Check credential before vote**
    - Query `api.membershipCredential.getByProjectAndUser` before showing vote buttons
    - Show "Credential required" message for non-members
    - Link to credential earning path
- [ ] **[P1] Vote eligibility badge on proposal page**
    - "Eligible to vote" / "Not eligible" indicator

## 2. Delegated Voting
- [ ] **[P2] Delegate vote UI**
    - Button to delegate your vote to another member
    - Select member from credentialed members list
    - Calls `api.delegatedVote.delegate` mutation
- [ ] **[P2] Delegation status display**
    - Show who you've delegated to
    - Show who has delegated to you
    - Combined vote weight display

## 3. Proposal Execution Feedback
- [ ] **[P2] Execution result display**
    - After proposal executes, show outcome on proposal detail page
    - Timeline: Created → Voting → Passed → Executed (with result)
- [ ] **[P2] Execution history**
    - List of executed proposals with outcomes per project

## 4. Governance Settings
- [ ] **[P2] Project governance configuration (owner)**
    - Quorum threshold percentage
    - Voting duration (days)
    - Allowed proposal types
    - Stored in project settings
