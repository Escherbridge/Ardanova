import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";

export const PAYOUT_PROCESSING_PAUSED = true as const;
export const PAYOUT_PROCESSING_PAUSED_MESSAGE =
  "Payout requests are paused until a verified provider transfer and durable settlement reconciliation are available.";

export const payoutStageLabel = {
  PENDING: "Submitted record — settlement unconfirmed",
  PROCESSING: "Processing record — settlement unconfirmed",
  COMPLETED: "Completed record — verify settlement evidence",
  FAILED: "Failed",
  CANCELLED: "Cancelled",
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
