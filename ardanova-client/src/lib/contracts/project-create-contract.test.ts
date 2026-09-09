import { readFileSync } from "node:fs";

import { describe, expect, it } from "vitest";

import {
  ARDA_GENESIS_USD_RATE,
  ARDA_TICKER,
  PROJECT_FUNDING_GOAL_MAX_DOLLARS,
  PROJECT_FUNDING_GOAL_MIN_DOLLARS,
  PROJECT_FUNDING_UNIT,
  convertDollarsToPlatformUnits,
  createEmptyProjectFormData,
  describeFundingGoalInPlatformUnits,
  describeValueActionUnderReview,
  evaluateValueActionAccount,
  isProjectRoleValueBearing,
  isValueActionAccountReady,
  parseFundingGoalDollars,
  parseProjectCreateDraft,
  serializeProjectCreateDraft,
  toProjectRoleOpportunityInput,
  toOpportunityCompensation,
  toOptionalIsoDate,
  VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE,
  VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE,
  VALUE_ACTION_UNDER_REVIEW_KYC_STATUS,
} from "./project-create-contract";

describe("project creation contract", () => {
  it("maps the backend share enum values to opportunity compensation", () => {
    expect(toOpportunityCompensation("FIXED_SHARES")).toBe("fixed");
    expect(toOpportunityCompensation("HOURLY_SHARES")).toBe("hourly");
    expect(toOpportunityCompensation("EQUITY_PERCENT")).toBe("negotiable");
    expect(toOpportunityCompensation("BOUNTY")).toBe("fixed");
  });

  it("requires concrete wallet evidence before a value-bearing action", () => {
    const ready = readyAccountStatus();

    expect(isValueActionAccountReady(ready)).toBe(true);
    expect(
      isValueActionAccountReady({
        ...ready,
        walletId: null,
        walletReady: false,
      }),
    ).toBe(false);
    expect(
      isValueActionAccountReady({ ...ready, kycStatus: "Pending" }),
    ).toBe(false);
    expect(
      isValueActionAccountReady({ ...ready, kycStatus: "Rejected" }),
    ).toBe(false);
    expect(
      isValueActionAccountReady({ ...ready, avatarId: null }),
    ).toBe(false);
    expect(
      isValueActionAccountReady({ ...ready, walletAddress: null }),
    ).toBe(false);
    expect(isValueActionAccountReady({ ...ready, ready: false })).toBe(false);
    expect(
      isValueActionAccountReady({ ...ready, identityReady: false }),
    ).toBe(false);
    expect(isValueActionAccountReady({ ...ready, kycReady: false })).toBe(false);
  });

  it("reads an unfinished account as setup rather than an outage", () => {
    const evaluation = evaluateValueActionAccount({
      ...readyAccountStatus(),
      kycStatus: "Pending",
      kycReady: false,
      ready: false,
    });

    expect(evaluation.state).toBe("setup-incomplete");
    expect(evaluation.ready).toBe(false);
    expect(evaluation.message).toBe(VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE);
  });

  it("never blames a person's setup when the account could not be read", () => {
    const unreadable = [
      evaluateValueActionAccount(undefined),
      evaluateValueActionAccount(null),
      evaluateValueActionAccount(readyAccountStatus(), { loadFailed: true }),
      evaluateValueActionAccount({
        ...readyAccountStatus(),
        identityReady: false,
        kycReady: false,
        walletReady: false,
        ready: false,
        unavailableReason: "Custody is disabled on this node.",
      }),
    ];

    for (const evaluation of unreadable) {
      expect(evaluation.state).toBe("service-unavailable");
      expect(evaluation.ready).toBe(false);
      expect(evaluation.message).toBe(VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE);
    }
  });

  it("fails closed when a node outage arrives beside a ready-looking account", () => {
    const evaluation = evaluateValueActionAccount({
      ...readyAccountStatus(),
      unavailableReason: "Custody is disabled on this node.",
    });

    expect(evaluation.state).toBe("service-unavailable");
    expect(
      isValueActionAccountReady({
        ...readyAccountStatus(),
        unavailableReason: "Custody is disabled on this node.",
      }),
    ).toBe(false);
  });

  it("tells a held account a reviewer owes it a decision, not to redo setup", () => {
    const evaluation = evaluateValueActionAccount({
      ...readyAccountStatus(),
      kycStatus: VALUE_ACTION_UNDER_REVIEW_KYC_STATUS,
      reviewDueAt: "2026-09-21T13:30:00Z",
    });

    expect(evaluation.state).toBe("under-review");
    expect(evaluation.ready).toBe(false);
    expect(evaluation.message).toContain("21 September 2026");

    // The two messages this outcome exists to stop being mistaken for.
    expect(evaluation.message).not.toBe(VALUE_ACTION_SETUP_INCOMPLETE_MESSAGE);
    expect(evaluation.message).not.toBe(VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE);
  });

  it("refuses a held account even though every other readiness signal is present", () => {
    expect(
      isValueActionAccountReady({
        ...readyAccountStatus(),
        kycStatus: VALUE_ACTION_UNDER_REVIEW_KYC_STATUS,
      }),
    ).toBe(false);
  });

  it("says a decision is due shortly when the node named no review date", () => {
    const evaluation = evaluateValueActionAccount({
      ...readyAccountStatus(),
      kycStatus: VALUE_ACTION_UNDER_REVIEW_KYC_STATUS,
    });

    expect(evaluation.state).toBe("under-review");
    expect(evaluation.message).toBe(describeValueActionUnderReview(null));
    expect(describeValueActionUnderReview("not-a-date")).toBe(
      describeValueActionUnderReview(null),
    );
  });

  it("never discloses why a review is open", () => {
    const message = describeValueActionUnderReview("2026-09-21T13:30:00Z");

    for (const disclosure of [
      "aml",
      "sanction",
      "screening",
      "watchlist",
      "risk",
      "match",
    ]) {
      expect(message.toLowerCase()).not.toContain(disclosure);
    }
  });

  it("reports a node outage ahead of a pending review", () => {
    const evaluation = evaluateValueActionAccount({
      ...readyAccountStatus(),
      kycStatus: VALUE_ACTION_UNDER_REVIEW_KYC_STATUS,
      reviewDueAt: "2026-09-21T13:30:00Z",
      unavailableReason: "Custody is disabled on this node.",
    });

    expect(evaluation.state).toBe("service-unavailable");
    expect(evaluation.message).toBe(VALUE_ACTION_SERVICE_UNAVAILABLE_MESSAGE);
  });

  it("keeps explicitly compensated roles out of pre-setup project drafts", () => {
    const baseRole = {
      id: "role-1",
      title: "Contributor",
      description: "A valid contributor role description",
      compensationModel: "" as const,
      shareAmount: "",
      equityPercent: "",
      isOpenForApplications: true,
      projectRole: "CONTRIBUTOR" as const,
    };

    expect(isProjectRoleValueBearing(baseRole)).toBe(false);
    expect(
      isProjectRoleValueBearing({
        ...baseRole,
        compensationModel: "FIXED_SHARES",
      }),
    ).toBe(true);
    expect(
      isProjectRoleValueBearing({ ...baseRole, shareAmount: "120" }),
    ).toBe(true);
    expect(
      isProjectRoleValueBearing({ ...baseRole, equityPercent: "2.5" }),
    ).toBe(true);
  });

  it("does not label an unpaid role as negotiable compensation", () => {
    const opportunity = toProjectRoleOpportunityInput({
      projectId: "project-1",
      projectTitle: "Neighborhood heat commons",
      tags: ["mapping"],
      role: {
        id: "role-1",
        title: "Volunteer mapper",
        description: "Help coordinate field mapping and document observations.",
        compensationModel: "",
        shareAmount: "",
        equityPercent: "",
        isOpenForApplications: true,
        projectRole: "CONTRIBUTOR",
      },
    });

    expect(opportunity.compensationModel).toBeUndefined();
    expect(opportunity.compensationAmount).toBeUndefined();
    expect(opportunity.equityPercent).toBeUndefined();
  });

  it("omits undated milestones without constructing an invalid date", () => {
    expect(toOptionalIsoDate("")).toBeUndefined();
    expect(toOptionalIsoDate("   ")).toBeUndefined();
    expect(toOptionalIsoDate("2026-08-01")).toBe("2026-08-01T00:00:00.000Z");
    expect(() => toOptionalIsoDate("not-a-date")).toThrow(
      "Invalid milestone target date",
    );
  });

  it("preserves role allocation and application state in the opportunity contract", () => {
    const opportunity = toProjectRoleOpportunityInput({
      projectId: "project-1",
      projectTitle: "Neighborhood heat commons",
      tags: ["mapping"],
      role: {
        id: "role-1",
        title: "Field mapper",
        description: "",
        compensationModel: "HYBRID",
        shareAmount: "120",
        equityPercent: "2.5",
        isOpenForApplications: false,
        projectRole: "CONTRIBUTOR",
      },
    });

    expect(opportunity).toMatchObject({
      compensationAmount: 120,
      equityPercent: 2.5,
      isOpenForApplications: false,
      compensationModel: "negotiable",
    });
    expect(opportunity.description.length).toBeGreaterThanOrEqual(20);
  });

  it("rejects invalid typed role compensation instead of dropping it", () => {
    expect(() =>
      toProjectRoleOpportunityInput({
        projectId: "project-1",
        projectTitle: "Project",
        tags: [],
        role: {
          id: "role-1",
          title: "Contributor",
          description: "A valid contributor role description",
          compensationModel: "EQUITY_PERCENT",
          shareAmount: "",
          equityPercent: "101",
          isOpenForApplications: true,
          projectRole: "",
        },
      }),
    ).toThrow("between 0 and 100");
  });

  it("round-trips a versioned in-progress draft", () => {
    const formData = {
      ...createEmptyProjectFormData(),
      title: "Neighborhood heat commons",
      milestones: [
        {
          id: "milestone-1",
          title: "Map cooling spaces",
          description: "",
          targetDate: "",
        },
      ],
    };

    const serialized = serializeProjectCreateDraft({
      currentStep: 3,
      formData,
    });

    expect(parseProjectCreateDraft(serialized)).toEqual({
      version: 1,
      currentStep: 3,
      formData,
    });
  });

  it("round-trips a validated partial-child recovery plan", () => {
    const formData = createEmptyProjectFormData();
    const recovery = {
      projectId: "project-1",
      projectSlug: "project-one",
      resourceIds: ["resource-1"],
      milestoneIds: [],
      roleIds: ["role-1"],
      founderMembershipPending: true,
      warnings: ["Resource could not be saved"],
    };

    const serialized = serializeProjectCreateDraft({
      currentStep: 4,
      formData,
      recovery,
    });

    expect(parseProjectCreateDraft(serialized)?.recovery).toEqual(recovery);
  });

  it("rejects corrupt, oversized, and unsupported draft payloads", () => {
    expect(parseProjectCreateDraft("not json")).toBeNull();
    expect(parseProjectCreateDraft("x".repeat(2_000_001))).toBeNull();
    expect(
      parseProjectCreateDraft(
        JSON.stringify({
          version: 2,
          currentStep: 0,
          formData: createEmptyProjectFormData(),
        }),
      ),
    ).toBeNull();
    expect(
      parseProjectCreateDraft(
        JSON.stringify({
          version: 1,
          currentStep: 0,
          formData: {
            ...createEmptyProjectFormData(),
            title: "x".repeat(20_001),
          },
        }),
      ),
    ).toBeNull();
  });
});

