---
type: decision-ledger
track: mvp-readiness-loop_20261026
created: 2026-11-02
updated: 2026-11-03
status: independently-reviewed
source_session: c17ff8f2-e847-43ba-bca8-5bf8a66e911e
repos: [ardanova, oasis-sleek]
---

# Current payment decisions and authorization record

This is the current owner decision queue for the payment closeout. The broader
[historic decisions ledger](DECISIONS-LEDGER.md) retains its original rows and
decision history; this file does not reset their implementation status. Use the
[payment release runbook](PAYMENT-RELEASE-RUNBOOK-2026-11-01.md) for technical
gates and the [implementation plan](PAYMENT-COMPLETION-2026-11-01.md) for scope.

On November 2 the owner instructed: “resume work as needed, defer any decisions
I need to make in a decision ledger; all planned work is game.” Continue routine
planned engineering and collect the choices below without asking new interactive
questions. An open choice blocks only the capability that depends on it. Broad
engineering authorization does not choose a new recipient-identity policy or
authorize real-money activation.

## Owner choices carried forward

| ID | Choice or follow-up | Current accepted position and next decision | Affected gate and source |
| --- | --- | --- | --- |
| PAY-D01 | Released first-cohort value mode after provider readiness | The owner already chose to pursue Identomat and keep the released money journey deferred until legitimate KYC/KYB configuration exists; do not reopen that choice. Keep unavailable capabilities closed. Record the intended released cohort/mode when readiness permits it: the historic recommendation was an invite-only Stripe TEST cohort. Identomat configuration is an external prerequisite, not something engineering can declare satisfied. | Released value journey and provider switches. Runbook §5; implementation plan final release constraints; historic P1-02, P7-26, the first-cohort owner call and the October 26 answer resolving P7-17. |
| PAY-D02 | Recipient-identity policy follow-up | The tenant-asserted delivery address and its residual takeover risk were explicitly accepted for the claim-and-consent plan. Keep that acceptance in force. The carried follow-up is whether and when to require an identity re-check at claim matched to `KycPersonKey`; no stronger policy or new blocker is inferred from this ledger. | A future identity-policy change and its rollout, rather than reopening current P5-20 delivery design. Historic P5-20 and P5-21; runbook §5 still requires legitimate rights/provider evidence. |
| PAY-D03 | Real-money activation | Remains unapproved by the payment plan/runbook. Once technical, provider and rights gates have evidence, the owner must explicitly choose whether to activate real money and the intended capability scope. Engineering should prepare and verify the release while leaving live-money switches closed. This ledger does not invent amounts, limits or launch thresholds. | Live-money activation only. Runbook §5: “This procedure does not authorize real-money activation”; implementation plan final release constraints. Historic first-cohort call also requires P3-13/P3-14 before real money. |
| PAY-D04 | Designate the required reviewer identity for normal production promotion | Existing deployment documentation already requires reviewers for the protected promotion environment; no reviewed source names the authorized GitHub user/team to assign. Select that identity before the normal environment is enabled. This implements the existing policy, not a new blanket approval requirement for planned engineering. Credential provisioning is an administrator prerequisite; nonsecret target IDs/URLs remain routine configuration. | Normal AZOA production promotion only. Missing environment/configuration is recorded in runbook §1. The separate publication-only maintenance branch has no GitHub environment and does not deploy. |

Provider credentials, account capabilities, KYC/KYB configuration and legitimate
verification evidence are external prerequisites to PAY-D01/PAY-D03. Their
absence is not an unresolved code-design decision. Record the actual failure or
capability result; do not replace it with a synthetic approval or a passing
health endpoint.

## Resolved scope and authorizations

The following authorization updates are dated November 2 and supersede older
“owner question pending” wording in the runbook only where expressly stated.
Authorization and execution evidence are recorded separately.

