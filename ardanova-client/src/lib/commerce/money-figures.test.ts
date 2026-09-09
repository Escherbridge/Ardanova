import { describe, expect, it } from "vitest";
import { asDollarFigure } from "./money-figures";

/**
 * The inputs here are the SERVER's real output shape, not a convenient fixture.
 *
 * `ProjectLedgerService.FormatAmount` is `value.ToString("F" + scale, InvariantCulture)`, so a
 * $1,250.00 sponsorship total crosses the wire as the string `"1250.00"` — no symbol, no
 * grouping. The project money tab printed that straight into a `text-2xl` mono headline
 * directly beneath a balance rendered as `1250.00 ARDA`, giving a sponsor two large numbers,
 * one labelled a unit and one labelled nothing.
 *
 * The contract test that was meant to cover this used the fixture value `"$1,250.00"`, which
 * the server never produces, so it asserted a property of its own fixture and stayed green
 * whatever the real string looked like. These cases use the real one.
 */
describe("marking the dollar figures the API sends bare", () => {
  it("marks the exact string ProjectLedgerService emits for a sponsorship total", () => {
    expect(asDollarFigure("1250.00")).toBe("$1250.00");
  });

  it("passes the digits through untouched, so the screen shows the ledger's own figure", () => {
    // No re-grouping and no re-rounding: the server sends text precisely so the record is exact.
    expect(asDollarFigure("1234567.89")).toBe("$1234567.89");
    expect(asDollarFigure("0.00")).toBe("$0.00");
    expect(asDollarFigure("0.05")).toBe("$0.05");
  });

  it("does not double-mark a figure that already carries a symbol", () => {
    expect(asDollarFigure("$1250.00")).toBe("$1250.00");
    expect(asDollarFigure("US$1250.00")).toBe("US$1250.00");
  });

  it("returns null rather than a lone symbol when there is no figure", () => {
    // "$" on its own reads as a value of nothing, which is a claim; absence is not.
    expect(asDollarFigure(null)).toBeNull();
    expect(asDollarFigure(undefined)).toBeNull();
    expect(asDollarFigure("")).toBeNull();
    expect(asDollarFigure("   ")).toBeNull();
  });

  it("trims the surrounding whitespace it would otherwise mark around", () => {
    expect(asDollarFigure("  1250.00  ")).toBe("$1250.00");
  });
});
