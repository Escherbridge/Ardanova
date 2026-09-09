import { readFileSync } from "node:fs";

import { describe, expect, it } from "vitest";

import type { PayoutRequestDto } from "~/lib/api/ardanova/endpoints/payouts";
import {
  describeHoldRelease,
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
  it("no longer claims selling is paused, because the sell path is live", () => {
    expect(PAYOUT_PROCESSING_PAUSED).toBe(false);
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
    expect(payoutStageLabel.COMPLETED).toContain("in your balance");
    expect(payoutStageLabel.COMPLETED.toLowerCase()).not.toContain(
      "reconciled",
    );
  });
});

describe("a held holding's release date", () => {
  it("dates the hold when the API sent a date", () => {
    const release = describeHoldRelease("2026-03-14T00:00:00Z");

    expect(release.kind).toBe("dated");
    expect(release.kind === "dated" && release.on.toISOString()).toBe(
      "2026-03-14T00:00:00.000Z",
    );
  });

  it("gives no date when the API sent none, rather than inventing one", () => {
    // This is the shape a gate hold arrives in: the units are held, the release turns on the
    // project's own progress, and the API deliberately sends no date. A screen that filled one
    // in here would be promising a day the platform cannot keep.
    expect(describeHoldRelease(null).kind).toBe("undated");
    expect(describeHoldRelease(undefined).kind).toBe("undated");
    expect(describeHoldRelease("").kind).toBe("undated");
  });

  it("gives no date for a value it cannot read", () => {
    expect(describeHoldRelease("whenever the gate moves").kind).toBe("undated");
  });
});

/**
 * The half of the release-date fix that a unit test on a pure function cannot see.
 *
 * What it exists to stop coming back: the date was first rendered by
 * `components/equity/portfolio-holding-card.tsx`, which **nothing imported**. The service
 * populated the field, a component read it, and no route mounted that component — so the whole
 * item shipped a control nobody could see. This walks the actual chain, file by file: the
 * portfolio route mounts the component, and the component asks this module what the hold says.
 */
describe("the release date reaches a real screen", () => {
  const read = (path: string) => readFileSync(path, "utf8");

  it("is rendered by the component the portfolio route actually mounts", () => {
    const route = read("src/app/portfolio/page.tsx");

    expect(route).toContain('from "~/components/portfolio/holdings"');
    expect(route).toContain("<Holdings");
  });

  it("is read through this module, not re-derived in the component", () => {
    const component = read("src/components/portfolio/holdings.tsx");

    expect(component).toContain("describeHoldRelease");
    expect(component).toContain("holding.restrictedUntil");

    // Both branches must be present: a date when there is one, and an honest refusal to infer
    // one when there is not. A component carrying only the second is the state this fix left.
    expect(component).toContain("are held until");
    expect(component).toContain("This screen does not infer a release date");
  });
});
