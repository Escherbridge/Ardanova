# Authentication boundaries

## Real session authority

`auth()` is the only session helper authorized for API routes, tRPC context, realtime endpoints, SDK routes, privileged layouts, ownership checks, or server-side data access. It always delegates to NextAuth.

## Development page preview

`authForPage()` may return a synthetic session only when all local-preview guards pass. Use it only to render non-privileged interface pages for local visual testing. A preview session must never authorize a query, mutation, realtime connection, ownership decision, or admin view.

The preview is denied when `NODE_ENV` is not `development` and when Railway or Vercel hosted-runtime variables are present. Keep both layers: production denial prevents accidental deployment, while separation from `auth()` prevents a local preview from reaching real services as an authenticated identity.

The preview user identifier intentionally has no corresponding database record. Data-backed controls should surface their normal unavailable or unauthorized state during preview rather than creating a special mutation path.

## Authentication logs

Auth.js debug logging stays disabled in every environment because its structured provider diagnostics can include client configuration, callback state, and other credentials. The custom logger records only stable error or warning codes; never pass raw provider payloads, tokens, cookies, profile data, or secrets into shared logs.

## OAuth profile validation

Google sign-in fails closed unless the provider reports `email_verified: true`, a non-empty normalized email, and a non-empty `sub` matching the provider account identifier. Treat optional provider fields as untrusted runtime data even when an upstream declaration is permissive; copy names and images only after checking their primitive types.

The immutable identity anchor is the existing `Account(provider, providerAccountId)` unique key. New users and their Google account link are created atomically. A verified email collision with an unlinked local user is not enough to establish identity and therefore fails closed pending an explicit account-linking flow. Existing linked identities resolve through the provider account; subsequent JWT refreshes resolve by the already-authorized user ID, never by mutable email.

Auth.js omits `user` when refreshing an existing JWT despite declaring it as required in the callback type. The local callback contract models that field as optional. Missing users and database failures clear authorization claims rather than preserving stale privileges.
## Google OAuth deployment contract

- `AUTH_URL` is the canonical public frontend origin, with no path or trailing slash.
- The Google web client must authorize `${AUTH_URL}/api/auth/callback/google` exactly; origins alone are insufficient.
- The production OAuth client must be owned by a Google Cloud project accessible to the deployment operators. A client ID from an inaccessible project cannot be repaired when domains change.
- On Railway, set `AUTH_TRUST_HOST=true` and keep `AUTH_URL` pinned to the stable public domain. Trusting forwarded host headers does not replace the canonical URL or Google redirect allowlist.
- Verify the generated Google authorization URL after every domain/client change; its `redirect_uri` must equal the callback above byte-for-byte.
