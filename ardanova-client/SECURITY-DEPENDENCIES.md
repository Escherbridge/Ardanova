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

- **Reachability.** `deepmerge-ts` is called only by `@prisma/config` when it loads `prisma.config.ts` (`merger: deepmerge`), and only from the Prisma CLI. The Next runtime never loads it. `Dockerfile:71` copies all of `node_modules/@prisma` into the runner image (not "only `@prisma/client`" — `@prisma/config` is present too), so presence-in-the-image is not the reason this is unreachable. The real reason: `deepmerge-ts` is hoisted to top-level `node_modules/deepmerge-ts` (`package-lock.json` resolves it outside `@prisma/`, not nested under `@prisma/config`), and the runner image never copies generic top-level `node_modules` — only Next's traced `.next/standalone` plus the explicit `.prisma`/`@prisma` overlay. Nothing at runtime imports `deepmerge-ts`, so Next's standalone trace omits it, and the Dockerfile never re-adds it the way it re-adds `@prisma`. The input (`prisma.config.ts`) is a developer-authored config file, never a request, and the image has no `prisma` CLI to load it from.
- **Compatibility.** Prisma 7.10.1's own `@prisma/config` ships `deepmerge-ts` 8.0.2. The 8.0 breaking changes (Map deep-merge, type renames, `deepmergeInto` no longer mutating its input) do not touch a plain-object `deepmerge(...)` call. `prisma generate`, `prisma format`, and `loadConfigFromFile` with and without a config file all work on 8.0.2.

## Overrides: why each exists and when to remove it

- `postcss` — next 15 pins `postcss` 8.4.31. Remove the override when next stops pinning a vulnerable postcss.
- `sharp` — next's range still admits vulnerable 0.34.x. Remove it once next's floor is 0.35.5 or higher.
- `@prisma/config` → `deepmerge-ts` — remove it in the Prisma 7 migration, which brings 8.0.2 natively.

## Unbounded-array DoS: fixed 2026-10-31

zod 4.6.7 stops early only on arrays that have a length limit. Eight tRPC inputs — `governance.ts:101,112` (`options`), `opportunity.ts:31,55` (`skills`), `project.ts:39,64,228,246` (`categories` ×2, `options` ×2) — had none, so an attacker-sized array (all `protectedProcedure`, but sign-up is open to any verified Google email, so effectively public) produced one zod issue per element: a 2 MB body → ~1,000,000 issues, ~3.5 s event-loop block, ~816 MB heap, and a ~179 MB echoed error (`ZodError.message` is `JSON.stringify(issues)`; `flatten()` groups every element issue under one field key). This is fixed. Three independent layers, so a regression in one does not reopen the hole:

1. **Every array now has `.max()`, every element string now has `.max()`, and every other free-text string in the same input objects now has `.max()`** (parsing a request this size was the amplification itself, not just the string values). Limits chosen below; `N` = array length, `M` = element/string length.

   | Field                                                                                                                                                                                                       | File:line              | Array max (N) | Element/string max (M)      | Source                                                                                                                                                                          |
   | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- | ------------- | --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
   | `options` (create proposal)                                                                                                                                                                                 | `governance.ts:101`    | 20            | 200                         | Product default — `Proposal.options` is `jsonb`; .NET only requires ≥2 choices (`GovernanceServices.cs` `ValidateProposalShape`, ~line 911)                                     |
   | `options` (update proposal)                                                                                                                                                                                 | `governance.ts:112`    | 20            | 200                         | same                                                                                                                                                                            |
   | `title` / `description` / `projectId` (create/update proposal)                                                                                                                                              | `governance.ts:98-109` | —             | 200 / 20,000 / 100          | Product default (title ≈ existing `summary` cap; description long-text tier; id tier)                                                                                           |
   | `skills` (create opportunity)                                                                                                                                                                               | `opportunity.ts:31`    | 20            | 200                         | Product default — `Opportunity.skills` is a free `text` column with no .NET validation (`database-architecture.dbml:2050`, `OpportunityServices.cs`)                            |
   | `skills` (update opportunity)                                                                                                                                                                               | `opportunity.ts:55`    | 20            | 200                         | same                                                                                                                                                                            |
   | `title` / `description` / `type` / `experienceLevel` / `compensationModel` / `location` / `deadline` / `projectId` / `guildId` / `taskId` (opportunity create/update)                                       | `opportunity.ts:28-62` | —             | 200 (100 for ids)           | Product default                                                                                                                                                                 |
   | `categories` (create project)                                                                                                                                                                               | `project.ts:39`        | 20            | **50**                      | **Mirrors .NET exactly**: `ProjectServices.cs:512-513` (`ValidateCategories`) refuses a custom category over 50 characters; `ProjectCategory.cs` lists the 10 known enum values |
   | `categories` (update project)                                                                                                                                                                               | `project.ts:64`        | 20            | **50**                      | same                                                                                                                                                                            |
   | `title` / `description` / `problemStatement` / `solution` / `targetAudience` / `expectedImpact` / `timeline` / `tags` / `images` / `videos` / `documents` / `storefrontDescription` (project create/update) | `project.ts:30-94`     | —             | 200 (title) / 20,000 (rest) | Product default — 20,000 matches the sibling `mediaPaths` cap already in the same schema                                                                                        |
   | `options` (project router create proposal)                                                                                                                                                                  | `project.ts:228`       | 20            | 200                         | Product default, same reasoning as `governance.ts` options                                                                                                                      |
   | `options` (project router update proposal)                                                                                                                                                                  | `project.ts:246`       | 20            | 200                         | same                                                                                                                                                                            |
   | `title` / `description` / `projectId` / `proposalId` (project router proposal create/update)                                                                                                                | `project.ts:223-250`   | —             | 200 / 20,000 / 100          | Product default                                                                                                                                                                 |

   **Follow-up for the .NET side** (none of these are edited here; client-only fix):
   - Add an explicit cap on the number of proposal options (`Proposal.options` jsonb) — today `ValidateProposalShape` only requires ≥2, no upper bound.
   - Add an explicit cap on `Opportunity.skills` length/count — today it is an unvalidated free `text` column.
   - Consider capping the _count_ of `Project.categories` (today `ValidateCategories` bounds each custom category to 50 chars but not how many may be submitted).
   - None of `Project.title`, `description`, `problemStatement`, `solution`, `targetAudience`, `expectedImpact`, `timeline`, `tags`, `images`, `videos`, `documents`, `storefrontDescription` have a FluentValidation validator or DB column length limit (`varchar`/`text` unbounded in `database-architecture.dbml`); the 200/20,000 caps added here are a client-side product default, not a mirrored backend rule.

