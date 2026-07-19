# ArdaNova frontend release playbook

This playbook turns the frontend's design, contract, accessibility, authentication, and deployment standards into a repeatable release workflow. It applies to public pages, authenticated workspace routes, Nova-assisted surfaces, and trust or transaction flows.

## Release principles

1. **Doing over scrolling.** Every primary surface should help someone discover a problem, define a solution, or iterate on accountable work. Finite snapshots and explicit next moves are preferred to endless feeds.
2. **People remain the authority.** Nova can draft, organize, review, present, and rehearse. It cannot publish, vote, approve, fund, swap, release escrow, issue credentials, pay contributors, or change rights.
3. **Interface state is evidence, not decoration.** Draft, submitted, confirmed, completed-record, and reconciled are different states. The UI must not promote one into another without a backend record that supports it.
4. **Ownership is explicit.** Membership credentials, project-token utility, and separately approved ownership shares remain distinct. Activity, membership, or a token alone does not imply ownership.
5. **No dead controls.** A visible action must have a route, a working local behavior, or a contract-backed mutation. Otherwise remove it or label it as unavailable without presenting it as actionable.

## 1. Contract gate

For every changed data-bearing surface, trace the complete path before release:

```text
.NET DTO and endpoint -> TypeScript endpoint type -> tRPC input/output -> UI state and language
```

Verify all four layers agree on:

- field names, nullability, enums, identifiers, units, and timestamps;
- authentication mode: anonymous, signed-in user, admin API key, or service API key;
- submitted versus confirmed behavior and any asynchronous reconciliation;
- error semantics, especially unavailable services and partial records;
- pagination or limits, so sampled results are never described as platform totals;
- monetary and token units, including which values the backend actually computes.

Do not use `any`, frontend-invented DTO fields, or fallback values that make an unsupported claim. When a backend operation is known to be unsafe or incomplete, fail closed before the mutation and explain the limitation in the UI.

Current release constraints:

- New payout requests are intentionally paused in both the frontend and backend because verified payout processing is unavailable. Existing pending records can be cancelled only through the backend's atomic unlock-and-cancel path.
- Token distribution is paused until allocation reservation can transition atomically with durable idempotency.
- Credential utility and membership lifecycle mutations are paused until scope and grant authority are actor-bound and auditable; a caller-supplied grant reason never authorizes issuance.
- Governance and vote-delegation mutations are read-only until proposal, vote, comment, delegation, and lifecycle authority is enforced at the backend boundary.
- Referral reward claims are paused until rewards are server-derived, beneficiary-bound, atomic, and idempotent.
- Administrative dispute review is read-only until the backend exposes an authenticated, auditable adjudication contract; funder release/refund endpoints are not admin-resolution substitutes.
- Investment previews describe project-token utility, not equity, and show only amounts returned by the exchange preview contract.
- `/api/sdk/auth/session` is intentionally unavailable until a real server-verifiable SDK exchange is implemented.

A green build is a development checkpoint, not production-promotion approval. Promotion remains gated until the backend binds the authenticated actor and resource authority for guild invitation, application, membership, and verification decisions; server-derives XP, achievement, and leaderboard rights; and enforces membership or ownership on chat and attachment reads and mutations. Lower-risk activity, event, notification, opportunity, post, product, user-experience, skill, streak, and ordinary guild/social CRUD also remains in the actor-ownership hardening backlog. Do not broaden this checkpoint into those workflows without their own contract tests and review.

The next implementation track is end-to-end integration coverage for every ArdaNova flow. That track must exercise Azoa financial-workflow conformance at the service boundary and introduce a clean dependency seam for future agent or partner integrations; it is intentionally outside this design/BFF checkpoint.

## 2. Authentication and authorization gate

Exercise this route matrix with production-style environment values:

| Route                              | Expected behavior                                                             |
| ---------------------------------- | ----------------------------------------------------------------------------- |
| `/`                                | Public story renders; signed-in users may enter the workspace                 |
| `/auth/signin`                     | Branded sign-in page starts Google OAuth through NextAuth                     |
| `/auth/signup`                     | Redirects to `/auth/signin?mode=signup` and preserves the sign-up entry point |
| `/auth/error`                      | Safe branded error state without raw provider details                         |
| `/api/auth/providers`              | Lists the configured Google provider in production                            |
| `/dashboard`                       | Requires a real session outside local preview mode                            |
| `/api/trpc/*` protected procedures | Reject missing or synthetic identities                                        |
| `/api/sdk/auth/session`            | Returns the documented fail-closed response                                   |

