---
type: spec
---
# Spec: Profile entry editing, people directory reach, and auth QA hygiene

Source: loop-4 auth QA and its adversarial critique. Loop 4 fixed public email lookup in
`user.search`, unsaved-edit loss on profile refetch, and the experience-month timezone shift. The
items below were too large for the final loop or sit outside the auth lane.

## 1. Edit an existing skill or experience entry (auth lane, medium)
`/settings/profile` can only add and remove entries. `api.profile.updateSkill` and
`api.profile.updateExperience` already exist and enforce the .NET `UserProfileRules`, but no UI calls
them. A member who mistypes a skill must delete and recreate it, because a duplicate name is refused.

Acceptance:
- Each skill row has an edit control that changes the level in place. Renaming is delete-and-add, since
  `UpdateUserSkillDto` carries only the level.
- Each experience row opens an inline form prefilled with title, organization, dates, the "current"
  flag and the description, and it saves through `updateExperience`.
- Refusals surface through `describeMutationError`, as add and remove already do.
- Component flow test: edit a level, reload `getMyProfile`, and assert the new level is shown.

## 2. People directory finds every member (people lane, medium)
`app/people/page.tsx` loads `user.getAll` with limit 50, page 1, and filters on the client, so
member #51 onward can never be found. The total shown is the size of that page, and `matchesEmail` is
dead code because `toPublicUser` strips email.

Acceptance: search goes to the server-side `user.search` (name-only since loop 4), results page using
`totalCount`/`hasNextPage`, the header shows the true total, and the email match is removed.

## 3. Invite a known person by email (projects lane, low)
Loop 4 removed the exact-email clause from the public `UserService.SearchAsync`, which let anonymous
callers find out who owns an address. The team tab already searches by name only
("Search users by name..."). If inviting by email is wanted, use the manager-only invitation path
described in `ArdaNova.Application/Services/AGENTS.md` §Guild authority. Do not bring back a public
email lookup.

## 4. Correct the closure-gap wording (docs)
`DELETE /api/users/{id}` is not unguarded: it requires the user's own actor assertion (`IsActor`).
What it lacks is safety. It is a hard delete with no blocker checks on projects, guilds or stakes. Fix
the description in `tracks.md` and in the account-closure track when that track is next touched.

## 5. QA hygiene (QA harness)
- The loop-4 QA lane renamed the shared seeded user (`local-preview-user`) to "QA Auth Preview
  Steward" and rewrote its bio and links. Restore it to "ArdaNova Preview Steward" with empty
  `qa-auth-*` fields. From now on, QA must create its own user for profile write flows.
- `03-dashboard-desktop.png` shows no onboarding nudge, but it does show a Next.js "1 Issue" dev
  overlay. Expand that overlay on `/dashboard` and record its stack. Then recapture the nudge, or mark
  the dashboard-onboarding-nudge flow as unverified.
- The profile JSON export covers only the profile, skills and experience. It does not close half of
  the export gap. The full export stays in `account-closure-and-export_20261018`.

## Out of scope
Consent and the self-service userType choice (`auth-consent-and-account-linking`), and account
closure (`account-closure-and-export_20261018`).
