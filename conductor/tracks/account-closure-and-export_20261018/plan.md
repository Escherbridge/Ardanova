---
type: plan
---
# Plan: Account closure and full data export

## Phase 0 - Decision [blocking]
- [ ] Product decision: soft delete with anonymization, a grace period, and the list of holdings that block closure.

## Phase 1 - Backend closure [P1]
- [ ] `AccountClosureService.GetBlockersAsync(userId)`: owned projects, owned guilds, non-zero token balances, open escrow and payouts.
- [ ] `AccountClosureService.CloseAsync(userId)`: refuse while blockers remain, anonymize PII, mark the user closed, unlink provider accounts.
- [ ] Replace the hard delete in `UserService.DeleteAsync`, and route `UsersController.Delete` through the service.
- [ ] Flow tests in `ArdaNova.Application.Tests` built on `InMemoryRepository`: one per blocker, plus the full anonymization path.

## Phase 2 - Export [P1]
- [ ] .NET `GET /api/users/{id}/export` (actor == id) returning profile, skills, experience, memberships, credentials, holdings and activity.
- [ ] Point the `/settings/profile` "Your data" card at the export instead of `profile.getMyProfile`.

## Phase 3 - UI [P1]
- [ ] Settings > Account section (added to `settings-nav.tsx`) that shows blockers and closes the account after a typed confirmation.
