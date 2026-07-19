# ArdaNova authentication setup

ArdaNova uses NextAuth v5 with Google OAuth, JWT sessions, and custom Prisma user synchronization. Google is the only configured provider in the current frontend. A successful provider callback is not sufficient on its own: protected tRPC procedures also require a persisted ArdaNova user ID in the session.

## Prerequisites

- Node.js 20+ and npm 10+
- PostgreSQL reachable through `DATABASE_URL`
- a Google Cloud project with an OAuth 2.0 web client
- separate Google OAuth credentials for local development and production

## 1. Generate the session secret

Generate at least 32 cryptographically random characters. For example:

```bash
node -e "console.log(require('node:crypto').randomBytes(48).toString('base64url'))"
```

Store the value as `AUTH_SECRET`. Never commit it, expose it through a `NEXT_PUBLIC_` variable, or reuse the production value in local development.

## 2. Configure Google OAuth

In Google Cloud Console, create an OAuth 2.0 client with application type **Web application**.

For local development, configure:

```text
Authorized JavaScript origin: http://localhost:3000
Authorized redirect URI:     http://localhost:3000/api/auth/callback/google
```

If a different local port is used, both values must use that exact origin. For Railway, add the final HTTPS domain and its callback:

```text
Authorized JavaScript origin: https://your-frontend-domain
Authorized redirect URI:     https://your-frontend-domain/api/auth/callback/google
```

Copy the client ID and secret into `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`.

## 3. Configure the frontend environment

Use `ardanova-client/.env.example` as the source list. The authentication subset is:

```env
AUTH_SECRET=replace-with-a-random-secret-of-at-least-32-characters
AUTH_URL=http://localhost:3000
GOOGLE_CLIENT_ID=replace-with-google-client-id
GOOGLE_CLIENT_SECRET=replace-with-google-client-secret
DEV_AUTH_BYPASS=false
```

`AUTH_URL` must be the browser-visible origin, not the .NET backend URL. In Railway it must use the final public HTTPS domain.

The application also needs a working PostgreSQL `DATABASE_URL`. On first successful Google sign-in, the callback requires Google's verified-email claim and atomically creates both the ArdaNova user and an `Account` link keyed by Google's immutable provider subject. JWT claims resolve through that link on sign-in and through the persisted user ID on refresh; email is never the account-linking key.

An existing `User` row with the same email but no Google `Account` row is deliberately not linked during sign-in. Legacy accounts from before immutable provider linking need an explicit, operator-reviewed link or a future authenticated account-linking flow. Do not backfill provider subjects from email alone.

For an operator-reviewed legacy link:

1. Have the user complete a fresh Google OpenID Connect flow for the same deployed OAuth client through a controlled server-side linking workflow. Verify the returned ID token with Google Auth Library's `OAuth2Client.verifyIdToken({ idToken, audience: GOOGLE_CLIENT_ID })`; do not merely decode it. Require the verified payload's `email_verified` to be `true`, then record only its immutable `sub`. Never place the raw token in shell history or logs.
2. Independently confirm the intended persisted ArdaNova user ID. An email match alone is not sufficient evidence.
3. In a shell with the target database's `DATABASE_URL`, set `LEGACY_LINK_USER_ID`, `LEGACY_LINK_GOOGLE_SUB`, and the one-run opt-in `ALLOW_LEGACY_GOOGLE_ACCOUNT_LINK=true`.
4. From `ardanova-client`, run `npm run auth:link-google-account`. The command refuses missing users, subject collisions, and users that already have a Google link.
5. Clear all three linking variables, then complete a normal Google sign-in and verify `/api/auth/session` contains the expected persisted user ID.

Before rollout, count users that still need coordination without returning identity data:

```sql
SELECT COUNT(*) AS unlinked_google_users
FROM "User" AS users
WHERE NOT EXISTS (
  SELECT 1
  FROM "Account" AS accounts
  WHERE accounts."userId" = users.id
    AND accounts.provider = 'google'
);
```

A brand-new verified Google identity can use either the sign-in or sign-up entry point: the callback creates its user and immutable account link in one write. A returning identity with an existing Google `Account` link also continues normally. Any nonzero legacy count represents users who will be denied after rollout until the explicit linking procedure is completed; do not deploy this boundary as a silent migration.

## 4. Local interface preview

