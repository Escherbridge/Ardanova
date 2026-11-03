---
type: runbook
track: mvp-readiness-loop_20261026
date: 2026-11-01
updated: 2026-11-03
status: awaiting-database-recovery-and-payment-release-gates
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment release evidence and rollout

Writer-pass snapshot updated November 3 UTC; supplements
[the implementation plan](PAYMENT-COMPLETION-2026-11-01.md). Combined Arda
[draft PR #9](https://github.com/Escherbridge/Ardanova/pull/9) is green at exact
head `072585db831a93914d80452d112a387805f71383`, CI `41462170432`. Its backend
and published-SDK frontend validators passed; three image jobs were skipped as
a PR. Backend-only PR #8 is closed as superseded. The pushed combined branch
contains frontend `d4faa5a`, backend harness correction `83f38c9`, ledger
`63b119e`, SDK adoption `62d3e52` and reviewed documentation `072585db`.
Node source is committed and pushed at
`f1b8824b30901cfb2073f2e14e9e08f2219b33e0`, including the reviewed 16-file hosted
handoff/login-validator repair. Current PR CI `41449375013` is **SUCCESS with all
nine jobs successful**, including full current-head .NET suites. Its Playwright job provides a
**single current-head run of eight passing cases in 1.8 minutes**, including all
three new handoff journeys. Prior head `1f83837b` retains fully green CI
`41377213143`, including full .NET suites. These are distinct head-specific
results. The new Arda payment UI now has full combined-head CI proof against
published SDK 0.6.0: 1,035 tests across 111 files, typecheck, lint, guards and build
passed. Earlier overlay and clean Linux install/import evidence remain retained.
The deployed
Arda-to-node-to-bank withdrawal journey and operational gates remain open.

**Protected storage A1/A2 is CLOSED with independent C approval.** API deployment
`3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d` is SUCCESS on the verified public exact-c7
maintenance image, with protected volume `3c9b266d-586b-495b-98d1-6113e981f1f7`
persistently mounted at `/app/data`. The original key and original `9098` challenge
survived an observed controlled restart under UID/GID `1654` with zero supplementary
groups; health is `200 Healthy`. Section 3 records image IDs, process ticks and
receipts. Helper `a6863596-908b-4671-ac80-6ebe03306e39` remains idle without a mount
for later probe work; staging is empty. Earlier rejected attachment/staging
experiments are historical, not current blockers. Runtime remains source `c7cfa4af`,
Staging and `AZOA_SKIP_MIGRATIONS=1`; Node PR3 `f1b8824b` is not deployed.
Database recovery, shared-data migrations, payment deployment and real-money
activation remain open; the storage operation changed none of them.
The owner authorized all planned engineering; routine choices proceed, and owner
choices are recorded in the current [decision ledger](DECISION-LEDGER.md).
Unexecuted commands below are proposed future
instructions for independent review. Stop on an unfilled placeholder or failed
gate. The feed milestone is already live at `f915686` with CI `41362843685` green;
that evidence does not release payment changes. The owner chose inherited green
CI audit evidence; no fresh local dependency audit is requested.

## Verified evidence and remaining gates

| Area | Completed evidence | Remaining gate |
| --- | --- | --- |
| SDK 0.6 | Node CI `41449375013`: **646 tests across 45 files**. Exact public archive/SRI and Linux adoption `62d3e52` verified; local clean install/lifecycle/ESM+CJS proof and full combined Arda registry CI `41462170432` passed | Released deployment |
| Arda Application | Combined PR #9 CI `41462170432`: **single full run 1,606 passed, zero failed, one existing skip out of 1,607**, 3m 4s | Shared migration, release-image publication and deployed readiness |
| Arda API | Same combined CI: **single full run 652 passed, zero failed/skipped**, 3m 32s | Shared migration, release-image publication and deployed readiness |
| Backend payment safety | All 29 new cases passed, including real PostgreSQL immutable-command, deadline and race cases | Released cross-node journey |
| Arda client | Combined exact-head CI: **1,035 tests/111 files passed**, 49.98s; fresh registry `npm ci`, SDK/platform/vocabulary/diff/format checks, typecheck and 48-page Next build passed. Earlier overlay and isolated Linux 603-package lifecycle/import proofs retained | Immutable release-image publication and deployed checks |
| Client lint and production audit | Combined CI release gate passed lint with four existing unused-variable warnings; ordinary CI `audit:prod` reported zero vulnerabilities | No new local audit was run; production audit result does not describe all development dependencies |
| Node unit | Current-head CI `41449375013`: **single full run 5,687 passed, zero failed/skipped**, 1m 7s. Local login-validator evidence separately passed 20 cases, including 17 new | Release and operational gates |
| Node frontend | Current-head CI lint/typecheck/build passed; **326/326 tests across 59 files passed** | Deployed journey |
| Node browser slices and historical CI | Current-head Chromium job: **24 tests across eight files passed**. Prior Playwright job had five passes in 38.0s; the current full eight-case result is separate below | Slice/historical suites alone do not exercise the new member-handoff/claim/consent pages |
| Hosted handoff browser proof | Current-head CI `41449375013` completed **eight of eight tests in one run**, 1.8m: three new handoff cases, sign-in shell, signup/KYC, on-ramp, KYB and webhook. Local proof separately accumulated three existing plus two new plus final renewal passes | Full deployed Arda-to-node-to-bank journey remains unproven |
| Windows E2E setup and privacy | Verified Compose 5.6.1/private current-user pipe and four healthy owned containers; fake-provider HTTP 200 and three negative TLS controls passed. Privacy canary found no password/ARIA/media leak. All owned containers, volumes, networks, generated task directory and forwarding children were cleaned up; other containers unchanged | Local resources are closed; no production activation implied |
| Node integration | Current-head CI `41449375013`: **single full normal integration run 1,205 passed, zero failed, one existing G3 analyzer skip out of 1,206**, 49m 14s | Release approval and deployed journey |
| Node conformance gates | Same current-head CI: G1/G2/G3 each two passed; G5/G7 each one passed, no failures or skips in those gate runs | These do not erase the separately recorded normal-integration analyzer skip or prove browser journeys |
| Node Ethereum prerequisites | Pinned `npm ci --no-audit` and artifact build passed; 12 contract tests passed in 55.3s. All three Ethereum HTTP protocol tests then passed in the targeted batch | No unresolved Ethereum setup failure in that batch |
| Cash-out fence races | All four actual `CashOutRequestVoidRaceTests` passed in the full run; 16 paired races exercised | Released drain/activation gate remains separate |
| Protected files | **CLOSED A1/A2:** original one-file 1,001-byte key/hash preserved on persistent volume `3c9b` at `/app/data`, file `0600`/ring `0700`, UID/GID `1654`; owner-authorized encrypted backup/recovery retained | Closed for the observed deployment/restart; refresh inventory and protect recovery material for future upgrades |
| Continuity proof | Wrapper `c056985b...efb3a`: **53 Windows + six actual Linux checks passed**. Same original `9098` challenge verified before/after actual API restart as UID/GID `1654`, zero supplementary groups; DLL and transfer unchanged | Independent C closure recorded; this is storage continuity, not deployed payment/provider proof |
| Preservation infrastructure | API `3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d` SUCCESS at public c7 image index `f001e8c5...b1934`, persistent volume `3c9b266d-586b-495b-98d1-6113e981f1f7`; helper `a6863596-908b-4671-ac80-6ebe03306e39` idle/unmounted; staging empty | Retain the helper for later probe work. Do not replay the historical rejected skip-deploy/staged-mount procedures |
| Database backups | PostgreSQL `55258d25-f404-4dec-acaa-54ada95a6e11` and SurrealDB `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` catalog records created at 12:59 UTC November 2 | Workflow status query returned Not Authorized; catalog existence is neither completed-backup nor disposable-restore proof |
| PostgreSQL migration preflight | PG 17.11 read-only catalog and independent Git-blob checks: **27 applied checksums MATCH**, one pending `20261101000000_sale_admission_proof`, Linux SHA256 `92adcf8e3617eac53e539545794d2839b7795a2ca03d248dc366343fbcf75bf8`; SQL/catalog compatibility approved | Recoverability remains open; recheck pending list and apply only through the reviewed migration job after that gate |

The original local full integration run collected all failures before repair. Its
targeted batch verified all six `ConsentLoginCutFlowTests`, both store-family
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

The earlier local evidence sequence is full `1188/13/1` (pass/fail/skip, 1,202 cases), targeted
`13/5/0` (18), handoff `6/1/0` (seven), final claim proof `1/0/0` (one).
The root independently parsed each TRX and retained the latest result per case:
1,206 unique cases, 1,205 passing, zero unresolved failures and one known skip.
That local result was cumulative full-plus-targeted evidence, not a green local
full rerun. The later remote PR CI independently supplied the single successful
full normal-integration and unit runs recorded above. No failing case was
suppressed or newly skipped.

Arda's first backend PR CI `41379404571` failed only because the older feed
registration test omitted the existing test-only provider-count warning helper.
The two-file harness/documentation correction preserves production registration,
real Npgsql/enum configuration, scope validation and all assertions. Its 11-case
targeted class passed; the corrected full CI `41381713622` then passed Application
and API suites. No production warning policy was changed.

Known explicit skips are Arda treasury reconciliation and the node G3 analyzer
integration case. The API/SDK and existing UI-test gates have passing local
evidence and successful current-head PR CI, including full .NET and browser jobs.
These CI results alone do not resolve operational gates. Separate evidence now
closes protected-storage/API restart continuity; payment release-image publication,
database recoverability, shared migrations and deployed end-to-end validation remain.

### Hosted browser coverage boundary

Current-head Playwright job `125290468008` in CI `41449375013` completed at
13:07 UTC on November 2: **eight passed in one 1.8-minute run**. Its three new
handoff tests cover:

1. Claim/resend, with the fragment scrubbed before preview, exact persisted grant approval and the fixed return URL.
2. Loss of the claim reply after server commit, followed by recovery through the member's own first-party login.
3. Tokenless renewal decline that preserves a prior independent grant.

The cases took 1.1 minutes, 4.3 seconds and 5.0 seconds respectively. The same run
also passed the sign-in shell and four existing signup/KYC, Stripe on-ramp,
tenant KYB and webhook journeys. The downloaded completed-job log supplies this
proof; the overall run is now successful with all nine jobs. The old five-test CI and
24-test Chromium slices remain separate historical regression evidence.

Local browser proof was cumulative: three existing regressions passed, two new
handoff cases passed in a later run, and the final renewal case passed after its
repair. It was not a single green six-test local run. The local webhook test was
excluded because the unchanged SSRF policy rejects its receiver subnet; the
current-head remote eight-test run now includes that webhook test successfully.
All 20 local login-validator cases passed, including 17 new cases. The privacy
canary observed its intended failure and verified no raw/encoded password, ARIA
snapshot, trace, screenshot or video remained.

Windows setup required a verified official Compose 5.6.1 provider and an ignored
`CurrentUserOnly`/owner-guarded task pipe, with actual Podman 5.7.1/API 1.41 checks.
The owned stack reached four healthy containers; positive fake-provider HTTP 200
and three negative TLS controls passed. The final cleanup proves zero owned
containers, volumes, networks, forwarding children or task pipe remained, and
the generated task directory was removed. Other container identities were
unchanged. No duplicate or persistent local stack is required.

The reviewed 16-file repair is now pushed at `f1b8824b`, including the login
validator and hosted UI changes; it has not been deployed or opened production
flags. Neither this browser proof nor healthy test providers proves the complete
deployed Arda-to-node-to-bank-to-withdraw journey. That still depends on SDK
publication/adoption, protected storage, migrations, deployment and legitimate
provider readiness.

Evidence is local and gitignored:

- `.omc/research/payment-arda-final-results-2026-11-01.json` and `payment-arda-repair-results-2026-11-01.json`.
- `.omc/research/payment-client-final-verification-2026-11-01.json`, `payment-client-final-pin-exits-2026-11-01.json` and the seven-file targeted test log.
- `.omc/research/payment-node-frontend-repair-exit-codes-2026-11-01.json` and `payment-node-frontend-test-repair-2026-11-01.log`.
- `.omc/research/payment-node-ci-sdk-41377213143.log`, `payment-node-ci-frontend-41377213143.log`, `payment-node-ci-browser-41377213143.log` and `payment-node-ci-e2e-41377213143.log`: downloaded completed CI job logs, with the coverage limits stated above.
- `.omc/research/payment-node-ci-dotnet-41377213143.log`: completed full unit/normal-integration and conformance-gate CI results.
- `.omc/research/payment-node-ci-dotnet-41449375013.log`, `payment-node-ci-sdk-41449375013.log`, `payment-node-ci-frontend-41449375013.log`, `payment-node-ci-browser-41449375013.log` and `payment-node-ci-e2e-41449375013.log`: downloaded completed current-head jobs, including single full unit/normal-integration and eight-case browser runs; all nine CI jobs succeeded.
- `.omc/payment-node/.omc/research/payment-handoff-final-candidate-2026-11-02.json`, `payment-login-validator-results/payment-login-validator-2026-11-02.trx`, `payment-handoff-browser-repair-2026-11-02.log` and `payment-renewal-browser-final-2026-11-02.log`: reviewed 16-file repair, 20 validator passes and separate local browser results.
- `.omc/payment-node/.omc/research/payment-handoff-privacy-canary-proof-2026-11-02.json` and `payment-e2e-cleanup-proof-2026-11-02.json`: canary privacy and owned-resource cleanup.
- `.omc/research/payment-arda-backend-ci-api-41381713622.log`: corrected full Application/API CI results; the original `payment-arda-backend-ci-api-41379404571.log` retains the one harness failure.
- `.omc/research/payment-arda-combined-ci-proof-2026-11-03.json`, `payment-arda-combined-ci-41462170432.json`, `payment-arda-combined-ci-api-41462170432.log` and `payment-arda-combined-ci-client-41462170432.log`: exact 072585db terminal combined CI, full Application/API/client counts and actual release-check coverage. Three image-publication/record jobs were skipped as a PR.
- `.omc/research/payment-backend-ci-feed-projector-repair-evidence-2026-11-01.json` and matching TRX/log: all 11 targeted harness cases passed before the corrected CI push.
- `.omc/payment-node/.omc/research/payment-node-unit-final-2026-11-01.log`, `payment-node-unit-repair-2026-11-01.log`, `payment-node-integration-repair-2026-11-01.log` and `payment-node-integration-targeted-2026-11-01.log`. Final/targeted TRX files are under `payment-node-final-results/` beside those logs.
- `.omc/payment-node/.omc/research/payment-node-handoff-final-2026-11-01.log` and `payment-node-handoff-claim-proof-2026-11-01.log`, with matching TRX files in `payment-node-final-results/`.
- `.omc/research/payment-node-independent-final-evidence-2026-11-01.json`: independently parsed run counts, latest outcomes for 1,206 unique cases, zero unresolved failures and the exact existing skip.
- `.omc/payment-node/.omc/research/payment-ethereum-install-repair-2026-11-01.log`, `payment-ethereum-build-repair-2026-11-01.log` and `payment-ethereum-test-repair-2026-11-01.log`.
- `.omc/research/payment-key-ring-wrapper-native-synthetic-evidence-2026-11-01.json`, `payment-continuity-probe-evidence-2026-11-01.json` and `payment-key-continuity-wrapper-synthetic-evidence-2026-11-02.json`: synthetic transfer/probe checks and final 50-case wrapper run. Real backup metadata and live challenge/receipts remain under owner-only `.omc/private/payment-key-preservation` and `.omc/private/payment-key-continuity`; do not print their payloads.
- `.omc/research/payment-key-preservation-resources-2026-11-02.json`, `payment-old-api-image-identity-2026-11-02.json`, `payment-backup-new-metadata-2026-11-02.json` and `payment-backup-status-2026-11-02.json`: helper/volume and old-image identities, new snapshot catalog entries and unavailable workflow status. These are not seed/cutover/restore proof.
- `.omc/research/payment-key-seeded-volume-proof-2026-11-02.json`: historical successful one-file seed and matching inventory/hash/ownership/modes; current API persistence/restart closure is recorded below.
- `.omc/research/payment-api-a2-post-restart-proof-2026-11-03.json` and `payment-protected-storage-C-final-closure-20261103.json`: actual API mount, restart, same original challenge/key, UID/GID/groups and health; independent C closed A1/A2.
- `.omc/research/payment-continuity-appuid-final-proof-20261103.json`: 53 Windows and six actual Linux checks, exact scoped fixture cleanup, unchanged DLL and the UID-safe wrapper hash.
- `.omc/research/payment-live-pg-pending-migration-proof-2026-11-03.json`, `payment-pg-release-blob-checksum-review-20261103.json`, `payment-pending-pg-catalog-preflight-result-20261103.log` and `payment-pending-pg-sql-review-20261103.md`: exact released SQL bytes, all 27 applied checksums, actual read-only catalog and compatibility review; no migration execution.
- `.omc/research/payment-sdk-registry-integrity-proof-2026-11-02.json`, `payment-sdk-adoption-candidate-proof-2026-11-02.json`, `payment-sdk-linux-clean-install-import-proof-2026-11-02.json`, its install/hash logs and `payment-sdk-adoption-container-cleanup-2026-11-02.json`: exact public archive, reviewed Linux adoption, actual lifecycle/import proof and owned-container removal.
- `.omc/research/payment-N-peer-review-2026-11-01.md` and parent/security review messages cover the implementation and verified repairs; release approval still requires CI and operational gates.

Verification used scratch PostgreSQL databases on port 5433 and dedicated
`codex-payment-surreal-c17ff8f2` on port 8127. Both
`AZOA_TEST_SURREAL_ENDPOINT` and `AZOA_SURREALDB_CONTAINER_NAME` were set. No shared
database or demo node on port 8020 supplied this evidence.

## 1. Freeze approved inputs

[Combined draft PR #9](https://github.com/Escherbridge/Ardanova/pull/9), branch
`codex/payment-integration-closeout-20261101`, is pushed at
`072585db831a93914d80452d112a387805f71383`.
[CI 41462170432](https://github.com/Escherbridge/Ardanova/actions/runs/41462170432)
is terminal **SUCCESS**: both full backend and public-SDK frontend validation
passed. Three image-publication/record jobs were skipped as a PR. This is the
exact combined source containing frontend `d4faa5a`, harness repair `83f38c9`,
ledger `63b119e`, SDK adoption `62d3e52` and reviewed docs `072585db`.
Backend-only [PR #8](https://github.com/Escherbridge/Ardanova/pull/8) is closed as
superseded; its earlier green CI `41381713622` and harness-repair evidence remain
historical. Arda main is unchanged; no Windows dependency installation changed.
Node current commit `f1b8824b30901cfb2073f2e14e9e08f2219b33e0`, based on reviewed
`1f83837ba746a0dfd2dbfe5f270a07f05db4dfdd` and original main `c7cfa4af`, is
pushed to [the node review branch](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/tree/codex/payment-integration-closeout-20261101).
The original 103-file attestation, including the member-handoff DTO, and the new
reviewed 16-file repair are retained. The node main branch is unchanged; this push
did not deploy. [Draft PR #3](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/pull/3)
is open. [Current PR CI 41449375013](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/actions/runs/41449375013)
is **SUCCESS with all nine jobs successful**, including full .NET and new browser coverage.
[Prior-head CI 41377213143](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/actions/runs/41377213143)
is fully green with all nine jobs successful. The redundant old push run
`41377039546` was canceled. PR bodies retain the separate publication,
deployed-journey and operational limits. No PR merge,
release-image promotion or deployment is implied. Preserve the original dirty
AZOA checkout and owner-held worktree.

The release ledger must name project/environment/service UUIDs, old/new image
digests, backup IDs/hashes, schema-job deployment ID, health observations, flag
changes and approvers. Never print credentials or protected file contents.

Normal AZOA promotion has a concrete administration prerequisite. The root's
repository reads found only GitHub environment `ardanova / production`, with no
protection rules; `conformance-promotion` and `railway-production` were not
listed. Node repository secret and variable lists were initially empty. Root
subsequently configured and read back the eight nonsecret target variables below
against the exact Railway service/environment IDs and service-domain reads;
no secret or environment setting changed. Receipt:
`.omc/research/payment-normal-promotion-public-vars-2026-11-03.json`. The existing
normal workflow requires those protected environments, designated required
reviewers, scoped `RAILWAY_TOKEN` and eight variables: `RAILWAY_PROJECT_ID`,
`RAILWAY_ENVIRONMENT_ID`, `RAILWAY_SURREAL_SERVICE_ID`, `RAILWAY_SCHEMA_SERVICE_ID`,
`RAILWAY_API_SERVICE_ID`, `RAILWAY_FRONTEND_SERVICE_ID`, `RAILWAY_API_HEALTH_URL`
and `RAILWAY_FRONTEND_HEALTH_URL`. Provisioning credentials and reviewer identity
needs the appropriate administrator; nonsecret IDs/URLs are routine scoped
configuration, not a blanket engineering reapproval. See PAY-D04 in the current
ledger for reviewer selection. No environment change or token transfer occurred.
Arda's repository secret list was also empty, with only
`NEXT_PUBLIC_AZOA_BASE_URL` listed as a variable; its image workflow uses built-in
`GITHUB_TOKEN` and does not itself deploy, so an empty secret list alone is not
an additional Arda publication blocker.

The isolated exact-C7 maintenance publisher is a distinct publication-only
workflow: it references no GitHub environment, deploys nothing, has no Railway
credential and requires reviewed workflow/source/provenance proof before any
image is accepted. Its branch-only exception does not waive normal production
promotion protections or authorize a private-package visibility change.

## 2. Verified SDK publication and Linux adoption

Prepared archive:
`.omc/artifacts/payment-sdk/publication/azoa-sdk-0.6.0.tgz`, SHA256
`7431db9e1a5bfc5e628b8764f0371d2128c8fa68b6e41442032542d9d48ec3fd`.
Tested runtime archive SHA256:
`f410c323d3be601c58aae96a99882fb4b571ef00f820a19211343ffb6a6a9197`.
All 123 payloads were compared: only `package/README.md` differs; runtime and
`package.json` are byte-identical. Proof:
`.omc/research/payment-sdk-publication-artifact-2026-11-01.json`.
The updated source changelog is excluded by the existing package file list.

npm owner `jadeoni-az` published the exact archive. Public metadata became
available at 13:23 UTC; the coordinator downloaded and independently verified it
at 13:24 UTC. The public tarball SHA256 equals the full reviewed hash above, with
registry SRI
`sha512-5zrnRGC14wV10mHedkUcOkqbTvj9NgIaCCyfQbyvDHT7WahYr1LfFewG+nxedeZvEjxpdovbjNH2hwzemRuxQA==`.
Publication and authentication are resolved; do not republish or repeat login.

The two-file `^0.6.0` adoption was generated in an isolated genuine Linux
Node 20.20.2/npm 11.21.1 container, independently reviewed, then committed as
`62d3e52f9b6339ce409acbf5dc494f2b088d934b`. Only the manifest SDK range, lock root
range and SDK entry changed; all 732 lock package keys remained, with zero other
entry changes. Actual guard commands passed before installation:

```bash
# Completed in isolated Linux staging, not the Windows installation.
npm pkg set dependencies.azoa-sdk=^0.6.0
npm install --package-lock-only --ignore-scripts --no-audit --no-fund --registry=https://registry.npmjs.org
node scripts/verify-azoa-sdk-pin.mjs
node scripts/verify-lockfile-platform.mjs
```

Manifest SHA256:
`b57c34ada3636aa5fbab82e9e7c6ae96138394db1233e418048559535904f645`.
Lock SHA256:
`075eb80650e6111fb739eb2cbe632cb113f9caf57b57d6fc27af3829e4d3ecde`.

A separate clean directory in that container used tracked client source from
`83f38c9` plus the approved overlay, excluding `.env*`, Windows dependencies and
untracked outputs. Registry `npm ci --no-audit --no-fund` installed 603 packages
in about one minute. Client postinstall generated Prisma Client and Zod schemas
and completed the patcher, with a deliberately closed-port fixture datasource
and no database operations requested. Installed pin and all three wasm-binding
guards passed. Actual `azoa-sdk/money` and `azoa-sdk/react` exports imported
through ESM and CommonJS from regular installed SDK 0.6.0, with no symlink/vendor
fallback. npm reported six dependency install-script allowlist warnings; the
project lifecycle and imports passed. This proof does not replace build/tests.

The exact owned container was stopped and removed after its ID/labels matched;
other container identities were unchanged. Host evidence and candidate files
remain. Windows `node_modules` remains untouched. The diagnostic tests were
statically checked: their real-repository expectation reads the current manifest
range dynamically; fixed older versions are intentional isolated fixtures.
No new test repair or broad local rerun was needed. Full combined PR CI
`41462170432` subsequently passed against the public package. Immutable main
release-image publication and the remaining operational gates are still open.

## 3. Preserve protected files and back up data

### Completed protected storage and controlled restart

**A1/A2 is CLOSED**, independently approved by C on November 3. The owner-authorized
one-file DPAPI CurrentUser backup/recovery, exact outbound Seed and fresh inventory
checks preceded the actual API cutover and controlled restart. Preserve the
protected manifest (`2a301d...559c`, abbreviated), encrypted backup and named volume;
completed storage maintenance is not permission to discard recovery material.

Current API deployment is `3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d`, SUCCESS, at:

`ghcr.io/jadezaher/azoa-autonomous-zones-of-action@sha256:f001e8c54bd337b11dcae69a3024df9f793c8361304efce47839b6c1a76b1934`

Its linux/amd64 manifest is
`sha256:434a2d39da60e55626ec0f1312d4b95efd7d2f3e5c348236660280578bbd242e`.
Publication `41555084980` at reviewed workflow `4bfe2eb2` bound exact build source
`c7cfa4af82c99e130a26064054ea7c52ab2603bb`; public pull and provenance were verified.
This maintenance image is not byte-identical to the original private image `2b7`.
All 60 dependency library metadata objects and the actual .NET/ASP.NET 10.0.14
versions matched the old API; the public entrypoint equals exact c7 source.

The 1,024 MB protected volume `3c9b266d-586b-495b-98d1-6113e981f1f7`
(`payment-protected-data-c17ff8f2`) is now persistently mounted on the API at
`/app/data`. The ring remains `/app/data/data-protection-keys`, application name
`AZOA.WebAPI.NodeTransparency.v1`. One original 1,001-byte key is preserved with
SHA256 `dc95ebb839c254e5b519c521e815be6dae22b90d6daf71ebd28ca94827783795`,
file mode `0600`, ring mode `0700` and UID/GID `1654`. Runtime remains Staging with
`AZOA_SKIP_MIGRATIONS=1`. Application source remains c7 and flags are unchanged;
no schema migration or real-money activation occurred. Node PR3 `f1b8824b`
remains undeployed.

The same original challenge `9098ac72ddc74d1e8ef8998f99c3d8ef` verified on the API
before and after the controlled restart, using real/effective UID/GID `1654` and
zero supplementary groups. PID1 start ticks changed from `418222674` to `418252693`
under the same deployment and boot identity; post-restart health was `200 Healthy`.
This is observed API persistence/restart proof, beyond the historical helper proof.
Sanitized execution and independent verification receipts are
[the A2 proof](../../../.omc/research/payment-api-a2-post-restart-proof-2026-11-03.json)
and [C closure](../../../.omc/research/payment-protected-storage-C-final-closure-20261103.json).
Protected challenge and key payloads remain only in owner-private storage.

The UID-safe live wrapper SHA256 is
`c056985b4c1e0a1f8a8c61cd7d0a96691370e33f66b6041aae427222b03efb3a`.
Its 53 Windows checks and six actual Linux cases passed, including real/effective
application identity, zero groups and permission-denial negatives without key
repair. Exact owned test containers are removed. The unchanged probe DLL remains
`996e048e0bb1a94b91cdf29ce52b3e732df5c38d2aae6beb343592beffb7d73f`,
and the unchanged transfer wrapper remains
`d5856ec3dec909faeb847b77251d31ac30e7ab282d187eb27b2c1c35e09ab74e`.
The earlier 50-case wrapper evidence remains historical. Current aggregate proof:
`payment-continuity-appuid-final-proof-20261103.json`.

Helper service `950e8300-8103-488e-af69-e27736374f42` is retained idle at deployment
`a6863596-908b-4671-ac80-6ebe03306e39`, with no mount, for later probe work. All
staging is empty. The protected volume is mounted on the API. Future payment
promotion still requires fresh key/revocation
inventory and review of every configured conformance identity/checkpoint path;
this one-key receipt does not infer that other paths are empty.

Historical receipts retain old API `3177`/private image `2b7` and helper `8265` identities,
the rejected `skipDeploys: true` attachment and the negative empty-volume staged
mount test. Their patches were cleared and the never-used test volume removed.
Those attempts were superseded by the completed pinned-image procedure; do not
replay them. Earlier automatic backup/Seed review rejections were subsequently
resolved by the owner's exact authorizations and successful execution receipts.
Keep remaining owner choices in [DECISION-LEDGER.md](DECISION-LEDGER.md).

### Database recoverability remains open

New catalog entries named `payment-before-integration-2026-11-02` exist for
PostgreSQL backup `55258d25-f404-4dec-acaa-54ada95a6e11` (12:59:23.474 UTC) and
SurrealDB backup `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` (12:59:23.544 UTC).
The workflow-status query returned **Not Authorized**. Catalog entries alone
prove neither terminal backup completion nor restoration. Obtain independently
readable completion evidence and prove restoration into disposable targets;
record time, size, hash and restored schema/row checks before shared migrations. Node
`scripts/surrealdb/backup.ps1` and `restore.ps1` print password-bearing arguments;
use a provider snapshot or reviewed nonlogging export/import for live credentials.

An independently reviewed remote PostgreSQL operator shell may use the following
after loading the exact target's libpq `PG*` variables from its secret store;
never echo them. Keep the dump and row contents in the approved private remote
context, and export only sanitized schema/aggregate proof:

```bash
umask 077
psql -X -v ON_ERROR_STOP=1 -Atc 'SELECT current_database(), current_user, inet_server_addr(), inet_server_port();'
pg_dump --format=custom --no-owner --file="$RELEASE_DIR/ardanova-before-payment.dump"
sha256sum "$RELEASE_DIR/ardanova-before-payment.dump"
pg_restore --list "$RELEASE_DIR/ardanova-before-payment.dump" > "$RELEASE_DIR/ardanova-backup-catalog.txt"
# Independently reviewed disposable restoration is required before migration.
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

Current read-only PG 17.11 inspection and independent comparison with exact
`072585db` Git blobs found **27 applied checksums MATCH** and only this migration
pending. Its clean Linux release SHA256 is
`92adcf8e3617eac53e539545794d2839b7795a2ca03d248dc366343fbcf75bf8`;
do not substitute a checksum of workstation CRLF bytes. The actual old 18-column
PayoutRequest catalog, timestamp precision 3, native enums and absence of new
object-name collisions match the reviewed SQL. Independent SQL/catalog
compatibility review passed. **Recoverability remains open; no shared migration
has run.** Recheck the exact pending list/catalog at execution after that gate.

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
Data restoration is a separately reviewed maintenance action, not an automatic
application rollback.

Retain exact keys, named records, quotes, deadlines and protected evidence for
possibly admitted requests. Reconcile by the same key or authoritative
void-or-reveal, never by clearing browser data or guessing absence. A rollback
must retain admission fencing after any `voided` receipt has been issued;
turning the flag off does not make unfenced old writers safe. Preserve the
key ring and identity history through rollback. Close the release only after
independent evidence review and the deployment/activation ledger are complete.
