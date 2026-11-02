---
type: runbook
track: mvp-readiness-loop_20261026
date: 2026-11-01
status: blocked-before-publication-and-deployment
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment release evidence and rollout

Writer-pass snapshot after the node review-branch push on November 1; supplements
[the implementation plan](PAYMENT-COMPLETION-2026-11-01.md). Arda source is now in
local commits `c760b51` (backend) and `d4faa5a` (frontend), on
`codex/payment-integration-closeout-20261101`; neither is pushed or released.
Node source is committed and pushed at `1f83837b` on its review branch; its main
branch is unchanged and no deployment was triggered. Independent reviews approved the implementation
and repair batches. **Local implementation verification is complete:** the independently parsed latest
results resolve all 13 original node integration failures and include four new
regression cases. Final release review, review-branch CI and operational
release gates remain separate; no single green full rerun is claimed.

Publication, shared-data migration, protected-file transfer, payment deployment
and activation have **not** been performed. Commands below are proposed future
instructions for independent review. Stop on an unfilled placeholder or failed
gate. The feed milestone is already live at `f915686` with CI `41362843685` green;
that evidence does not release payment changes. The owner chose inherited green
CI audit evidence; no fresh local dependency audit is requested.

## Verified evidence and remaining gates

| Area | Completed evidence | Remaining gate |
| --- | --- | --- |
| SDK 0.6 | Typecheck/build passed; full run 643 passed/one parity expectation failed; repaired parity file 38/38 passed, resolving all 644 original cases without a full repeat | npm login and publication |
| Arda Application | Full run 1,604 passed/two fixture failures/one existing skip; fixture-only repair 14/14 passed, resolving 1,606 unique cases plus the skip | Release CI and shared migration |
| Arda API | Full run 650 passed/two fixture failures; fixture-only repair 41/41 passed, resolving 652 unique cases | Release CI and deployed readiness |
| Backend payment safety | All 29 new cases passed, including real PostgreSQL immutable-command, deadline and race cases | Released cross-node journey |
| Arda client | Full run 1,028 passed/one outdated pin-diagnostic assertion failed; repaired affected files 57/57 passed across seven files, including three actual SDK permission-recovery cases; typecheck/build/vocabulary/format/diff passed | Published-package clean CI build |
| Client lint | Full repair run found only an unnecessary test assertion; removed it and that file passed ESLint/Prettier. Four existing unused-variable warnings remain | No unresolved lint error; earlier full command was not exit zero |
| Node unit | Full run 5,669 passed/one architecture failure; reviewed production CAS repair and targeted architecture 50/50 passed, resolving 5,670 unique cases | Review-branch CI and release review |
| Node frontend | Lint/typecheck/build passed; 326/326 tests across 59 files passed | Deployed hosted-flow proof |
| Node integration | Full run: 1,188 passed/13 failed/one known G3 skip in 61m 08s; targeted batches 13 passed/five failed, then six passed/one failed, then one passed/zero failed. Latest outcome across 1,206 unique cases: **1,205 passed, zero failed, one existing skip** | Final review-branch CI, release approval and deployed journey; no repeated full green sweep |
| Node Ethereum prerequisites | Pinned `npm ci --no-audit` and artifact build passed; 12 contract tests passed in 55.3s. All three Ethereum HTTP protocol tests then passed in the targeted batch | No unresolved Ethereum setup failure in that batch |
| Cash-out fence races | All four actual `CashOutRequestVoidRaceTests` passed in the full run; 16 paired races exercised | Released drain/activation gate remains separate |
| Protected files | Read-only inventory found one live Data Protection XML key, 1,001 bytes, under `/app/data/data-protection-keys`; API has no persistent volume | Encrypted backup, complete configured-path inventory, preseed/transfer and restart proof |
| Preservation tooling | Reviewed transfer wrapper passed 16 synthetic cases; native Railway SSH harmless nonce stream passed. Continuity helper passed synthetic positive/negative checks | Real key export was rejected by automatic approval review; exact owner authorization is pending. No real backup or transfer occurred |

