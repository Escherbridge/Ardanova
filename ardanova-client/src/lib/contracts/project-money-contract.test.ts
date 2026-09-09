import { describe, expect, it } from "vitest";
import {
  projectMoneyViewSchema,
  type ProjectMoneyView,
} from "./project-money-contract";

/**
 * The zod boundary is a filter, not just a check.
 *
 * `ProjectLedgerService` correctly branches its payload on membership: a non-member is sent
 * the aggregate and NO entries at all. But `summary`, `entriesVisible` and
 * `entriesHiddenNote` were missing from this schema, so `z.object` stripped all three off the
 * server response — type-non-breaking and meaning-breaking. The money tab then saw an empty
 * entry list and rendered "NOTHING HAS MOVED YET" on a project that HAD been sponsored,
 * while printing the names note in the same view, which contradicted it.
 *
 * A project publicly asking for sponsorship has to be able to show that it has support.
 */
describe("a project's money as it crosses the zod boundary", () => {
  const nonMemberResponse = {
    projectId: "project-1",
    projectUnitLabel: "RIVER",
    balance: {
      isAvailable: true,
      fundsBaseUnits: 125_000,
      // The SERVER's shape. ProjectLedgerService.FormatAmount is ToString("F2",
      // InvariantCulture), so these figures cross the wire bare -- no symbol, no grouping.
      // The fixture used to say "$1,250.00", which the server never produces, and the
      // assertions below were therefore assertions about the fixture. Currency marking is a
      // presentation concern and is tested in ~/lib/commerce/money-figures.test.ts.
      fundsText: "1250.00",
      fundsScale: 2,
      fundsUnitLabel: "ARDA",
      observedAt: "2026-09-01T00:00:00Z",
      unavailableReason: null,
    },
    balanceProvenance: "These entries explain the balance above.",
    summary: {
      sponsorCount: 4,
      contributorCount: 2,
      sponsorshipTotalUsdMinorUnits: 125_000,
      sponsorshipTotalUsdText: "1250.00",
    },
    entries: [],
    entriesVisible: false,
    entriesHiddenNote: "Individual movements are shown to project members only.",
    counterpartyNamesVisible: false,
    counterpartyNamesNote: "People's names are shown to project members only.",
  };

  it("keeps the support summary a non-member is answered with", () => {
    const parsed = projectMoneyViewSchema.parse(nonMemberResponse);

    expect(parsed.summary.sponsorCount).toBe(4);
    expect(parsed.summary.sponsorshipTotalUsdText).toBe("1250.00");
    expect(parsed.summary.contributorCount).toBe(2);
  });

  it("keeps the reason the entries are absent, so an empty list is never read as 'nothing happened'", () => {
    const parsed = projectMoneyViewSchema.parse(nonMemberResponse);

    expect(parsed.entriesVisible).toBe(false);
    expect(parsed.entriesHiddenNote).toContain("project members only");
    // The two facts a renderer needs to tell the difference between the two empty lists.
    expect(parsed.entries).toHaveLength(0);
    expect(parsed.summary.sponsorCount).toBeGreaterThan(0);
  });

  it("distinguishes a project with no support from one whose entries are withheld", () => {
    const unfunded = projectMoneyViewSchema.parse({
      ...nonMemberResponse,
      summary: {
        sponsorCount: 0,
        contributorCount: 0,
        sponsorshipTotalUsdMinorUnits: 0,
        sponsorshipTotalUsdText: "0.00",
      },
      entriesVisible: true,
      entriesHiddenNote: null,
    });

    expect(unfunded.entriesVisible).toBe(true);
    expect(unfunded.summary.sponsorCount).toBe(0);
  });

  it("refuses a response with no summary rather than defaulting one", () => {
    const { summary: _summary, ...withoutSummary } = nonMemberResponse;

    expect(() => projectMoneyViewSchema.parse(withoutSummary)).toThrow();
  });

  it("types the summary as always present, never optional", () => {
    // A compile-time assertion: if `summary` ever goes optional again this stops building.
    const parsed: ProjectMoneyView =
      projectMoneyViewSchema.parse(nonMemberResponse);
    const total: number = parsed.summary.sponsorshipTotalUsdMinorUnits;

    expect(total).toBe(125_000);
  });
});
