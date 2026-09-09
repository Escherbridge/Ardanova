import { describe, expect, it } from "vitest";

import {
  createEmptyProjectFormData,
  describeValueActionUnderReview,
  evaluateValueActionAccount,
  isProjectRoleValueBearing,
  isValueActionAccountReady,
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