2. **The request body is capped at the edge**, before tRPC/zod ever read it (`src/app/api/trpc/[trpc]/route.ts`): a `POST` whose `Content-Length` declares more than 1 MiB is rejected with 413 immediately; a body with no (or an understated/chunked) `Content-Length` is still bounded because the handler reads the stream itself and aborts once it has seen 1 MiB, never buffering more:

   ```ts
   const MAX_BODY_BYTES = 1 * 1024 * 1024; // 1 MiB

   async function readBodyWithinLimit(stream, limitBytes) {
     const reader = stream.getReader();
     const chunks = [];
     let total = 0;
     for (;;) {
       const { done, value } = await reader.read();
       if (done) break;
       total += value.byteLength;
       if (total > limitBytes) {
         await reader.cancel();
         return null; // caller responds 413 without ever buffering the rest
       }
       chunks.push(value);
     }
     /* ...reassemble and hand to fetchRequestHandler via `new Request(...)`... */
   }
   ```

   1 MiB was sized above the largest legitimate payload in this app today (a project create/update with every long-text field at its new 20,000-character cap is ≈230 KB; this app has no base64/data-URL upload path through tRPC — uploads go through `S3StorageService`/`LocalStorageService`, not the tRPC body) with headroom for `httpBatchStreamLink` batching a handful of calls into one request, while still rejecting the finding's 2 MB attack body before it is read into memory. `GET` requests carry tRPC's batch-query input in the URL, not a body; that is left to the platform's own URL-length limit, not covered by this cap.

3. **The echoed error is capped regardless of how it was produced** (`src/server/api/trpc.ts` `errorFormatter`): `ZodError.message` (`JSON.stringify` of every issue) and `flatten().fieldErrors[field]` (every issue for that field) are both truncated to the first 10 issues, with an "…and N more issue(s) omitted" suffix on the message. Every invalid field's key is still present (so "check the X and Y fields" copy still works; see `src/server/api/root.test.ts`'s `idempotencyKey` assertion, unaffected since that failure has exactly one issue). `moneyCode`/`moneyIntentId` are untouched — they are read from the `TRPCError` itself, not from the zod issue list.

Revisit before any public, unauthenticated write surface ships new unbounded collections.

## Changing a dependency

Never run `npm install` on Windows. Edit `package.json`, then regenerate the lockfile on the builder platform with the command in `scripts/verify-lockfile-platform.mjs` (`conductor/RUNBOOK.md` §2a). Then run `npm run verify:lockfile-platform`, `npm run verify:sdk-pin`, and `npm ci`.

An advisory that cannot be fixed needs a reviewed exception before the gate will pass. Key it by GHSA id, give it an owner and an expiry of 30 days or less, and teach `audit:prod` to fail once it expires. Never use an exception for a critical advisory.
