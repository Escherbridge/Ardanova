import { describe, expect, it } from "vitest";

import type { ValueActionAccountStatus } from "~/lib/contracts/project-create-contract";

import {
  ADD_FUNDS_UNAVAILABLE_MESSAGE,
  describeAddFundsAmountProblem,
  getAddFundsReadiness,
  getOnRampPresentation,
  isOnRampSettled,
  parseDollarsToMinorUnits,
  type OnRampReport,
} from "./on-ramp-status";

const readyAccount: ValueActionAccountStatus = {
  avatarId: "avatar-1",
  walletId: "wallet-1",
  walletAddress: "addr-1",
  kycStatus: "Approved",
  identityReady: true,
  kycReady: true,
  walletReady: true,
  ready: true,
};

describe("getAddFundsReadiness when the account read fails or resolves", () => {
  it.each([
    {
      name: "the site cannot open accounts yet (409 -> PRECONDITION_FAILED)",
      status: undefined,
      options: { loadFailed: true, loadErrorCode: "PRECONDITION_FAILED" },
      state: "not-open",
      canRetry: false,
    },
    {
      name: "a transient server failure",
      status: undefined,
      options: { loadFailed: true, loadErrorCode: "INTERNAL_SERVER_ERROR" },
      state: "unavailable",
      canRetry: true,
    },
    {
      name: "a failure with no code (network)",
      status: undefined,
      options: { loadFailed: true, loadErrorCode: null },
      state: "unavailable",
      canRetry: true,
    },
    {
      name: "still loading",
      status: undefined,
      options: { pending: true },
      state: "checking",
      canRetry: false,
    },
    {
      name: "a ready account",
      status: readyAccount,
      options: {},
      state: "ready",
      canRetry: false,
    },
    {
      name: "an account held for review",
      status: { ...readyAccount, ready: false, kycStatus: "UnderReview" },
      options: {},
      state: "under-review",
      canRetry: false,
    },
  ])("$name -> $state", ({ status, options, state, canRetry }) => {
    const readiness = getAddFundsReadiness(status, options);

    expect(readiness.state).toBe(state);
    expect(readiness.canRetry).toBe(canRetry);
    expect(readiness.canContinue).toBe(state === "ready");
  });

  it("never promises a retry will help when the site is not open, and says nothing was charged", () => {
    const readiness = getAddFundsReadiness(undefined, {
      loadFailed: true,
      loadErrorCode: "PRECONDITION_FAILED",
    });
    const copy = `${readiness.heading} ${readiness.detail}`.toLowerCase();

    expect(copy).not.toContain("try again");
    expect(copy).toContain("nothing was charged");
    expect(readiness.action).toBeNull();
  });
});

describe("describeAddFundsAmountProblem", () => {
  it.each([
    ["", "Choose an amount"],
    ["1,50", "Use a dot for cents"],
    ["1 000", "Use a dot for cents"],
    ["1.000,00", "Use a dot for cents"],
    ["40.505", "whole cents"],
    ["abc", "like 40 or 40.50"],
    ["0.50", "smallest amount"],
    ["10000.01", "most you can add"],
  ])("%j -> mentions %j", (raw, expected) => {
    expect(describeAddFundsAmountProblem(raw)).toContain(expected);
  });

  it.each(["40", "40.50", "$25", " 1 ", "10000"])(
    "%j is a usable amount",
    (raw) => {
      expect(describeAddFundsAmountProblem(raw)).toBeNull();
    },
  );

  it("stays strict: a locale-formatted amount is refused, never re-read as another charge", () => {
    expect(parseDollarsToMinorUnits("1,50")).toBeNull();
    expect(parseDollarsToMinorUnits("1 000")).toBeNull();
    expect(parseDollarsToMinorUnits("40.50")).toBe(4050);
  });
});

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
