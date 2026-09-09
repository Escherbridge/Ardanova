import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";

/**
 * Selling is live (Wave 8): the node runs the exchange and ArdaNova records what it did.
 * The flag is kept because a deployment can still be without a wired exchange, in which case
 * the API refuses each sale on its own - this constant claims nothing the server does not.
 */
export const PAYOUT_PROCESSING_PAUSED = false as const;
export const PAYOUT_PROCESSING_PAUSED_MESSAGE =
  "Selling units is not switched on for this deployment, so nothing was moved.";

/** Plain words for a sale's stage. The API's own status names are never shown to anyone. */
export const payoutStageLabel = {
  PENDING: "Waiting to go through",
  PROCESSING: "Going through now",
  COMPLETED: "Done — the funds are in your balance",
  FAILED: "Did not go through — your units were returned",
  CANCELLED: "Called off — your units were returned",
} as const satisfies Record<PayoutRequestDto["status"], string>;

export interface PayoutRecordSummary {
  submittedUsd: number;
  completedRecordUsd: number;
  pendingCount: number;
}

export function summarizePayoutRecords(
  payouts: readonly PayoutRequestDto[],
): PayoutRecordSummary {
  return payouts.reduce<PayoutRecordSummary>(
    (summary, payout) => {
      const amount = payout.usdAmount ?? 0;

      if (payout.status === "PENDING" || payout.status === "PROCESSING") {
        summary.submittedUsd += amount;
      }
      if (payout.status === "PENDING") summary.pendingCount += 1;
      if (payout.status === "COMPLETED") {
        summary.completedRecordUsd += amount;
      }

      return summary;
    },
    { submittedUsd: 0, completedRecordUsd: 0, pendingCount: 0 },
  );
}

export function getCancellablePayouts(
  payouts: readonly PayoutRequestDto[],
  sourceProjectTokenConfigId?: string,
): PayoutRequestDto[] {
  return payouts.filter(
    (payout) =>
      payout.status === "PENDING" &&
      payout.sourceProjectTokenConfigId !== null &&
      payout.sourceTokenAmount > 0 &&
      (!sourceProjectTokenConfigId ||
        payout.sourceProjectTokenConfigId === sourceProjectTokenConfigId),
  );
}

/**
 * What a held holding may honestly say about WHEN it is released.
 *
 * The API sends `restrictedUntil` only when the date is the whole of the remaining hold: a
 * project gate moves on an outcome, not on a date, so a holding the gate is also holding comes
 * back with no date at all (TokenBalanceService.ReadLiquidityAsync -> UnitWindow.DatedReleaseFor).
 * This function therefore never has to decide whether a date is honest - it only has to refuse to
 * invent one, which is what an unparseable value gets.
 */
export function describeHoldRelease(
  restrictedUntil: string | null | undefined,
): { readonly kind: "dated"; readonly on: Date } | { readonly kind: "undated" } {
  if (!restrictedUntil) return { kind: "undated" };

  const on = new Date(restrictedUntil);
  return Number.isNaN(on.getTime()) ? { kind: "undated" } : { kind: "dated", on };
}
