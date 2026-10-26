import { readFileSync } from "node:fs";
import { join } from "node:path";

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
      "operator wording only (dashboard caller passes no code)",
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

describe("verification surfaces wire the plain copy", () => {
  const srcRoot = join(__dirname, "..", "..");
  const source = (relative: string) =>
    readFileSync(join(srcRoot, relative), "utf8");

  it("the card gates Start account setup on the notice and never renders raw errors", () => {
    const card = source("components/azoa/custodial-account-card.tsx");

    expect(card).toMatch(
      /offerSetup\s*=\s*needsProvisioning && notice\.setupOpen/,
    );
    expect(card).toMatch(/\{offerSetup && \(/);
    expect(card).not.toMatch(/\bensureError \?\?|\bstatusError \?\?/);
  });

  it("each step-state span carries role=img so its aria-label is permitted", () => {
    expect(source("components/azoa/custodial-account-card.tsx")).toMatch(
      /role="img"\s+aria-label=\{step\.complete \? "Complete" : "Not complete"\}/,
    );
  });

  it("the verification page passes the status code and drops the infrastructure jargon", () => {
    const page = source("app/settings/verification/page.tsx");

    expect(page).toContain("statusErrorCode={custodyStatus.error?.data?.code}");
    expect(page).not.toMatch(
      /Azoa manages|via Azoa|Provider selected by node operator|on this Azoa node|Azoa approved|\{(?:beginKyc|capabilities)\.error\?\.message/,
    );
  });

  it("the mobile drawer opens on the first nav link, not Sign out", () => {
    expect(source("components/app-topbar.tsx")).toMatch(
      /onOpenAutoFocus=\{focusFirstNavigationLink\}/,
    );
  });
});
