# Project-token asset-scale migration contract

This is a release contract, not executable migration SQL. It exists because a
project-token decimal scale is an economic fact: a guessed value can multiply a
historic balance, award, or exchange quote by a power of ten. Follow this
contract together with `GATED_COMMERCE_MIGRATION_RUNBOOK.md`. Do not generate,
apply, or hand-edit a Prisma migration from this document.

## Non-negotiable invariants

- `ProjectTokenConfig.assetScale` is the immutable decimal scale of one
  project-token asset. It is not a display preference and it never has a
  database default.
- The only supported scale range is **0 through 18 inclusive**. This matches
  `FixedScaleAmount` and the `numeric(38,18)` commerce columns. The current
  legacy 0--19 service validation must be narrowed in the implementation
  cutover; scale 19 must not be accepted or backfilled.
- A historical value comes from an authoritative chain-asset record, never a
  project name, unit name, UI choice, current SDK default, or `DEFAULT 6`.
- A row with no verified `assetId` or no verified scale is non-settleable. It
  may remain a draft project configuration, but it cannot create a commerce
  agreement, quote, allocation, payout, escrow release, or AZOA dispatch.
- Existing immutable agreements and settlements retain their captured scale.
  A later config correction never rewrites a commercial snapshot.

## Required staged release sequence

Each stage is a separately reviewed release. Stop on any mismatch; do not fold
the stages into one convenience migration.

| Stage | Database shape / application behavior | Required proof to leave the stage |
| --- | --- | --- |
| 0. Freeze and inventory | Keep value-moving flows disabled; pin the baseline fingerprint. | Approved baseline report, duplicate-payment-intent resolution decision, and complete inventory of existing `ProjectTokenConfig` rows. |
| 1. Introduce nullable evidence field | Add `assetScale integer NULL` with **no default**. Do not add `NOT NULL`, `DEFAULT 6`, a trigger, or application-side bulk update. New configs must require an explicit validated scale rather than fall back to six. | Reviewed additive SQL from a disposable clone; post-deploy catalog report proving `integer`, nullable, and no default. |
| 2. Capture authoritative evidence | For every existing config, obtain and record the asset identity and decimal scale from the selected chain/network's canonical asset record. Configs without a minted/verified asset remain null and blocked. | Per-row signed/reviewed evidence manifest and read-only reconciliation report; all source reads are reproducible by a second operator. |
| 3. Validate and reconcile | Backfill only rows whose manifest matches the current database row and authoritative chain response. Re-read after write; verify no null, out-of-range, or changed values and verify every historical agreement/settlement scale is consistent with its immutable snapshot. | Reviewed backfill code/SQL, before/after row counts and hashes, discrepancy log resolved or blocked, additive preflight with zero asset-scale findings, and concurrency/rollback evidence. |
| 4. Harden | In a separate no-default migration, add `NOT NULL` only after Stage 3 is proven. Keep the no-default catalog assertion permanently. | New approved fingerprint, `NOT NULL`/no-default catalog proof, application contract tests, and release approval. |

The current DBML declaration (`NOT NULL DEFAULT 6`) is a desired-model conflict,
not permission to infer historic decimals. A future schema change must first
represent Stage 1's nullable/no-default state. Only the separately approved
Stage 4 change may represent the final non-null/no-default state.

## Authoritative evidence manifest

The Stage 2 manifest is release evidence, not a user-supplied import. Its
minimum immutable record per configuration is:

- `projectTokenConfigId`, `projectId`, expected `assetId`, selected `chain`,
  and selected `network`;
- canonical asset-record locator and the exact returned decimals/scale;
- asset creator/issuer and asset/unit-name checks when the provider exposes
  them, so an unrelated same-symbol asset cannot be substituted;
- source response digest, retrieval timestamp, provider endpoint/version, and
  operator/reviewer identities;
- database row version or stable before-image digest, proposed scale, and a
  deterministic manifest-row digest.

