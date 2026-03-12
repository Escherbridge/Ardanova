# Governance & Voting Polish — Technical Specification

## Overview

Backend has full GovernanceServices with proposals, voting, delegated votes, quorum calculation, and proposal execution. Frontend has listing and detail pages that work with real data. This track polishes the governance experience with credential-gated voting, delegation UI, and execution feedback.

## Current State

- **Backend Complete**: GovernanceController with proposals CRUD, voting (cast/get/summary), lifecycle (execute/cancel/publish), DelegatedVotesController
- **Frontend**: Governance listing page with real data, proposal detail page, proposal creation form, voting results display
- **Missing**: Credential-gated voting checks, delegated voting UI, execution result display, governance settings

## What Needs to Be Built

### Credential-Gated Voting
- Before casting a vote, check if user holds active MembershipCredential for the project/guild
- Show "You need a credential to vote" message with link to earn one
- Use `api.membershipCredential.getByProjectAndUser` to check

### Delegated Voting
- UI to delegate your vote to another credentialed member
- Show who has delegated to you
- Vote weight display (own + delegated)

### Proposal Execution Feedback
- After a proposal passes and is executed, show what happened
- Execution results (e.g., "New member added", "Parameter changed")

### Governance Settings (Project Owner)
- Configure quorum threshold, voting duration, proposal types
- Per-project governance parameters
