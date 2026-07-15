import { describe, expect, it } from "vitest";

import { getFundingIntentPresentation } from "./funding-intent-status";

describe("funding intent return presentation", () => {
  it.each([
    ["DRAFT", true],
    ["AWAITING_PAYMENT", true],
    ["PAYMENT_VERIFIED", true],
    ["SETTLEMENT_PENDING", true],
    ["SETTLED", false],
    ["REJECTED", false],
    ["CANCELLED", false],
    ["FAILED", false],
  ])("uses server state %s and polling=%s", (status, poll) => {
    expect(getFundingIntentPresentation(status).poll).toBe(poll);
  });

  it("does not infer payment success from a missing or unknown state", () => {
    expect(getFundingIntentPresentation()).toMatchObject({
      heading: "FUNDING STATUS UNAVAILABLE",
      poll: false,
    });
    expect(getFundingIntentPresentation("provider_says_success")).toMatchObject(
      {
        heading: "FUNDING STATUS UNAVAILABLE",
        poll: false,
      },
    );
  });
});
