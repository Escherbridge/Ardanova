import { describe, expect, it } from "vitest";

import {
  ADD_FUNDS_UNAVAILABLE_MESSAGE,
  getOnRampPresentation,
  isOnRampSettled,
  type OnRampReport,
} from "./on-ramp-status";

/**
 * These are the product rules for the add-funds return page, pinned as assertions rather than
 * left as a doc comment: only one input may say the money arrived, an unrecognised word is never
 * read as success, and no copy on this path may name a piece of blockchain machinery.
 */
describe("getOnRampPresentation", () => {
  it("treats funds_applied as the ONE input that means the money is there", () => {
    const presentation = getOnRampPresentation({
      settlementState: "funds_applied",
    });

    expect(presentation.outcome).toBe("added");
    expect(presentation.poll).toBe(false);
  });

  it("says we will write, and keeps polling, while the payment is only received", () => {
    const presentation = getOnRampPresentation({
      settlementState: "payment_received",
    });

    expect(presentation.outcome).toBe("on-its-way");
    expect(presentation.poll).toBe(true);
  });

  it("says the payment did not go through, and that nothing was charged", () => {
    const presentation = getOnRampPresentation({
      settlementState: "payment_failed",
    });

    expect(presentation.outcome).toBe("went-wrong");
    expect(presentation.detail).toContain("Nothing was charged");
  });

  it("refuses to guess: an unrecognised word is unconfirmed, never added", () => {
    for (const report of [
      { settlementState: "definitely_fine" },
      { status: "totally_paid" },
      { status: "", settlementState: "" },
      {},
    ] satisfies OnRampReport[]) {
      expect(getOnRampPresentation(report).outcome).toBe("unconfirmed");
    }
  });

  it("treats an absent report as unconfirmed, because a redirect is not a receipt", () => {
    expect(getOnRampPresentation(null).outcome).toBe("unconfirmed");
    expect(getOnRampPresentation(undefined).outcome).toBe("unconfirmed");
  });

  it("lets a settlement word outrank a payment word when both are present", () => {
    const presentation = getOnRampPresentation({
      status: "failed",
      settlementState: "funds_applied",
    });

    expect(presentation.outcome).toBe("added");
  });

  it("normalises casing and surrounding whitespace from the server", () => {
    expect(
      getOnRampPresentation({ settlementState: "  FUNDS_APPLIED " }).outcome,
    ).toBe("added");
  });

  it("only reports settled once the answer will not change on its own", () => {
    expect(isOnRampSettled({ settlementState: "funds_applied" })).toBe(true);
    expect(isOnRampSettled({ settlementState: "payment_received" })).toBe(false);
    expect(isOnRampSettled({ status: "awaiting_payment" })).toBe(false);
    expect(isOnRampSettled(null)).toBe(true);
  });
});

describe("the words a non-crypto sponsor is shown", () => {
  const forbidden = [
    "wallet",
    "chain",
    "blockchain",
    "token",
    "stripe",
    "gas",
    "address",
    "mnemonic",
    "asset id",
    "transaction hash",
  ];

  const everyReport: (OnRampReport | null | undefined)[] = [
    { settlementState: "funds_applied" },
    { settlementState: "payment_received" },
    { settlementState: "payment_failed" },
    { status: "starting" },
    { status: "awaiting_payment" },
    { status: "processing" },
    { status: "complete" },
    { status: "failed" },
    { status: "expired" },
    { status: "who_knows" },
    null,
    undefined,
  ];

  it("never names a piece of blockchain machinery in any reachable copy", () => {
    for (const report of everyReport) {
      const { heading, detail } = getOnRampPresentation(report);
      const copy = `${heading} ${detail}`.toLowerCase();

      for (const word of forbidden) {
        expect(copy, `"${word}" reached the sponsor via: ${copy}`).not.toContain(
          word,
        );
      }
    }
  });

  it("keeps the unavailable notice free of internals and clear that nothing can charge", () => {
    const copy = ADD_FUNDS_UNAVAILABLE_MESSAGE.toLowerCase();

    for (const word of forbidden) {
      expect(copy).not.toContain(word);
    }
    expect(copy).toContain("charge");
  });
});
