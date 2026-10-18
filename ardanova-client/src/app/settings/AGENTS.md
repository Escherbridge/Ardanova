# Settings UI boundaries

Wallet settings only manage public external-address records and their displayed
verification state. Never request secrets, invoke a signer, export assets, or
imply that a listed address is a custody, settlement, or payout destination.
Payout controls remain disabled until a server-issued provider flow, policy
checks, and durable settlement reconciliation are implemented. See
`conductor/tracks/gated-commerce-and-azoa-settlement/plan.md`.

## Sections

`/settings` redirects to `/settings/profile`. `settings-nav.tsx` lists the
sections (Profile, Verification, Payouts); add a new section there, not as
a separate link elsewhere. Links that mean "finish identity verification"
should keep pointing at `/settings/verification` directly.

## Profile

`/settings/profile` renders `components/profile/profile-editor.tsx`, which
talks only to the `profile` tRPC router: `getMyProfile`, `updateProfile`,
and the skill and experience mutations. The router always acts for the
session subject. `profileUpdateFrom` (`profile-form.ts`) sends only the
fields that changed, and sends a cleared field as `""` because the .NET
`UserService.UpdateAsync` treats `null` as "leave unchanged". A cleared name
is never sent. Failed edits show `describeMutationError`, so a refusal from
.NET, such as a non-http link, an end date before the start date or a
duplicate skill, appears as its own reason. The router maps the backend
status onto the matching tRPC code (`backendFailure`), and only 5xx replies
are reduced to a generic line. `ProfileDataCard` downloads the
`getMyProfile` record as JSON. It does not offer account closure, which is
blocked on the policy in `conductor/tracks/account-closure-and-export_20261018`.
When `getMyProfile` refetches (after a skill or experience change, or on
window focus), the form is reseeded only if the saved scalar fields changed,
and `reseedProfileForm` keeps any field the member is still editing. A plain
reseed used to wipe an unsaved bio whenever a skill was added. Experience
dates are calendar dates stored as UTC midnight, so `formatExperienceMonth`
formats them in UTC; formatting in the viewer's zone showed "Mar 1" as
"Feb" for everyone west of UTC.
Phone is the only private field on the form. Email comes from
the sign-in account and cannot be edited here. userType is still assigned
by the platform; the self-service choice belongs to
`conductor/tracks/auth-consent-and-account-linking`.
