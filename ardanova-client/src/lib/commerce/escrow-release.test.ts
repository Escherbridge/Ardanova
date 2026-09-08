import { describe, expect, it } from "vitest";

import { describeEscrowActionError } from "./escrow-release";

describe("describeEscrowActionError", () => {
  it("flags the backend's account-readiness refusal", () => {
    const result = describeEscrowActionError(
      "Complete your account setup before you can accept paid project work. " +
        "You can still create projects and collaborate while setup is pending.",
    );
    expect(result.requiresAccountSetup).toBe(true);
    expect(result.message).toContain("Complete your account setup");
  });

  it("leaves an unrelated failure alone", () => {
    const result = describeEscrowActionError(
      "Cannot release escrow in status DISPUTED",
    );
    expect(result.requiresAccountSetup).toBe(false);
    expect(result.message).toBe("Cannot release escrow in status DISPUTED");
  });

  it("is not confused by the phrase appearing mid-message", () => {
    const result = describeEscrowActionError(
      "The worker was told to complete your account setup manually.",
    );
    expect(result.requiresAccountSetup).toBe(false);
  });
});