For frontend design and browser work without Google credentials, local development can use:

```env
DEV_AUTH_BYPASS=true
```

The preview is deliberately narrow:

- it is accepted only when `NODE_ENV=development` and the process is not hosted by Railway or Vercel;
- it creates a visibly marked page session so authenticated layouts can be inspected;
- it does not authorize tRPC, SDK, realtime, admin, ownership, or transaction APIs;
- it must be absent or `false` in every deployed environment.

Turn the flag off before testing the real provider flow.

## 5. Exercise the flow locally

Start the frontend and open `/auth/signin`:

```bash
npm run dev
```

Verify:

1. **Continue with Google** starts the NextAuth Google flow.
2. Google returns to `/api/auth/callback/google` on the same origin.
3. The callback verifies the Google identity, creates or resolves its immutable account link, and redirects to `/dashboard`. Returning linked users resolve their persisted ArdaNova profile; the callback does not overwrite those profile fields from Google.
4. `/api/auth/session` contains the persisted ArdaNova user ID.
5. Protected tRPC operations reject requests after sign-out.
6. `/auth/error` presents a safe branded recovery state without raw provider details.

## 6. Production and Railway validation

Configure all authentication variables as Railway service variables. The frontend also requires two distinct sealed server secrets: `ADMIN_API_KEY` for privileged BFF-to-API calls and `ACTOR_ASSERTION_HMAC_KEY` for binding ordinary BFF calls to the signed-in actor. Each must contain at least 32 bytes and use the same respective value on the .NET API service. Do not expose either key to the browser, and do not configure `DEV_AUTH_BYPASS`.

After deploying, verify the released domain rather than only the local build:

| Check                                | Expected result                                                   |
| ------------------------------------ | ----------------------------------------------------------------- |
| `/auth/signin`                       | Branded sign-in page renders                                      |
| `/api/auth/providers`                | Google provider is present                                        |
| `/api/auth/session` while signed out | No authenticated user                                             |
| `/dashboard` while signed out        | Redirects to `/auth/signin`                                       |
| Google authorization request         | Uses the deployed HTTPS origin                                    |
| Google callback                      | Matches the approved production callback exactly                  |
| `/api/ready`                         | Returns HTTP 200 with valid configuration and a reachable backend |

Also inspect Railway logs for environment validation, provider, database, or callback errors. The frontend release sequence is documented in `documentation/FRONTEND_RELEASE_PLAYBOOK.md`.

## Troubleshooting

### Configuration error on `/api/auth/error`

Check that all four required auth variables are present, `AUTH_SECRET` is at least 32 characters, the database is reachable, and the deployed `AUTH_URL` matches the current domain.

### Redirect URI mismatch

Compare protocol, hostname, port, path, and trailing slash character-for-character between Google Cloud and the request. The callback path is `/api/auth/callback/google`.

Use `/api/auth/providers` as the runtime source of truth: its Google `callbackUrl`
must match the browser origin and the Google Cloud allow-list. For example, a
development server that moved from port `3100` back to `3000` must use
`http://localhost:3000/api/auth/callback/google` in all three places: `AUTH_URL`,
the provider response, and Google Cloud. Restart the frontend after changing
`AUTH_URL`; NextAuth reads it when the server process starts.

### Sign-in succeeds but protected APIs reject the session

Check the Prisma user synchronization and server logs. The authorization boundary requires a non-empty persisted user ID; an OAuth profile without a synchronized ArdaNova user is intentionally not enough.

### Local preview does not appear

Confirm `DEV_AUTH_BYPASS=true`, `NODE_ENV=development`, and restart the development server. Preview mode is intentionally ignored in hosted environments.

## Security invariants

- Use Google OAuth through the branded server action; do not construct provider URLs manually.
- Require `email_verified: true` and bind identity to Google's immutable subject through the Prisma `Account` unique key; never implicitly link a pre-existing local user by email.
- Never use email addresses, predictable values, or frontend-generated strings as bearer tokens.
- Do not add provider scopes unless a product feature requires them and the consent impact is documented.
- Clear stale custom JWT claims when the database user is missing or synchronization fails.
- Keep auth diagnostics free of email addresses, tokens, raw redirects, and provider secrets in production.
- Re-run the auth route matrix after changing domains, credentials, session callbacks, or user synchronization.
