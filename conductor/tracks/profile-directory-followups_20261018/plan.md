---
type: plan
---
# Plan: Profile entry editing, people directory reach, and auth QA hygiene

## Phase 1 - Entry editing UI [auth lane]
- [ ] Inline level editor on each skill row in `components/profile/profile-editor.tsx`, wired to `api.profile.updateSkill`.
- [ ] Inline edit form on each experience row, wired to `api.profile.updateExperience`, with dates sent as calendar dates.
- [ ] Component flow tests: edit a skill level and edit an experience entry, then assert the refetched values.

## Phase 2 - People directory [people lane]
- [ ] Replace the client-side filter in `app/people/page.tsx` with server `user.search` plus pagination.
- [ ] Show the true `totalCount`, and remove the dead `matchesEmail` branch.

## Phase 3 - Invite by email [projects lane, optional]
- [ ] If the product wants it, add an email field to the team-tab invite that uses the manager-only invitation path. Never use public search.

## Phase 4 - Docs and QA hygiene
- [ ] Correct the "unguarded hard delete" wording: the delete is actor-guarded but has no blocker checks.
- [ ] Restore the shared seeded user's profile, and make QA profile-write flows use a lane-owned user.
- [ ] Triage the `/dashboard` dev-overlay issue, and recapture or downgrade the onboarding-nudge evidence.
