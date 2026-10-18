---
type: plan
---
# Plan: Auth Consent, Account Linking, and Honest Sign-in Errors

## Phase 1: Read-surface and copy fixes (no schema)
- [ ] Apply `toPublicUser` in `routers/profile.ts` `getProfileById`; add a router test that an anonymous read has no `email` or `phone` (owner: profile lane)
- [ ] `/dashboard/profile` badge shows `session.user.userType` and `verificationLevel` (owner: dashboard lane)
- [ ] `/auth/error`: separate ArdaNova refusals from provider denials (owner: auth lane, `src/app/auth/error/**`)
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
