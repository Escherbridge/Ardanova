import { describe, expect, it } from "vitest";

import {
  SPONSOR_AMOUNT_HELP,
  SPONSOR_MAXIMUM_DOLLARS,
  SPONSOR_MINIMUM_DOLLARS,
  dollarsToPlatformUnits,
  formatSponsorDollars,
  parseSponsorDollars,
  platformUnitsToDollars,
  toPlatformUnitApiAmount,
} from "./sponsor-amount-contract";

describe("what a sponsor types is dollars", () => {
  it("takes a plain dollar amount, with or without the affix a person retypes", () => {
    expect(parseSponsorDollars("100")?.value).toBe(100);
    expect(parseSponsorDollars("$100")?.value).toBe(100);
    expect(parseSponsorDollars("$1,234.56")?.value).toBe(1234.56);
    expect(parseSponsorDollars(" 25.5 ")?.value).toBe(25.5);
  });

  it("refuses a figure finer than a cent, or outside the bounds", () => {
    expect(parseSponsorDollars("1.005")).toBeNull();
    expect(parseSponsorDollars("0")).toBeNull();
    expect(parseSponsorDollars("0.50")).toBeNull();
    expect(parseSponsorDollars(String(SPONSOR_MAXIMUM_DOLLARS + 1))).toBeNull();
    expect(parseSponsorDollars("-5")).toBeNull();
    expect(parseSponsorDollars("abc")).toBeNull();
    expect(parseSponsorDollars("")).toBeNull();
  });

  it("accepts both ends of the bound rather than only the middle", () => {
    expect(parseSponsorDollars(String(SPONSOR_MINIMUM_DOLLARS))?.value).toBe(
      SPONSOR_MINIMUM_DOLLARS,
    );
    expect(parseSponsorDollars(String(SPONSOR_MAXIMUM_DOLLARS))?.value).toBe(
      SPONSOR_MAXIMUM_DOLLARS,
    );
  });

  it("says dollars in the help text, never a ticker", () => {
    expect(SPONSOR_AMOUNT_HELP).not.toMatch(/ARDA/i);
    expect(SPONSOR_AMOUNT_HELP).toContain("$");
  });
});

describe("dollars become platform units at the node's rate", () => {
  // The genesis seed: 1 unit = $0.01.
  const GENESIS = 0.01;

  it("derives the unit figure the way the founder's caption does", () => {
    expect(dollarsToPlatformUnits(50, GENESIS)).toBe(5_000);
    expect(dollarsToPlatformUnits(1, GENESIS)).toBe(100);
  });

  it("rounds DOWN, so a person never spends more than the dollars they named", () => {
    // $10 at $0.03/unit is 333.333... units. Rounding up would charge $10.005.
    const units = dollarsToPlatformUnits(10, 0.03)!;
    expect(units).toBe(333.33);
    expect(platformUnitsToDollars(units, 0.03)!).toBeLessThanOrEqual(10);
  });

  it("says nothing rather than quoting against an invented rate", () => {
    expect(dollarsToPlatformUnits(100, undefined)).toBeNull();
    expect(dollarsToPlatformUnits(100, 0)).toBeNull();
    expect(dollarsToPlatformUnits(100, Number.NaN)).toBeNull();
    expect(dollarsToPlatformUnits(0, GENESIS)).toBeNull();
    expect(platformUnitsToDollars(100, undefined)).toBeNull();
  });

  it("sends the route a plain decimal string in the unit the route still takes", () => {
    expect(toPlatformUnitApiAmount(5_000)).toBe("5000.00");
    expect(toPlatformUnitApiAmount(333.33)).toBe("333.33");
  });

  it("shows what will actually be spent, not what was typed", () => {
    // The pair that must agree on a receipt: the derived units and the dollars they cost.
    const units = dollarsToPlatformUnits(123.45, GENESIS)!;
    expect(units).toBe(12_345);
    expect(platformUnitsToDollars(units, GENESIS)).toBe(123.45);
    expect(formatSponsorDollars(123.45)).toBe("$123.45");
  });
});