The first full integration run collected all failures before repair. The first
targeted batch now verifies all six `ConsentLoginCutFlowTests`, both store-family
inventory checks, all three Ethereum protocol cases, the actual Fiat THROW/
rollback guard and the member-store disabled/stale guard (four variants). These
results replace the earlier inventory, missing-Ethereum and login-cut-fixture
failures; they do not convert the full run's exit code into success.

The handoff presence repair is now verified: explicit presence flags and the
non-null datetime placeholder distinguish raw SurrealDB `NONE` from a bound null
without weakening strict validation. The targeted handoff run passed six of
seven cases. Its last claim-proof fixture now supplies the required approved KYB;
the final one-case rerun passed. It verifies tenant void receives typed
`409 REQUEST_KEY_IN_USE`, first-party void and replay receive `200`, and grant
replay returns the exact original grant. All four member CAS predicates remain intact.
Four new regression cases, including positive claimed renewal, are included in
the unique-case evidence; changed login-cut denial is verified as well.

The evidence sequence is full `1188/13/1` (pass/fail/skip, 1,202 cases), targeted
`13/5/0` (18), handoff `6/1/0` (seven), final claim proof `1/0/0` (one).
The root independently parsed each TRX and retained the latest result per case:
1,206 unique cases, 1,205 passing, zero unresolved failures and one known skip.
This is cumulative full-plus-targeted evidence, not a green rerun of the full
suite. No failing case was suppressed or newly skipped.

Known explicit skips are Arda treasury reconciliation and the node G3 analyzer
integration case. Local cross-repository execution gates are resolved; publication
and clean registry adoption, release CI, protected-key continuity, shared-data
migrations and deployed end-to-end validation remain pending.

Evidence is local and gitignored:

- `.omc/research/payment-arda-final-results-2026-11-01.json` and `payment-arda-repair-results-2026-11-01.json`.
- `.omc/research/payment-client-final-verification-2026-11-01.json`, `payment-client-final-pin-exits-2026-11-01.json` and the seven-file targeted test log.
- `.omc/research/payment-node-frontend-repair-exit-codes-2026-11-01.json` and `payment-node-frontend-test-repair-2026-11-01.log`.
- `.omc/payment-node/.omc/research/payment-node-unit-final-2026-11-01.log`, `payment-node-unit-repair-2026-11-01.log`, `payment-node-integration-repair-2026-11-01.log` and `payment-node-integration-targeted-2026-11-01.log`. Final/targeted TRX files are under `payment-node-final-results/` beside those logs.
- `.omc/payment-node/.omc/research/payment-node-handoff-final-2026-11-01.log` and `payment-node-handoff-claim-proof-2026-11-01.log`, with matching TRX files in `payment-node-final-results/`.
- `.omc/research/payment-node-independent-final-evidence-2026-11-01.json`: independently parsed run counts, latest outcomes for 1,206 unique cases, zero unresolved failures and the exact existing skip.
- `.omc/payment-node/.omc/research/payment-ethereum-install-repair-2026-11-01.log`, `payment-ethereum-build-repair-2026-11-01.log` and `payment-ethereum-test-repair-2026-11-01.log`.
- `.omc/research/payment-key-ring-wrapper-native-synthetic-evidence-2026-11-01.json` and `payment-continuity-probe-evidence-2026-11-01.json`; these identify synthetic evidence, not a real key backup.
- `.omc/research/payment-N-peer-review-2026-11-01.md` and parent/security review messages cover the implementation and verified repairs; release approval still requires CI and operational gates.

Verification used scratch PostgreSQL databases on port 5433 and dedicated
`codex-payment-surreal-c17ff8f2` on port 8127. Both
`AZOA_TEST_SURREAL_ENDPOINT` and `AZOA_SURREALDB_CONTAINER_NAME` were set. No shared
database or demo node on port 8020 supplied this evidence.