describe("the ask is entered in dollars", () => {
  const wizardSource = readFileSync(
    new URL("../../app/projects/create/page.tsx", import.meta.url),
    "utf8",
  );
  const contractSource = readFileSync(
    new URL("./project-create-contract.ts", import.meta.url),
    "utf8",
  );

  it("takes a five-thousand-dollar ask as five thousand, not five hundred thousand", () => {
    // A founder asking for $5,000 types 5000. What is stored is dollars, which is also the
    // unit the raise accumulates in, so the ask and the progress against it never need a
    // conversion. The 500,000 figure now lives only in the caption, derived for them.
    expect(parseFundingGoalDollars("5000")).toBe(5000);
    expect(PROJECT_FUNDING_UNIT).toBe("USD");
    expect(convertDollarsToPlatformUnits(5000, null)).toBe(500_000);
  });

  it("derives the platform-unit figure exactly at the genesis rate", () => {
    expect(ARDA_GENESIS_USD_RATE).toBe(0.01);
    expect(convertDollarsToPlatformUnits(1, null)).toBe(100);
    expect(convertDollarsToPlatformUnits(5000, 0.02)).toBe(250_000);
    // No usable rate means no figure, never an invented one.
    expect(convertDollarsToPlatformUnits(5000, 0)).toBeNull();
    expect(convertDollarsToPlatformUnits(Number.NaN, null)).toBeNull();
    expect(describeFundingGoalInPlatformUnits(5000, 0)).toBe("");
  });

  it("puts the ticker in the caption and says the figure is approximate", () => {
    const caption = describeFundingGoalInPlatformUnits(5000, null);

    expect(caption).toContain(`500,000 ${ARDA_TICKER}`);
    expect(caption).toContain("≈");
    expect(caption).toContain("launch reference rate");
    expect(describeFundingGoalInPlatformUnits(5000, ARDA_GENESIS_USD_RATE)).toContain(
      "today's rate",
    );
  });

  it("keeps the ticker out of the field a founder types into", () => {
    // The control, not the claim: the wizard's funding-goal field carries a dollar affix and
    // the ticker appears nowhere in the wizard at all - it reaches the screen only through
    // the caption builder below.
    const fieldStart = wizardSource.indexOf('id="project-funding-goal"');
    expect(fieldStart).toBeGreaterThan(-1);
    const field = wizardSource.slice(fieldStart - 900, fieldStart + 900);

    expect(field).toMatch(/>\s*\$\s*</);
    expect(field).toContain('aria-label="Funding goal in US dollars"');
    expect(wizardSource).not.toContain(ARDA_TICKER);
  });

  it("exports the ticker only for the caption that needs it", () => {
    const declaration = `export const ARDA_TICKER = "${ARDA_TICKER}";`;
    const captionStart = contractSource.indexOf(
      "export function describeFundingGoalInPlatformUnits",
    );
    expect(captionStart).toBeGreaterThan(-1);
    const captionBody = contractSource.slice(
      captionStart,
      contractSource.indexOf("\n}", captionStart),
    );

    expect(captionBody).toContain("ARDA_TICKER");
    expect(
      contractSource.replace(declaration, "").replace(captionBody, ""),
    ).not.toContain("ARDA_TICKER");
  });

  it("refuses an ask that is not a usable dollar amount", () => {
    expect(parseFundingGoalDollars("  ")).toBeUndefined();
    expect(parseFundingGoalDollars("$5,000")).toBe(5000);
    expect(parseFundingGoalDollars(String(PROJECT_FUNDING_GOAL_MIN_DOLLARS))).toBe(
      PROJECT_FUNDING_GOAL_MIN_DOLLARS,
    );
    expect(() => parseFundingGoalDollars("0.50")).toThrow(/at least/);
    expect(() =>
      parseFundingGoalDollars(String(PROJECT_FUNDING_GOAL_MAX_DOLLARS + 1)),
    ).toThrow(/cannot be more than/);
    expect(() => parseFundingGoalDollars("5000.123")).toThrow(/finer than a cent/);
    expect(() => parseFundingGoalDollars("not-an-amount")).toThrow(/dollar amount/);
  });
});

function readyAccountStatus() {
  return {
    avatarId: "avatar-1",
    walletId: "wallet-1",
    walletAddress: "address-1",
    kycStatus: "Approved",
    identityReady: true,
    kycReady: true,
    walletReady: true,
    ready: true,
  };
}
