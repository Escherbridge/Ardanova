import { describe, expect, it } from "vitest";

import { normalizeInternalCallbackUrl } from "~/lib/auth-navigation";

import { destinationLabel } from "./destination-label";

describe("sign-in return destination label", () => {
  it.each([
    ["/portfolio/withdraw", "withdraw money"],
    ["/portfolio/withdraw?amount=10", "withdraw money"],
    ["/portfolio/add-funds#card", "add money to your balance"],
    ["/portfolio", "your balance and holdings"],
    ["/portfolio/history", "your balance and holdings"],
    ["/swap?project=alpha", "sell your units"],
    ["/settings/payouts", "your payout details"],
    ["/settings/verification", "verification"],
    ["/projects/alpha", "projects"],
    ["/portfolios", null],
    ["/settings", null],
    ["/dashboard", null],
    ["https://evil.example/portfolio/withdraw", null],
    ["//evil.example/portfolio", null],
  ])("the sign-in page names %s as %s", (requested, expected) => {
    // Run through the same normalizer the page uses, so external URLs collapse to the default first.
    expect(destinationLabel(normalizeInternalCallbackUrl(requested))).toBe(expected);
  });
});