| ID | Resolved authorization or accepted choice | Execution and verification status at this snapshot |
| --- | --- | --- |
| PAY-A01 | Continue the remaining payment integration and all planned engineering work; retain decisions needing the owner in this file. | Implementation and reviewed repair branches exist. Release, deployment and activation must still satisfy their own technical gates; this authorization is not evidence that they happened. |
| PAY-A02 | Use existing green CI audit evidence. | Continue targeted verification and release CI as needed. No fresh local npm/NuGet dependency audit is requested. This does not waive ordinary tests or new-head CI. |
| PAY-A03 | Back up the live protected key using the reviewed encrypted procedure. | The coordinator reports the one-file DPAPI backup and recoverability verification completed. The runbook records this completed backup separately from continuity across API restart. |
| PAY-A04 | Restore that key to the explicitly named protected volume and carry out the reviewed storage-only cutover/restart. | **CLOSED with independent C approval.** API `3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d` is SUCCESS at verified public exact-c7 index `f001e8c5...b1934`, persistent volume `3c9b266d-586b-495b-98d1-6113e981f1f7` at `/app/data`. The original 1,001-byte key/hash/modes and application name are preserved; the same original `9098` challenge passed before and after observed restart under UID/GID `1654` with zero supplementary groups. Wrapper `c056985b...efb3a` passed 53 Windows+six Linux checks; DLL/transfer unchanged. Helper `a6863596-908b-4671-ac80-6ebe03306e39` is retained idle without a mount; staging empty. This deployed existing c7 maintenance code, not Node PR3 or payment/schema/real-money changes. No key or challenge payload belongs in this ledger. |
| PAY-A05 | Publish the exact reviewed SDK archive; publication is now independently verified. | Public `azoa-sdk@0.6.0` download exactly matches reviewed SHA256 `7431db9e1a5bfc5e628b8764f0371d2128c8fa68b6e41442032542d9d48ec3fd` and SRI `sha512-5zrnRGC14wV10mHedkUcOkqbTvj9NgIaCCyfQbyvDHT7WahYr1LfFewG+nxedeZvEjxpdovbjNH2hwzemRuxQA==`. Linux adoption `62d3e52`, local clean lifecycle/import proof and full combined PR9 registry CI `41462170432` at `072585db` passed. The client passed 1,035 tests/111 files and build; the owned local container is removed and Windows dependencies are untouched. Backend-only PR8 is closed as superseded. Image publication/deployment remain separate. Do not launch a duplicate publish attempt. |
| PAY-A06 | Carry out planned safe backup/recovery engineering. | Prepare complete logical export and private disposable-restore proof for PostgreSQL 17 and SurrealDB 3. Independent review of the concrete plan/scripts precedes mutations. Original database services remain read-only for this work; no native in-place snapshot restore or original-service staging is authorized by this procedure. |
| PAY-A07 | Retain accepted P5-16/P5-20 claim-and-consent choices. | Exact scopes are `kyc:read`, `kyc:submit`, `units:issue`, `swap:sign`, `transfer:sign`. Keep fragment-only single-use tokens with immediate scrubbing, resend retirement, protected operator delivery stub with explicit production acknowledgement, fresh first-party post-claim login and explicit immutable consent, pending-consent dispatch pause and renewal. Real email delivery is outside this accepted stub scope. Do not reopen these decisions as questions. |
| PAY-A08 | Retain the October 26 P7-17 decision: pursue Identomat; no node test seam or fabricated KYB row. | Released money E2E remains deferred behind real provider configuration. Local isolated E2E can use its existing development sidecar; that proof does not satisfy released KYC/KYB readiness. |

The authorization and Seed/publication/storage execution updates above are
reflected in the runbook's November 3 snapshot. Sanitized receipts
are [the seeded-volume proof](../../../.omc/research/payment-key-seeded-volume-proof-2026-11-02.json)
and [the registry integrity proof](../../../.omc/research/payment-sdk-registry-integrity-proof-2026-11-02.json).
The [post-restart proof](../../../.omc/research/payment-api-a2-post-restart-proof-2026-11-03.json)
and [independent C closure](../../../.omc/research/payment-protected-storage-C-final-closure-20261103.json)
now close actual API persistence and controlled-restart continuity using the same
original challenge. They do not close database recovery or payment activation.
The coordinator independently approved this ledger's structure/history; current
execution wording is reviewed separately before commit.

