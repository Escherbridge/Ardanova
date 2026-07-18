import { describe, expect, it } from "vitest";

import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";
import {
  getCancellablePayouts,
  PAYOUT_PROCESSING_PAUSED,
  payoutStageLabel,
  summarizePayoutRecords,
} from "./portfolio-contract";

function payout(overrides: Partial<PayoutRequestDto> = {}): PayoutRequestDto {
  return {
    id: "payout-1",
    userId: "user-1",
    sourceProjectTokenConfigId: "config-1",
    sourceTokenAmount: 10,
    ardaTokenAmount: 9,
    usdAmount: 5,
    status: "PENDING",
    holderClass: "CONTRIBUTOR",
    gateStatusAtRequest: "ACTIVE",
    conversionTxHash: null,
    payoutTxHash: null,
    stripePayoutId: null,
    failureReason: null,
    requestedAt: "2026-07-17T12:00:00Z",
    processedAt: null,
    completedAt: null,
    ...overrides,
  };
}

describe("portfolio payout contract", () => {
  it("keeps creation disabled while settlement processing is unavailable", () => {
    expect(PAYOUT_PROCESSING_PAUSED).toBe(true);
  });

  it("keeps submitted and completed-record totals separate", () => {
    const summary = summarizePayoutRecords([
      payout({ status: "PENDING", usdAmount: 10 }),
      payout({ id: "payout-2", status: "PROCESSING", usdAmount: 20 }),
      payout({ id: "payout-3", status: "COMPLETED", usdAmount: 30 }),
      payout({ id: "payout-4", status: "COMPLETED", usdAmount: null }),
    ]);

    expect(summary).toEqual({
      submittedUsd: 30,
      completedRecordUsd: 30,
      pendingCount: 1,
    });
  });

  it("allows cancellation only for pending records in the selected holding", () => {
    const records = [
      payout(),
      payout({ id: "payout-2", sourceProjectTokenConfigId: "config-2" }),
      payout({ id: "payout-3", status: "PROCESSING" }),
      payout({ id: "payout-4", sourceTokenAmount: 0 }),
      payout({ id: "payout-5", sourceProjectTokenConfigId: null }),
    ];

    expect(
      getCancellablePayouts(records, "config-1").map(({ id }) => id),
    ).toEqual(["payout-1"]);
  });

  it("does not label an API completion state as reconciled", () => {
    expect(payoutStageLabel.COMPLETED).toContain("verify settlement evidence");
    expect(payoutStageLabel.COMPLETED.toLowerCase()).not.toContain(
      "reconciled",
    );
  });
});