`DEV_AUTH_BYPASS=true` is for local interface preview only. It must remain guarded by development mode, must be absent from Railway, and must never authorize tRPC, SDK, realtime, ownership, admin, or financial operations.

Before deploy, confirm `AUTH_SECRET` is at least 32 characters, `AUTH_URL` matches the public HTTPS domain, Google OAuth credentials are production credentials, and the Google callback URI matches the deployed NextAuth callback. Configure distinct random 32+ byte `ADMIN_API_KEY` and `ACTOR_ASSERTION_HMAC_KEY` values, with each respective value shared by the frontend BFF and .NET API services. Never expose either with a `NEXT_PUBLIC_` prefix.

Each enabled Azoa custody, value, or quest capability also needs its own random
32+ UTF-8 byte credential. Production rejects placeholder markers, published
documentation examples, and reuse across capability boundaries.

## 3. Humane interaction and accessibility gate

Review at desktop and narrow/mobile widths:

- the authenticated shell remains navigable without nested viewport traps;
- focus order, focus visibility, labels, landmarks, dialogs, sheets, and keyboard escape behavior work;
- loading, empty, unavailable, forbidden, and error states give a next move;
- destructive or rights-changing actions state their consequence and require human confirmation;
- Nova output identifies context, sources, assumptions, uncertainty, and whether it is still a draft;
- animations respect reduced-motion preferences and do not block reading or action;
- flat neobrutalist rules, signal color, type scale, and spacing follow the brand guide without obscuring content;
- finite discovery surfaces expose the end of the current result set instead of manufacturing an attention loop.

## 4. Automated release gate

Run both service gates once after all changes are integrated:

```bash
cd ardanova-backend-api-mcp
dotnet test ardanova.sln --configuration Release
dotnet list ardanova.sln package --vulnerable --include-transitive

cd ../ardanova-client
npm run release:check
```

The frontend command runs, in order:

```text
all-file whitespace diff check -> changed Prettier-supported source/docs check -> production dependency audit -> lint -> typecheck -> test -> production build
```

The changed-file formatter covers hand-authored JavaScript/TypeScript, CSS,
JSON, Markdown, MDX, and YAML. Generated `src/lib/zod/**` files are regenerated
and compared byte-for-byte in Git CI instead of being reformatted. Dockerfiles
and TOML remain covered by `git diff --check` plus their service build/config
validation rather than an unsupported Prettier parser.

Do not deploy a partial pass. Fix the integrated findings, then repeat both
service gates so the final evidence describes the exact tree being released.

## 5. Browser smoke matrix

Use a real production build or the final local build and verify:

- public: `/`, `/auth/signin`, `/auth/signup`, `/auth/signin?mode=signup`, `/auth/error`;
- solutionary loop: `/dashboard`, `/projects`, a project detail, and project creation;
- work: `/tasks`, `/opportunities`, `/guilds`, `/people`, and `/chats`;
- value and trust: `/portfolio`, `/portfolio/withdraw`, `/swap`, a credential, and any supported escrow detail;
- Nova: global Ask/Draft/Review/Present actions and `/studio` accept, edit, reject, undo, context, source, assumption, and uncertainty states.

For each route, check desktop, narrow/mobile layout, keyboard navigation, console errors, failed network requests, error recovery, and truthful copy. Backend unavailability should render an explicit state rather than an empty or misleading success screen.

## 6. Railway release

This release changes the replay-ledger access path, the escrow dispute contract,
and both service boundaries. Confirm the exact Railway project, environment,
service, and database first; do not echo the database URL. Follow
`GATED_COMMERCE_MIGRATION_RUNBOOK.md` to choose the new-database or established-
schema path. Existing installations must pass the satisfiable
`additive-source` commerce gate **and** the runbook's independently reviewed
full Prisma diff before the complete baseline can be marked applied; new
databases apply the immutable baseline directly.

```bash
cd ardanova-client
MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT=<reviewed-pre-migration-64-character-sha256> \
  railway run --service <database-linked-service> -- \
  npm run db:preflight-commerce -- --phase additive-source
# Existing schema with no migration history only, after catalog approval:
railway run --service <database-linked-service> -- \
  npx prisma migrate resolve --applied 20260718000000_schema_baseline
railway run --service <database-linked-service> -- npm run db:migrate
MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT=<reviewed-post-migration-64-character-sha256> \
  railway run --service <database-linked-service> -- \
  npm run db:preflight-commerce -- --phase additive
```