## Authorized engineering work and remaining evidence

These are routine actions or external evidence gates, not new owner questions.
Proceed in the reviewed dependency order and stop only the dependent action when
its gate fails.

| Work | Required evidence or boundary |
| --- | --- |
| SDK registry adoption | Registry integrity, Linux adoption `62d3e52`, local clean lifecycle/import proof and exact combined PR9 CI `41462170432` at `072585db` are complete. The owned container is removed; immutable release-image publication and deployment remain. Preserve the existing Windows installation and original owner worktrees. Runbook §2. |
| Protected storage cutover | **CLOSED.** API `3a0e8eb9-06fd-4a9a-87c7-199e15b93f7d`, exact-c7 public image, actual persistent mount and original-key continuity passed before/after an observed restart; independent C closure recorded. Helper `a6863596-908b-4671-ac80-6ebe03306e39` remains idle/unmounted and staging empty. Reconcile inventory for future upgrades; no payment deployment or activation is implied. Runbook §3. |
| Database recovery proof | Native PostgreSQL backup `55258d25-f404-4dec-acaa-54ada95a6e11` and SurrealDB backup `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` have catalog records. Those records alone do not prove terminal completion or recoverability. Obtain available completion evidence and a reviewed complete logical export/private disposable restore with schema and aggregate checks. Keep snapshot evidence separate from the logical restore chain; keep credentials and row contents remote and out of logs. Runbook §3. |
| Migration and deployment | PG 17.11 read-only catalog and exact release-blob review found 27 applied migrations/checksums matching and one pending `20261101000000_sale_admission_proof`, SHA256 `92adcf8e3617eac53e539545794d2839b7795a2ca03d248dc366343fbcf75bf8`. SQL/catalog compatibility passed; recoverability and actual shared migration remain open. Use schema jobs and attested Git CI image digests in node-before-consumer order; Node PR3 `f1b8824b` is not deployed. Verify identities, readiness and preserved files. Shared-data `db push`, destructive reconciliation and workstation source upload remain excluded. Runbook §4 and [workflow](../../workflow.md). |
| Normal promotion administration | Node repository initially listed only `ardanova / production` with no protection rules, no `conformance-promotion`/`railway-production`, and empty secret/variable lists. Root configured and read back all eight nonsecret `RAILWAY_*` target IDs/health URLs against exact Railway metadata. The existing reviewer policy and scoped `RAILWAY_TOKEN` remain prerequisites. Reviewer identity is PAY-D04; credential provisioning needs the designated administrator. No environment or secret changed. The maintenance publication-only exception remains separate. Runbook §1. |
| Current-head verification | Node `f1b8824b` CI `41449375013` passed all nine jobs: unit 5,687; integration 1,205 plus one existing G3 skip; gates 2/2/2/1/1; SDK 646/45, frontend 326/59, Chromium 24/eight and full Playwright 8/8. Combined Arda `072585db` PR9 CI `41462170432` also completed SUCCESS: Application 1,606 plus one existing skip, API 652, client 1,035/111 and 48-page build; image jobs skipped as a PR. PR8 is closed. Prior-head runs remain historical; promotion must bind the exact released commits. Runbook evidence table and §1. |
| Capability activation and deployed journey | Keep claim-handoff flags closed until the node/consumer/hosted return gates pass. Keep cash-out void disabled until old unfenced writers drain and the admission/reveal gate passes. Verify the deployed Arda-to-node-to-bank journey and rights/provider readiness separately, enabling only a proven capability within the accepted mode. PAY-D03 still controls real-money activation. Runbook §5. |

No row in this file is a substitute for an execution receipt. Record the exact
project/environment/service and image identities, reviewed procedure, observation
time and independent verifier with each completed operational gate. Preserve
protected evidence privately; link sanitized receipts rather than copying secrets
or customer records into Conductor.
