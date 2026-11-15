---
type: runbook
track: mvp-readiness-loop_20261026
date: 2026-11-01
updated: 2026-11-03
status: pending-independent-evidence-update-review
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
---

# Payment release evidence and rollout

Writer-pass snapshot updated November 3 UTC; supplements
[the implementation plan](PAYMENT-COMPLETION-2026-11-01.md). Combined Arda
[draft PR #9](https://github.com/Escherbridge/Ardanova/pull/9) is now green at
documentation head `710530360cbcbf8dc37960769d85262c22835c6c`, CI `41564070587`.
The detailed runtime test counts below remain tied to exact prior combined head
`072585db831a93914d80452d112a387805f71383`, CI `41462170432`. Its backend
and published-SDK frontend validators passed; three image jobs were skipped as
a PR. Backend-only PR #8 is closed as superseded. The pushed combined branch
contains frontend `d4faa5a`, backend harness correction `83f38c9`, ledger
`63b119e`, SDK adoption `62d3e52` and reviewed documentation `072585db`.
Node source is now committed and pushed at
`9fa94356f0f65ab5c1332ee7200c024fa3bce14b`, the reviewed five-file fixture repair
on the 26-file Surreal 3.2/SurrealKV contract commit
`3f0862b39c4af67d6c7d15d29ab48ded724bb17c`. Branch push CI
`41629476233` is **SUCCESS, nine of nine jobs**, at exact `3f0862b3`: unit
5,688 passed and integration 1,205 passed/one existing skip. PR CI `41629483717`
at synthetic merge `22d9cc94...` failed one expiry-poll fixture (1,204 passed,
one failed, one existing skip). That failure remains retained. The fixture-only repair passed its one affected
13-case run and is pushed at `9fa94356`; fresh complete `9fa94356` CI remains pending. Prior payment-head
CI `41449375013` is **SUCCESS with all
nine jobs successful**, including full .NET suites at that prior head. Its Playwright job provides a
**single prior payment-head run of eight passing cases in 1.8 minutes**, including all
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
for later probe work. The separate public-drill residual `b99f6e5a...` was cleared
once and reconciled to empty staged content at 05:49 UTC November 3. Subsequent
exact public-resource cleanup is independently closed: original nine services and
four visible volumes remain, with null staging. Detached historical instance rows
persist; physical-media erasure is not claimed. Earlier rejected attachment/staging
experiments are historical, not current blockers. Runtime remains source `c7cfa4af`,
Staging and `AZOA_SKIP_MIGRATIONS=1`; Node PR3 `f1b8824b` is not deployed.
Selected real PostgreSQL and SurrealDB backup usability and the PostgreSQL
clone's SQL/catalog rollback rehearsal are independently CLOSED. Private clone
cleanup is independently CLOSED. Config-only immutable adoption is independently CLOSED; fresh Node 3.2/SurrealKV CI,
shared-data migrations,
payment deployment and real-money activation remain open; these are distinct
from protected-storage continuity.
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
| Node unit | Prior payment-head CI `41449375013`: **single full run 5,687 passed, zero failed/skipped**, 1m 7s. Local login-validator evidence separately passed 20 cases, including 17 new | Release and operational gates |
| Node frontend | Prior payment-head CI lint/typecheck/build passed; **326/326 tests across 59 files passed** | Deployed journey |
| Node browser slices and historical CI | Prior payment-head Chromium job: **24 tests across eight files passed**. Prior Playwright job had five passes in 38.0s; the current full eight-case result is separate below | Slice/historical suites alone do not exercise the new member-handoff/claim/consent pages |
| Hosted handoff browser proof | Prior payment-head CI `41449375013` completed **eight of eight tests in one run**, 1.8m: three new handoff cases, sign-in shell, signup/KYC, on-ramp, KYB and webhook. Local proof separately accumulated three existing plus two new plus final renewal passes | Full deployed Arda-to-node-to-bank journey remains unproven |
| Windows E2E setup and privacy | Verified Compose 5.6.1/private current-user pipe and four healthy owned containers; fake-provider HTTP 200 and three negative TLS controls passed. Privacy canary found no password/ARIA/media leak. All owned containers, volumes, networks, generated task directory and forwarding children were cleaned up; other containers unchanged | Local resources are closed; no production activation implied |
| Node integration | Prior payment-head CI `41449375013`: **single full normal integration run 1,205 passed, zero failed, one existing G3 analyzer skip out of 1,206**, 49m 14s | Release approval and deployed journey |
| Node conformance gates | Same prior payment-head CI: G1/G2/G3 each two passed; G5/G7 each one passed, no failures or skips in those gate runs | These do not erase the separately recorded normal-integration analyzer skip or prove browser journeys |
| Node Ethereum prerequisites | Pinned `npm ci --no-audit` and artifact build passed; 12 contract tests passed in 55.3s. All three Ethereum HTTP protocol tests then passed in the targeted batch | No unresolved Ethereum setup failure in that batch |
| Cash-out fence races | All four actual `CashOutRequestVoidRaceTests` passed in the full run; 16 paired races exercised | Released drain/activation gate remains separate |
| Protected files | **CLOSED A1/A2:** original one-file 1,001-byte key/hash preserved on persistent volume `3c9b` at `/app/data`, file `0600`/ring `0700`, UID/GID `1654`; owner-authorized encrypted backup/recovery retained | Closed for the observed deployment/restart; refresh inventory and protect recovery material for future upgrades |
| Continuity proof | Wrapper `c056985b...efb3a`: **53 Windows + six actual Linux checks passed**. Same original `9098` challenge verified before/after actual API restart as UID/GID `1654`, zero supplementary groups; DLL and transfer unchanged | Independent C closure recorded; this is storage continuity, not deployed payment/provider proof |
| Preservation infrastructure | API `3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d` SUCCESS at public c7 image index `f001e8c5...b1934`, persistent volume `3c9b266d-586b-495b-98d1-6113e981f1f7`; helper `a6863596-908b-4671-ac80-6ebe03306e39` idle/unmounted | Protected storage remains closed. Synthetic `b99f6e5a...` staged content was cleared/reconciled at 05:49 UTC; fresh preflight still precedes unrelated staging |
| Database backups | PostgreSQL `55258d25-f404-4dec-acaa-54ada95a6e11` and SurrealDB `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` catalog records created at 12:59 UTC November 2 | Workflow status query returned Not Authorized; catalog existence is neither completed-backup nor disposable-restore proof |
| Selected real PostgreSQL recovery | **CLOSED by C:** selected backup `55258d25...` restored to private service `9facd0d0...`, deployment `68ab86ff...`, volume `28c77df2...`/instance `6b6a354f...`; actual PG 17.11 read-only proof scanned 108 tables/materialized views and 204 rows, with 27 applied migrations and zero failed migrations, invalid indexes or unvalidated constraints | Selected snapshot usability only; terminal provider workflow remains UNKNOWN. No current-source equality, cluster-wide or full application recovery claim; private cleanup is independently closed below |
| Private clone DDL rollback | **CLOSED by C:** exact Git migration `92adcf8e...` applied transactionally to that private clone; six columns, two tables, five indexes and one check asserted, explicit rollback verified, history/catalog restored, and all 108 private R-lines equal | SQL/catalog rehearsal only; no Prisma migrate-deploy/history-recording test or shared migration execution |
| Selected real SurrealDB recovery | **CLOSED by C:** backup `5e3d247b...` recovered on private A14/deployment `47a05f6c...`, volume `ae821465...`/instance `1feb1c0a...`; one positive canonical nonsymlink SurrealKV v1 store, pinned 3.2 and strict complete query frame: users 9, wallets 1, API keys 5, consent grants 0 | Selected-point usability only. Three earlier UNKNOWN attempts remain intact; no source path/options/current-data equality, full schema/content hash, native archive checksum or terminal workflow claim. Cleanup is independently closed; fresh engine/CI remains open; config-only pin adoption is closed |
| Public PG17 logical rehearsal | **CLOSED_VERIFIED by C:** nine unique scenarios have cumulative passing coverage; original full receipt was 8 pass/1 faulty expired-cut oracle, followed by 3/3 targeted setup/oracle/shutdown and 17/17 offline repairs. Real Linux snapshot/dump/restore, native array/type checks and encrypted archive round-trip passed; owned container/image removed, six baseline IDs unchanged | This is public local rehearsal, not a new single full-green run, fallback-main execution or real database recovery |
| Public native snapshot recovery | **SYNTHETIC_FIXTURE_RECOVERED by C:** exact restored target `3cf20829...` passed read-only fixture checks with canary absent; original source `748cbf28...` retained its canary and original PID/mount in verified sample windows | Terminal restore workflow remains UNKNOWN. No continuous coverage through later target steps, zero-downtime claim or real-data recovery; public environment membership/workload cleanup is separately closed |
| PostgreSQL migration preflight | PG 17.11 read-only catalog and independent Git-blob checks: **27 applied checksums MATCH**, one pending `20261101000000_sale_admission_proof`, Linux SHA256 `92adcf8e3617eac53e539545794d2839b7795a2ca03d248dc366343fbcf75bf8`; SQL/catalog compatibility and private clone rollback rehearsal approved | Selected PG/Surreal recovery and cleanup are closed; fresh engine/CI validation and shared migration remain open. Recheck pending list and apply only through the reviewed migration job after its gates |

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
evidence and successful prior payment-head PR CI, including full .NET and browser jobs.
These CI results alone do not resolve operational gates. Separate evidence now
closes protected-storage/API restart continuity; payment release-image publication,
fresh engine/CI validation, shared migrations and
deployed end-to-end validation remain.

### Hosted browser coverage boundary

Prior payment-head Playwright job `125290468008` in CI `41449375013` completed at
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
prior payment-head remote eight-test run includes that webhook test successfully.
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
- `.omc/research/payment-node-ci-dotnet-41449375013.log`, `payment-node-ci-sdk-41449375013.log`, `payment-node-ci-frontend-41449375013.log`, `payment-node-ci-browser-41449375013.log` and `payment-node-ci-e2e-41449375013.log`: downloaded completed prior payment-head jobs, including single full unit/normal-integration and eight-case browser runs; all nine CI jobs succeeded.
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
Node prior payment commit `f1b8824b30901cfb2073f2e14e9e08f2219b33e0`, based on reviewed
`1f83837ba746a0dfd2dbfe5f270a07f05db4dfdd` and original main `c7cfa4af`, is
pushed to [the node review branch](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/tree/codex/payment-integration-closeout-20261101).
The original 103-file attestation, including the member-handoff DTO, and the new
reviewed 16-file repair are retained. The node main branch is unchanged; this push
did not deploy. [Draft PR #3](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/pull/3)
is open. Exact current Node head is `9fa94356f0f65ab5c1332ee7200c024fa3bce14b`;
branch CI `41629476233` is green at exact `3f0862b3`; synthetic-merge PR CI
`41629483717` retains one expiry-poll fixture failure. Fresh complete CI after the verified five-file fixture repair remains pending. [Prior payment-head CI 41449375013](https://github.com/JadeZaher/azoa-autonomous-zones-of-action/actions/runs/41449375013)
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
staging was empty at that storage-closeout snapshot. The later public native
drill's residual `b99f6e5a-b523-411e-a11c-c0cc5f8e5a81` was cleared once and
reconciled to empty content at 05:49 UTC November 3. Later exact public cleanup
closed environment membership/workloads, retaining the original nine services, four
visible volumes and all five raw baseline volume rows unchanged; staging is null.
Detached historical rows are not live mounts or a physical-erasure receipt.
Do not reuse a historical empty-stage claim without fresh preflight. The protected
volume is mounted on the API. Future payment
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

### Selected real backup usability and cleanup closed; release gates remain

The public Linux PG17 logical rehearsal is independently **CLOSED_VERIFIED** in
`payment-pg17-fallback-rehearsal-C-final-review-20261103.json`. C retained the
original 8-pass/1-oracle-failure receipt separately from the later 3/3 targeted
result; cumulative nine-scenario coverage is not a single new full-green run.
The unchanged fallback `229bda...136ad7` demonstrated one exported read-only cut,
native array/type/content fidelity, dump/restore and encrypted archive round-trip
using only public fixtures. The targeted real SQLSTATE `42704`/exit `3` case
verified expired-cut rejection and owner/importer cleanup. All owned local
resources were removed; six pre-existing container IDs were preserved.

The separate public Railway native drill is **SYNTHETIC_FIXTURE_RECOVERED** in
`payment-native-public-recovery-C-final-closure-20261103.json`. Backup
`df0ce91d-8d80-4544-8b1d-3093a77851c0`, restored volume `c1bd2109...`/instance
`d0b12b5a...`, and target deployment `3cf20829...` have reviewed causal lineage
and actual PG17 fixture proof (`F706B5C7...C86B0`): source fixture values recovered,
post-catalog canary and after-sentinel absent, PostgreSQL UID/GID `999:999`.
The final source receipt `C196115C...11A15` shows its canary/after-sentinel present,
same initialization PID/mount, seven actual samples over 3.36 seconds and maximum
gap 0.67 seconds. The earlier 550.36-second stream had 1,045 samples/max gap 0.7
and spans the known pre-invocation approval through first captured READY record;
later target operations are separate windows. Neither proves zero downtime or
absence of sub-gap interruptions. Terminal restore status is still UNKNOWN.

The closure itself did not clean staging/resources. A separately reviewed one-shot
operator `ff3d276d...` then cleared residual `b99f6e5a...` at 05:49 UTC and
reconciled empty staged content, clone retained, with the compared source,
original PostgreSQL/SurrealDB and protected API configurations/mounts unchanged.
Those intermediate receipts are `payment-native-public-stage-clear-intent-20261103.json`
and `payment-native-public-cleanup-post-clear-services-20261103.json`. Subsequent
exact resource deletions/readbacks are independently closed in
`payment-native-public-cleanup-C-final-closure-20261103.json`: that final cleanup snapshot
has exactly the original nine services/four visible volumes, all five
original raw volume rows unchanged, null staging and an empty raw patch. The two
owned public services/volumes are absent from membership and the source backup
catalog is empty. Raw retired instance rows remain READY with `serviceId:null`,
like a pre-existing deleted-volume row. This proves membership/workload cleanup,
not physical media/archive erasure; it does not authorize additional global
deletes or reuse of retired IDs.
These public method proofs alone are not real PostgreSQL/SurrealDB recovery.

New catalog entries named `payment-before-integration-2026-11-02` exist for
PostgreSQL backup `55258d25-f404-4dec-acaa-54ada95a6e11` (12:59:23.474 UTC) and
SurrealDB backup `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` (12:59:23.544 UTC).
The workflow-status query returned **Not Authorized**. Catalog entries alone
prove neither terminal backup completion nor restoration. Record available
provider completion evidence without inventing terminal status, and prove actual
restoration into new private disposable targets before migration. Record available
time/size metadata, exact backup/resource identities, sanitized proof-artifact
hashes and restored schema/aggregate checks. A copied opaque provider ID hash is
not an archive checksum; do not invent byte-level evidence the provider does not
expose. Node
`scripts/surrealdb/backup.ps1` and `restore.ps1` print password-bearing arguments;
use a provider snapshot or reviewed nonlogging export/import for live credentials.

The reviewed real-data route is a source-specific native procedure. Under
the owner's all-planned-engineering authorization, only the provider-generated,
uncommitted source-mount staging may be accommodated under an exclusive window,
solely to replace it with a verified private-target-only patch before any commit.
Within this restore procedure, original database contents and live source
deployment/configuration/mount must remain unchanged; the later config-only
image adoption below is a distinct reviewed maintenance operation. Applying
the generated source mount delta, in-place restoration, source
deployment or exporting customer rows/credentials to local files/logs remain
prohibited. Pin the actual source/backup/destination and baseline, establish
causal replacement lineage, prove isolated restored schema/aggregates and sampled
source preservation, and keep UNKNOWN on ambiguous admission without replay.
`payment-real-native-scope-C-judgment-20261103.md` records this prospective scope
interpretation; it is not a real restore execution receipt. The logical path is
an available alternative, not a new mandatory worker prerequisite.
The observed public Surreal `/version` route response is `surrealdb-3.2.0`, HTTP 200,
with redirect/final URL unrecorded; it does not attest an exact live binary or deployment,
recorded in `payment-surreal-public-version-20261103.json`. Do not substitute the
local rehearsal's assumed/test version for that observation. This unauthenticated
response is not authenticated source/backup/namespace proof; the SSH version
attempt remained UNKNOWN after shell-transport failure.

The selected real PostgreSQL backup's recovery-usability minimum is now
**CLOSED**, independently recorded in
`payment-real-pg-recovery-usability-C-final-review-20261103.json`
(SHA256 `87464f611ecccac801dcdbb2e1d03ced96ee9cdb56ddb011dc97d130fb9f9818`).
Exact backup `55258d25-f404-4dec-acaa-54ada95a6e11` was restored to private service
`9facd0d0-d6dc-4a29-b6f1-8262383d5aa8`, running deployment
`68ab86ff-b80a-4e96-af13-1f60ee010d8c`, new volume
`28c77df2-19cd-4048-9a68-2963b951832f`/instance
`6b6a354f-5119-4b16-8ce9-69cc41e2c92b`. Its guarded PostgreSQL 17.11 read-only
proof scanned 108 eligible application tables/materialized views and 204 rows;
27 applied migrations, zero failed migrations, invalid indexes or unvalidated
constraints were observed. Rows, schema dumps and per-table manifests stayed
private. Original-source observations cover the 550.1-second/1,017-sample window
bracketing restore admission/first READY and distinct seven-sample stage/commit
pulses, not continuous later coverage or zero interruption. Terminal provider
workflow status remains UNKNOWN. No equality with today's source, cluster-global
roles/ACLs, other databases or application/provider end-to-end recovery is attested.

The same private clone's canonical SQL/catalog rollback rehearsal is separately
**CLOSED** in `payment-pg-clone-ddl-C-final-review-20261103.json` (SHA256
`8d7fa3f17082c2b428f363e37962152dc2bbf5440009dc9f0fdd27580d8c5907`).
All 27 applied checksums matched Git and only the one pending migration remained.
Exact canonical `92adcf8e...` SQL asserted six added columns, two tables, five
indexes and one check constraint, then explicitly rolled back. Introduced objects
were absent afterward, old catalog/history were unchanged, and all 108 retained
private R-lines matched. This is a transactional SQL/catalog rehearsal, not
Prisma migrate deploy or its migration-history recording. Physical WAL/catalog
storage and database size may change; neither whole-manifest nor run-specific
pg_dump byte equality is claimed. No shared migration has run. The real private
target was retained through rehearsal and subsequently removed under the separately
closed private-cleanup procedure below.

Selected SurrealDB backup usability is now independently **CLOSED** in
`payment-real-surreal-recovery-usability-C-final-review-20261103.json` (SHA256
`0654f633b8676a6361611a56052d1691bae062a1f1e78b32263d4f2e07b1de75`).
Exact backup `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` was restored to private service
`a14d07f5-4843-4a8a-96ef-432be20bc7f4`, deployment
`47a05f6c-4026-4b00-a68a-733c485821db`, volume
`ae821465-55e0-4274-9810-907d4cee68f9`/instance
`1feb1c0a-b25d-43a2-85e3-efe6dc8bdcf5`. The pinned 3.2.0 image remains
`docker.io/surrealdb/surrealdb@sha256:9096858706a86e6ed8500ef8611dc15b8bab1188c6833846781eeb6ea46e2916`.
The actual positive scan found exactly one canonical nonsymlink SurrealKV v1
store on the owned mount; the proof rediscovered it before embedded engine
opening. Only the two remote API namespace/database references were used, with
explicit clone-only `sync=every`. No original source path or option equality
was assumed, and no missing store was initialized.

The fourth separately gated attempt returned the exact nine-null/count/hash
frame and COMPLETE, with private SQL stderr empty: users **9**, wallets **1**,
API keys **5**, consent grants **0**. Schema-name fingerprint is
`ecb7544047805e1c927a2c559d078d622ecbf969f3009aef8ed3df1a21e4d6da`;
it covers selected names, not full definitions or row content. Native provider
lineage and exact SUCCESS/sole READY mount checks bracket this proof. The first
three attempts remain UNKNOWN with their frozen artifacts and sanitized
diagnostics; they are not converted to successes or erased by this result.
This closes selected-point usability only: no current-source data, source
path/options, full-schema/content or copied-size equality, native archive
checksum or terminal workflow status is claimed. Original-source operations,
rows, resolved paths and credentials were excluded from local output. The
post-proof original metadata readback is independently closed in
`payment-private-recovery-post-proof-C-review-20261103.json` (SHA256
`558a546162411e04a605d917202cdcd5c4e06038e497c4f8d1d2e74c088c5b0a`):
all 11 service IDs and eight pre-Surreal raw rows unchanged, the sole new mount
attached, five expected deployments SUCCESS and staging empty. This is metadata
preservation, not current-data equality. Exact private cleanup is now independently
CLOSED in `payment-private-recovery-cleanup-C-final-review-20261103.json` (SHA256
`ecbe5cbc76f23445f4d5dbbae5df31548fbcad89f4ab9777b1c5a7ea4640b098`).
Both private services and scoped visible volumes were removed, their final
deployments are REMOVED, and the original nine complete service objects/four
visible volumes are preserved with empty staging. Seven nonowned raw rows and
the original PG two/Surreal one backup identity catalogs are unchanged. Two
owned historical rows remain detached; this closes active membership/workload
cleanup, not physical-media/archive erasure or opaque checksum verification.

Normal promotion still requires fresh full validation; live desired-image adoption is now closed:
`payment-normal-promotion-surreal-contract-C-review-20261103.json` records old
3.1.4/RocksDB release/CI pins versus the mutable source `v3` reference and observed
3.2.0/SurrealKV evidence. Establish the intended immutable supported identity and
test application/schema/conformance against it before promotion. Do not redeploy
an older engine over this store, weaken the pin check or turn this engineering
work into an owner-policy question. The reviewed 26-file repair is now committed
as `3f0862b39c4af67d6c7d15d29ab48ded724bb17c`. One canonical checked-out contract
feeds release, CI, G1 and isolated E2E; the E2E volume label refuses an old store
before build/up while preserving owner-scoped cleanup. Local C verification in
`payment-node-surreal32-local-C-final-verification-20261103.json` (SHA256
`4d9eebfc37f414ec182cb62cb185e9d4eae66d0e6741e9a256f56cbd2ee5c868`)
retains **41 unique passing cases**: seven Python release guards, six Node owned-
volume guards, 16 .NET least-privilege cases, one isolated static G1 check and
11 cumulative conformance-evidence cases. Initial fixture/CNG failures remain
retained; isolated/normal-profile targeted passes do not convert that sequence
into a new full-suite run. Renderer/JS/YAML checks also passed. Exact-branch CI `41629476233` passed all nine jobs at exact `3f0862b3`, including unit
5,688/0/0 and integration 1,205/0/one existing skip; synthetic-merge PR run
`41629483717` retains the one expiry-poll failure. The distinct source identities
and results are preserved in `payment-node-surreal32-ci-terminal-evidence-20261103.json`.
The five-file fixture-only repair is committed as
`9fa94356f0f65ab5c1332ee7200c024fa3bce14b`; its fresh complete CI is pending.
C closure `payment-node-kyc-fixture-C-local-final-review-20261103.json` (SHA256
`795300867a1cf11ab3fe1acd25a30cb59883d5eb3d0e3056d317e2d866ffc4e4`)
verified one affected run of **13 passed, zero failed/skipped**, with exact case
identities and unchanged reviewed source hashes. The fixture makes the poll due
deterministically and adds a per-token positive vendor counter/accounting case;
production polling, CAS, backoff and timeouts are unchanged. Exact owned local
resources were removed, preserving 16 baseline containers, 82 image identities
and 50 volume names. This targeted proof does not erase the earlier synthetic-
merge failure or replace final-head CI. The original deployment's provider metadata now reports the exact canonical
production index `sha256:cd2b5ef11e77c00c59808ca8bb68a868a836c5bc6aba7af71f718cd6e73acb04`.
A separately reviewed config-only adoption staged and committed only desired
`source.image` to
`docker.io/surrealdb/surrealdb@sha256:cd2b5ef11e77c00c59808ca8bb68a868a836c5bc6aba7af71f718cd6e73acb04`
with `skipDeploys:true`. Actual postconditions are verified in
`payment-surreal-config-only-pin-post-read-1793707929201693800-20261103.json`
(SHA256 `80ac8bf31ac47502266bf148a3f6795ef537b8c30645b2535716b00e273313ba`):
all nine actual latest/live deployment IDs, including original Surreal `9a1b2b91...`,
remain unchanged, raw mounts/rows and backup ID/name/date catalogs are unchanged,
and staging is empty. Three bounded public health/version pairs returned
200/3.2. No deployment/restart/schema/variable/storage/payment change was
requested. Final independent C closure is recorded in
`payment-surreal-config-only-pin-C-final-review-20261103.json` (SHA256
`e43a9d7ff9eaff1fd042c5f8fbb92b392aff3824832d4da47c307f68e77605ee`).
The desired-configuration pin technical prerequisite is CLOSED, not bypassed. Twelve cumulative offline maintenance controls (nine earlier plus three
new distinct cases) are separate from the 41 source checks. This is provider
configuration/deployment metadata plus sampled public responses, not runtime
attestation, current data/path equality, continuous uptime or terminal provider
workflow proof. Full CI, protected promotion and payment deployment remain
separate; no payment branch was deployed by the config-only operation.

Before normal rollout, obtain fresh exact-current original Surreal storage-mode
and synchronous-commit verification through separately reviewed, bounded
boolean-only source-side evidence. The preserve-only guard checks image/source/
index, SUCCESS and operator health; the CI `sync=every` contract and config-only
pin do not certify live storage mode or durability. No whole variable map, raw
path, credentials or source mutation belongs in that check. This is a remaining
technical evidence gate, not an owner choice, and it does not reopen selected
backup usability or the desired-image pin closure.

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
compatibility review passed. **Selected PG recovery and private clone SQL/catalog
rollback rehearsal, selected SurrealDB usability and cleanup are closed; fresh
engine/CI validation and shared migration remain open; config-only pin adoption is closed.** Recheck the exact
pending list/catalog at execution after the remaining gates.

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