The pre- and post-migration commerce fingerprints are different reviewed values. Each is
mandatory and must come from the two-pass review in the migration runbook;
never copy an unreviewed value from a failed run merely to make the gate pass.
Neither fingerprint substitutes for the full-schema adoption diff required for
an established database without Prisma migration history.
The `baseline` phase is for an older pre-additive clone and is a stop signal for
this release path, `additive-source` is the established schema before the index
upgrade, and `additive` is the required final state.

The API `/ready` probe verifies runtime configuration, bounded database
connectivity, and the small critical release catalog defined by the API: the
actor-assertion replay table and valid composite cleanup index, plus the task
escrow table and the exact nullable text/timestamp(3) dispute-column contract. It does not prove the complete Prisma
or additive-commerce schema, data reconciliation, migration history, or the
reviewed fingerprint. Treat the full preflight and catalog evidence as separate
release gates.

Before promoting the client, audit persisted Google identities as described in
`AUTHENTICATION_SETUP.md`. Every returning production user needs an immutable
Google `Account` link. Coordinate any reviewed legacy links before cutover so
the hardened callback does not lock out an existing operator.

Frontend readiness depends on the backend `/ready` endpoint. The artifact
workflow never receives a production database URL and never deploys either
service. After the guarded, separately approved database step, promote and
validate the API before the frontend. Releases are Git-CI artifacts, not
workstation source uploads:

1. Push the reviewed, coupled release commit to `main`.
2. Wait for the `ArdaNova release artifacts` GitHub workflow to validate both
   services and finish successfully.
3. Download the `ardanova-release-images-<commit>` manifest (or use the workflow
   summary) and retain both exact digest references. Do not substitute a branch
   or mutable tag.
4. Complete the production-protected migration procedure in
   `GATED_COMMERCE_MIGRATION_RUNBOOK.md`. This is an explicit operator action;
   an ordinary push never migrates production and no workflow may resolve the
   baseline automatically.
5. In the Railway API service settings, set **Source -> Docker Image** to the
   recorded `ghcr.io/escherbridge/ardanova-api@sha256:<digest>` and create the
   deployment. Wait for terminal success and verify `/ready`.
6. Set the Railway frontend service's Docker Image source to the paired
   `ghcr.io/escherbridge/ardanova-client@sha256:<digest>`. Record both image
   digests and deployment IDs, then wait for terminal success before browser
   validation.

Do not use `railway up`, FTP, or any other local/source-upload path. Promotion
must reuse the image Git CI already validated and published.

Do not release the frontend against an older API deployment: its readiness
probe must observe the new backend readiness and contract surface before
Railway promotes the client.

The frontend service must have `DATABASE_URL`, `API_URL`, `API_KEY`, `ADMIN_API_KEY`, `AUTH_SECRET`, `AUTH_URL`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, and `ACTOR_ASSERTION_HMAC_KEY`. `DEV_AUTH_BYPASS` and `NODE_TLS_REJECT_UNAUTHORIZED` must be absent. The admin and actor keys must each match their respective backend value exactly; do not print or copy them through logs.

The .NET API service must use the same sealed `ADMIN_API_KEY` and
`ACTOR_ASSERTION_HMAC_KEY` values. Keep both out of image layers and committed
settings. AutoMapper is pinned to the MIT-licensed 14.x line, so the service has
no mapping-library license secret or readiness requirement. The application
globally forces `MaxDepth=64` for every registered map as the bounded mitigation
for `GHSA-rvv3-g6hj-g44x`; the repository suppresses only that exact audit URL,
and a regression test must continue proving complete map coverage.

AZOA is disabled-safe by default for this release: `Azoa__Mode=Simulated`,
`Azoa__EnableFundingCheckout=false`, `Azoa__EnableSettlementOutboxWorker=false`,
`Azoa__EnableCustodialAccounts=false`,
and `Algorand__Provider=Simulated`. Leave
`Azoa__BaseUrl`, `Azoa__CustodyApiKey`, `Azoa__ValueApiKey`,
`Azoa__QuestApiKey`, and the browser-only `NEXT_PUBLIC_AZOA_BASE_URL` unset while
those surfaces remain dormant. Enabling
the AZOA provider or live mode is a separate release decision and requires an
HTTPS base URL, separate server-only scoped keys, bounded timeout, selected-node and
reconciliation evidence for funding, and a dedicated live-provider smoke test.
`Azoa__TenantApiKey` is only a non-Production migration fallback; Production
ignores it and rejects any key reused across custody, value, or quest clients.

