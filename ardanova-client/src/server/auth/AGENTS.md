# Authentication boundaries

## Real session authority

`auth()` is the only session helper authorized for API routes, tRPC context, realtime endpoints, SDK routes, privileged layouts, ownership checks, or server-side data access. It returns the NextAuth provider session, or — only when every development-preview guard passes and no provider session exists — the seeded preview identity described below. `providerAuth()` is the provider session alone; the sign-in page uses it so it stays reachable during a preview.

## Development preview

The preview is denied when `NODE_ENV` is not `development`, when Railway or Vercel hosted-runtime variables are present, and when `DEV_AUTH_BYPASS` is not exactly `true`. Production readiness (`src/lib/health/readiness.ts`) independently refuses any configured `DEV_AUTH_BYPASS`. Keep all layers.

Data-capable preview identity: `auth()` returns a preview session only for the database row written by `scripts/seed-local-demo.ts` (`id = local-preview-user`, `email = preview@local.ardanova.test`); the seed itself is gated by `assertLocalDemoSeedAllowed`. Its claims (`name`, `role`, `userType`, `isVerified`, `verificationLevel`) are read from that row on every request exactly as a JWT refresh reads them, so the shell, the tRPC context, the client session, and the .NET API all agree on one subject. `protectedProcedure` signs actor assertions for it like any other user, so backend permissions still apply — the preview skips only the OAuth round trip, never authorization. This replaced the earlier split (page-only synthetic session, real `auth()` for data), which rendered a signed-in shell over a data layer that returned `UNAUTHORIZED` for every protected call.

Page-only fallback: on an unseeded database, `authForPage()` still returns a hard-coded synthetic session so pages render for visual QA, but `auth()` returns `null` and every data-backed control surfaces its normal unauthorized state. A synthetic identity with no row never authorizes anything.

Client session parity: `handlers.GET` answers `/api/auth/session` with the seeded preview session when the guards pass and no provider cookie exists, so `useSession()` consumers outside `AuthenticatedLayout`'s seeded `SessionProvider` see the same subject.

## User directory privacy

`user.getAll`, `user.search`, `user.getById` and `user.getFollowingWithUsers` are browseable without sign-in or across users, so `routers/user.ts` projects every record through `toPublicUser`, which drops `email`, `emailVerified`, `phone`, `role` and `trustScore`. `getById` returns the full record only when the signed-in subject asks for their own id. The .NET `UserService.SearchAsync` matches name by substring but email only by exact, case-insensitive equality, so a substring query cannot enumerate addresses while an inviter who already knows an address can still find that person. `profile.getProfileById` applies the same rule: the full record only to the subject themself, `toPublicUser` for everyone else.

Projection alone is not enough, because `toPublicUser` only strips top-level keys. Both routers therefore pass every backend answer through `requireRequestedUser`, which throws `NOT_FOUND` unless the response is an object whose `id` equals the id that was requested. A list or a different user can never be "projected" and returned.

## Path-safe ids

The ArdaNova API client interpolates ids straight into .NET paths (`/api/users/${id}`), and it calls the backend with the server's API key. An id such as `paged?pageSize=3`, `../../api/projects?x=` or `email/<address>` would therefore turn any public procedure into a confused-deputy GET against an arbitrary API route; URL parsing collapses `..` and a trailing `x=` swallows the rest of the path. Every id input in `routers/user.ts` and `routers/profile.ts` uses `recordIdSchema` (`^[A-Za-z0-9_-]{1,128}$`, which covers cuid, uuid and the seeded `local-preview-user` slug), so such input fails as `BAD_REQUEST` before a request is built. `recordIdSchema` also refuses the literal route words that sit beside `{id}` templates (`paged`, `search`, `email`, plus `me` and `admin` as cheap insurance). ASP.NET matches literal segments before templates, so `getById("paged")` would otherwise reach the full listing at `GET /api/Users/paged`; `requireRequestedUser` still caught it, but only after the request had gone out. The input boundary now rejects it, and the downstream check stays as a second layer. Use `recordIdSchema` for any new id that reaches an API path. Other routers and the `endpoints/*.ts` path builders still need the same treatment; that work is tracked in `conductor/tracks/api-path-segment-safety`.

When adding an endpoint for a resource someone owns, keep both layers: the actor assertion check in the controller, and a service-level check that the resource belongs to that actor. Skills and experience already have both, so an attacker who puts the victim's resource id under their own user id still gets 404.

## Sign-in refusal codes

When ArdaNova itself refuses a sign-in, the `signIn` callback returns a redirect to `/auth/error?error=<code>` (`error-codes.ts`) rather than `false`. Returning `false` makes Auth.js report `AccessDenied`, and that page cannot tell a provider refusal from an ArdaNova one. The only such code today is `AccountNotLinked`, for a verified Google email that already belongs to an unlinked local user. Revealing that is acceptable because the person signing in has just proven, through Google, that they control that address. Incomplete or unverified provider identities and database failures still return `false`.

## Authentication logs

Auth.js debug logging stays disabled in every environment because its structured provider diagnostics can include client configuration, callback state, and other credentials. The custom logger records only stable error or warning codes; never pass raw provider payloads, tokens, cookies, profile data, or secrets into shared logs.

## OAuth profile validation

Google sign-in fails closed unless the provider reports `email_verified: true`, a non-empty normalized email, and a non-empty `sub` matching the provider account identifier. Treat optional provider fields as untrusted runtime data even when an upstream declaration is permissive; copy names and images only after checking their primitive types.

The immutable identity anchor is the existing `Account(provider, providerAccountId)` unique key. New users and their Google account link are created atomically. A verified email collision with an unlinked local user is not enough to establish identity and therefore fails closed pending an explicit account-linking flow. Existing linked identities resolve through the provider account; subsequent JWT refreshes resolve by the already-authorized user ID, never by mutable email.

Production users created by the legacy Google-only callback may be migrated through `AUTH_GOOGLE_LEGACY_LINK_USER_IDS`. The operator must list exact persisted user IDs after reviewing the affected rows. The callback still requires Google's verified email and matching subject, rechecks the same normalized email and absence of all provider accounts in a serializable transaction, and stores only the immutable Google account link. Remove each allowlist entry after that user signs in; never use email addresses or broad flags in this setting.

Auth.js omits `user` when refreshing an existing JWT despite declaring it as required in the callback type. The local callback contract models that field as optional. Missing users and database failures clear authorization claims rather than preserving stale privileges.

## Google OAuth deployment contract

- `AUTH_URL` is the canonical public frontend origin, with no path or trailing slash.
- The Google web client must authorize `${AUTH_URL}/api/auth/callback/google` exactly; origins alone are insufficient.
- The production OAuth client must be owned by a Google Cloud project accessible to the deployment operators. A client ID from an inaccessible project cannot be repaired when domains change.
- On Railway, set `AUTH_TRUST_HOST=true` and keep `AUTH_URL` pinned to the stable public domain. Trusting forwarded host headers does not replace the canonical URL or Google redirect allowlist.
- Verify the generated Google authorization URL after every domain/client change; its `redirect_uri` must equal the callback above byte-for-byte.
