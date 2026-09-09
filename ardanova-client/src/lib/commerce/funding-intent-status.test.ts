import { describe, expect, it } from "vitest";

import {
  describeFundingError,
  getFundingIntentPresentation,
} from "./funding-intent-status";

describe("funding intent return presentation", () => {
  it.each([
    ["DRAFT", true, "waiting"],
    ["SETTLEMENT_PENDING", true, "waiting"],
    ["SETTLED", false, "confirmed"],
    ["HOLDING", false, "went-wrong"],
    ["REJECTED", false, "went-wrong"],
    ["CANCELLED", false, "went-wrong"],
    ["FAILED", false, "went-wrong"],
  ] as const)(
    "uses server state %s with polling=%s and outcome %s",
    (status, poll, outcome) => {
      const presentation = getFundingIntentPresentation({ status });
      expect(presentation.poll).toBe(poll);
      expect(presentation.outcome).toBe(outcome);
    },
  );

  it("does not infer settlement from a missing or unknown state", () => {
    expect(getFundingIntentPresentation()).toMatchObject({
      outcome: "went-wrong",
      poll: false,
    });
    expect(
      getFundingIntentPresentation({ status: "provider_says_success" }),
    ).toMatchObject({
      outcome: "went-wrong",
      poll: false,
    });
  });

  it("surfaces the server's own reason detail on a stopped sponsorship", () => {
    const presentation = getFundingIntentPresentation({
      status: "HOLDING",
      reasonDetail: "Your account setup lapsed. Nothing was taken.",
    });
    expect(presentation.detail).toBe(
      "Your account setup lapsed. Nothing was taken.",
    );
  });

  it("falls back to a safe detail when the server gives no reason", () => {
    const presentation = getFundingIntentPresentation({ status: "FAILED" });
    expect(presentation.detail).toContain("Nothing was taken");
  });

  it("counts down to a supplied release date instead of the fallback window", () => {
    const soon = new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString();
    const presentation = getFundingIntentPresentation({
      status: "SETTLED",
      releaseNotBefore: soon,
    });
    expect(presentation.heading).toMatch(/yours in 2 days?/);
  });

  it("names no day at all when the server supplied no release date", () => {
    const presentation = getFundingIntentPresentation({ status: "SETTLED" });
    // The old rule invented a 30-day window the system never committed to. A date is
    // the server's to state, so with none the copy stays vague rather than promising one.
    expect(presentation.heading).toBe("Confirmed — yours after a short hold");
    expect(presentation.heading).not.toMatch(/\d+ days?/);
  });
});

describe("server error codes mapped to human copy", () => {
  it("prefers the server's own message when one is present", () => {
    expect(
      describeFundingError({
        message: "This project has no price yet, so nothing was taken.",
        data: { code: "BAD_REQUEST" },
      }),
    ).toBe("This project has no price yet, so nothing was taken.");
  });

  it("falls back to a code-keyed message when the server sent none", () => {
    expect(
      describeFundingError({ message: "", data: { code: "UNAUTHORIZED" } }),
    ).toBe("Please sign in again, then try supporting this project.");
  });

  it("never blames the sponsor for an unrecognised or missing error", () => {
    expect(describeFundingError(null)).toBe(
      "Something went wrong, and nothing was taken. Try again in a moment.",
    );
    expect(describeFundingError({ data: { code: "IM_A_TEAPOT" } })).toBe(
      "Something went wrong, and nothing was taken. Try again in a moment.",
    );
  });
});