`Algorand__Provider` accepts only `Simulated`, `Azoa`, or `Legacy`; unknown
values fail startup. `Simulated` performs no HTTP, chain, or signing operation.
Production `Legacy` is rejected unless the operator explicitly enables the
custodial-signer break glass and supplies both signer settings. Selecting Azoa
does not claim support for address-based credential mint/burn flows.

Tenant custody now has a typed capability/ensure/status/KYC-session contract.
Capabilities report identity, KYC-provider, and wallet-provisioning readiness
separately; account status reports identity, KYC, and existing-wallet readiness.
The ensure operation is convergent: an identity created while custody is
unavailable can acquire its wallet after the operator later configures custody.
Enabling it requires `Azoa__EnableCustodialAccounts=true`, a canonical
`Azoa__TenantId`, and `Azoa__CustodyApiKey` carrying exactly the required
onboarding scopes (`tenant:provision`, `wallet:manage`, `kyc:read`,
`kyc:submit`) without value-signing scopes. The backend `/ready` path performs a
bounded Azoa capability probe and returns 503 when the selected node, custody,
chain, or KYC provider is not actually ready. KYC-session creation carries a
stable idempotency key and must resume a live attempt without trapping the user
on an expired attempt. Azoa's current development-only
custody implementation is not a production KMS/HSM; production onboarding must
remain disabled until a reviewed custody adapter reports ready.

Allocation and fungible mint calls use only `Azoa__ValueApiKey`, currently with
the literal `nft:mint` scope and no onboarding scopes. Quest calls use only
`Azoa__QuestApiKey`; publishing requires `dapp:develop` and an API-key owner with
the persisted dApp Developer role. Current quest reads, validation, and signals
require valid authentication but no additional controller scope; do not invent
or add `quest:execute` to compensate. Value, quest, and custody transports each
reject paths outside their own route family, and anonymous avatar registration
carries no API key. Azoa also exposes
`developmentSimulation` on capabilities and sessions only for its available
Manual provider in Development; ArdaNova may start that labeled review loop but
must never present it as identity proof or accept public document URLs.

Funding readiness is false unless the bounded hosted outbox loop is explicitly
registered and the selected-node gateway attests dispatch, reconciliation, and
the complete canonical funding-asset contract. The current gateway remains
disabled, so these flags alone cannot activate checkout. Custodial account
readiness never authorizes checkout, minting, bridging, swapping, release, or
payout.

Wait for Railway's terminal success. Then validate the exact public service and domain:

1. `/api/health` returns HTTP 200 for process liveness over HTTPS.
2. `/api/ready` returns HTTP 200 only when required runtime configuration is valid and the bounded backend readiness request succeeds. The backend `/ready` check also requires valid server configuration, bounded PostgreSQL connectivity, and its defined critical release catalog; both Railway services use readiness paths for deployment health. Full migration and reconciliation evidence remains a separate gate.
3. `/` and `/auth/signin` render the released visual system.
4. `/api/auth/providers` exposes the expected Google provider.
5. The OAuth redirect uses the public `AUTH_URL` and approved callback origin.
6. Protected workspace and API routes reject anonymous access.
7. Railway logs contain no startup, environment validation, database, provider-configuration, or backend-readiness errors.

If validation fails, stop traffic promotion where available, preserve the
failing deployment logs, and set the Railway Docker Image source back to the
last known-good immutable GHCR digest. Do not rebuild that rollback artifact or
"fix forward" a financial, authorization, or data-contract failure while
leaving the unsafe release active.

## 7. Release record

Capture these items in the release note or handoff:

- commit, immutable GHCR digest, and deployment ID;
- public domain and validation timestamp;
- release-gate output;
- preflight, manual-index, and post-apply index-verification evidence;
- routes manually exercised and viewport classes used;
- known backend constraints or intentionally disabled actions;
- auth provider and callback verification result;
- last known-good rollback digest.

The release is complete only when the deployed artifact, not merely the local tree, passes the health, auth, and representative route checks.
