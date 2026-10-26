import { describe, expect, it } from "vitest";

import {
  ACCOUNT_SETUP_NOT_OPEN,
  ACCOUNT_SETUP_START_FAILED,
  ACCOUNT_SETUP_START_NOT_OPEN,
  ACCOUNT_STATUS_UNREADABLE,
  IDENTITY_VERIFICATION_NOT_OPEN,
  describeAccountSetupNotice,
  describeIdentityVerificationProblem,
} from "./account-setup-copy";

/** The backend 409 text both getStatus and ensure return when tenant onboarding is switched off. */
const NOT_CONFIGURED =
  "AZOA tenant-bound custodial account onboarding is not configured.";
const OPERATOR_WORDS = /azoa|tenant|custodial|not configured|node/i;

describe("account setup notice", () => {
  it.each([
    [
      "the backend's 409 code",
      { statusError: "Setup refused.", statusErrorCode: "PRECONDITION_FAILED" },
    ],
    [
      "operator wording only (no code attached)",
      { statusError: NOT_CONFIGURED },
    ],
  ])(
    "closes setup and says so plainly when the status read fails by %s",
    (_how, input) => {
      const notice = describeAccountSetupNotice(input);

      expect(notice.setupOpen).toBe(false);
      expect(notice.statusMessage).toBe(ACCOUNT_SETUP_NOT_OPEN);
    },
  );

  it("keeps setup on offer when the status read failed transiently", () => {
    expect(
      describeAccountSetupNotice({
        statusError: "Service temporarily unreachable.",
        statusErrorCode: "INTERNAL_SERVER_ERROR",
      }),
    ).toEqual({
      setupOpen: true,
      statusMessage: ACCOUNT_STATUS_UNREADABLE,
      ensureMessage: null,
    });
  });

  it.each([
    [NOT_CONFIGURED, ACCOUNT_SETUP_START_NOT_OPEN],
    ["Upstream timed out.", ACCOUNT_SETUP_START_FAILED],
  ])(
    "turns ensure failure %j into its own plain result",
    (ensureError, expected) => {
      const notice = describeAccountSetupNotice({ ensureError });

      expect(notice.ensureMessage).toBe(expected);
      expect(notice.ensureMessage).not.toBe(notice.statusMessage);
      expect(notice.ensureMessage).not.toMatch(OPERATOR_WORDS);
    },
  );
});

describe("identity verification problem copy", () => {
  const refused = {
    message: NOT_CONFIGURED,
    data: { code: "PRECONDITION_FAILED" },
  };

  it.each([
    ["capabilities refused as not configured", { capabilitiesError: refused }],
    ["begin refused as not configured", { beginKycError: refused }],
    ["no provider ready", { kycReady: false }],
  ])("reads as plainly not open when %s", (_case, input) => {
    expect(describeIdentityVerificationProblem(input)).toBe(
      IDENTITY_VERIFICATION_NOT_OPEN,
    );
  });

  it("explains a transient capabilities failure without the server's words", () => {
    const copy = describeIdentityVerificationProblem({
      capabilitiesError: {
        message: "boom",
        data: { code: "INTERNAL_SERVER_ERROR" },
      },
    });

    expect(copy).toMatch(
      /couldn't check whether identity verification is open/,
    );
    expect(copy).not.toContain("boom");
  });

  it("reports nothing when verification is open and nothing failed", () => {
    expect(describeIdentityVerificationProblem({ kycReady: true })).toBeNull();
  });
});