For an Algorand ASA, the canonical source is the selected network's asset
information record for that ASA id and its `params.decimals`; the exact provider
adapter used for production must expose an equivalent typed fact. A provider
timeout, mismatch, unavailable asset, missing asset id, or scale outside 0--18
is a blocking discrepancy, not a retry-with-default case.

The manifest must be append-only release evidence outside mutable project-token
records. It must not be stored only in logs, a spreadsheet without a digest, or
an editable `successCriteria` field. The production implementation needs a
durable audited evidence table or an approved external immutable artifact before
any historical backfill runs.

## Reconciliation and cutover requirements

Before Stage 3 writes a scale:

1. lock or version-check the `ProjectTokenConfig` row and compare its id,
   project id, and asset id with the manifest;
2. read the chain asset again through the selected provider; require the exact
   asset id, network, decimals, and identity checks recorded in the manifest;
3. validate the scale is an integer in 0--18 and has no database default;
4. write only the verified scale, then read it back in the same transaction or
   with a version/CAS guard;
5. reconcile dependent immutable records:
   `TaskCommerceAgreement.scale` and its terms snapshot, plus any project-token
   `EconomicSettlement.scale`/terms snapshot. A mismatch is a release blocker;
   never rewrite an accepted agreement or settlement to make the report green;
6. produce a deterministic before/after report with total configs, verified
   configs, unavailable assets, null scales, mismatch count, and digest of the
   reviewed manifest.

The first application release that can observe nullable scale must fail closed
for a missing or invalid scale. It must not rely on CLR `int` default zero,
AutoMapper construction, or a DTO default. The final non-null constraint is a
defense in depth, not the validation mechanism.

## Required implementation cutover map

No application change is made by this contract. The implementation slice must
update and test all of the following as one reviewable change:

| Surface | Current behavior | Required cutover |
| --- | --- | --- |
| `CreateProjectTokenConfigDto` and `ProjectTokenConfigDto` | Both default `AssetScale` to `6`. | Require an explicit scale for a newly created configuration; the response maps the persisted verified value and does not manufacture six. |
| `ProjectTokenService.CreateConfigAsync` | Validates 0--19 then persists the caller value. | Validate 0--18 and bind the new config's scale to an authoritative asset-creation/lookup result before it can be commerce-eligible. |
| `ProjectTokenConfig` persistence model | Required `int` conceals a nullable database transition. | Use a nullable transition representation or a dedicated verified-scale value so missing evidence cannot become zero; make final non-null representation only after Stage 4. |
| `TaskCommerceService.AcceptBidAsync` | Checks 0--19 and freezes the config scale in an agreement and terms snapshot. | Require a verified 0--18 scale and matching asset identity before agreement creation; retain the frozen value thereafter. |
| `FundingIntentService.CreateTermsSnapshot` | Copies `config.assetScale` into funding terms without validation. | Require verified 0--18 scale/asset evidence before checkout/intent construction and capture it with the funding terms. |
| `FixedScaleAmount` / outbox | Supports scales 0--18. | Remains the canonical numeric bound; do not widen it independently of database precision and provider limits. |

The existing legacy allocation, balance, exchange, payout, and treasury paths
still use integer/double contracts. They are not evidence that a historical
scale is correct and remain disabled for value movement until their separate
fixed-scale migration is complete.

## Operator evidence checklist

- Reviewed baseline and post-stage fingerprints from `db:preflight-commerce`.
- Reviewed SQL and rollback/locking plan for each stage, produced on a
  disposable clone and never by `db push` against a shared database.
- Manifest digest, dual-operator chain-source verification, and a discrepancy
  report for every existing config.
- A test/proof that a null, missing asset id, mismatched asset, out-of-range
  scale, stale config version, or chain-read failure creates no agreement,
  intent, settlement, allocation, payout, or dispatch.
- Read-back reconciliation proving zero unresolved null/mismatch records before
  the Stage 4 `NOT NULL` migration.
- Production release approval; successful schema preflight alone does not
  authorize payments, token allocation, or AZOA settlement.
