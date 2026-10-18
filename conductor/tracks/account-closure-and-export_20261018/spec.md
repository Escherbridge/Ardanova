---
type: spec
---
# Account closure and full data export

## Context

Loop 3 auth QA found that no settings page lets a member leave or take their data. Loop 3 added
a "Download my profile data" control on `/settings/profile`. It covers only the profile, skills
and experience from `profile.getMyProfile`. It does not offer closure.

The backend half is the risk. `UsersController.Delete` (actor == id) calls
`UserService.DeleteAsync`, which runs `_repository.DeleteAsync(user)`. That is a hard delete with
no check for owned projects, guild ownership, open payouts, escrow or token balances, and no
retention step. A "Delete account" button wired to it as it stands would cascade into or orphan
records that carry funds.

## Goals

1. Make a product decision on closure: soft delete with anonymization, a grace period, and which
   holdings block closure (projects owned, guilds owned, non-zero balances, open escrow or payouts).
2. Build a .NET `AccountClosureService` that enforces those blockers and anonymizes PII
   (name, email, phone, bio, links, image) while keeping the ledger and governance history intact.
   Tests go in `ArdaNova.Application.Tests`.
3. Retire or guard the raw `DELETE /api/users/{id}` so the only closure path runs through the service.
4. Add Settings > Account with a confirmation step that lists blockers and explains what is kept.
5. Extend the export to everything the member owns: memberships, credentials, token holdings,
   activity, and consents once `auth-consent-and-account-linking` lands. Serve it from a
   .NET export endpoint, not from data the client puts together.

## Non-goals

- KYC record deletion at AZOA. It follows the provider's retention policy and gets a separate track.

## Acceptance

- A member with no blockers can close their account. Afterwards their public profile is anonymized
  and sign-in creates a new account rather than reviving the old one.
- A member who owns a project or guild, or holds a balance, sees exactly which blocker applies and
  how to clear it.
- The export file contains every record family listed in goal 5.
