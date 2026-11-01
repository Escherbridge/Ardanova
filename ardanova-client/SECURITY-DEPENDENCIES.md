# Production dependency security

`npm run audit:prod` (`npm audit --omit=dev --audit-level=high`) is step 5 of `npm run release:check`. It has no allowlist: any high or critical advisory in the production tree fails the release gate.

**Status on 2026-10-31: 0 vulnerabilities. No exceptions are in force.** The audit had found 13 (3 critical, 9 high, 1 moderate). Every one was fixed by an upgrade or by overriding a dependency to its patched version. None was suppressed.

## Decisions, 2026-10-31

| Package                                          | Was                    | Now                    | Advisories                                                                                                                          | How                                                                                          |
| ------------------------------------------------ | ---------------------- | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| `next`                                           | 15.5.20                | 15.5.28                | GHSA-2464-fhr4-w9xw, GHSA-2xp9-vwfh-vxw4, GHSA-p293-qw3h-jr36 (critical RCE); Server Action DoS/SSRF, rewrite SSRF, cache poisoning | Patch release on the same 15.5 line                                                          |
| `next-auth` / `@auth/core`                       | 5.0.0-beta.31 / 0.41.2 | 5.0.0-beta.32 / 0.41.3 | GHSA-8fpg-xm3f-6cx3, GHSA-7rqj-j65f-68wh (critical), GHSA-xmf8-cvqr-rfgj, GHSA-x445-f3h2-j279                                       | Exact pin kept                                                                               |
| `postcss` (+ `nanoid`, `source-map-js`)          | 8.5.19 (3.3.16, 1.2.1) | 8.5.32 (3.3.20, 1.2.2) | GHSA-fxqj-rqcc-2cmp, GHSA-2v37-7h3g-55p8, GHSA-68fv-2mgg-jv7q                                                                       | devDependency and override floor raised                                                      |
| `sharp`                                          | 0.34.3                 | 0.35.5                 | GHSA-f88m-g3jw-g9cj, GHSA-rgj7-g3m4-5g8c, GHSA-wq5f-xc86-pv6w                                                                       | Override, kept inside next's own `^0.34.3 \|\| ^0.35.5` range                                |
| `@t3-oss/env-nextjs` / `env-core`                | 0.12.0                 | 0.13.11                | Inherited from zod (peer range was `^3.24.0`)                                                                                       | Upgrade. The only break is in the `CreateEnv` generic types, which `src/env.js` does not use |
| `zod`                                            | 3.25.76                | 4.6.7                  | GHSA-mrhx-c7xc-m3w5                                                                                                                 | Major upgrade (see below)                                                                    |
| `deepmerge-ts` (via `prisma` → `@prisma/config`) | 7.1.5                  | 8.0.2                  | GHSA-ggr8-5vv4-36mx                                                                                                                 | Scoped override, not Prisma 7 (see below)                                                    |

### next-auth beta.32: behaviour checked

- When its configuration is broken, `auth()` now returns no session instead of an error object. `providerAuth` in `src/server/auth/index.ts` already required `session.user.id`, so this app was never open to that bypass.
- OAuth `state`/`nonce`/PKCE check cookies are now bound to the provider that created them. Google is the only provider. A sign-in that starts on the old build and finishes on the new one may need one retry.
- The NFKC email normalisation only affects the email provider, which this app does not configure. The app never calls `getToken()`.

### zod 4: why the upgrade, and what changed

The advisory is reachable. Every tRPC input is a zod schema that parses untrusted request bodies, and zod 3 parses every element of an array even when the array is over its `.max()`. Each bad element adds one issue. With 100,000 bad elements, `z.array(z.string()).max(10)` produced 100,001 issues on 3.25.76 and 1 issue on 4.6.7.

The upgrade typechecked cleanly and the full suite passed. Differences that affect this app:

- `zod-prisma-types` picks its output syntax from the installed zod major. The only change it made to `src/lib/zod/**` was `z.string().cuid()` → `z.cuid()`.
- `.uuid()` now follows RFC 9562 strictly. The only use is `fundingIntent` `idempotencyKey`, and those keys come from `crypto.randomUUID()`, which conforms.
- `.datetime({ offset: true })` now rejects offsets without a colon, such as `+0000`. .NET serialises offsets as `+00:00`, which is still accepted.
- `.default()` now applies inside `.optional()`. The only affected field is treasury `getTransactions` `limit`, which still ends up as 50.
- `.merge/.extend/.pick/.omit/.partial` on an object schema that has refinements now throws when the module loads. `src/server/api/root.test.ts` imports the whole `appRouter` so this is caught in tests instead of as a 500 on every call.

### Prisma: why an override instead of Prisma 7

Prisma 7 is a migration, not a version bump. It needs driver adapters, a `prisma.config.ts` datasource, changes to the DBML→Prisma generator, and re-proving `migrate deploy`. Overriding the one vulnerable package is much smaller:

- **Reachability.** `deepmerge-ts` is called only by `@prisma/config` when it loads `prisma.config.ts` (`merger: deepmerge`), and only from the Prisma CLI. The Next runtime never loads it: the runner image has no `prisma` CLI, and the `@prisma/client` runtime neither imports `@prisma/config` nor bundles deepmerge. The input is a developer-authored config file, never a request.
- **Compatibility.** Prisma 7.10.1's own `@prisma/config` ships `deepmerge-ts` 8.0.2. The 8.0 breaking changes (Map deep-merge, type renames, `deepmergeInto` no longer mutating its input) do not touch a plain-object `deepmerge(...)` call. `prisma generate`, `prisma format`, and `loadConfigFromFile` with and without a config file all work on 8.0.2.

## Overrides: why each exists and when to remove it

- `postcss` — next 15 pins `postcss` 8.4.31. Remove the override when next stops pinning a vulnerable postcss.
- `sharp` — next's range still admits vulnerable 0.34.x. Remove it once next's floor is 0.35.5 or higher.
- `@prisma/config` → `deepmerge-ts` — remove it in the Prisma 7 migration, which brings 8.0.2 natively.

## Residual risk (not covered by the audit)

zod 4.6.7 stops early only on arrays that have a length limit. Untrusted router inputs with no `.max()` still produce one issue per bad element:

- `governance.ts` `options` (×2)
- `opportunity.ts` `skills` (×2)
- `project.ts` `categories` (×2) and `options` (×2)

Add a `.max()` to each, matching the limit the .NET API enforces. Revisit before any public, unauthenticated write surface ships.

## Changing a dependency

Never run `npm install` on Windows. Edit `package.json`, then regenerate the lockfile on the builder platform with the command in `scripts/verify-lockfile-platform.mjs` (`conductor/RUNBOOK.md` §2a). Then run `npm run verify:lockfile-platform`, `npm run verify:sdk-pin`, and `npm ci`.

An advisory that cannot be fixed needs a reviewed exception before the gate will pass. Key it by GHSA id, give it an owner and an expiry of 30 days or less, and teach `audit:prod` to fail once it expires. Never use an exception for a critical advisory.
