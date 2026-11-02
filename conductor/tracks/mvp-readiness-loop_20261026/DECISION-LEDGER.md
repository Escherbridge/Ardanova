---
type: decision-ledger
track: mvp-readiness-loop_20261026
created: 2026-11-02
updated: 2026-11-02
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
| PAY-A04 | Restore that key to the explicitly named protected volume through the reviewed private helper. | Exact destination approval is resolved. Seed and one-file inventory/hash/ownership/modes verification succeeded through helper `950e8300-8103-488e-af69-e27736374f42`, volume `3c9b266d-586b-495b-98d1-6113e981f1f7` (`payment-protected-data-c17ff8f2`); the same original challenge verified while mounted there. The protected volume is now retained unattached. Current helper deployment `8265f5ac-e914-46fc-aee8-c780be2c5fc3` is idle on the same pinned image with no mount, all staging empty. API attachment with `skipDeploys: true` failed; a separate empty-volume test proved same-build redeploy did not apply the staged mount. The test patch and never-used empty test volume were removed. API deployment `3177e8e7-fa01-4f73-aa4e-d482a9bf8340` remains unchanged; no API cutover/restart is proven. No key or challenge payload belongs in this ledger. |
| PAY-A05 | Publish the exact reviewed SDK archive; publication is now independently verified. | Public `azoa-sdk@0.6.0` download exactly matches the reviewed archive: SHA256 `7431db9e1a5bfc5e628b8764f0371d2128c8fa68b6e41442032542d9d48ec3fd`, SRI `sha512-5zrnRGC14wV10mHedkUcOkqbTvj9NgIaCCyfQbyvDHT7WahYr1LfFewG+nxedeZvEjxpdovbjNH2hwzemRuxQA==`. Independently reviewed Linux manifest/lock adoption is committed at `62d3e52`; clean public-registry `npm ci` installed 603 packages, completed project lifecycle generation/patching, passed installed-pin/all-three-wasm guards and actual ESM/CJS money/React imports. The owned verification container is removed; Windows dependencies are untouched. Full combined-branch adoption CI remains pending. Do not launch a duplicate publish attempt. |
| PAY-A06 | Carry out planned safe backup/recovery engineering. | Prepare complete logical export and private disposable-restore proof for PostgreSQL 17 and SurrealDB 3. Independent review of the concrete plan/scripts precedes mutations. Original database services remain read-only for this work; no native in-place snapshot restore or original-service staging is authorized by this procedure. |
| PAY-A07 | Retain accepted P5-16/P5-20 claim-and-consent choices. | Exact scopes are `kyc:read`, `kyc:submit`, `units:issue`, `swap:sign`, `transfer:sign`. Keep fragment-only single-use tokens with immediate scrubbing, resend retirement, protected operator delivery stub with explicit production acknowledgement, fresh first-party post-claim login and explicit immutable consent, pending-consent dispatch pause and renewal. Real email delivery is outside this accepted stub scope. Do not reopen these decisions as questions. |
| PAY-A08 | Retain the October 26 P7-17 decision: pursue Identomat; no node test seam or fabricated KYB row. | Released money E2E remains deferred behind real provider configuration. Local isolated E2E can use its existing development sidecar; that proof does not satisfy released KYC/KYB readiness. |

The November 2 authorization and Seed/publication execution updates above are
reflected in the runbook's current snapshot. Sanitized receipts
are [the seeded-volume proof](../../../.omc/research/payment-key-seeded-volume-proof-2026-11-02.json)
and [the registry integrity proof](../../../.omc/research/payment-sdk-registry-integrity-proof-2026-11-02.json).
The coordinator's reviewed continuity receipt establishes successful verification
of the same challenge before helper detachment, while API persistence and restart continuity
remain separate gates. The coordinator independently approved this ledger's
structure/history and requested these receipt corrections before final freeze.

## Authorized engineering work and remaining evidence

These are routine actions or external evidence gates, not new owner questions.
Proceed in the reviewed dependency order and stop only the dependent action when
its gate fails.

| Work | Required evidence or boundary |
| --- | --- |
| SDK registry adoption | Registry integrity, reviewed Linux manifest/lock adoption commit `62d3e52`, clean registry installation/lifecycle, installed guards and ESM/CJS import proof are complete. The owned container is removed; obtain full combined-branch adoption CI next. Preserve the existing Windows installation and original owner worktrees. Runbook §2. |
| Protected storage cutover | Seeded inventory/hash/modes and the original challenge's pre-detachment helper verification are complete; the protected volume is retained unattached. API skip-deploy attachment was rejected `BAD_USER_INPUT`, and the empty-volume test proved same-build redeploy did not apply its staged mount. All staging is cleared and the never-used empty test volume deleted; API deployment `3177e8e7-fa01-4f73-aa4e-d482a9bf8340` is unchanged. A supported pinned-image alternative is under independent review, with no automatic API fallback. Fresh inventory, actual API attachment and restart continuity remain. These remain already-authorized planned engineering, subject to independent procedure review. Preserve application name, paths, UID and private file mode. Keep the storage-only cutover separate from the payment upgrade. Runbook §3. |
| Database recovery proof | Native PostgreSQL backup `55258d25-f404-4dec-acaa-54ada95a6e11` and SurrealDB backup `5e3d247b-907e-4f3f-a20e-f1ebd4066bcd` have catalog records. Those records alone do not prove terminal completion or recoverability. Obtain available completion evidence and a reviewed complete logical export/private disposable restore with schema and aggregate checks. Keep snapshot evidence separate from the logical restore chain; keep credentials and row contents remote and out of logs. Runbook §3. |
| Migration and deployment | Review the entire pending additive migration list against recoverable backups; use schema jobs and attested Git CI image digests in node-before-consumer order. Verify actual deployment identities, health/readiness and preserved-file continuity. Shared-data `db push`, destructive reconciliation and workstation source upload remain excluded. Runbook §4 and [workflow](../../workflow.md). |
| Current-head verification | Node head `f1b8824b30901cfb2073f2e14e9e08f2219b33e0` CI `41449375013` completed SUCCESS with all nine jobs: full unit 5,687/zero failed/skipped; normal integration 1,205 passed/zero failed/one existing G3 skip out of 1,206; selected gates 2/2/2/1/1 passed; SDK 646/45 files, frontend 326/59, Chromium 24/eight and single full Playwright 8/8 passed. Prior-head green CI is separate historical evidence. Combined Arda branch CI remains pending; record terminal CI for the exact released commits. Runbook evidence table and §1. |
| Capability activation and deployed journey | Keep claim-handoff flags closed until the node/consumer/hosted return gates pass. Keep cash-out void disabled until old unfenced writers drain and the admission/reveal gate passes. Verify the deployed Arda-to-node-to-bank journey and rights/provider readiness separately, enabling only a proven capability within the accepted mode. PAY-D03 still controls real-money activation. Runbook §5. |

No row in this file is a substitute for an execution receipt. Record the exact
project/environment/service and image identities, reviewed procedure, observation
time and independent verifier with each completed operational gate. Preserve
protected evidence privately; link sanitized receipts rather than copying secrets
or customer records into Conductor.
