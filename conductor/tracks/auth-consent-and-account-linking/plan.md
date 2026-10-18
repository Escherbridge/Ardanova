---
type: plan
---
# Plan: Auth Consent, Account Linking, and Honest Sign-in Errors

## Phase 1: Read-surface and copy fixes (no schema)
- [x] Apply `toPublicUser` in `routers/profile.ts` `getProfileById`; add a router test that an anonymous read has no `email` or `phone` (loop 2, `profile.test.ts`)
- [x] `/dashboard/profile` badge shows the real `userType` and `verificationLevel` (loop 2, `components/profile/profile-identity-badges.tsx`)
- [x] `/auth/error`: separate ArdaNova refusals from provider denials. The callback returns `AccountNotLinked`, and "Try sign-in again" keeps `callbackUrl` (loop 2)
- [x] Self-service profile editing at `/settings/profile`, plus Follow on other members' profiles (loop 2)
- [ ] Self-service `userType` choice at first sign-in. Today `UsersController.UpdateUserType` is admin-only and `UpdateUserDto` has no `userType`. Add a .NET self-service endpoint limited to non-privileged types, with tests, and an onboarding step
- [ ] Account deletion: a self-service `DELETE /api/users/{id}` already exists (actor == id), but no UI calls it, and the cascade or retention policy for owned projects and funds has not been decided
- [ ] `ExceptionHandlingMiddleware`: return a generic message for `InvalidOperationException` (owner: API platform lane)
- [ ] `ArdaNovaApiClient.VerifyAsync` (`ArdaNova.API/Client`): remove it, because `/verify` now returns 410

## Phase 2: Consent
- [ ] DBML: `UserConsent` table plus a hand-written additive migration; regenerate Prisma and EF
- [ ] .NET `ConsentService` and `POST /api/users/{id}/consents` (ActorAssertion, actor == id), with tests
- [ ] `/onboarding/consent` page; `AuthenticatedLayout` redirects there until the current versions are accepted
- [ ] Adversarial review: `/security-review`

## Phase 3: Account linking
- [ ] Single-use code issuance and verification (.NET), rate-limited, with expiry
- [ ] Linking page; the `signIn` callback redirects a collision there instead of `AccessDenied`
- [ ] Flow tests: collision, then code, then a linked account that signs in; a wrong or expired code stays fail-closed
- [ ] Adversarial review: `/security-review` (auth boundary)
