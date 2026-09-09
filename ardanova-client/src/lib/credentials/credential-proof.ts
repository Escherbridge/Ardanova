/**
 * Turns the node's chain vocabulary into the only things a member is told about a credential's
 * independent proof: it is verified, it is still being prepared, it is paused, or it was
 * withdrawn.
 *
 * Nothing here says chain, blockchain, Algorand, asset, mint, burn or wallet. The node's own
 * fields (`isOnChain`, `chainVerified`, `mintTxHash`, `asaInfo.creatorAddress`, ...) never reach
 * a screen; they are matched here and replaced. This mirrors `~/lib/commerce/on-ramp-status.ts`.
 *
 * Why it exists: a credential detail page is how a worker's earned tier is displayed, so it is a
 * primary non-crypto surface, not an operator one. It previously rendered "Blockchain Status",
 * "Minted on Algorand", "Algorand Standard Asset", "ASA #", raw transaction hashes and a raw
 * creator address to any signed-in member. `scripts/verify-product-vocabulary.mjs` is the control
 * that keeps that from coming back; this module is what the screens read instead.
 */

/** What the node reports about one credential. Every field here is the node's own word. */
export type CredentialProofReport = {
  /** The credential's lifecycle status, in the API's enum values. */
  status?: string | null;
  /**
   * Whether the credential went through the issuance node — the one path that puts a proof in
   * the MEMBER's hands. Read as `=== true`, so a report that omits it (an older API, a caller
   * that only holds a partial record) reads as NOT verifiable.
   *
   * This is the gate, and it lives HERE rather than at each screen. Every identifier below is
   * ignored while it is anything but true: the shipped simulated provider fabricated an
   * `assetId` outright, and the address-based mint it replaced put a real asset in the
   * PLATFORM's hands. Two screens read this module — the card and the credential detail page
   * — and gating at one of them is how they came to say "Verification pending" and "Verified"
   * about the same credential.
   */
  isIndependentlyVerifiable?: boolean | null;
  /** The node's proof identifiers. Either one, if present, is a quotable reference. */
  assetId?: string | null;
  mintTxHash?: string | null;
  /** The node's chain readings. Absent when the caller only holds the credential record. */
  isOnChain?: boolean | null;
  chainVerified?: boolean | null;
};

/** Whether the node says this credential went through the issuance path. Absent means no. */
function wasIndependentlyIssued(
  report: CredentialProofReport | null | undefined,
): boolean {
  return report?.isIndependentlyVerifiable === true;
}

/** The only proof readings a member-facing screen may show. */
export type CredentialProofState =
  | "withdrawn"
  | "paused"
  | "verified"
  | "recorded"
  | "preparing";

export type CredentialProofPresentation = {
  state: CredentialProofState;
  label: string;
  detail: string;
};

const withdrawn: CredentialProofPresentation = {
  state: "withdrawn",
  label: "Withdrawn",
  detail: "This credential was withdrawn and is no longer in effect.",
};

const paused: CredentialProofPresentation = {
  state: "paused",
  label: "Paused",
  detail: "This credential is paused. It stays on record and can be restored.",
};

const verified: CredentialProofPresentation = {
  state: "verified",
  label: "Verified",
  detail:
    "Checked against the independent record just now, and it matches what ArdaNova holds.",
};

const recorded: CredentialProofPresentation = {
  state: "recorded",
  label: "Verified",
  detail: "Independently verifiable outside ArdaNova.",
};

const preparing: CredentialProofPresentation = {
  state: "preparing",
  label: "Verification pending",
  detail:
    "This credential is in effect. Its independent proof is still being prepared.",
};

function normalize(value: string | null | undefined): string {
  return (value ?? "").trim().toUpperCase();
}

/**
 * The single reference a member can quote to prove this credential. One value, never a list of
 * identifiers, and never labelled with what produced it.
 */
export function getCredentialProofReference(
  report: CredentialProofReport | null | undefined,
): string | null {
  if (!report) return null;
  // An un-issued credential has no reference, however many identifiers the record carries.
  if (!wasIndependentlyIssued(report)) return null;
  for (const candidate of [report.assetId, report.mintTxHash]) {
    const trimmed = candidate?.trim() ?? "";
    if (trimmed.length > 0) return trimmed;
  }
  return null;
}

/**
 * Maps one node-owned report onto the copy a member sees.
 *
 * Status outranks proof: a withdrawn or paused credential reads as withdrawn or paused however
 * good its proof is. Below that, a live verification outranks a recorded one, and a report with
 * no reference at all is "still being prepared" — a waiting state, never a failure.
 */
export function getCredentialProofPresentation(
  report: CredentialProofReport | null | undefined,
): CredentialProofPresentation {
  if (!report) return preparing;

  const status = normalize(report.status);
  if (status === "REVOKED") return withdrawn;
  if (status === "SUSPENDED") return paused;

  // Below status, the issuance path decides. Identifier PRESENCE decides nothing: a row
  // carrying a fabricated identifier reads "still being prepared", which is true — no proof
  // the member can show exists for it.
  if (!wasIndependentlyIssued(report)) return preparing;

  if (report.chainVerified === true) return verified;
  if (report.isOnChain === true) return recorded;
  if (getCredentialProofReference(report)) return recorded;

  return preparing;
}

/** Whether a member's credential is transferable, said without the word "soulbound". */
export function getTransferabilityLabel(
  isTransferable: boolean | null | undefined,
): string {
  return isTransferable === true ? "Yes" : "No — it stays with you";
}
