-- The column that says a credential was issued THROUGH THE ISSUANCE NODE (Wave L0 remediation).
--
-- Why a new column and not a derivation. `IsIndependentlyVerifiable` shipped with no producer at
-- all; wave L0 gave it one, reading `mintTxHash`. That read is exclusive of HEAD's code and wrong
-- about the DATA: the removed address-based path wrote the same column from
-- `MintSoulboundASAAsync(_platformAddress, ...)`, and the simulated provider fabricated `assetId`
-- outright. So every row created before the node path carries both columns and would map to
-- IsIndependentlyVerifiable = true -- "Verified", "Independently verifiable outside ArdaNova" --
-- over credentials that were never issued to anybody, and on the detail page could reach
-- `verified` outright, because the ownership check behind `chainVerified` asks whether the
-- PLATFORM holds the asset.
--
-- Nullable with NO default and NO backfill, deliberately. Null is the fail-closed reading, so
-- every pre-existing row reads "Verification pending" the moment this lands -- which is what the
-- card said about those rows before wave L0, and is true: no proof the member can show exists for
-- them. A member or operator recovers a genuinely-issued credential through the existing
-- re-verification route (`POST /api/credential-utility/{id}/retry-mint` ->
-- `CredentialUtilityService.IssueThroughNodeAsync`), which is idempotent under the stable
-- per-credential issuance key and rewrites this column from the node's own answer.
--
-- Written in exactly one place: `MembershipCredentialServices.UpdateMintInfoAsync`, and only when
-- the caller states `CredentialProofOrigin.IssuanceNode` -- a REQUIRED DTO field, so a new writer
-- of the proof columns cannot reach this one without saying which path produced the proof, and
-- the enum's default value (`PlatformHeldMint` = 0) is the one that clears it.

ALTER TABLE public."MembershipCredential"
  ADD COLUMN IF NOT EXISTS "issuedThroughNodeAt" TIMESTAMP(3);
