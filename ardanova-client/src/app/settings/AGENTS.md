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
is never sent. Phone is the only private field on the form. Email comes from
the sign-in account and cannot be edited here. userType is still assigned
by the platform; the self-service choice belongs to
`conductor/tracks/auth-consent-and-account-linking`.
