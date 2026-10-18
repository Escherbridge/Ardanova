---
type: spec
---
# Auth Consent, Account Linking, and Honest Sign-in Errors

## Outcome

A first-time ArdaNova member records Terms and Privacy consent before the workspace opens. A
person whose verified Google email already belongs to an unlinked local account gets an explicit,
verifiable linking path instead of a dead end. The sign-in error page names who refused the
attempt. Contact fields never leave the profile router for anyone but the owner.

## Context (loop 1 auth review, 2026-10-17)

Loop 1 fixed the critical and high findings: the preview identity is shared by pages, the tRPC
context and the client session; skill/experience writes are owner-bound; denials return 401/403
instead of 400; `user.*` reads go through a public projection; self-verification is retired.
This track holds the parts that need schema, product decisions, or another lane's files.

## Requirements

1. **Consent record.** Add an append-only `UserConsent` row (userId, document `TERMS|PRIVACY`,
   version, acceptedAt) through the DBML. The NextAuth `signIn` callback keeps creating the user,
   and a `/onboarding/consent` step blocks `AuthenticatedLayout` until the current versions are
   accepted. The write is a .NET endpoint bound to the actor assertion.
2. **User type is chosen, not defaulted.** The `userType: VOLUNTEER` default in
   `src/server/auth/config.ts` becomes a placeholder that onboarding replaces. The user-type step
   is already specified in [17-onboarding](../17-onboarding/spec.md); this track only makes the
   consent step come first.
3. **Explicit account linking.** When a verified Google email collides with an unlinked user,
   send the person to a linking page and prove control of that email (a single-use emailed code)
   before writing `Account(provider, providerAccountId)`. The fail-closed default stays until
   that proof exists.
4. **Honest error copy.** `/auth/error` separates `AccessDenied` caused by an ArdaNova refusal
   (collision, failed user sync) from a provider denial, so it never blames Google for ArdaNova's
   own decision.
5. **Profile router projection.** `profile.getProfileById` (`routers/profile.ts`) applies
   `toPublicUser` from `routers/user.ts`.
6. **Own-claims display.** `/dashboard/profile` shows the session's `userType` and
   `verificationLevel` in place of the hard-coded "Member" badge.
7. **Exception echo.** `ExceptionHandlingMiddleware` stops echoing `InvalidOperationException`
   messages to callers.

## Non-goals

- Changing the Google-only provider set.
- Any KYC or verification-level authority; AZOA KYC stays authoritative.