## 1. Freeze approved inputs

Arda backend `c760b51` and frontend `d4faa5a` are reviewable local source commits
on `codex/payment-integration-closeout-20261101`, based on released feed
`f915686`. The main manifest/lock still select SDK 0.5; all new frontend execution
evidence used the isolated actual 0.6 overlay, not an ordinary clean install.
Node commit `1f83837ba746a0dfd2dbfe5f270a07f05db4dfdd`, based on `c7cfa4af`, is
pushed to [the node review branch](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/tree/codex/payment-integration-closeout-20261101).
All 103 reviewed files matched the final hash attestation before commit,
including the member-handoff DTO. The node main branch is unchanged; this push
did not deploy. [Draft PR #3](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/pull/3)
is open, and [PR CI run 41377213143](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/actions/runs/41377213143)
is running. The redundant push run `41377039546` was canceled after the PR run
started. Record the retained run's result before release promotion. Arda source remains local and unpushed at this
snapshot. Preserve the original dirty AZOA checkout and owner-held worktree.

The release ledger must name project/environment/service UUIDs, old/new image
digests, backup IDs/hashes, schema-job deployment ID, health observations, flag
changes and approvers. Never print credentials or protected file contents.

## 2. Publish the exact SDK, then adopt it on Linux

Prepared archive:
`.omc/artifacts/payment-sdk/publication/azoa-sdk-0.6.0.tgz`, SHA256
`7431db9e1a5bfc5e628b8764f0371d2128c8fa68b6e41442032542d9d48ec3fd`.
Tested runtime archive SHA256:
`f410c323d3be601c58aae96a99882fb4b571ef00f820a19211343ffb6a6a9197`.
All 123 payloads were compared: only `package/README.md` differs; runtime and
`package.json` are byte-identical. Proof:
`.omc/research/payment-sdk-publication-artifact-2026-11-01.json`.
The updated source changelog is excluded by the existing package file list.

npm authentication still returns `E401`. The web-login CLI attempt ended without
authentication; manual owner login is pending, and nothing has been published.
After execution gates and publication approval, the owner uses an interactive
terminal without pasting tokens into chat or logs:

```powershell
# Workspace root. Verify the full archive hash above before publication.
npm login --registry=https://registry.npmjs.org
npm whoami --registry=https://registry.npmjs.org
$sdkArchive = '.omc/artifacts/payment-sdk/publication/azoa-sdk-0.6.0.tgz'
Get-FileHash -Algorithm SHA256 -LiteralPath $sdkArchive
npm publish $sdkArchive --registry=https://registry.npmjs.org --access public --ignore-scripts
npm view azoa-sdk@0.6.0 version dist.integrity dist.tarball --json
```

Require registry download/integrity comparison to the prepared archive. Only
then, in a clean **Linux** checkout of the reviewed Arda release:

```bash
cd ardanova-client
npm install --package-lock-only --ignore-scripts --no-audit 'azoa-sdk@^0.6.0'
node scripts/verify-azoa-sdk-pin.mjs
npm ci --no-audit
npm run verify:sdk-pin
git diff -- package.json package-lock.json
```

Review/commit manifest and lock together and require registry-based release CI.
Current main remains on 0.5 intentionally; its isolated 0.6 overlay proves local
compatibility only. Do not invent integrity hashes, use a vendor/symlink fallback,
or change the Windows main installation. Clean dependency/CI verification after
the lock change is a new adoption gate, not a repeat of the completed local sweep.

## 3. Preserve protected files and back up data

Complete this before the first node API restart, volume attachment or promotion.
The live key ring is ephemeral; mounting an empty volume over `/app/data` would
hide the existing files. Follow the separately reviewed preservation procedure:

1. Inventory every key/revocation and every configured conformance identity or checkpoint path. Do not infer that other paths are empty from the `/app/data` listing.
2. Take an encrypted, verified backup of the current bytes. Preseed a volume using an isolated maintenance helper with no public endpoint. Railway file upload requires an attached active service; do not assume direct unattached-volume seeding.
3. Recompare the live inventory/hashes immediately before cutover; transfer the populated volume to the API before startup. Preserve UID 1654, private files at mode 0600 and exact `DataProtection__ApplicationName`.
4. Prove that a protected probe made before cutover survives restart. The continuity helper is reviewed and synthetic-tested only; no live key-continuity probe or transfer has run. A harmless live SSH nonce-stream check proved transport, not key recoverability or restart continuity.

The reviewed transfer wrapper SHA256 is
`d5856ec3dec909faeb847b77251d31ac30e7ab282d187eb27b2c1c35e09ab74e`.
Its 16 passing cases use synthetic data, including synthetic DPAPI recoverability.
The reviewed continuity helper SHA256 is
`996e048e0bb1a94b91cdf29ce52b3e732df5c38d2aae6beb343592beffb7d73f`;
its positive/negative checks likewise do not access live keys.

Automatic approval review rejected exporting the real key ring into the local
DPAPI-encrypted backup destination `.omc/private/payment-key-preservation`,
because authorization had not explicitly named that sensitive key and
destination. The exact owner authorization question is pending. The rejected
export was not executed: no real key is backed up, no new volume or maintenance
helper has been created, and no key-preservation redeployment has happened.
Stop at this gate until authorization and the reviewed preservation procedure
are both in place; synthetic or harmless-channel success does not substitute.

Take separately identified SurrealDB `/data` and PostgreSQL backups and prove
restoration into disposable targets. Record time, size, hash and restored
schema/row checks. A backup catalog alone is not restore proof. Node
`scripts/surrealdb/backup.ps1` and `restore.ps1` print password-bearing arguments;
use a provider snapshot or reviewed nonlogging export/import for live credentials.

An approved PostgreSQL operator shell may use the following after loading the
exact target's libpq `PG*` variables from its secret store; never echo them:

```bash
umask 077
psql -X -v ON_ERROR_STOP=1 -Atc 'SELECT current_database(), current_user, inet_server_addr(), inet_server_port();'
pg_dump --format=custom --no-owner --file="$RELEASE_DIR/ardanova-before-payment.dump"
sha256sum "$RELEASE_DIR/ardanova-before-payment.dump"
pg_restore --list "$RELEASE_DIR/ardanova-before-payment.dump" > "$RELEASE_DIR/ardanova-backup-catalog.txt"
# Separately approved disposable restoration is required before migration.
```

Preserve SurrealDB image/storage/synchronous-write settings. Do not rotate the
AZOA value key while P6-03 is open or regenerate conformance identity to bypass
startup. File preservation is separate from database backup.

## 4. Migrate first and promote in dependency order

Use AZOA `deploy/railway/DEPLOY.md` and `RUNBOOK.md` section 2. Its protected
GitHub promotion runs `deploy/railway/serial-rollout.py` with explicit project,
environment and every service UUID, plus attested image digests. Choose
`--surreal-mode already-healthy` for the preserved database. Require this order:

1. Existing SurrealDB health and latest deployment `SUCCESS`.
2. `azoa-schema` from the proposed API digest: terminal `SUCCESS` and `Schema job completed successfully`.
3. `azoa-api` from that same digest: terminal `SUCCESS`, `/health`, preserved-file continuity proof.
4. Paired `azoa-frontend` digest: terminal `SUCCESS` and `/api/health`.

Only the schema job receives owner credentials. API uses its database-scoped
runtime account with `AZOA_SKIP_MIGRATIONS=1`. Do not force checksums, reconcile
old schema destructively or migrate during production API boot. New generated
tables include `member_handoff` and `fiat_payout_request_fence`.

Arda migration `20261101000000_sale_admission_proof` adds immutable payout
request/quote/command/deadline and denomination fields, `PayoutSaleQuote`,
`PayoutSaleVoid` and subject/key unique indexes. Review **all** pending migration
SQL against the backed-up catalog; `migrate deploy` applies the entire pending
list. Existing databases retain the baseline/catalog gates in
[the commerce runbook](../../../documentation/GATED_COMMERCE_MIGRATION_RUNBOOK.md).

```bash
# Clean Linux Arda checkout; approved migration job injects DATABASE_URL.
cd ardanova-client
npx prisma migrate status --schema prisma/schema.prisma
npx prisma migrate deploy --schema prisma/schema.prisma
npx prisma migrate status --schema prisma/schema.prisma
```

Record the migration ledger and independently verify columns/tables/indexes.
Never use shared-data `db push`, `migrate dev` or automatic baseline resolution.
Keep the old compatible application serving until its additive migration gate
passes. Then follow [the Arda release playbook](../../../documentation/FRONTEND_RELEASE_PLAYBOOK.md)
section 6: obtain Git CI's `ardanova-release-images-<commit>` manifest, promote the
API digest and require terminal `SUCCESS` plus `/ready`, then the paired client
digest and require terminal `SUCCESS` plus `/api/health`. Record deployment IDs
and observed digests. Use validated Git CI images; no `railway up` or workstation
source upload.

Check browser-visible HTTPS `AUTH_URL` behind Railway's internal request URL;
same-origin mutations work and another origin is rejected before body reading.
BFF `NEXTJS_URL` must produce the exact allowlisted `/portfolio/withdraw` return
with no query/fragment. Health does not prove rights, provider or bank readiness.

## 5. Activate only the separately proven capabilities

| Configuration | Posture until its gate passes |
| --- | --- |
| Node `Tenant__ClaimInvites__Enabled` | `false` until hosted claim/renewal, delivery and return proof |
| Node `Tenant__ClaimInvites__StubDeliveryAcknowledged` | Explicit acknowledgement of the protected operator stub; never imply email was sent |
| Node `Tenant__ClaimInvites__HostedBaseUrl` / `AllowedReturnOrigins` | Exact reviewed HTTPS origins; only `/portfolio/withdraw` return path |
| Node `Fiat__OffRamp__RequestVoidEnabled` | `false` until all old unfenced API/worker processes drain and fence admission/reveal proof passes |
| BFF `Azoa__ClaimInvites__Enabled` | `false` until node and consumer deployment plus hosted journey proof |
| KYC/provider/value switches | Unavailable capabilities stay closed; Identomat readiness, valid consent and Stripe TEST prerequisites remain separate |

Stage the full journey with a real test member: resend retires old links;
operator delivery reveals no secret through Arda; claim/login obtains fresh owner
proof; exact stored scopes are approved/declined; setup uses the member's node
session; fixed return completes; current consent and bank readiness are read
separately. Check cross-tenant denial, expired/declined renewal, lost replies,
same-key retries, quote expiry, void/admission races and revocation after a
possibly accepted payment. Unknown outcomes retain keys. Enable one approved
capability at a time and record observations. No flag substitutes for KYC/KYB
evidence. This procedure does not authorize real-money activation.

## 6. Stop and recover without replaying value

A failed backup/restore, key continuity, schema, digest, readiness or rights gate
stops promotion; leave later services and new capability flags unchanged. Restore
prior compatible API/frontend digests when appropriate; schema is forward-only.
Data restoration is a separately approved maintenance action, not an automatic
application rollback.

Retain exact keys, named records, quotes, deadlines and protected evidence for
possibly admitted requests. Reconcile by the same key or authoritative
void-or-reveal, never by clearing browser data or guessing absence. A rollback
must retain admission fencing after any `voided` receipt has been issued;
turning the flag off does not make unfenced old writers safe. Preserve the
key ring and identity history through rollback. Close the release only after
independent evidence review and the deployment/activation ledger are complete.
